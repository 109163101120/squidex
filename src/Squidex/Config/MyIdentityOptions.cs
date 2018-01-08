﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Config
{
    public sealed class MyIdentityOptions
    {
        public string AdminEmail { get; set; }

        public string AdminPassword { get; set; }

        public string GoogleClient { get; set; }

        public string GoogleSecret { get; set; }

        public string MicrosoftClient { get; set; }

        public string MicrosoftSecret { get; set; }

        public string AuthorityUrl { get; set; }

        public bool RequiresHttps { get; set; }

        public bool AllowPasswordAuth { get; set; }

        public bool LockAutomatically { get; set; }

        public bool IsAdminConfigured()
        {
            return !string.IsNullOrWhiteSpace(AdminEmail) && !string.IsNullOrWhiteSpace(AdminPassword);
        }

        public bool IsGoogleAuthConfigured()
        {
            return !string.IsNullOrWhiteSpace(GoogleClient) && !string.IsNullOrWhiteSpace(GoogleSecret);
        }

        public bool IsMicrosoftAuthConfigured()
        {
            return !string.IsNullOrWhiteSpace(MicrosoftClient) && !string.IsNullOrWhiteSpace(MicrosoftSecret);
        }
    }
}