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
    public sealed class WebhookJob
    {
        public string RequestUrl { get; set; }

        public string RequestSignature { get; set; }

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

    public sealed class WebhookActionHandler : RuleActionHandler<WebhookAction, WebhookJob>
    {
        private readonly RuleEventFormatter formatter;

        public WebhookActionHandler(RuleEventFormatter formatter)
        {
            Guard.NotNull(formatter, nameof(formatter));

            this.formatter = formatter;
        }

        protected override (string Description, WebhookJob Data) CreateJob(EnrichedEvent @event, WebhookAction action)
        {
            var requestBody = formatter.ToEnvelope(@event).ToString(Formatting.Indented);
            var requestUrl = formatter.Format(action.Url.ToString(), @event);

            var ruleDescription = $"Send event to webhook '{requestUrl}'";
            var ruleJob = new WebhookJob
            {
                RequestUrl = requestUrl,
                RequestSignature = $"{requestBody}{action.SharedSecret}".Sha256Base64(),
                RequestBodyV2 = requestBody
            };

            return (ruleDescription, ruleJob);
        }

        protected override async Task<(string Dump, Exception Exception)> ExecuteJobAsync(WebhookJob job)
        {
            var requestBody = job.Body;
            var requestMessage = BuildRequest(job, requestBody);

            HttpResponseMessage response = null;

            try
            {
                response = await HttpClientPool.GetHttpClient().SendAsync(requestMessage);

                var responseString = await response.Content.ReadAsStringAsync();
                var requestDump = DumpFormatter.BuildDump(requestMessage, response, requestBody, responseString, TimeSpan.Zero, false);

                Exception ex = null;

                if (!response.IsSuccessStatusCode)
                {
                    ex = new HttpRequestException($"Response code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).");
                }

                return (requestDump, ex);
            }
            catch (Exception ex)
            {
                var requestDump = DumpFormatter.BuildDump(requestMessage, response, requestBody, ex.ToString(), TimeSpan.Zero, false);

                return (requestDump, ex);
            }
        }

        private static HttpRequestMessage BuildRequest(WebhookJob job, string requestBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, job.RequestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Signature", job.RequestSignature);
            request.Headers.Add("User-Agent", "Squidex Webhook");

            return request;
        }
    }
}
