// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Collections.Generic;

namespace System.Diagnostics
{
    public abstract partial class ExtensibleActivity
    {
        public ExtensibleActivity(Activity activity) { }
        protected Activity Activity { get; private set; }
        public abstract void SetExternalParentId(string parentId);
        public abstract void ActivityStarted();
        public abstract void ActivityStopped();
    }
}

