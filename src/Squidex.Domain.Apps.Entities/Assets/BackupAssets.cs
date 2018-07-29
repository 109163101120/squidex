﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Squidex.Domain.Apps.Entities.Assets.Repositories;
using Squidex.Domain.Apps.Entities.Assets.State;
using Squidex.Domain.Apps.Entities.Backup;
using Squidex.Domain.Apps.Entities.Tags;
using Squidex.Domain.Apps.Events.Assets;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Assets;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.States;
using Squidex.Infrastructure.Tasks;

namespace Squidex.Domain.Apps.Entities.Assets
{
    public sealed class BackupAssets : BackupHandlerWithStore
    {
        private readonly HashSet<Guid> assetIds = new HashSet<Guid>();
        private readonly IAssetStore assetStore;
        private readonly IAssetRepository assetRepository;
        private readonly ITagService tagService;
        private readonly IEventDataFormatter eventDataFormatter;

        public BackupAssets(IStore<Guid> store,
            IEventDataFormatter eventDataFormatter,
            IAssetStore assetStore,
            IAssetRepository assetRepository,
            ITagService tagService)
            : base(store)
        {
            Guard.NotNull(eventDataFormatter, nameof(eventDataFormatter));
            Guard.NotNull(assetStore, nameof(assetStore));
            Guard.NotNull(assetRepository, nameof(assetRepository));
            Guard.NotNull(tagService, nameof(tagService));

            this.eventDataFormatter = eventDataFormatter;
            this.assetStore = assetStore;
            this.assetRepository = assetRepository;
            this.tagService = tagService;
        }

        public override async Task RemoveAsync(Guid appId)
        {
            await tagService.ClearAsync(appId, TagGroups.Assets);

            await assetRepository.RemoveAsync(appId);
        }

        public override Task BackupEventAsync(EventData @event, Guid appId, BackupWriter writer)
        {
            if (@event.Type == "AssetCreatedEvent" ||
                @event.Type == "AssetUpdatedEvent")
            {
                var parsedEvent = eventDataFormatter.Parse(@event);

                switch (parsedEvent.Payload)
                {
                    case AssetCreated assetCreated:
                        return WriteAssetAsync(assetCreated.AssetId, assetCreated.FileVersion, writer);
                    case AssetUpdated assetUpdated:
                        return WriteAssetAsync(assetUpdated.AssetId, assetUpdated.FileVersion, writer);
                }
            }

            return TaskHelper.Done;
        }

        public override Task RestoreEventAsync(Envelope<IEvent> @event, Guid appId, BackupReader reader)
        {
            switch (@event.Payload)
            {
                case AssetCreated assetCreated:
                    assetIds.Add(assetCreated.AssetId);

                    return ReadAssetAsync(assetCreated.AssetId, assetCreated.FileVersion, reader);
                case AssetUpdated assetUpdated:
                    return ReadAssetAsync(assetUpdated.AssetId, assetUpdated.FileVersion, reader);
            }

            return TaskHelper.Done;
        }

        public override Task RestoreAsync(Guid appId, BackupReader reader)
        {
            return RebuildManyAsync(assetIds, id => RebuildAsync<AssetState, AssetGrain>(id, (e, s) => s.Apply(e)));
        }

        private Task WriteAssetAsync(Guid assetId, long fileVersion, BackupWriter writer)
        {
            return writer.WriteAttachmentAsync(GetName(assetId, fileVersion), stream =>
            {
                return assetStore.DownloadAsync(assetId.ToString(), fileVersion, null, stream);
            });
        }

        private Task ReadAssetAsync(Guid assetId, long fileVersion, BackupReader reader)
        {
            return reader.ReadAttachmentAsync(GetName(assetId, fileVersion), async stream =>
            {
                try
                {
                    await assetStore.UploadAsync(assetId.ToString(), fileVersion, null, stream);
                }
                catch (AssetAlreadyExistsException)
                {
                    return;
                }
            });
        }

        private static string GetName(Guid assetId, long fileVersion)
        {
            return $"{assetId}_{fileVersion}.asset";
        }
    }
}
