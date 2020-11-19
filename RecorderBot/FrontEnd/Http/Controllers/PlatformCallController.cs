// <copyright file="PlatformCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.RecorderBot.FrontEnd.Http
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Sample.RecorderBot.Models;
    using Sample.RecorderBot.FrontEnd.Bot;
    using Sample.Common.Logging;

    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    public class PlatformCallController : ApiController
    {
        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        private Logger Logger => Bot.Instance.Logger;

        /// <summary>
        /// Handle a callback for an existing call.
        /// </summary>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingRequestRoute)]
        public async Task<HttpResponseMessage> OnIncomingRequestAsync()
        {
            this.Logger.Info($"Received HTTP {this.Request.Method}, {this.Request.RequestUri}");

            // Pass the incoming message to the sdk. The sdk takes care of what to do with it.
            var response = await Bot.Instance.Client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// The making outbound call async.
        /// </summary>
        /// <param name="makeCallBody">
        /// The making outgoing call request body.
        /// </param>
        /// <returns>
        /// The action result.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [Route(HttpRouteConstants.OnMakeCallRoute)]
        public async Task<string> MakeOutgoingCallAsync([FromBody] TargetParticipant targetParticipant)
        {
            try
            {
                var callId = await Bot.Instance.MakeCallAsync(targetParticipant).ConfigureAwait(false);
                return callId;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Adding participant to call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <param name="targetParticipant"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnAddParticipantRoute)]
        public async Task AddParticipantAsync(string callLegId, [FromBody] TargetParticipant targetParticipant)
        {
            try
            {
                await Bot.Instance.AddParticipantAsync(callLegId, targetParticipant).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Hangup call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns></returns>
        [HttpDelete]
        [Route(HttpRouteConstants.OnDeleteCallRoute)]
        public async Task HangupCallAsync(string callLegId)
        {
            try
            {
                await Bot.Instance.HangupCallAsync(callLegId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}