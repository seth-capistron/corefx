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
        private const int BaseLength = 23;
        private const int MaxVectorLength = 127;

        private const string ResetChar = "#";
        private const string SpinChar = "_";

        [Fact]
        public void BasicCorrelationVector()
        {
            var correlationVector = new CorrelationVector();

            Assert.NotNull(correlationVector.Value);
            Assert.False(string.IsNullOrWhiteSpace(correlationVector.Value));
            // +2 accounts for .0
            Assert.Equal(BaseLength + 2, correlationVector.Value.Length);
            Assert.True(correlationVector.Value.EndsWith(".0"));
            Assert.Equal(1, correlationVector.Value.Count(c => c == '.'));
            Assert.Null(correlationVector.PreviousValue);
        }

        [Fact]
        public void ExtendAndIncrement()
        {
            string originalVector = "4I2lwitul4NUsfs9Cl7mOfA.1";
            var vector = CorrelationVector.Extend(originalVector);

            Assert.Equal(string.Concat(originalVector, ".0"), vector.ToString());

            var incrementedVector = vector.Increment();

            Assert.Equal(string.Concat(originalVector, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendAndIncrementWithInsufficientBaseCharacters()
        {
            // Less than 22 characters in the base
            string originalValue = "tul4NUsfs9Cl7mOA.1";

            // This shouldn't throw
            var vector = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), vector.ToString());

            vector.Increment();

            Assert.Equal(string.Concat(originalValue, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendAndIncrementWithTooManyBaseCharacters()
        {
            // Greater than 22 characters in the base
            string originalValue = "tul4NUsfs9Cl7mOfN/dupslA.1";

            // This shouldn't throw
            var vector = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), vector.ToString());

            vector.Increment();

            Assert.Equal(string.Concat(originalValue, ".1"), vector.ToString());
        }

        [Fact]
        public void ExtendNullOrEmptyCorrelationVector()
        {
            // None of the below should throw
            var vector = CorrelationVector.Extend(null);
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
            string originalValue = "KZY+dsX2jEaZesgCPjJ2NgA.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.201442.20144";

            var correlationVector = CorrelationVector.Extend(originalValue);

            ValidateResetCorrelationVector(originalValue, correlationVector, expectedExtension: "0");
        }

        [Fact]
        public void ExtendTriggersResetSortValidation()
        {
            // 126 characters (will be 128 after an Extend)
            string originalValue = "KZY+dsX2jEaZesgCPjJ2NgA.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.201442.20144";
            ulong lastResetValue = 0;
            int wrappedCounter = 0;

            for (int i = 0; i < 100; i++)
            {
                CorrelationVector extendedVector = CorrelationVector.Extend(originalValue);

                ValidateResetCorrelationVector(originalValue, extendedVector, expectedExtension: "0");

                // The cV after an Extend that got reset will look like <cvBase>.#<resetValue>.0, 
                // so the resetValue is at index = 1.
                string spinElement = extendedVector.Value.Split('.')[1].Replace(ResetChar, string.Empty);

                ulong resetValue = ulong.Parse(spinElement);

                // Count the number of times the counter wraps.
                if (resetValue <= lastResetValue)
                {
                    wrappedCounter++;
                }

                lastResetValue = resetValue;

                // Wait for 10ms.
                Task.Delay(10).Wait();
            }

            // The counter should wrap at most 1 time.
            Assert.True(wrappedCounter <= 1);
        }

        [Fact]
        public void ExtendWithTooBigExtension()
        {
            // Bigger than UInt.MaxValue
            string originalValue = "tul4NUsfs9Cl7mOfA.11111111111111111111111111111";

            var vector = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), vector.ToString());
        }

        [Fact]
        public void ExtendWithTooBigValue()
        {
            // Bigger than 127 characters
            string originalValue = "KZY+dsX2jEaZesgCPjJ2NgA.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.2147483647.214748364";

            var vector = CorrelationVector.Extend(originalValue);
            
            ValidateResetCorrelationVector(originalValue, vector, expectedExtension: "0");
        }

        [Fact]
        public void ExtendWithTooBigValueAndNoDot()
        {
            // Bigger than 127 characters
            string originalValue = "KZY+dsX2jEaZesgCPjJ2NgA2147483647214748364721474836472147483647214748364721474836472147483647214748364721474836472147483647214748364";

            var vector = CorrelationVector.Extend(originalValue);

            string[] parts = vector.Value.Split('.');
            ulong resetElement;

            // Extend should take the first 23 characters and assume it's the vector base
            Assert.Equal(originalValue.Substring(0, BaseLength), parts[0]);
            Assert.StartsWith(ResetChar, parts[1]);
            Assert.True(ulong.TryParse(parts[1].Substring(1), out resetElement));
            Assert.Equal("0", parts[2]);

            Assert.Equal(originalValue, vector.PreviousValue);
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
                Assert.False(unique.Contains(actual));
                unique.Add(actual);
            }
        }

        [Fact]
        public void IncrementTriggersReset()
        {
            // 125 characters (will be 127 after an Extend)
            string originalValue = "KZY+dsX2jEaZesgCPjJ2NgA.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20144";
            string incrementedValue = null;

            CorrelationVector aboutToOverflow = CorrelationVector.Extend(originalValue);

            Assert.Equal(string.Concat(originalValue, ".0"), aboutToOverflow.Value);
            Assert.Equal(127, aboutToOverflow.Value.Length);

            for (int i = 1; i < 10; i++)
            {
                incrementedValue = aboutToOverflow.Increment();

                Assert.Equal(string.Concat(originalValue, ".", i), aboutToOverflow.Value);
            }

            // One final increment should cause a reset (.9 -> .10 causes length > 127)
            aboutToOverflow.Increment();

            // Validate the properties of a Reset Correlation Vector
            ValidateResetCorrelationVector(
                preResetValue: incrementedValue,
                resetVector: aboutToOverflow,
                expectedExtension: "10");

            // Validate that an additional Increment behaves like normal
            aboutToOverflow.Increment();

            Assert.EndsWith(".11", aboutToOverflow.Value);
        }

        [Fact]
        public void SpinAddsRandomElement()
        {
            var originalCorrelationVector = new CorrelationVector();

            var correlationVector = CorrelationVector.Spin(originalCorrelationVector.Value);

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
        public void SpinSortValidation()
        {
            var vector = new CorrelationVector();
            ulong lastSpinValue = 0;
            int wrappedCounter = 0;

            for (int i = 0; i < 100; i++)
            {
                CorrelationVector spunVector = CorrelationVector.Spin(vector.Value);
                
                // The cV after a Spin will look like <cvBase>.0_<spinValue>.0, so the spinValue is at index = 2.
                string spinElement = spunVector.Value.Split('_')[1].Replace(".0", string.Empty);

                ulong spinValue = ulong.Parse(spinElement);

                // Count the number of times the counter wraps.
                if (spinValue <= lastSpinValue)
                {
                    wrappedCounter++;
                }

                lastSpinValue = spinValue;

                // Wait for 10ms.
                Task.Delay(10).Wait();
            }

            // The counter should wrap at most 1 time.
            Assert.True(wrappedCounter <= 1);
        }

        [Fact]
        public void SpinTriggersReset()
        {
            // 124 characters, which will gaurantee that Spin will cause a Reset
            string originalValue = "KZY+dsX2jEaZesgCPjJ2NgA.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.20442.2044";
            string originalBase = originalValue.Substring(0, originalValue.IndexOf('.'));

            var correlationVector = CorrelationVector.Spin(originalValue);

            ValidateResetCorrelationVector(originalValue, correlationVector, expectedExtension: "0");
        }

        private void ValidateResetCorrelationVector(
            string preResetValue,
            CorrelationVector resetVector,
            string expectedExtension)
        {
            string originalBase = preResetValue.Substring(0, preResetValue.IndexOf('.'));

            string[] parts = resetVector.Value.Split('.');
            ulong resetElement;

            Assert.Equal(originalBase, parts[0]);
            Assert.StartsWith(ResetChar, parts[1]);
            Assert.True(ulong.TryParse(parts[1].Substring(1), out resetElement));
            Assert.Equal(expectedExtension, parts[2]);

            Assert.Equal(preResetValue, resetVector.PreviousValue);
        }
    }
}
