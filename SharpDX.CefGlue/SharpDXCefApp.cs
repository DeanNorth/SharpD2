namespace SharpDX.Toolkit.CefGlue
{
    using System.Collections.Generic;
    using Xilium.CefGlue;
    //using Xilium.CefGlue.Wrapper;

    public sealed class SharpDXCefApp : CefApp
    {
        private CefRenderProcessHandler _renderProcessHandler = new RenderProcessHandler();

        public SharpDXCefApp()
        {
        }

        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            return _renderProcessHandler;
        }
    }

    //class JavascriptRenderProcessHandler : CefRenderProcessHandler
    //{
    //    public JavascriptRenderProcessHandler()
    //    {
    //        MessageRouter = new CefMessageRouterRendererSide(new CefMessageRouterConfig());
    //    }

    //    internal CefMessageRouterRendererSide MessageRouter { get; private set; }

    //    protected override void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
    //    {
    //        MessageRouter.OnContextCreated(browser, frame, context);
    //    }

    //    protected override void OnContextReleased(CefBrowser browser, CefFrame frame, CefV8Context context)
    //    {
    //        MessageRouter.OnContextReleased(browser, frame, context);
    //    }

    //    protected override bool OnProcessMessageReceived(CefBrowser browser, CefProcessId sourceProcess, CefProcessMessage message)
    //    {
    //        var handled = MessageRouter.OnProcessMessageReceived(browser, sourceProcess, message);
    //        if (handled) return true;

    //        if (message.Name == "JavascriptEval")
    //        {
    //            string code = message.Arguments.GetString(0);
    //            CefV8Value returnValue = null;
    //            CefV8Exception exception = null;
    //            var mainFrame = browser.GetMainFrame();
    //            var context = mainFrame.V8Context;
    //            context.TryEval(code, out returnValue, out exception);

    //            if (exception == null)
    //            {
    //                var resultMessage = CefProcessMessage.Create("JavascriptResult");
    //                var arguments = resultMessage.Arguments;

    //                if (returnValue.IsNull) arguments.SetNull(0);
    //                else if (returnValue.IsString) arguments.SetString(0, returnValue.GetStringValue());
    //                else if (returnValue.IsInt) arguments.SetInt(0, returnValue.GetIntValue());
    //                else if (returnValue.IsDouble) arguments.SetDouble(0, returnValue.GetDoubleValue());
    //                else if (returnValue.IsBool) arguments.SetBool(0, returnValue.GetBoolValue());

    //                browser.SendProcessMessage(CefProcessId.Browser, resultMessage);
    //            }
    //            else
    //            {
    //                var exceptionMessage = CefProcessMessage.Create("JavascriptException");
    //                exceptionMessage.Arguments.SetString(0, exception.Message);
    //                browser.SendProcessMessage(CefProcessId.Browser, exceptionMessage);
    //            }

    //            return true;
    //        }

    //        return false;
    //    }
    //}

    internal sealed class RenderProcessHandler : CefRenderProcessHandler
    {
        private static readonly Dictionary<int, CefBrowser> Browsers = new Dictionary<int, CefBrowser>();
        internal static CefBrowser GetBrowser(int id)
        {
            CefBrowser result;
            lock (RenderProcessHandler.Browsers)
            {
                result = RenderProcessHandler.Browsers[id];
            }
            return result;
        }
        protected override void OnBrowserCreated(CefBrowser browser)
        {
            lock (RenderProcessHandler.Browsers)
            {
                RenderProcessHandler.Browsers.Add(browser.Identifier, browser);
            }
        }
        protected override void OnBrowserDestroyed(CefBrowser browser)
        {
            lock (RenderProcessHandler.Browsers)
            {
                RenderProcessHandler.Browsers.Remove(browser.Identifier);
            }
        }
    }

}
