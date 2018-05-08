// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class CorrelationVectorTests
    {
        private const int BaseLength = 22;
        private const int MaxVectorLength = 127;

        private const string ResetChar = "#";
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
            Assert.Null(correlationVector.PreviousBase);
        }

        [Fact]
        public void ExtendAndIncrement()
        {
            string originalVector = "4I2lwitul4NUsfs9Cl7mOf.1";
            var vector = CorrelationVector.Extend(originalVector);

            var splitVector = vector.Value.Split('.');

            Assert.Equal(3, splitVector.Length);
            Assert.Equal("0", splitVector[2]);

            var incrementedVector = vector.Increment();
            splitVector = incrementedVector.Split('.');

            Assert.Equal(3, splitVector.Length);
            Assert.Equal("1", splitVector[2]);

            Assert.Equal(string.Concat(originalVector, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendNullCorrelationVector()
        {
            // This shouldn't throw
            var vector = CorrelationVector.Extend(null);
            Assert.Equal(".0", vector.ToString());
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
            Assert.Equal(
                originalCorrelationVector.Value.Substring(0, originalCorrelationVector.Value.IndexOf('.')),
                correlationVector.PreviousBase);
        }

        [Fact]
        public void IncrementCorrelationVector()
        {
            var vector = new CorrelationVector();

            var splitVector = vector.Value.Split('.');

            Assert.Equal(2, splitVector.Length);
            Assert.Equal(BaseLength, splitVector[0].Length);
            Assert.Equal("0", splitVector[1]);

            var incrementedVector = vector.Increment();
            splitVector = incrementedVector.Split('.');

            Assert.Equal(incrementedVector, vector.Value);
            Assert.Equal(2, splitVector.Length);
            Assert.Equal("1", splitVector[1]);
        }

        [Fact]
        public void IncrementIsUniqueAcrossMultipleThreads()
        {
            CorrelationVector root = new CorrelationVector();
            Task<string>[] all = new Task<string>[1000];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = Task.Run(async () =>
                {
                    await Task.Yield();
                    return root.Increment();
                });
            }
            Task.WaitAll(all);
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < all.Length; i++)
            {
                string actual = all[i].Result;
                //AssertCV.CausedBy(root.Value, actual);
                Assert.False(unique.Contains(actual));
                unique.Add(actual);
            }
        }

        [Fact]
        public void IncrementTriggersReset()
        {
            var correlationVector = new CorrelationVector();

            string correlationVectorString = correlationVector.Value;
            string originalCorrelationVector = correlationVector.Value;

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
            Assert.Equal(
                originalCorrelationVector.Substring(0, originalCorrelationVector.IndexOf('.')),
                aboutToOverflow.PreviousBase);

            aboutToOverflow.Increment();
            incrementCounter++;

            Assert.EndsWith(string.Concat(".", incrementCounter), aboutToOverflow.Value);
        }
    }
}
