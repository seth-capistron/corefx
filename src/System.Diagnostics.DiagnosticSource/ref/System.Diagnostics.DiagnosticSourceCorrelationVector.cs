// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class CorrelationVector
    {
        public CorrelationVector() { }
        public string PreviousValue { get { throw null; } }
        public string Value { get { throw null; } }
        public bool Equals(CorrelationVector correlationVector) { throw null; }
        public string Increment() { throw null; }
        public static CorrelationVector Extend(string correlationVector) { throw null; }
        public static CorrelationVector Spin(string correlationVector) { throw null; }
    }
}
