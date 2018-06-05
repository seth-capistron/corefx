#if ALLOW_PARTIALLY_TRUSTED_CALLERS
    using System.Security;
#endif
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// An <see cref="ActivityExtension"/> that replicates the legacy Request-Id behavior.
    /// </summary>
    public class LegacyActivityExtension : ActivityExtension
    {
        /// <summary>
        /// Constructs a new <see cref="LegacyActivityExtension"/> instance.
        /// </summary>
        /// <param name="activity">The activity that this instance is associated with.</param>
        public LegacyActivityExtension(Activity activity)
            : base(activity)
        { }

        /// <summary>
        /// This Id of this instance.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The Root Id of this instance.
        /// </summary>
        public string RootId
        {
            get
            {
                // We expect RootId to be requested at any time after activity is created, 
                // possibly even before it was started for sampling or logging purposes
                // Presumably, it will be called by logging systems for every log record, so we cache it.
                if (_rootId == null)
                {
                    if (Id != null)
                    {
                        _rootId = GetRootId(Id);
                    }
                    else if (ParentId != null)
                    {
                        _rootId = GetRootId(ParentId);
                    }
                }
                return _rootId;
            }
        }

        /// <summary>
        /// The Id of the parent of this instance.
        /// </summary>
        public string ParentId { get; private set; }

        /// <summary>
        /// Sets the Parent Id of this instance. This is intended to be used only at 'boundary' 
        /// scenarios where an activity from another process logically started this activity.
        /// </summary>
        /// <param name="parentId"></param>
        public void SetParentId(string parentId)
        {
            if (ParentId != null)
            {
                NotifyError(new InvalidOperationException($"{nameof(ParentId)} is already set"));
            }
            else if (string.IsNullOrEmpty(parentId))
            {
                NotifyError(new ArgumentException($"{nameof(parentId)} must not be null or empty"));
            }
            else
            {
                ParentId = parentId;
            }
        }

        /// <summary>
        /// This method is called by <see cref="Activity"/> when the linked activity is started.
        /// </summary>
        public override void ActivityStarted()
        {
            Id = GenerateId();
        }

        /// <summary>
        /// This method is called by <see cref="Activity"/> when the linked activity is stopped.
        /// </summary>
        public override void ActivityStopped()
        { }

        #region Private
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [SecuritySafeCritical]
#endif
        private static unsafe long GetRandomNumber()
        {
            // Use the first 8 bytes of the GUID as a random number.  
            Guid g = Guid.NewGuid();
            return *((long*)&g);
        }

        private string GenerateId()
        {
            string ret;
            if (Activity.Parent != null)
            {
                LegacyActivityExtension parentExtension = Activity.Parent.GetActivityExtension<LegacyActivityExtension>();

                // Normal start within the process
                Debug.Assert(!string.IsNullOrEmpty(parentExtension.Id));
                ret = AppendSuffix(parentExtension.Id, Interlocked.Increment(ref parentExtension._currentChildId).ToString(), '.');
                ParentId = parentExtension.Id;
            }
            else if (ParentId != null)
            {
                // Start from outside the process (e.g. incoming HTTP)
                Debug.Assert(ParentId.Length != 0);

                //sanitize external RequestId as it may not be hierarchical. 
                //we cannot update ParentId, we must let it be logged exactly as it was passed.
                string parentId = ParentId[0] == '|' ? ParentId : '|' + ParentId;

                char lastChar = parentId[parentId.Length - 1];
                if (lastChar != '.' && lastChar != '_')
                {
                    parentId += '.';
                }

                ret = AppendSuffix(parentId, Interlocked.Increment(ref s_currentRootId).ToString("x"), '_');
            }
            else
            {
                // A Root Activity (no parent).  
                ret = GenerateRootId();
            }
            // Useful place to place a conditional breakpoint.  
            return ret;
        }

        private string AppendSuffix(string parentId, string suffix, char delimiter)
        {
#if DEBUG
            suffix = Activity.OperationName.Replace('.', '-') + "-" + suffix;
#endif
            if (parentId.Length + suffix.Length < RequestIdMaxLength)
                return parentId + suffix + delimiter;

            //Id overflow:
            //find position in RequestId to trim
            int trimPosition = RequestIdMaxLength - 9; // overflow suffix + delimiter length is 9
            while (trimPosition > 1)
            {
                if (parentId[trimPosition - 1] == '.' || parentId[trimPosition - 1] == '_')
                    break;
                trimPosition--;
            }

            //ParentId is not valid Request-Id, let's generate proper one.
            if (trimPosition == 1)
                return GenerateRootId();

            //generate overflow suffix
            string overflowSuffix = ((int)GetRandomNumber()).ToString("x8");
            return parentId.Substring(0, trimPosition) + overflowSuffix + '#';
        }

        private string GenerateRootId()
        {
            // It is important that the part that changes frequently be first, because
            // many hash functions don't 'randomize' the tail of a string.   This makes
            // sampling based on the hash produce poor samples.
            return '|' + Interlocked.Increment(ref s_currentRootId).ToString("x") + s_uniqSuffix;
        }

        private string GetRootId(string id)
        {
            //id MAY start with '|' and contain '.'. We return substring between them
            //ParentId MAY NOT have hierarchical structure and we don't know if initially rootId was started with '|',
            //so we must NOT include first '|' to allow mixed hierarchical and non-hierarchical request id scenarios
            int rootEnd = id.IndexOf('.');
            if (rootEnd < 0)
                rootEnd = id.Length;
            int rootStart = id[0] == '|' ? 1 : 0;
            return id.Substring(rootStart, rootEnd - rootStart);
        }

        private static void NotifyError(Exception exception)
        {
            // Throw and catch the exception.  This lets it be seen by the debugger
            // ETW, and other monitoring tools.   However we immediately swallow the
            // exception.   We may wish in the future to allow users to hook this 
            // in other useful ways but for now we simply swallow the exceptions.  
            try
            {
                throw exception;
            }
            catch { }
        }

        private const int RequestIdMaxLength = 1024;

        // Used to generate an ID it represents the machine and process we are in.  
        private static readonly string s_uniqSuffix = "-" + GetRandomNumber().ToString("x") + ".";

        //A unique number inside the appdomain, randomized between appdomains. 
        //Int gives enough randomization and keeps hex-encoded s_currentRootId 8 chars long for most applications
        private static long s_currentRootId = (uint)GetRandomNumber();

        private string _rootId;
        private int _currentChildId;  // A unique number for all children of this activity.  

        #endregion
    }
}
