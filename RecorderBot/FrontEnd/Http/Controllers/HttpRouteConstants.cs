// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpRouteConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   HTTP route constants for routing requests to CallController methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.RecorderBot.FrontEnd.Http
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {

        /// <summary>
        /// Route prefix for all api requests.
        /// </summary>
        public const string ApiPrefix = "api";

        /// <summary>
        /// The logs route for GET.
        /// </summary>
        public const string Logs = ApiPrefix + "/logs";

        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallbackPrefix = ApiPrefix + "/callback";

        /// <summary>
        /// Route for incoming requests including notifications, callbacks and incoming call.
        /// </summary>
        public const string OnIncomingRequestRoute = CallbackPrefix + "/calling";

        /// <summary>
        /// Route for making outgoing call request.
        /// </summary>
        public const string OnMakeCallRoute = ApiPrefix + "/makeCall";

        /// <summary>
        /// The calls suffix.
        /// </summary>
        public const string CallsPrefix = ApiPrefix + "/calls";

        /// <summary>
        /// Route for getting Image for a call.
        /// </summary>
        public const string CallRoutePrefix = CallsPrefix + "/{callLegId}";

        /// <summary>
        /// Route for delete request.
        /// </summary>
        public const string OnDeleteCallRoute = CallRoutePrefix;

        /// <summary>
        /// Route for adding participant request.
        /// </summary>
        public const string OnAddParticipantRoute = CallRoutePrefix + "/addParticipant";

        /// <summary>
        /// Route for starting the recording.
        /// </summary>
        public const string OnStartPHQ = CallRoutePrefix + "/startPHQ";

        /// <summary>
        /// Route for ending the recording.
        /// </summary>
        public const string OnEndPHQ = CallRoutePrefix + "/endPHQ/{acsUserId}";

        /// <summary>
        /// Route for ending the recording & get the recorded file.
        /// </summary>
        public const string OnEndPHQFileRead = CallRoutePrefix + "/endPHQ/getFile/{acsUserId}";
    }
}