// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    /// Represents a mapping that will be logged after a Correlation Vector has been Reset
    /// due to its length exceeding the maximum allowed.
    /// </summary>
    public class CorrelationVectorResetResult
    {
        /// <summary>
        /// Correlation Vector base prior to the Reset.
        /// </summary>
        public string BaseVector { get; set; } = null;

        /// <summary>
        /// Correlation Vector extension that resulted from the Reset.
        /// </summary>
        public string ResetExtension { get; set; } = null;
    }
}
