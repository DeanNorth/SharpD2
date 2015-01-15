using System;
using Xilium.CefGlue;

namespace SharpDX.Toolkit.CefGlue
{
    internal sealed class SharpDXCefDisplayHandler : CefDisplayHandler
    {
        SharpDXCefBrowser _owner;

        public SharpDXCefDisplayHandler(SharpDXCefBrowser owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");

            _owner = owner;
        }

        //protected override void OnLoadingStateChange(CefBrowser browser, bool isLoading, bool canGoBack, bool canGoForward)
        //{
        //}

        protected override void OnAddressChange(CefBrowser browser, CefFrame frame, string url)
        {
        }

        protected override void OnTitleChange(CefBrowser browser, string title)
        {
        }

        protected override bool OnTooltip(CefBrowser browser, string text)
        {
            return true; // _owner.OnTooltip(text);
        }

        protected override void OnStatusMessage(CefBrowser browser, string value)
        {
        }

        protected override bool OnConsoleMessage(CefBrowser browser, string message, string source, int line)
        {
            return false;
        }
    }
}
