// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class CorrelationVectorTests
    {
        [Fact]
        public void BasicCorrelationVector()
        {
            var correlationVector = new CorrelationVector();

            Assert.NotNull(correlationVector.Value);
            Assert.False(string.IsNullOrWhiteSpace(correlationVector.Value));
            Assert.Equal(24, correlationVector.Value.Length);
            Assert.True(correlationVector.Value.EndsWith(".0"));
            Assert.Equal(1, correlationVector.Value.Count(c => c == '.'));
        }

        [Fact]
        public void IncrementTriggersReset()
        {
            var correlationVector = new CorrelationVector();

            string correlationVectorString = correlationVector.Value;

            while (correlationVectorString.Length < 124)
            {
                correlationVectorString += ".1";
            }

            CorrelationVector aboutToOverflow = CorrelationVector.Extend(correlationVectorString);

            Assert.Equal(string.Concat(correlationVectorString, ".0"), aboutToOverflow.Value);
            int incrementCounter = 0;

            while (aboutToOverflow.Value.Length > 124)
            {
                string incrementedValue = aboutToOverflow.Increment();
                incrementCounter++;

                Assert.Equal(incrementedValue, aboutToOverflow.Value);
                Assert.False(aboutToOverflow.Value.Length > 127);
            }

            Assert.StartsWith("#", aboutToOverflow.Value);
            Assert.EndsWith(string.Concat(".", incrementCounter), aboutToOverflow.Value);
            Assert.Equal(1, aboutToOverflow.Value.Count(c => c == '.'));
            Assert.DoesNotContain(
                correlationVector.Value.Substring(0, correlationVector.Value.IndexOf('.')),
                aboutToOverflow.Value);

            aboutToOverflow.Increment();
            incrementCounter++;

            Assert.EndsWith(string.Concat(".", incrementCounter), aboutToOverflow.Value);
        }

        [Fact]
        public void ExtendWithSpanId()
        {
            string spanId = "abcd12345";
            var originalCorrelationVector = new CorrelationVector();

            Assert.EndsWith(".0", originalCorrelationVector.Value);

            CorrelationVector extendedVector = CorrelationVector.Extend(originalCorrelationVector.Value, spanId);

            Assert.EndsWith(".0-" + spanId + ".0", extendedVector.Value);
        }

        [Fact]
        public void ExtendWithSpanIdTriggersReset()
        {
            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Extend(originalCorrelationVector.Value);

            while (correlationVector.Value.Length < 120)
            {
                // Keep extending until we get reasonably close to max length
                correlationVector = CorrelationVector.Extend(correlationVector.Value);
            }

            string spanId = new string('*', 127 - correlationVector.Value.Length - 2);

            correlationVector = CorrelationVector.Extend(correlationVector.Value, spanId);

            Assert.StartsWith("#", correlationVector.Value);
            Assert.EndsWith("-" + spanId + ".0", correlationVector.Value);
            Assert.DoesNotContain(
                originalCorrelationVector.Value.Substring(0, originalCorrelationVector.Value.IndexOf('.')),
                correlationVector.Value);
        }

        [Fact]
        public void ExtendTriggersReset()
        {
            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Extend(originalCorrelationVector.Value);

            while (correlationVector.Value.Length > 25)
            {
                // Keep extending until a reset happens (length is reduced to less than 25)
                correlationVector = CorrelationVector.Extend(correlationVector.Value);
            }

            Assert.StartsWith("#", correlationVector.Value);
            Assert.EndsWith(".0", correlationVector.Value);
            Assert.DoesNotContain(
                originalCorrelationVector.Value.Substring(0, originalCorrelationVector.Value.IndexOf('.')),
                correlationVector.Value);
        }
    }
}
