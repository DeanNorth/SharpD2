using System;
using Xilium.CefGlue;

namespace SharpDX.Toolkit.CefGlue
{
    internal sealed class SharpDXCefLifeSpanHandler : CefLifeSpanHandler
    {
        private readonly SharpDXCefBrowser _owner;

        public SharpDXCefLifeSpanHandler(SharpDXCefBrowser owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");

            _owner = owner;
        }

        protected override void OnAfterCreated(CefBrowser browser)
        {
            _owner.HandleAfterCreated(browser);
        }
    }
}
