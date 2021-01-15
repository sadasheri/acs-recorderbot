// <copyright file="CallHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.RecorderBot.FrontEnd.Bot
{
    using Azure.Communication;
    using Azure.Communication.Calls;
    using Microsoft.Skype.Bots.Media;
    using Sample.FrontEnd.Extensions;
    using System;
    using System.Linq;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Sample.Common.Logging;
    using System.Net.Http;

    /// <summary>
    /// Call Handler Logic.
    /// </summary>
    internal class CallHandler
    {
        public readonly Call Call;

        private Logger logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="call">The call.</param>
        public CallHandler(Call call, Logger logger)
        {
            this.Call = call;
            this.logger = logger;

            this.Call.CallStateChanged += OnCallStateChanged;
            this.Call.RemoteParticipantsUpdated += OnRemoteParticipantsUpdated;
            // this.Call.AudioSocket.AudioMediaReceived += this.OnAudioMediaReceived;
        }

        public async Task AddParticipantAsync(CommunicationIdentifier identifier)
        {
            await this.Call.AddParticipantAsync(identifier).ConfigureAwait(false);
        }

        public async Task HangupCallAsync()
        {
            await this.Call.HangUpAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Call.AudioSocket.AudioMediaReceived -= this.OnAudioMediaReceived;
            this.Call.CallStateChanged -= this.OnCallStateChanged;
            this.Call.RemoteParticipantsUpdated -= this.OnRemoteParticipantsUpdated;
        }

            /// <summary>
            /// Event handling for call participants update.
            /// </summary>
            /// <param name="sender">The call object.</param>
            /// <param name="e">Object containing list of added or removed participants.</param>
            private void OnRemoteParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs e)
        {
            logger.Info("Added Participants Count: " + e.AddedParticipants.Count());
            if (e.AddedParticipants.Count > 0)
            {
                logger.Info("Call Participants Added: " + e.AddedParticipants.Count());

                foreach(var participant in e.AddedParticipants)
                {
                    if (participant.Identity is CommunicationUser)
                    {
                        logger.Info("Added User:" + ((CommunicationUser)participant.Identity).Id);
                    }
                    else if (participant.Identity is CallingApplication)
                    {
                        logger.Info("Added Application:" + ((CallingApplication)participant.Identity).Id);
                    }
                    
                }
            }

            if (e.RemovedParticipants.Count > 0)
            {
                logger.Info("Call Participants Removed: " + e.RemovedParticipants.Count());

                foreach (var participant in e.RemovedParticipants)
                {
                    if (participant.Identity is CommunicationUser)
                    {
                        logger.Info("Removed User:" + ((CommunicationUser)participant.Identity).Id);
                    }
                    else if (participant.Identity is CallingApplication)
                    {
                        logger.Info("Removed Application:" + ((CallingApplication)participant.Identity).Id);
                    }
                }
            }
        }

        /// <summary>
        /// Updated call handler.
        /// </summary>
        private void OnCallStateChanged(object sender, PropertyChangedEventArgs args)
        {
            var call = sender as Call;

            logger.Info("Call State: " + call.State.ToString());
        }

        /// <summary>
        /// Event handling for audio media received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAudioMediaReceived(object sender, AudioMediaReceivedEventArgs e)
        {
            if (e.Buffer.UnmixedAudioBuffers != null)
            {
                logger.Info("UnmixedAudioBuffers is not null");
                foreach (var buffer in e.Buffer.UnmixedAudioBuffers)
                {
                    var activeSpeakerIdentifier = GetParticipantIdentitySetFromMSI(this.Call, buffer.ActiveSpeakerId);
                    logger.Info("activeSpeakerIdentifier :" + activeSpeakerIdentifier);
                    string identity = "";
                    if (activeSpeakerIdentifier is CommunicationUser)
                    {
                        var user = activeSpeakerIdentifier as CommunicationUser;
                        identity = user.Id.Replace("8:acs:", "");
                        logger.Info("Trying to store the audio for identity :" + identity);
                    }

                    if(activeSpeakerIdentifier is PhoneNumber)
                    {
                        var phoneIdentity = activeSpeakerIdentifier as PhoneNumber;
                        identity = phoneIdentity.Value;
                        logger.Info("Trying to store the audio for Phonenumber :" + identity);
                    }

                    StoreAudio(identity ?? "", buffer.Data, buffer.Length);
                }
            }
            else    // P2P call as UnmixedAudioBuffers will not be available during a P2P call.
            {
                
                logger.Info($"Trying to store the audio for p2p_{this.Call.Id}");
                StoreAudio($"p2p_{this.Call.Id}", e.Buffer.Data, e.Buffer.Length);
            }
            e.Buffer.Dispose();
        }

        private void StoreAudio(string participant, IntPtr buffer, long bufferLength)
        {
            using (var stream = new FileStream($"audio_{participant}.wav", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var bytes = new byte[bufferLength];
                Marshal.Copy(buffer, bytes, 0, (int)bufferLength);

                stream.AppendWaveData(bytes);
            }
        }






        /// <summary>
        /// Gets the identitySet of the participant corresponding to the given MSI.
        /// </summary>
        /// <param name="call">call object</param>
        /// <param name="msi">media stream id.</param>
        /// <returns>
        /// The <see cref="IdentitySet"/>.
        /// </returns>
        private static CommunicationIdentifier GetParticipantIdentitySetFromMSI(Call call, uint msi)
        {
            var participant = GetParticipantFromMSI(call, msi);
            return participant?.Identity;
        }

        /// <summary>
        /// Gets the participant with the corresponding MSI.
        /// </summary>
        /// <param name="call">call object</param>
        /// <param name="msi">media stream id.</param>
        /// <returns>
        /// The <see cref="IParticipant"/>.
        /// </returns>
        private static RemoteParticipant GetParticipantFromMSI(Call call, uint msi)
        {
            return call.RemoteParticipants.SingleOrDefault(x => x.MediaStreams.Any(y => y.SourceId == msi.ToString()));
        }

        public void StartPHQ()
        {
            this.Call.AudioSocket.AudioMediaReceived += this.OnAudioMediaReceived;
        }

        public StreamContent EndPHQ(string acsUserId)
        {
            this.Call.AudioSocket.AudioMediaReceived -= this.OnAudioMediaReceived;
            this.logger.Info("Recording Ended");
            return RetrieveAudio(acsUserId);
        }

        private StreamContent RetrieveAudio(string acsUserId)
        {
            // FileStream fileStream = new FileStream($"audio_{acsUserId}.wav", FileMode.Open, FileAccess.Read);
            //StreamContent streamContent =  new StreamContent(fileStream);
            // fileStream.Close();
            this.logger.Info("Current directory :" + System.Environment.CurrentDirectory);
            return new StreamContent(new FileStream($"audio_{acsUserId}.wav", FileMode.Open, FileAccess.Read));
        }

    }
}
