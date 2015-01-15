using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace SharpDX.Toolkit.CefGlue
{
    public static class CefConfig
    {
        public static void Initialize(string cefRoot)
        {
            string environmentVariable = Environment.GetEnvironmentVariable("PATH");
            if (Marshal.SizeOf(typeof(IntPtr)) == 4)
            {
                Environment.SetEnvironmentVariable("PATH", cefRoot + "\\cef_x86;" + environmentVariable);
            }
            else
            {
                Environment.SetEnvironmentVariable("PATH", cefRoot + "\\cef_x64;" + environmentVariable);
            }

            CefRuntime.Load();
            CefMainArgs args2 = new CefMainArgs(new string[]{});
            var cefApp = new SharpDXCefApp();
            CefSettings settings = new CefSettings
            {
                SingleProcess = false,
                MultiThreadedMessageLoop = true,
                LogSeverity = CefLogSeverity.Disable,
                LogFile = "Cef.log"
            };

            try
            {
                CefRuntime.Initialize(args2, settings, cefApp);
            }
            catch (CefRuntimeException ex)
            {
                throw new Exception("Cef failed to initialize");
            }
        }

        public static void Shutdown()
        {
            CefRuntime.Shutdown();
        }
    }
}
