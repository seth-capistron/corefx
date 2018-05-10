// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// This class represents a lightweight vector for identifying and measuring
    /// causality.
    /// </summary>
    public class CorrelationVector
    {
        private const byte BaseLength = 23;
        private const byte MaxVectorLength = 127;

        private const char ElementChar = '.';
        private const char ResetChar = '#';
        private const char SpinChar = '_';
        private const char VersionChar = 'A';

        private string _baseVector = null;
        private int _extension = 0;
        private object _resetLock = new object();

        private static Random randomGenerator = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVector"/> class.
        /// </summary>
        public CorrelationVector()
        {
            string generatedCharacters = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            _baseVector = string.Concat(
                generatedCharacters.Substring(0, CorrelationVector.BaseLength - 1),
                VersionChar);
        }

        /// <summary>
        /// Gets the previous value of the Vector prior to a reset having been performed.
        /// Value will be null if reset has not yet been performed.
        /// </summary>
        public string PreviousValue { get; private set; } = null;

        /// <summary>
        /// Gets the value of the Correlation Vector as a string.
        /// </summary>
        public string Value
        {
            get
            {
                return string.Concat(_baseVector, ElementChar, _extension);
            }
        }

        /// <summary>
        /// Increments the current extension by one. Do this before passing the value to an
        /// outbound message header.
        /// </summary>
        /// <returns>
        /// The new value as a string that you can add to the outbound message header.
        /// </returns>
        public string Increment()
        {
            int snapshot = 0;
            int next = 0;
            do
            {
                snapshot = _extension;
                if (snapshot == int.MaxValue)
                {
                    // Don't proceed past int.MaxValue, since we don't have a performant way of reliably
                    // incrementing and rolling-over (Interlocked.CompareExchange doesn't have a uint implementation)
                    return Value;
                }
                next = snapshot + 1;
                int size = _baseVector.Length + 1 + (int)Math.Log10(next) + 1;
                if (size > CorrelationVector.MaxVectorLength)
                {
                    // Perform a reset
                    lock (_resetLock)
                    {
                        // Check size again in case another thread did the reset
                        size = _baseVector.Length + 1 + (int)Math.Log10(next) + 1;
                        if (size > CorrelationVector.MaxVectorLength)
                        {
                            PreviousValue = Value;
                            _baseVector = ResetVector(_baseVector);
                        }
                    }
                }
            }
            while (snapshot != Interlocked.CompareExchange(ref _extension, next, snapshot));

            return string.Concat(_baseVector, ElementChar, next);
        }

        /// <summary>
        /// Determines whether two instances of the <see cref="CorrelationVector"/> class
        /// are equal. 
        /// </summary>
        /// <param name="correlationVector">
        /// The correlation vector you want to compare with the current correlation vector.
        /// </param>
        /// <returns>
        /// True if the specified correlation vector is equal to the current correlation
        /// vector; otherwise, false.
        /// </returns>
        public bool Equals(CorrelationVector correlationVector)
        {
            return string.Equals(Value, correlationVector.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates a new correlation vector by extending an existing value. This should be
        /// done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public static CorrelationVector Extend(string correlationVector)
        {
            // 2 accounts for the ".0" at the end of the new CorrelationVector
            int size = ((correlationVector == null) ? 0 : correlationVector.Length) + 2;

            if (size > CorrelationVector.MaxVectorLength)
            {
                return new CorrelationVector()
                {
                    _baseVector = ResetVector(correlationVector),
                    _extension = 0,
                    PreviousValue = correlationVector
                };
            }

            return new CorrelationVector()
            {
                _baseVector = correlationVector,
                _extension = 0
            };
        }

        /// <summary>
        /// TBD - describe this thing
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public static CorrelationVector Spin(string correlationVector)
        {
            ulong spinElement = GetTimeSortedRandomLong();

            // 3 accounts for the "_" before the spin element and the
            // ".0" at the end of the new CorrelationVector
            int size = correlationVector.Length + 3 + (int)Math.Log10(spinElement) + 1;

            if (size > CorrelationVector.MaxVectorLength)
            {
                return new CorrelationVector()
                {
                    _baseVector = ResetVector(correlationVector),
                    _extension = 0,
                    PreviousValue = correlationVector
                };
            }

            return new CorrelationVector()
            {
                _baseVector = string.Concat(correlationVector, SpinChar, spinElement),
                _extension = 0
            };
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Value;
        }

        private static string GetBaseFromVector(string correlationVector)
        {
            if (correlationVector == null)
            {
                return correlationVector;
            }

            if (correlationVector.IndexOf(ElementChar) < 0)
            {
                // Not a well-formed vector, just assume the first 23 characters are the base
                return correlationVector.Substring(0, BaseLength);
            }

            return correlationVector.Substring(0, correlationVector.IndexOf(ElementChar));
        }

        private static ulong GetTimeSortedRandomLong()
        {
            byte[] entropy = new byte[4];
            randomGenerator.NextBytes(entropy);

            ulong spinElement = (ulong)(DateTime.UtcNow.Ticks >> 16);

            for (int i = 0; i < 4; i++)
            {
                spinElement = (spinElement << 8) | Convert.ToUInt64(entropy[i]);
            }

            return spinElement;
        }

        private static string ResetVector(string correlationVector)
        {
            string baseVector = GetBaseFromVector(correlationVector);

            return string.Concat(baseVector, ElementChar, ResetChar, GetTimeSortedRandomLong());
        }
    }
}
