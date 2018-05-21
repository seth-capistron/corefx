// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    /// 
    /// </summary>
    public abstract partial class ExtensibleActivity
    {
        /// <summary>
        /// 
        /// </summary>
        protected Activity Activity { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activity"></param>
        public ExtensibleActivity(Activity activity)
        {
            Activity = activity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentId"></param>
        public abstract void SetExternalParentId(string parentId);

        /// <summary>
        /// 
        /// </summary>
        public abstract void ActivityStarted();

        /// <summary>
        /// 
        /// </summary>
        public abstract void ActivityStopped();
    }
}
