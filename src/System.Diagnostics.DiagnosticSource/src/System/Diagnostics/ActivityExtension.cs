// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    /// An abstract class that is used to hold additional data for a single
    /// <see cref="System.Diagnostics.Activity"/> instance.
    /// </summary>
    public abstract partial class ActivityExtension
    {
        /// <summary>
        /// The <see cref="Diagnostics.Activity"/> instance this extension is linked to.
        /// </summary>
        public Activity Activity { get; private set; }

        /// <summary>
        /// Creates a new <see cref="ActivityExtension"/> instance.
        /// </summary>
        /// <param name="activity">The <see cref="Diagnostics.Activity"/> instance this extension
        /// is linked to.</param>
        public ActivityExtension(Activity activity)
        {
            Activity = activity;
        }

        /// <summary>
        /// Called after the linked <see cref="Diagnostics.Activity"/> is started.
        /// </summary>
        public abstract void ActivityStarted();

        /// <summary>
        /// Called after the linked <see cref="Diagnostics.Activity"/> is stopped.
        /// </summary>
        public abstract void ActivityStopped();
    }
}
