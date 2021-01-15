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
    using System.IO;
    using System.Net.Http.Headers;
    using System.Web.Http.Cors;
    using System.Net;
    using System.Web;

    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
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
            this.Logger.Info($"Received HTTP {this.Request.Method}, {this.Request.RequestUri}, {this.Request}");

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


        /// <summary>
        /// Start PHQ
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnStartPHQ)]
        public string StartPHQ(string callLegId)
        {
            try
            {
                Bot.Instance.StartPHQ(callLegId);
                return "Recording Started";
            }
            catch (Exception e)
            {
                this.Logger.Error($"Error in startPHQ call {e.Message} :: {e.StackTrace}");
                return "Internal Error";
            }
        }

        /// <summary>
        /// End PHQ
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnEndPHQ)]
        public async Task<string> EndPHQ(string callLegId, string acsUserId)
        {
            try
            {
                StreamContent stream = Bot.Instance.EndPHQ(callLegId, acsUserId);

                this.Logger.Info($"Stream returned from EndPHQ : {stream} for callLegId : {callLegId}");

                string filePath = $"audio_{acsUserId}.wav";

                using (var httpClient = new HttpClient())
                {
                    using (var form = new MultipartFormDataContent())
                    {
                        using (var streamContent = stream)
                        {
                            using (var fileContent = new ByteArrayContent(await streamContent.ReadAsByteArrayAsync()))
                            {
                                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");

                                // "file" parameter name should be the same as the server side input parameter name
                                form.Add(fileContent, "file", filePath);
                                HttpResponseMessage response = await httpClient.PostAsync("https://api.kintsugihello.com/predict/all", form);
                                this.Logger.Info($"kintsugi api response : {response}");
                                return response.Content.ReadAsStringAsync().Result;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.Logger.Error($"Error in endPHQ call {e.Message} :: {e.StackTrace}");
                return "Internal Error";
            }
        }
        [HttpPost]
        [Route(HttpRouteConstants.OnEndPHQFileRead)]
        public HttpResponseMessage getFileOnENDPHQ(string callLegId, string acsUserId)

        {
            try { 
                // StreamContent stream = Bot.Instance.EndPHQ(callLegId, acsUserId);

                // this.Logger.Info($"Stream returned from EndPHQ : {stream} for callLegId : {callLegId}");

                this.Logger.Info("Current directory :" + System.Environment.CurrentDirectory);
                string[] filePaths = Directory.GetFiles(System.Environment.CurrentDirectory, "*.wav",
                                         SearchOption.TopDirectoryOnly);

                foreach(String fpath in filePaths)
                {
                    this.Logger.Info("File in Current directory :" + fpath);
                }


                string filePath = $"{System.Environment.CurrentDirectory}\\audio_{acsUserId}.wav";

                //Create HTTP Response.
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);

            //Check whether File exists.
            if (!File.Exists(filePath))
            {
                //Throw 404 (Not Found) exception if File not found.
                response.StatusCode = HttpStatusCode.NotFound;
                response.ReasonPhrase = string.Format("File not found: {0} .", filePath);
                throw new HttpResponseException(response);
            }

            //Read the File into a Byte Array.
            byte[] bytes = File.ReadAllBytes(filePath);

            //Set the Response Content.
            response.Content = new ByteArrayContent(bytes);

            //Set the Response Content Length.
            response.Content.Headers.ContentLength = bytes.LongLength;

            //Set the Content Disposition Header Value and FileName.
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = filePath;

            //Set the File Content Type.
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(filePath));
            return response;
            }
            catch(HttpResponseException re)
            {
                this.Logger.Error($"Error in getRecordedFile call {re.Message} :: {re.StackTrace}");
                return re.Response;
            }
            catch (Exception e)
            {
                this.Logger.Error($"Error in getRecordedFile call {e.Message} :: {e.StackTrace}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    

}


}