﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Apps;
using Squidex.Domain.Apps.Entities.Apps.Commands;
using Squidex.Infrastructure;

namespace Squidex.Domain.Apps.Entities.Apps.Guards
{
    public static class GuardAppClients
    {
        public static void CanAttach(AppClients clients, AttachClient command)
        {
            Guard.NotNull(command, nameof(command));

            Validate.It(() => "Cannot attach client.", error =>
            {
                if (string.IsNullOrWhiteSpace(command.Id))
                {
                    error(new ValidationError("Client id is required.", nameof(command.Id)));
                }
                else if (clients.ContainsKey(command.Id))
                {
                    error(new ValidationError($"A client with id '{command.Id}' has already been added."));
                }
            });
        }

        public static void CanRevoke(AppClients clients, RevokeClient command)
        {
            Guard.NotNull(command, nameof(command));

            GetClientOrThrow(clients, command.Id);

            Validate.It(() => "Cannot revoke client.", error =>
            {
                if (string.IsNullOrWhiteSpace(command.Id))
                {
                    error(new ValidationError("Client id is required.", nameof(command.Id)));
                }
            });
        }

        public static void CanUpdate(AppClients clients, UpdateClient command)
        {
            Guard.NotNull(command, nameof(command));

            var client = GetClientOrThrow(clients, command.Id);

            Validate.It(() => "Cannot revoke client.", error =>
            {
                if (string.IsNullOrWhiteSpace(command.Id))
                {
                    error(new ValidationError("Client id is required.", nameof(command.Id)));
                }

                if (string.IsNullOrWhiteSpace(command.Name) && command.Permission == null)
                {
                    error(new ValidationError("Either name or permission must be defined.", nameof(command.Name), nameof(command.Permission)));
                }

                if (command.Permission.HasValue && !command.Permission.Value.IsEnumValue())
                {
                    error(new ValidationError("Permission is not valid.", nameof(command.Permission)));
                }

                if (client != null)
                {
                    if (!string.IsNullOrWhiteSpace(command.Name) && string.Equals(client.Name, command.Name))
                    {
                        error(new ValidationError("Client has already this name.", nameof(command.Name)));
                    }

                    if (command.Permission == client.Permission)
                    {
                        error(new ValidationError("Client has already this permission.", nameof(command.Permission)));
                    }
                }
            });
        }

        private static AppClient GetClientOrThrow(AppClients clients, string id)
        {
            if (id == null)
            {
                return null;
            }

            if (!clients.TryGetValue(id, out var client))
            {
                throw new DomainObjectNotFoundException(id, "Clients", typeof(IAppEntity));
            }

            return client;
        }
    }
}
