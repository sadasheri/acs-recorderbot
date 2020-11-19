// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionLogger.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   Defines the ExceptionLogger type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.RecorderBot.FrontEnd.Http
{
    using Sample.Common.Logging;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.ExceptionHandling;

    /// <summary>
    /// The exception logger.
    /// </summary>
    public class ExceptionLogger : IExceptionLogger
    {
        private Logger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionLogger"/> class.
        /// </summary>
        /// <param name="logger">Graph logger.</param>
        public ExceptionLogger(Logger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            this.logger.Error(context.Exception, "Exception processing HTTP request.");
            return Task.CompletedTask;
        }
    }
}