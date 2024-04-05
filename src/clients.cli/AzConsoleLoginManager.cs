﻿using Azure.AI.CLI.Common.Clients;
using Azure.AI.CLI.Common.Clients.Models;
using Azure.AI.Details.Common.CLI;
using Azure.AI.Details.Common.CLI.AzCli;
using Newtonsoft.Json.Linq;

namespace Azure.AI.CLI.Clients.AzPython
{
    /// <summary>
    /// Does login via a console using the AZ CLI
    /// </summary>
    public class AzConsoleLoginManager : ILoginManager
    {
        private readonly Func<bool> _getAllowInteractive;
        private readonly IDictionary<string, string> _cliEnv;
        private readonly TimeSpan _minValidity;
        private AuthenticationToken? _authToken;

        /// <summary>
        /// Creates a new instance of <see cref="AzConsoleLoginManager"/>
        /// </summary>
        /// <param name="getAllowInteractiveLogin">A function that returns whether interactive login is allowed</param>
        /// <param name="environmentVariables">Environment variables to use when running the AZ CLI</param>
        /// <param name="minValidity">The minimum validity an auth token should have before renewing it</param>
        public AzConsoleLoginManager(Func<bool> getAllowInteractiveLogin, IDictionary<string, string>? environmentVariables = null, TimeSpan? minValidity = null)
        {
            _getAllowInteractive = getAllowInteractiveLogin ?? throw new ArgumentNullException(nameof(getAllowInteractiveLogin));
            _cliEnv = environmentVariables ?? new Dictionary<string, string>();
            _minValidity = minValidity ?? TimeSpan.FromMinutes(5);
        }

        /// <inheritdoc />
        public bool CanAttemptLogin
        {
            get
            {
                try { return _getAllowInteractive(); }
                catch (Exception) { return false; }
            }
        }

        /// <inheritdoc />
        public async Task<ClientResult> LoginAsync(LoginOptions options, CancellationToken token)
        {
            try
            {
                var showDeviceCodeMessage = (string message) =>
                {
                    if (message.Contains("device") && message.Contains("code"))
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(message);
                        Console.WriteLine();
                        Console.ResetColor();
                    }
                };

                var stdErrHandler = options.Mode == LoginMode.UseDeviceCode ? showDeviceCodeMessage : null;
                var deviceCodePart = options.Mode == LoginMode.UseDeviceCode ? "--use-device-code" : "";
                var queryPart = $"--query \"[?state=='Enabled'].{{Name:name,Id:id,IsDefault:isDefault,UserName:user.name}}\"";

                var cmdOut = await ProcessHelpers.RunShellCommandAsync(
                    "az",
                    $"login --output json {queryPart} {deviceCodePart}",
                    _cliEnv,
                    stdErrHandler: stdErrHandler);
                if (cmdOut.HasError)
                {
                    return ClientResult.From(cmdOut);
                }

                return new ClientResult
                {
                    Outcome = ClientOutcome.Success
                };
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex);
            }
        }

        /// <inheritdoc />
        public async Task<ClientResult<AuthenticationToken>> GetOrRenewAuthToken(CancellationToken token)
        {
            try
            {
                if (_authToken?.Expires > (DateTimeOffset.Now + _minValidity))
                {
                    return new ClientResult<AuthenticationToken>()
                    {
                        Outcome = ClientOutcome.Success,
                        Value = _authToken.Value
                    };
                }

                var cmdOut = await ProcessHelpers.RunShellCommandAsync(
                    "az",
                    "account get-access-token --output json",
                    _cliEnv);
                if (cmdOut.HasError)
                {
                    return ClientResult.From(cmdOut, default(AuthenticationToken));
                }

                var json = JObject.Parse(cmdOut.StdOutput);
                var authToken = new AuthenticationToken()
                {
                    Expires = DateTimeOffset.FromUnixTimeSeconds(json?["expires_on"]?.Value<long>() ?? 0),
                    Token = json?["accessToken"]?.Value<string>() ?? string.Empty
                };

                if (authToken.Expires <= (DateTimeOffset.Now + _minValidity)
                    && string.IsNullOrWhiteSpace(authToken.Token))
                {
                    return new ClientResult<AuthenticationToken>()
                    {
                        Outcome = ClientOutcome.Failed,
                        ErrorDetails = "Could not parse auth token and/or renewed auth token was invalid",
                        Value = default
                    };
                }

                _authToken = authToken;
                return ClientResult.From(authToken);
            }
            catch (Exception ex)
            {
                return ClientResult.FromException(ex, default(AuthenticationToken));
            }
        }

        public void Dispose()
        {
            // TODO implement?
        }
    }
}