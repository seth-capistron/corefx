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
        private const byte BaseLength = 22;
        private const byte MaxVectorLength = 127;
        private const byte SpinLength = 4;

        private const char ResetChar = '#';
        private const char SpanChar = '-';
        private const char SpinChar = '_';

        private string baseVector = null;
        private uint extension = 0;
        object incrementLock = new object();
        private object resetLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationVector"/> class.
        /// </summary>
        public CorrelationVector()
        {
            this.baseVector = CorrelationVector.GetBaseFromGuid(isReset: false);
        }

        /// <summary>
        /// Gets the value of the correlation vector as a string.
        /// </summary>
        public string Value
        {
            get
            {
                return string.Concat(this.baseVector, ".", this.extension);
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
            uint snapshot = 0;
            uint next = 0;
            do
            {
                snapshot = this.extension;
                next = snapshot + 1;
                int size = baseVector.Length + 1 + (int)Math.Log10(next) + 1;
                if (size > CorrelationVector.MaxVectorLength)
                {
                    // Perform a reset
                    lock (resetLock)
                    {
                        // Check size again in case another thread did the reset
                        size = baseVector.Length + 1 + (int)Math.Log10(next) + 1;
                        if (size > CorrelationVector.MaxVectorLength)
                        {
                            this.baseVector = CorrelationVector.GetBaseFromGuid(isReset: true);
                        }
                    }
                }
            }
            while (!AttemptCommitIncrement(next, snapshot));

            return string.Concat(this.baseVector, ".", next);
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
            int size = correlationVector.Length + 2;

            if (size > CorrelationVector.MaxVectorLength)
            {
                return new CorrelationVector()
                {
                    baseVector = CorrelationVector.GetBaseFromGuid(isReset: true),
                    extension = 0
                };
            }

            return new CorrelationVector()
            {
                baseVector = correlationVector,
                extension = 0
            };
        }

        /// <summary>
        /// Creates a new correlation vector by extending an existing value. This should be
        /// done at the entry point of an operation.
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header.
        /// </param>
        /// <param name="spanId">
        /// ID of the Span.
        /// </param>
        /// <returns>A new correlation vector extended from the current vector.</returns>
        public static CorrelationVector Extend(string correlationVector, string spanId)
        {
            // 3 accounts for the dash '-' before spanId and ".0" at the end of the
            // new CorrelationVector
            int size = correlationVector.Length + spanId.Length + 3;

            if (size > CorrelationVector.MaxVectorLength)
            {
                return new CorrelationVector()
                {
                    baseVector = string.Concat(
                        CorrelationVector.GetBaseFromGuid(isReset: true),
                        SpanChar,
                        spanId),
                    extension = 0
                };
            }

            return new CorrelationVector()
            {
                baseVector = string.Concat(correlationVector, SpanChar, spanId),
                extension = 0
            };
        }

        /// <summary>
        /// TBD - describe this thing
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header.
        /// </param>
        /// <returns>TBD - describe this thing.</returns>
        public static CorrelationVector Spin(string correlationVector)
        {
            var random = new Random(DateTime.Now.Millisecond);
            int spinElement = random.Next(
                (int)Math.Pow(10, (SpinLength - 1)),
                (int)Math.Pow(10, (SpinLength)) - 1);

            // 3 accounts for the "_" before the spinElement and the ".0"  at the end of the new Correlation Vector
            int size = correlationVector.Length + (int)Math.Log10(spinElement) + 3;

            if (size > CorrelationVector.MaxVectorLength)
            {
                return new CorrelationVector()
                {
                    baseVector = CorrelationVector.GetBaseFromGuid(isReset: true),
                    extension = 0
                };
            }

            return new CorrelationVector()
            {
                baseVector = string.Concat(correlationVector, SpinChar, spinElement),
                extension = 0
            };
        }

        /// <summary>
        /// TBD - describe this thing
        /// </summary>
        /// <param name="correlationVector">
        /// Taken from the message header.
        /// </param>
        /// <param name="spanId">
        /// TBD - describe this thing.
        /// </param>
        /// <returns>TBD - describe this thing.</returns>
        public static CorrelationVector Spin(string correlationVector, string spanId)
        {
            var random = new Random(DateTime.Now.Millisecond);
            int spinElement = random.Next(
                (int)Math.Pow(10, (SpinLength - 1)),
                (int)Math.Pow(10, (SpinLength)) - 1);

            // 3 accounts for the "-" before the spanId, the "_" before the spinElement
            // and the ".0"  at the end of the new Correlation Vector
            int size = correlationVector.Length + spanId.Length + (int)Math.Log10(spinElement) + 4;

            if (size > CorrelationVector.MaxVectorLength)
            {
                return new CorrelationVector()
                {
                    baseVector = string.Concat(
                        CorrelationVector.GetBaseFromGuid(isReset: true),
                        SpanChar,
                        spanId),
                    extension = 0
                };
            }

            return new CorrelationVector()
            {
                baseVector = string.Concat(correlationVector, SpanChar, spanId, SpinChar, spinElement),
                extension = 0
            };
        }

        private bool AttemptCommitIncrement(uint next, uint snapshot)
        {
            lock (incrementLock)
            {
                if (this.extension != snapshot)
                {
                    return false;
                }

                this.extension = next;
                return true;
            }
        }

        private static string GetBaseFromGuid(bool isReset)
        {
            string generatedCharacters = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            
            // If isReset, then prepend '#'. Length will always be 22 total (including '#', if present).
            return string.Concat(
                isReset ? ResetChar.ToString() : string.Empty,
                generatedCharacters.Substring(0, CorrelationVector.BaseLength - (isReset ? 1 : 0)));
        }
    }
}
