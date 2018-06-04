// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// CorrelationPropagationHandler notifies CorrelationPropagationDelegates about outgoing Http requests,
    /// giving them an opportunity to attach outgoing correlation information.
    /// </summary>
    internal class CorrelationPropagationHandler : DelegatingHandler
    {
        public CorrelationPropagationHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        public Action<HttpRequestMessage> CorrelationPropagationOverride { get; set; }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (CorrelationPropagationOverride != null)
            {
                CorrelationPropagationOverride(request);
            }
            else
            {
                HttpClientHandler.s_CorrelationPropagationDelegates.ForEach(
                    propagationDelegate => { propagationDelegate(request); });
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
