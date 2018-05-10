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
            CorrelationVector vector = CorrelationVector.Extend(originalVector);

            Assert.Equal(string.Concat(originalVector, ".0"), vector.ToString());
            
            string incrementedVector = vector.Increment();

            Assert.Equal(string.Concat(originalVector, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendAndIncrementWithInsufficientBaseCharacters()
        {
            // Less than 22 characters in the base
            string originalValue = "tul4NUsfs9Cl7mO.1";

            // This shouldn't throw
            CorrelationVector vector = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), vector.ToString());

            vector.Increment();

            Assert.Equal(string.Concat(originalValue, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendAndIncrementWithTooManyBaseCharacters()
        {
            // Greater than 22 characters in the base
            string originalValue = "tul4NUsfs9Cl7mOfN/dupsl.1";

            // This shouldn't throw
            CorrelationVector vector = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), vector.ToString());

            vector.Increment();

            Assert.Equal(string.Concat(originalValue, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendNullOrEmptyCorrelationVector()
        {
            // None of the below should throw
            CorrelationVector vector = CorrelationVector.Extend(null);
            Assert.Equal(".0", vector.ToString());

            vector = CorrelationVector.Extend(string.Empty);
            Assert.Equal(".0", vector.ToString());

            vector = CorrelationVector.Extend("   ");
            Assert.Equal("   .0", vector.ToString());
        }

        [Fact]
        public void ExtendTriggersReset()
        {
            // 126 characters (will be 128 after an Extend)
            string originalValue = "KZY+dsX2jEaZesgCPjJ2Ng.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.201442.201442";
            string originalBase = originalValue.Substring(0, originalValue.IndexOf('.'));

            CorrelationVector correlationVector = CorrelationVector.Extend(originalValue);
            
            // Validate the properties of a Reset Correlation Vector
            Assert.StartsWith(ResetChar, correlationVector.Value);
            Assert.EndsWith(".0", correlationVector.Value);
            Assert.DoesNotContain(originalBase, correlationVector.Value);
            Assert.Equal(
                BaseLength,
                correlationVector.Value.Substring(0, correlationVector.Value.IndexOf('.')).Length);
            Assert.Equal(originalBase, correlationVector.PreviousBase);
        }

        [Fact]
        public void ExtendWithTooBigExtension()
        {
            // Bigger than UInt.MaxValue
            string originalValue = "tul4NUsfs9Cl7mOf.11111111111111111111111111111";

            CorrelationVector vector = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), vector.ToString());
        }

        [Fact]
        public void ExtendWithTooBigValue()
        {
            // Bigger than 127 characters
            string originalValue = "KZY+dsX2jEaZesgCPjJ2Ng.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647";

            CorrelationVector vector = CorrelationVector.Extend(originalValue);

            Assert.StartsWith(ResetChar, vector.ToString());
            Assert.EndsWith(".0", vector.ToString());
            Assert.Equal(originalValue.Substring(0, originalValue.IndexOf('.')), vector.PreviousBase);
        }

        [Fact]
        public void ExtendWithTooBigValueAndNoDot()
        {
            // Bigger than 127 characters
            string originalValue = "KZY+dsX2jEaZesgCPjJ2Ng21474836472147483647214748364721474836472147483647214748364721474836472147483647214748364721474836472147483647";

            CorrelationVector vector = CorrelationVector.Extend(originalValue);

            Assert.StartsWith(ResetChar, vector.ToString());
            Assert.EndsWith(".0", vector.ToString());
            Assert.Equal(originalValue, vector.PreviousBase);
        }

        [Fact]
        public void IncrementCorrelationVector()
        {
            CorrelationVector vector = new CorrelationVector();

            string[] splitVector = vector.Value.Split('.');

            Assert.Equal(2, splitVector.Length);
            Assert.Equal(BaseLength, splitVector[0].Length);
            Assert.Equal("0", splitVector[1]);

            string incrementedVector = vector.Increment();
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
                Assert.False(unique.Contains(actual));
                unique.Add(actual);
            }
        }

        [Fact]
        public void IncrementTriggersReset()
        {
            // 125 characters (will be 127 after an Extend)
            string originalValue = "KZY+dsX2jEaZesgCPjJ2Ng.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.201442";
            string originalBase = originalValue.Substring(0, originalValue.IndexOf('.'));

            CorrelationVector aboutToOverflow = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), aboutToOverflow.Value);
            Assert.Equal(127, aboutToOverflow.Value.Length);

            for (int i=1; i<10; i++)
            {
                string incrementedValue = aboutToOverflow.Increment();

                Assert.Equal(string.Concat(originalValue, ".", i), aboutToOverflow.Value);
            }

            // One final increment should cause a reset (.9 -> .10 causes length > 127)
            aboutToOverflow.Increment();

            // Validate the properties of a Reset Correlation Vector
            Assert.StartsWith(ResetChar, aboutToOverflow.Value);
            Assert.EndsWith(".10", aboutToOverflow.Value);
            Assert.Equal(1, aboutToOverflow.Value.Count(c => c == '.'));
            Assert.DoesNotContain(originalBase, aboutToOverflow.Value);
            Assert.Equal(BaseLength, aboutToOverflow.Value.Substring(0, aboutToOverflow.Value.IndexOf('.')).Length);
            Assert.Equal(originalBase, aboutToOverflow.PreviousBase);

            // Validate that an additional Increment behaves like normal
            aboutToOverflow.Increment();
            
            Assert.EndsWith(".11", aboutToOverflow.Value);
        }

        [Fact]
        public void SpinAddsRandomElement()
        {
            var originalCorrelationVector = new CorrelationVector();

            CorrelationVector correlationVector = CorrelationVector.Spin(originalCorrelationVector.Value);

            Assert.StartsWith(
                string.Concat(originalCorrelationVector.Value, SpinChar),
                correlationVector.Value);
            Assert.EndsWith(".0", correlationVector.Value);

            // Validate the random element added is an unsigned long
            int spinCharIndex = correlationVector.Value.IndexOf(SpinChar);
            int lastDotIndex = correlationVector.Value.LastIndexOf(".");

            string spinElementString = correlationVector.Value.Substring(
                spinCharIndex + 1,
                lastDotIndex - spinCharIndex - 1);
            ulong spinElement;

            Assert.True(ulong.TryParse(spinElementString, out spinElement));
        }

        [Fact]
        public void SpinTriggersReset()
        {
            // 124 characters, which will gaurantee that Spin will cause a Reset
            string originalValue = "KZY+dsX2jEaZesgCPjJ2Ng.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442";
            string originalBase = originalValue.Substring(0, originalValue.IndexOf('.'));

            CorrelationVector correlationVector = CorrelationVector.Spin(originalValue);

            // Validate the properties of a Reset Correlation Vector
            Assert.StartsWith(ResetChar, correlationVector.Value);
            Assert.EndsWith(".0", correlationVector.Value);
            Assert.DoesNotContain(originalBase, correlationVector.Value);
            Assert.Equal(
                BaseLength,
                correlationVector.Value.Substring(0, correlationVector.Value.IndexOf('.')).Length);
            Assert.Equal(originalBase, correlationVector.PreviousBase);
        }
    }
}
