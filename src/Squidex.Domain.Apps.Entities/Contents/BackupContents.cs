﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Squidex.Domain.Apps.Entities.Backup;
using Squidex.Domain.Apps.Entities.Contents.Repositories;
using Squidex.Domain.Apps.Entities.Contents.State;
using Squidex.Domain.Apps.Events.Contents;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;
using Squidex.Infrastructure.States;
using Squidex.Infrastructure.Tasks;

namespace Squidex.Domain.Apps.Entities.Contents
{
    public sealed class BackupContents : BackupHandlerWithStore
    {
        private readonly HashSet<Guid> contentIds = new HashSet<Guid>();
        private readonly IContentRepository contentRepository;

        public BackupContents(IStore<Guid> store, IContentRepository contentRepository)
            : base(store)
        {
            Guard.NotNull(contentRepository, nameof(contentRepository));

            this.contentRepository = contentRepository;
        }

        public override Task RemoveAsync(Guid appId)
        {
            return contentRepository.RemoveAsync(appId);
        }

        public override Task RestoreEventAsync(Envelope<IEvent> @event, Guid appId, BackupReader reader)
        {
            switch (@event.Payload)
            {
                case ContentCreated contentCreated:
                    contentIds.Add(contentCreated.ContentId);
                    break;
            }

            return TaskHelper.Done;
        }

        public override Task RestoreAsync(Guid appId, BackupReader reader)
        {
            return RebuildManyAsync(contentIds, id => RebuildAsync<ContentState, ContentGrain>(id, (e, s) => s.Apply(e)));
        }
    }
}
