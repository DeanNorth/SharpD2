using System;
using System.Collections.Generic;

namespace D2.Game
{
    /// <summary>
    /// Simple Game application using SharpDX.Toolkit.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
#if NETFX_CORE
        [MTAThread]
#else
        [STAThread]
#endif
        static void Main()
        {
            using (var program = new Game())
                program.Run();

        }
    }
}