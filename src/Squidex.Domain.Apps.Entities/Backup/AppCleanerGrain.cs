﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Squidex.Domain.Apps.Entities.Apps.State;
using Squidex.Domain.Apps.Entities.Rules;
using Squidex.Domain.Apps.Entities.Rules.State;
using Squidex.Domain.Apps.Entities.Schemas;
using Squidex.Domain.Apps.Entities.Schemas.State;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.Orleans;
using Squidex.Infrastructure.States;
using Squidex.Infrastructure.Tasks;

namespace Squidex.Domain.Apps.Entities.Backup
{
    [Reentrant]
    public sealed class AppCleanerGrain : GrainOfString, IRemindable, IAppCleanerGrain
    {
        private readonly IGrainFactory grainFactory;
        private readonly IStore<Guid> store;
        private readonly IEventStore eventStore;
        private readonly IEnumerable<ICleanableAppStorage> storages;
        private IPersistence<State> persistence;
        private bool isCleaning;
        private State state = new State();

        [CollectionName("Index_AppsByName")]
        public sealed class State
        {
            public HashSet<Guid> Apps { get; set; } = new HashSet<Guid>();

            public HashSet<Guid> PendingApps { get; set; } = new HashSet<Guid>();
        }

        public AppCleanerGrain(IGrainFactory grainFactory, IEventStore eventStore, IStore<Guid> store, IEnumerable<ICleanableAppStorage> storages)
        {
            Guard.NotNull(grainFactory, nameof(grainFactory));
            Guard.NotNull(store, nameof(store));
            Guard.NotNull(storages, nameof(storages));
            Guard.NotNull(eventStore, nameof(eventStore));

            this.grainFactory = grainFactory;

            this.store = store;
            this.storages = storages;

            this.eventStore = eventStore;
        }

        public async override Task OnActivateAsync(string key)
        {
            await RegisterOrUpdateReminder("Default", TimeSpan.Zero, TimeSpan.FromMinutes(2));

            persistence = store.WithSnapshots<AppCleanerGrain, State, Guid>(Guid.Empty, s =>
            {
                state = s;
            });

            await persistence.ReadAsync();

            await CleanAsync();
        }

        public Task EnqueueAppAsync(Guid appId)
        {
            state.Apps.Add(appId);

            return persistence.WriteSnapshotAsync(state);
        }

        public Task ActivateAsync()
        {
            CleanAsync().Forget();

            return TaskHelper.Done;
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            CleanAsync().Forget();

            return TaskHelper.Done;
        }

        private async Task CleanAsync()
        {
            if (isCleaning)
            {
                return;
            }

            isCleaning = true;
            try
            {
                foreach (var appId in state.Apps.ToList())
                {
                    try
                    {
                        await CleanAsync(appId);

                        state.Apps.Remove(appId);
                    }
                    catch (NotSupportedException)
                    {
                        state.Apps.Remove(appId);

                        state.PendingApps.Add(appId);
                    }
                    finally
                    {
                        await persistence.WriteSnapshotAsync(state);
                    }
                }
            }
            finally
            {
                isCleaning = false;
            }
        }

        private async Task CleanAsync(Guid appId)
        {
            await eventStore.DeleteManyAsync("AppId", appId);

            var ruleIds = await grainFactory.GetGrain<IRulesByAppIndex>(appId).GetRuleIdsAsync();

            foreach (var ruleId in ruleIds)
            {
                await store.RemoveSnapshotAsync<RuleState>(ruleId);
            }

            var schemaIds = await grainFactory.GetGrain<ISchemasByAppIndex>(appId).GetSchemaIdsAsync();

            foreach (var schemaId in schemaIds)
            {
                await store.RemoveSnapshotAsync<SchemaState>(schemaId);
            }

            foreach (var storage in storages)
            {
                await storage.ClearAsync(appId);
            }

            await store.RemoveSnapshotAsync<AppState>(appId);
        }

        private async Task DeleteAsync<TState>(Guid id)
        {
            await store.RemoveSnapshotAsync<TState>(id);
        }
    }
}
