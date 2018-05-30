// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientTestBase : RemoteExecutorTestBase
    {
        protected virtual bool UseSocketsHttpHandler => true;

        protected bool IsWinHttpHandler => !UseSocketsHttpHandler && PlatformDetection.IsWindows && !PlatformDetection.IsUap && !PlatformDetection.IsFullFramework;
        protected bool IsCurlHandler => !UseSocketsHttpHandler && !PlatformDetection.IsWindows;
        protected bool IsNetfxHandler => PlatformDetection.IsWindows && PlatformDetection.IsFullFramework;
        protected bool IsUapHandler => PlatformDetection.IsWindows && PlatformDetection.IsUap;

        protected HttpClient CreateHttpClient() => new HttpClient(CreateHttpClientHandler());

        protected HttpClientHandler CreateHttpClientHandler() => CreateHttpClientHandler(UseSocketsHttpHandler);

        protected static HttpClient CreateHttpClient(string useSocketsHttpHandlerBoolString) =>
            CreateHttpClient(useSocketsHttpHandlerBoolString, null);

        protected static HttpClient CreateHttpClient(
            string useSocketsHttpHandlerBoolString,
            Action<HttpRequestMessage> correlationPropagationOverride) =>
                new HttpClient(CreateHttpClientHandler(useSocketsHttpHandlerBoolString, correlationPropagationOverride));

        protected static HttpClientHandler CreateHttpClientHandler(string useSocketsHttpHandlerBoolString) =>
            CreateHttpClientHandler(bool.Parse(useSocketsHttpHandlerBoolString));

        protected static HttpClientHandler CreateHttpClientHandler(
            string useSocketsHttpHandlerBoolString,
            Action<HttpRequestMessage> correlationPropagationOverride) =>
                CreateHttpClientHandler(bool.Parse(useSocketsHttpHandlerBoolString), correlationPropagationOverride);

        protected static HttpClientHandler CreateHttpClientHandler(bool useSocketsHttpHandler) =>
            CreateHttpClientHandler(useSocketsHttpHandler, null);

        protected static HttpClientHandler CreateHttpClientHandler(
            bool useSocketsHttpHandler,
            Action<HttpRequestMessage> correlationPropagationOverride)
        {
            if (PlatformDetection.IsUap || PlatformDetection.IsFullFramework || useSocketsHttpHandler)
            {
                return new HttpClientHandler() { CorrelationPropagationOverride = correlationPropagationOverride };
            }

            // Create platform specific handler.
            ConstructorInfo ctor = typeof(HttpClientHandler).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
            Debug.Assert(ctor != null, "Couldn't find test constructor on HttpClientHandler");

            HttpClientHandler handler = (HttpClientHandler)ctor.Invoke(new object[] { useSocketsHttpHandler });
            Debug.Assert(useSocketsHttpHandler == IsSocketsHttpHandler(handler), "Unexpected handler.");

            handler.CorrelationPropagationOverride = correlationPropagationOverride;

            return handler;
        }

        protected static bool IsSocketsHttpHandler(HttpClientHandler handler) =>
            GetUnderlyingSocketsHttpHandler(handler) != null;

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            FieldInfo field = typeof(HttpClientHandler).GetField("_socketsHttpHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }
    }
}
