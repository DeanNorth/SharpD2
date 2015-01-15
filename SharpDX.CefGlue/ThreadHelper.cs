using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace SharpDX.Toolkit.CefGlue
{
    public static class ThreadHelpers
    {
        public static void PostTask(CefThreadId threadId, Action action)
        {
            CefRuntime.PostTask(threadId, new ActionTask(action));
        }

        public static void PostTaskUncertainty(CefThreadId threadId, Action action)
        {
            CefRuntime.PostTask(threadId, new ActionTask(action));
        }

        internal sealed class ActionTask : CefTask
        {
            public Action _action;

            public ActionTask(Action action)
            {
                _action = action;
            }

            protected override void Execute()
            {
                _action();
                _action = null;
            }
        }

        internal static void RequireUIThread()
        {
            if (!CefRuntime.CurrentlyOn(CefThreadId.UI))
                throw new InvalidOperationException("This method should be called on CEF UI thread.");
        }

        internal static void RequireRendererThread()
        {
            if (!CefRuntime.CurrentlyOn(CefThreadId.Renderer))
                throw new InvalidOperationException("This method should be called on CEF renderer thread.");
        }
    }
}
