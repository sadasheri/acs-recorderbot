// <copyright file="AuthenticationProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

// THIS CODE HAS NOT BEEN TESTED RIGOROUSLY.USING THIS CODE IN PRODUCTION ENVIRONMENT IS STRICTLY NOT RECOMMENDED.
// THIS SAMPLE IS PURELY FOR DEMONSTRATION PURPOSES ONLY.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
namespace Sample.Common.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Communication.Administration;
    using Azure.Communication.Identity;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Sample.Common.Logging;

    /// <summary>
    /// The authentication provider for this bot instance.
    /// </summary>
    public class AuthenticationProvider
    {
        /// <summary>
        /// ACS Application resource id.
        /// </summary>
        private readonly string ACSApplicationInstanceId;

        /// <summary>
        /// ACS Connection string.
        /// </summary>
        private readonly string ACSConnectionString;

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly Logger logger;

        /// <summary>
        /// The open ID configuration refresh interval.
        /// </summary>
        private readonly TimeSpan openIdConfigRefreshInterval = TimeSpan.FromHours(2);

        /// <summary>
        /// The previous update timestamp for OpenIdConfig.
        /// </summary>
        private DateTime prevOpenIdConfigUpdateTimestamp = DateTime.MinValue;

        /// <summary>
        /// The open identifier configuration.
        /// </summary>
        private OpenIdConnectConfiguration openIdConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProvider" /> class.
        /// </summary>
        /// <param name="acsApplicationInstanceId">The ACS application instance id.</param>
        /// <param name="logger">The logger.</param>
        public AuthenticationProvider(string acsApplicationInstanceId, string acsConnectionString, Logger logger)
        {
            this.ACSApplicationInstanceId = acsApplicationInstanceId;
            this.ACSConnectionString = acsConnectionString;
            this.logger = logger;
        }

        /// <summary>
        /// Validates the request asynchronously.
        /// This method will be called any time we have an incoming request.
        /// Returning invalid result will trigger a Forbidden response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        /// bool indicating if the request is valid.
        /// </returns>
        public async Task<bool> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            var token = request?.Headers?.Authorization?.Parameter;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            const string authDomain = "https://api.aps.skype.com/v1/.well-known/OpenIdConfiguration";
            if (this.openIdConfiguration == null || DateTime.Now > this.prevOpenIdConfigUpdateTimestamp.Add(this.openIdConfigRefreshInterval))
            {
                this.logger.Info("Updating OpenID configuration");

                // Download the OIDC configuration which contains the JWKS
                IConfigurationManager<OpenIdConnectConfiguration> configurationManager =
                    new ConfigurationManager<OpenIdConnectConfiguration>(
                        authDomain,
                        new OpenIdConnectConfigurationRetriever());
                this.openIdConfiguration = await configurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);

                this.prevOpenIdConfigUpdateTimestamp = DateTime.Now;
            }

            var authIssuers = new[]
            {
                "https://api.botframework.com",
            };

            // Configure the TokenValidationParameters.
            // Aet the Issuer(s) and Audience(s) to validate and
            // assign the SigningKeys which were downloaded from AuthDomain.
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidIssuers = authIssuers,
                ValidAudiences = new[] { this.ACSApplicationInstanceId, this.ACSApplicationInstanceId.Replace("28:acs:", "") },
                IssuerSigningKeys = this.openIdConfiguration.SigningKeys,
            };

            ClaimsPrincipal claimsPrincipal;
            try
            {
                // Now validate the token. If the token is not valid for any reason, an exception will be thrown by the method
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                claimsPrincipal = handler.ValidateToken(token, validationParameters, out _);
            }

            // Token expired... should somehow return 401 (Unauthorized)
            // catch (SecurityTokenExpiredException ex)
            // Tampered token
            // catch (SecurityTokenInvalidSignatureException ex)
            // Some other validation error
            // catch (SecurityTokenValidationException ex)
            catch (Exception ex)
            {
                // Some other error
                this.logger.Error(ex, $"Failed to validate token for client: {this.ACSApplicationInstanceId}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieve bot credential for outbound request.
        /// </summary>
        /// <returns>
        /// The <see cref="CommunicationUserCredential" /> structure.
        /// </returns>
        public async Task<CommunicationUserCredential> GetBotCredential()
        {
            var token = await GetApplicationToken();
            return new CommunicationUserCredential(true, null, asyncTokenRefresher: TokenRefresher, initialToken: token );
        }

        public async ValueTask<string> TokenRefresher(CancellationToken cancellationToken = default)
        {
            return await GetApplicationToken();
        }

        private async Task<string> GetApplicationToken()
        {
            var client = new CommunicationIdentityClient(this.ACSConnectionString);
            var callingApplication = new Azure.Communication.CallingApplication(this.ACSApplicationInstanceId);
            var tokenResult = await client.IssueTokenAsync(callingApplication, new List<CommunicationTokenScope>() { CommunicationTokenScope.VoIP }).ConfigureAwait(false);

            return tokenResult.Value.Token;
        }
    }
}