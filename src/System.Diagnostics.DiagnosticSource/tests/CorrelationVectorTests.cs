// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class CorrelationVectorTests
    {
        private const int BaseLength = 22;
        private const int MaxVectorLength = 127;

        private const string ResetChar = "#";
        private const string SpanChar = "-";
        private const string SpinChar = "_";

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
        public void ExtendTriggersReset()
        {
            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Extend(originalCorrelationVector.Value);

            while (correlationVector.Value.Length > (BaseLength + 3))
            {
                // Keep extending until a reset happens (length is reduced to less than 25)
                correlationVector = CorrelationVector.Extend(correlationVector.Value);
            }

            Assert.StartsWith(ResetChar, correlationVector.Value);
            Assert.EndsWith(".0", correlationVector.Value);
            Assert.DoesNotContain(
                originalCorrelationVector.Value.Substring(0, originalCorrelationVector.Value.IndexOf('.')),
                correlationVector.Value);
            Assert.Equal(
                BaseLength,
                correlationVector.Value.Substring(0, correlationVector.Value.IndexOf('.')).Length);
        }

        [Fact]
        public void ExtendWithSpanId()
        {
            string spanId = "abcd12345";
            var originalCorrelationVector = new CorrelationVector();

            Assert.EndsWith(".0", originalCorrelationVector.Value);

            CorrelationVector extendedVector = CorrelationVector.Extend(originalCorrelationVector.Value, spanId);

            Assert.EndsWith(
                string.Concat(".0", SpanChar, spanId, ".0"),
                extendedVector.Value);
        }

        [Fact]
        public void ExtendWithSpanIdTriggersReset()
        {
            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Extend(originalCorrelationVector.Value);

            while (correlationVector.Value.Length < (MaxVectorLength - 7))
            {
                // Keep extending until we get reasonably close to max length
                correlationVector = CorrelationVector.Extend(correlationVector.Value);
            }

            string spanId = new string('*', MaxVectorLength - correlationVector.Value.Length - 2);

            correlationVector = CorrelationVector.Extend(correlationVector.Value, spanId);

            Assert.StartsWith(ResetChar, correlationVector.Value);
            Assert.EndsWith(string.Concat(SpanChar, spanId, ".0"), correlationVector.Value);
            Assert.DoesNotContain(
                originalCorrelationVector.Value.Substring(0, originalCorrelationVector.Value.IndexOf('.')),
                correlationVector.Value);
        }

        [Fact]
        public void IncrementTriggersReset()
        {
            var correlationVector = new CorrelationVector();

            string correlationVectorString = correlationVector.Value;

            while (correlationVectorString.Length < (MaxVectorLength - 3))
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
            }

            Assert.StartsWith(ResetChar, aboutToOverflow.Value);
            Assert.EndsWith(string.Concat(".", incrementCounter), aboutToOverflow.Value);
            Assert.Equal(1, aboutToOverflow.Value.Count(c => c == '.'));
            Assert.DoesNotContain(
                correlationVector.Value.Substring(0, correlationVector.Value.IndexOf('.')),
                aboutToOverflow.Value);
            Assert.Equal(BaseLength, aboutToOverflow.Value.Substring(0, aboutToOverflow.Value.IndexOf('.')).Length);

            aboutToOverflow.Increment();
            incrementCounter++;

            Assert.EndsWith(string.Concat(".", incrementCounter), aboutToOverflow.Value);
        }

        [Fact]
        public void SpinWithSpanId()
        {
            string spanId = "abcd12345";

            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Spin(originalCorrelationVector.Value, spanId);

            Assert.StartsWith(
                string.Concat(originalCorrelationVector.Value, SpanChar, spanId, SpinChar),
                correlationVector.Value);

            Assert.EndsWith(".0", correlationVector.Value);
        }

        [Fact]
        public void SpinWithSpanIdTriggersReset()
        {
            string interimSpanId = "abc123";
            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Extend(originalCorrelationVector.Value);

            // Keep spinning until we get reasonably close to max length
            while (correlationVector.Value.Length < (MaxVectorLength - 20))
            {
                string correlationVectorBeforeSpin = correlationVector.Value;

                correlationVector = CorrelationVector.Spin(correlationVector.Value, interimSpanId);

                Assert.StartsWith(
                    string.Concat(correlationVectorBeforeSpin, SpanChar, interimSpanId, SpinChar),
                    correlationVector.Value);
                Assert.EndsWith(".0", correlationVector.Value);
            }

            // Perform a Spin using a SpanId that will cause a reset
            string spanId = new string('*', MaxVectorLength - correlationVector.Value.Length - 6);

            var resetCorrelationVector = CorrelationVector.Spin(correlationVector.Value, spanId);

            Assert.StartsWith(ResetChar, resetCorrelationVector.Value);
            Assert.EndsWith(SpanChar + spanId + ".0", resetCorrelationVector.Value);
            Assert.DoesNotContain(
                originalCorrelationVector.Value.Substring(0, originalCorrelationVector.Value.IndexOf('.')),
                resetCorrelationVector.Value);
        }
    }
}
