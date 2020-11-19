// <copyright file="Bot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.RecorderBot.FrontEnd.Bot
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Sample.RecorderBot.Models;
    using Sample.RecorderBot.FrontEnd;
    using Sample.Common.Authentication;
    using Sample.Common.Logging;
    using Azure.Communication.Calls;
    using Azure.Communication;
    using System.Net.Http;

    /// <summary>
    /// The core bot logic.
    /// </summary>
    internal class Bot : IDisposable
    {
        /// <summary>
        /// Gets the instance of the bot.
        /// </summary>
        public static Bot Instance { get; } = new Bot();

        /// <summary>
        /// Gets the Graph Logger instance.
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Gets the sample log observer.
        /// </summary>
        public SampleObserver Observer { get; private set; }

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        public ConcurrentDictionary<string, CallHandler> CallHandlers { get; } = new ConcurrentDictionary<string, CallHandler>();

        /// <summary>
        /// The call client.
        /// </summary>
        public CallClient Client { get; private set; }

        /// <summary>
        /// ACS application instance id.
        /// </summary>
        private string ACSApplicationInstanceId;

        /// <summary>
        /// 
        /// </summary>
        private string CallbackUrl;

        /// <inheritdoc />
        public void Dispose()
        {
            this.Observer?.Dispose();
            this.Observer = null;
            this.Logger = null;
            this.Client = null;
        }

        /// <summary>
        /// Initialize the instance.
        /// </summary>
        /// <param name="service">Service instance.</param>
        /// <param name="logger">Graph logger.</param>
        internal async Task Initialize(Service service, Logger logger)
        {
            this.Logger = logger;
            this.Observer = new SampleObserver(logger);
            this.ACSApplicationInstanceId = service.Configuration.ACSApplicationInstanceId;
            this.CallbackUrl = service.Configuration.CallControlBaseUrl.AbsoluteUri;

            var name = this.GetType().Assembly.GetName().Name;

            var authProvider = new AuthenticationProvider(
                            service.Configuration.ACSApplicationInstanceId,
                            service.Configuration.ACSConnectionString,
                            this.Logger);

            ValidateInboundRequestAsyncDelegate validateInboundRequestAsyncDelegate = async (HttpRequestMessage request) =>
            {
                return await authProvider.ValidateInboundRequestAsync(request).ConfigureAwait(false);
            };

            this.Client = new CallClient(service.Configuration.PlaceCallEndpointUrl.ToString(),
                                                            service.Configuration.ApplicationName,
                                                            await authProvider.GetBotCredential().ConfigureAwait(false),
                                                            validateInboundRequestAsyncDelegate,
                                                            service.Configuration.CallControlBaseUrl,
                                                            service.Configuration.MediaPlatformSettings);
            
        }
        /// <summary>
        /// Makes outgoing call asynchronously.
        /// </summary>
        /// <param name="makeCallRequest">The outgoing call request body.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<string> MakeCallAsync(TargetParticipant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            var source = new CallingApplication(this.ACSApplicationInstanceId);
            var targets = new List<CommunicationIdentifier>() { new CommunicationUser(participant.ACSId) };
            
            var call = await this.Client.CallAsync(source, targets, new StartCallOptions() { CallbackUri = this.CallbackUrl, AudioOptions = new AudioOptions() { ReceiveUnmixedMeetingAudio = true, StreamDirection = Azure.Communication.Calls.StreamDirection.SendReceive } });
            call.CallStateChanged += OnCallStateChanged;

            var callHandler = new CallHandler(call, this.Logger);
            this.CallHandlers.TryAdd(call.Id, callHandler);
            
            this.Logger.Info($"Call creation complete: {call.Id}");

            return call.Id;
        }

        /// <summary>
        /// Adds participants asynchronously.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <param name="targetParticipant">The participant to be a add.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task AddParticipantAsync(string callLegId, TargetParticipant targetParticipant)
        {
            if (string.IsNullOrEmpty(callLegId))
            {
                throw new ArgumentNullException(nameof(callLegId));
            }

            if (string.IsNullOrEmpty(targetParticipant.ACSId))
            {
                throw new ArgumentNullException(nameof(targetParticipant.ACSId));
            }

            CommunicationIdentifier target = new CommunicationUser(targetParticipant.ACSId);
            await this.CallHandlers[callLegId].AddParticipantAsync(target).ConfigureAwait(false);
        }

        /// <summary>
        /// Hang up call asynchronously.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task HangupCallAsync(string callLegId)
        {
            await this.CallHandlers[callLegId].HangupCallAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        private void OnCallStateChanged(object sender, PropertyChangedEventArgs args)
        {
            var call = sender as Call;

            if (call.State == CallState.Disconnected)
            {
                if (this.CallHandlers.TryRemove(call.Id.ToString(), out CallHandler handler))
                {
                    handler.Dispose();
                }
            }
        }
    }
}