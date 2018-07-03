﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Squidex.Domain.Apps.Core.HandleRules.EnrichedEvents;
using Squidex.Domain.Apps.Core.Rules.Actions;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Http;

#pragma warning disable SA1649 // File name must match first type name

namespace Squidex.Domain.Apps.Core.HandleRules.Actions
{
    public sealed class SlackJob
    {
        public string RequestUrl { get; set; }
        public string RequestBodyV2 { get; set; }

        public JObject RequestBody { get; set; }

        public string Body
        {
            get
            {
                return RequestBodyV2 ?? RequestBody.ToString(Formatting.Indented);
            }
        }
    }

    public sealed class SlackActionHandler : RuleActionHandler<SlackAction, SlackJob>
    {
        private const string Description = "Send message to slack";

        private readonly RuleEventFormatter formatter;

        public SlackActionHandler(RuleEventFormatter formatter)
        {
            Guard.NotNull(formatter, nameof(formatter));

            this.formatter = formatter;
        }

        protected override (string Description, SlackJob Data) CreateJob(EnrichedEvent @event, SlackAction action)
        {
            var body =
                new JObject(
                    new JProperty("text", formatter.Format(action.Text, @event)));

            var ruleJob = new SlackJob
            {
                RequestUrl = action.WebhookUrl.ToString(),
                RequestBodyV2 = body.ToString(Formatting.Indented),
            };

            return (Description, ruleJob);
        }

        protected override async Task<(string Dump, Exception Exception)> ExecuteJobAsync(SlackJob job)
        {
            var requestBody = job.Body;
            var request = BuildRequest(job, requestBody);

            HttpResponseMessage response = null;

            try
            {
                response = await HttpClientPool.GetHttpClient().SendAsync(request);

                var responseString = await response.Content.ReadAsStringAsync();
                var requestDump = DumpFormatter.BuildDump(request, response, requestBody, responseString);

                return (requestDump, null);
            }
            catch (Exception ex)
            {
                var requestDump = DumpFormatter.BuildDump(request, response, requestBody, ex.ToString());

                return (requestDump, ex);
            }
        }

        private static HttpRequestMessage BuildRequest(SlackJob job, string requestBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, job.RequestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            return request;
        }
    }
}
