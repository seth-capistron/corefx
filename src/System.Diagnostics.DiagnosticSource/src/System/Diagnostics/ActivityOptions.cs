// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    /// Flags to control behavior of an <see cref="Activity"/>.
    /// </summary>
    [Flags]
    public enum ActivityOptions
    {
        /// <summary>
        /// Use all default options.
        /// </summary>
        None = 0,

        /// <summary>
        /// Default to the behavior of the Parent <see cref="Activity"/>.
        /// </summary>
        DefaultToParent = 1,
        
        /// <summary>
        /// Create a <see cref="CorrelationVector"/> if Parent <see cref="Activity"/> does not
        /// have one defined and SetParentCorrelationVector was not called.
        /// </summary>
        CreateCorrelationVector = 2,

        /// <summary>
        /// Propagate the <see cref="CorrelationVector"/> value along (like in HttpClient).
        /// </summary>
        PropagateCorrelationVector = 4
    }
}
