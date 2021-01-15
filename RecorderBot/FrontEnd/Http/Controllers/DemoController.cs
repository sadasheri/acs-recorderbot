// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DemoController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.RecorderBot.FrontEnd.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Sample.RecorderBot.FrontEnd.Bot;
    using Sample.Common.Logging;
    using Newtonsoft.Json;
    using System.Web.Http.Cors;

    /// <summary>
    /// DemoController serves as the gateway to explore the bot.
    /// From here you can get a list of calls, and functions for each call.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DemoController : ApiController
    {
        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        private Logger Logger => Bot.Instance.Logger;

        /// <summary>
        /// Gets the sample log observer.
        /// </summary>
        private SampleObserver Observer => Bot.Instance.Observer;

        /// <summary>
        /// The GET logs.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <param name="take">The take.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "/")]
        public HttpResponseMessage OnGetLogs(
            int skip = 0,
            int take = 1000)
        {
            var logs = this.Observer.GetLogs(skip, take);

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(logs, Encoding.UTF8, "text/plain");
            return response;
        }

        /// <summary>
        /// The GET logs.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="take">The take.</param>
        /// <returns>
        /// The <see cref="Task" />.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Logs + "/{filter}")]
        public HttpResponseMessage OnGetLogs(
            string filter,
            int skip = 0,
            int take = 1000)
        {
            var logs = this.Observer.GetLogs(filter, skip, take);

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(logs, Encoding.UTF8, "text/plain");
            return response;
        }

        /// <summary>
        /// The GET calls.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.CallsPrefix + "/")]
        public HttpResponseMessage OnGetCalls()
        {
            this.Logger.Info("Getting calls");

            if (Bot.Instance.CallHandlers.IsEmpty)
            {
                return this.Request.CreateResponse(HttpStatusCode.NoContent);
            }

            var calls = new List<Dictionary<string, string>>();
            foreach (var callHandler in Bot.Instance.CallHandlers.Values)
            {
                var call = callHandler.Call;
                var callPath = "/" + HttpRouteConstants.CallRoutePrefix.Replace("{callLegId}", call.Id);
                var callUri = new Uri(Service.Instance.Configuration.CallControlBaseUrl, callPath).AbsoluteUri;
                var values = new Dictionary<string, string>
                {
                    { "legId", call.Id },
                    { "call", callUri },
                    { "logs", callUri.Replace("/calls/", "/logs/") },
                };
                calls.Add(values);
            }

            var json = JsonConvert.SerializeObject(calls, Formatting.Indented);
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}