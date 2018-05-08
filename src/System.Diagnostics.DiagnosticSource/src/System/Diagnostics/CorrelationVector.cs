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

        private string baseVector = null;
        private int extension = 0;
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
        /// Gets the value of the Vector Base prior to a reset being performed.
        /// </summary>
        public string PreviousBase { get; private set; } = null;

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
                            this.PreviousBase = GetBaseFromVector(this.baseVector);
                            this.baseVector = CorrelationVector.GetBaseFromGuid(isReset: true);
                        }
                    }
                }
            }
            while (snapshot != Interlocked.CompareExchange(ref this.extension, next, snapshot));

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
                    extension = 0,
                    PreviousBase = GetBaseFromVector(correlationVector)
                };
            }

            return new CorrelationVector()
            {
                baseVector = correlationVector,
                extension = 0
            };
        }

        private static string GetBaseFromGuid(bool isReset)
        {
            string generatedCharacters = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            
            // If isReset, then prepend '#'. Length will always be 22 total (including '#', if present).
            return string.Concat(
                isReset ? ResetChar.ToString() : string.Empty,
                generatedCharacters.Substring(0, CorrelationVector.BaseLength - (isReset ? 1 : 0)));
        }

        private static string GetBaseFromVector(string correlationVector)
        {
            return correlationVector.Substring(0, correlationVector.IndexOf('.'));
        }

        //private static void NotifyError(Exception exception)
        //{
        //    // Throw and catch the exception.  This lets it be seen by the debugger
        //    // ETW, and other monitoring tools.   However we immediately swallow the
        //    // exception.   We may wish in the future to allow users to hook this 
        //    // in other useful ways but for now we simply swallow the exceptions.  
        //    try
        //    {
        //        throw exception;
        //    }
        //    catch { }
        //}
    }
}
