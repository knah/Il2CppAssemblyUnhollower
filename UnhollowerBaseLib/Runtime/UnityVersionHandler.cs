using UnhollowerBaseLib.Runtime.VersionSpecific;

namespace UnhollowerBaseLib.Runtime
{
    public static class UnityVersionHandler
    {
        private static INativeClassStructHandler ourHandler;
        
        /// <summary>
        /// Initializes Unity interface for specified Unity version.
        /// </summary>
        /// <example>For Unity 2018.4.20, call <c>Initialize(2018, 4, 20)</c></example>
        public static void Initialize(int majorVersion, int minorVersion, int patchVersion)
        {
	        if (majorVersion <= 2018)
	        {
		        if (minorVersion < 4)
		        {
                    LogSupport.Trace("Using Unity2018.0 handler");
                    ourHandler = new Unity2018_0NativeClassStructHandler();
		        }
		        else
                {
	                LogSupport.Trace("Using Unity2018.4 handler");
                    ourHandler = new Unity2018_4NativeClassStructHandler();
		        }
	        }
	        else
	        {
		        LogSupport.Trace("Using Unity2019 handler");
                ourHandler = new Unity2019NativeClassStructHandler();
	        }
        }

        private static INativeClassStructHandler Handler
        {
            get
            {
                if (ourHandler == null)
                {
                    LogSupport.Warning("Using native interop infrastructure before setting Unity version, defaulting to 2018.4.20");
                    Initialize(2018, 4, 20);
                }

                return ourHandler;
            }
        }

        public static INativeClassStruct NewClass(int vTableSlots) => Handler.CreateNewClassStruct(vTableSlots);
        public static unsafe INativeClassStruct Wrap(Il2CppClass* classPointer) => Handler.Wrap(classPointer);
    }
}