// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        // This partial implementation contains members common to all HttpClientHandler implementations.
        private const string SocketsHttpHandlerEnvironmentVariableSettingName = "DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER";
        private const string SocketsHttpHandlerAppCtxSettingName = "System.Net.Http.UseSocketsHttpHandler";

        internal static List<Action<HttpRequestMessage>> s_CorrelationPropagationDelegates =
            new List<Action<HttpRequestMessage>>();

        private static bool UseSocketsHttpHandler
        {
            get
            {
                // First check for the AppContext switch, giving it priority over over the environment variable.
                if (AppContext.TryGetSwitch(SocketsHttpHandlerAppCtxSettingName, out bool useSocketsHttpHandler))
                {
                    return useSocketsHttpHandler;
                }

                // AppContext switch wasn't used. Check the environment variable to determine which handler should be used.
                string envVar = Environment.GetEnvironmentVariable(SocketsHttpHandlerEnvironmentVariableSettingName);
                if (envVar != null && (envVar.Equals("false", StringComparison.OrdinalIgnoreCase) || envVar.Equals("0")))
                {
                    // Use WinHttpHandler on Windows and CurlHandler on Unix.
                    return false;
                }

                // Default to using SocketsHttpHandler.
                return true;
            }
        }
        
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator { get; } = delegate { return true; };

        public static Action<HttpRequestMessage> DefaultActivityPropagationDelegate { get; } =
            (HttpRequestMessage request) =>
            {
                // If we are on at all, we propagate any activity information.  
                Activity currentActivity = Activity.Current;
                if (currentActivity != null)
                {
                    request.Headers.Add(DiagnosticsHandlerLoggingStrings.RequestIdHeaderName, currentActivity.Id);
                    //we expect baggage to be empty or contain a few items
                    using (IEnumerator<KeyValuePair<string, string>> e = currentActivity.Baggage.GetEnumerator())
                    {
                        if (e.MoveNext())
                        {
                            var baggage = new List<string>();
                            do
                            {
                                KeyValuePair<string, string> item = e.Current;
                                baggage.Add(new NameValueHeaderValue(item.Key, item.Value).ToString());
                            }
                            while (e.MoveNext());
                            request.Headers.Add(DiagnosticsHandlerLoggingStrings.CorrelationContextHeaderName, baggage);
                        }
                    }
                }
            };

        public static void RegisterCorrelationPropagationDelegate(Action<HttpRequestMessage> propagationDelegate)
        {
            if (propagationDelegate != null)
            {
                s_CorrelationPropagationDelegates.Add(propagationDelegate);
            }
        }

        static HttpClientHandler()
        {
            RegisterCorrelationPropagationDelegate(DefaultActivityPropagationDelegate);
        }
    }
}
