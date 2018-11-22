﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Squidex.Infrastructure.Log;
using Xunit;

namespace Squidex.Infrastructure.UsageTracking
{
    public class BackgroundUsageTrackerTests
    {
        private readonly IUsageRepository usageStore = A.Fake<IUsageRepository>();
        private readonly ISemanticLog log = A.Fake<ISemanticLog>();
        private readonly Guid appId = Guid.NewGuid();
        private readonly BackgroundUsageTracker sut;

        public BackgroundUsageTrackerTests()
        {
            sut = new BackgroundUsageTracker(usageStore, log);
        }

        [Fact]
        public Task Should_throw_exception_if_tracking_on_disposed_object()
        {
            sut.Dispose();

            return Assert.ThrowsAsync<ObjectDisposedException>(() => sut.TrackAsync(appId, "category1", 1, 1000));
        }

        [Fact]
        public Task Should_throw_exception_if_querying_on_disposed_object()
        {
            sut.Dispose();

            return Assert.ThrowsAsync<ObjectDisposedException>(() => sut.QueryAsync(appId, DateTime.Today, DateTime.Today.AddDays(1)));
        }

        [Fact]
        public Task Should_throw_exception_if_querying_montly_usage_on_disposed_object()
        {
            sut.Dispose();

            return Assert.ThrowsAsync<ObjectDisposedException>(() => sut.GetMonthlyCallsAsync(appId, DateTime.Today));
        }

        [Fact]
        public async Task Should_sum_up_when_getting_monthly_calls()
        {
            var date = new DateTime(2016, 1, 15);

            IReadOnlyList<StoredUsage> originalData = new List<StoredUsage>
            {
                new StoredUsage("category1", date.AddDays(1), Counters(10, 15)),
                new StoredUsage("category1", date.AddDays(3), Counters(13, 18)),
                new StoredUsage("category1", date.AddDays(5), Counters(15, 20)),
                new StoredUsage("category1", date.AddDays(7), Counters(17, 22))
            };

            A.CallTo(() => usageStore.QueryAsync($"{appId}_API", new DateTime(2016, 1, 1), new DateTime(2016, 1, 31)))
                .Returns(originalData);

            var result = await sut.GetMonthlyCallsAsync(appId, date);

            Assert.Equal(55, result);
        }

        [Fact]
        public async Task Should_fill_missing_days()
        {
            var f = DateTime.Today;
            var t = DateTime.Today.AddDays(4);

            var originalData = new List<StoredUsage>
            {
                new StoredUsage("MyCategory1", f.AddDays(1), Counters(10, 15)),
                new StoredUsage("MyCategory1", f.AddDays(3), Counters(13, 18)),
                new StoredUsage("MyCategory1", f.AddDays(4), Counters(15, 20)),
                new StoredUsage(null, f.AddDays(0), Counters(17, 22)),
                new StoredUsage(null, f.AddDays(2), Counters(11, 14))
            };

            A.CallTo(() => usageStore.QueryAsync($"{appId}_API", f, t))
                .Returns(originalData);

            var result = await sut.QueryAsync(appId, f, t);

            var expected = new Dictionary<string, List<DateUsage>>
            {
                ["MyCategory1"] = new List<DateUsage>
                {
                    new DateUsage(f.AddDays(0), 00, 00),
                    new DateUsage(f.AddDays(1), 10, 15),
                    new DateUsage(f.AddDays(2), 00, 00),
                    new DateUsage(f.AddDays(3), 13, 18),
                    new DateUsage(f.AddDays(4), 15, 20)
                },
                ["*"] = new List<DateUsage>
                {
                    new DateUsage(f.AddDays(0), 17, 22),
                    new DateUsage(f.AddDays(1), 00, 00),
                    new DateUsage(f.AddDays(2), 11, 14),
                    new DateUsage(f.AddDays(3), 00, 00),
                    new DateUsage(f.AddDays(4), 00, 00)
                }
            };

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task Should_fill_missing_days_with_star()
        {
            var f = DateTime.Today;
            var t = DateTime.Today.AddDays(4);

            A.CallTo(() => usageStore.QueryAsync($"{appId}_API", f, t))
                .Returns(new List<StoredUsage>());

            var result = await sut.QueryAsync(appId, f, t);

            var expected = new Dictionary<string, List<DateUsage>>
            {
                ["*"] = new List<DateUsage>
                {
                    new DateUsage(f.AddDays(0), 00, 00),
                    new DateUsage(f.AddDays(1), 00, 00),
                    new DateUsage(f.AddDays(2), 00, 00),
                    new DateUsage(f.AddDays(3), 00, 00),
                    new DateUsage(f.AddDays(4), 00, 00)
                }
            };

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task Should_not_track_if_weight_less_than_zero()
        {
            await sut.TrackAsync(appId, "MyCategory", -1, 1000);
            await sut.TrackAsync(appId, "MyCategory", 0, 1000);

            sut.Next();
            sut.Dispose();

            A.CallTo(() => usageStore.TrackUsagesAsync(A<UsageUpdate[]>.Ignored))
                .MustNotHaveHappened();
        }

        [Fact]
        public async Task Should_aggregate_and_store_on_dispose()
        {
            var appId1 = Guid.NewGuid();
            var appId2 = Guid.NewGuid();
            var appId3 = Guid.NewGuid();

            var today = DateTime.Today;

            await sut.TrackAsync(appId1, "MyCategory1", 1, 1000);

            await sut.TrackAsync(appId2, "MyCategory1", 1.0, 2000);
            await sut.TrackAsync(appId2, "MyCategory1", 0.5, 3000);

            await sut.TrackAsync(appId3, "MyCategory1", 0.3, 4000);
            await sut.TrackAsync(appId3, "MyCategory1", 0.1, 5000);

            await sut.TrackAsync(appId3, null, 0.5, 2000);
            await sut.TrackAsync(appId3, null, 0.5, 6000);

            UsageUpdate[] updates = null;

            A.CallTo(() => usageStore.TrackUsagesAsync(A<UsageUpdate[]>.Ignored))
                .Invokes((UsageUpdate[] u) => updates = u);

            sut.Next();
            sut.Dispose();

            updates.Should().BeEquivalentTo(new[]
            {
                new UsageUpdate(today, $"{appId1}_API", "MyCategory1", Counters(1.0, 1000)),
                new UsageUpdate(today, $"{appId2}_API", "MyCategory1", Counters(1.5, 5000)),
                new UsageUpdate(today, $"{appId3}_API", "MyCategory1", Counters(0.4, 9000)),
                new UsageUpdate(today, $"{appId3}_API", "*", Counters(1, 8000))
            }, o => o.ComparingByMembers<UsageUpdate>());

            A.CallTo(() => usageStore.TrackUsagesAsync(A<UsageUpdate[]>.Ignored))
                .MustHaveHappened();
        }

        private static Counters Counters(double count, long ms)
        {
            return new Counters
            {
                [BackgroundUsageTracker.CounterTotalCalls] = count,
                [BackgroundUsageTracker.CounterTotalElapsedMs] = ms
            };
        }
    }
}
