// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkerRole.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The worker role.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.RecorderBot.WorkerRole
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Sample.Common.Logging;
    using Sample.RecorderBot.FrontEnd;

    /// <summary>
    /// The worker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        /// <summary>
        /// The cancellation token source.
        /// </summary>
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The run complete event.
        /// </summary>
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        /// <summary>
        /// The graph logger.
        /// </summary>
        private readonly Logger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerRole"/> class.
        /// </summary>
        public WorkerRole()
        {
            this.logger = new Logger(redirectToTrace: true);
        }

        /// <summary>
        /// The run.
        /// </summary>
        public override void Run()
        {
            this.logger.Info("WorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        /// <summary>
        /// The on start.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool OnStart()
        {
            try
            {
                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 12;
                ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                // Create and start the environment-independent service.
                Service.Instance.Initialize(new AzureConfiguration(this.logger), this.logger);
                Service.Instance.Start();

                var result = base.OnStart();

                this.logger.Info("WorkerRole has been started");

                return result;
            }
            catch (Exception e)
            {
                this.logger.Error(e, "Exception on startup");
               throw;
            }
        }

        /// <summary>
        /// The on stop.
        /// </summary>
        public override void OnStop()
        {
            this.logger.Info("WorkerRole is stopping");

            Service.Instance.Stop();
            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            this.logger.Info("WorkerRole has stopped");
        }

        /// <summary>
        /// The run async.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                //this.logger.Info("Working");
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}
