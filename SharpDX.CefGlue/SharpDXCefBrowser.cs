using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.WPF;

namespace SharpDX.Toolkit.CefGlue
{
    public class SharpDXCefBrowser : IDisposable
    {
        private CefBrowser _browser;
        private CefBrowserHost _browserHost;
        private SharpDXCefClient _cefClient;
        
        private ManualResetEvent loaded = new ManualResetEvent(false);
        private bool loading = false;

        public int Width { get; set; }
        public int Height { get; set; }

        public Texture2D Texture
        {
            get
            {
                return _cefClient._renderHandler.Texture;
            }
        }

        public SharpDXCefBrowser(GraphicsDevice GraphicsDevice, int width = 1024, int height = 768)
        {
            Width = width;
            Height = height;

            var windowInfo = CefWindowInfo.Create();
            windowInfo.SetAsOffScreen(IntPtr.Zero);
            windowInfo.TransparentPainting = true;
                       

            var cefBrowserSettings = new CefBrowserSettings();

            _cefClient = new SharpDXCefClient(this, GraphicsDevice);

            CefBrowserHost.CreateBrowser(windowInfo, _cefClient, cefBrowserSettings);
        }

        public void HandleAfterCreated(CefBrowser browser)
        {
            if (_browser == null)
            {
                _browser = browser;
                _browserHost = _browser.GetHost();
            }
        }

        #region Loading Events

        public event LoadStartEventHandler LoadStart;
        public event LoadEndEventHandler LoadEnd;
        public event LoadingStateChangeEventHandler LoadingStateChange;
        public event LoadErrorEventHandler LoadError;

        internal void OnLoadStart(CefFrame frame)
        {
            if (frame.IsMain)
            {
                loading = true;
            }

            if (this.LoadStart != null)
            {
                var e = new LoadStartEventArgs(frame);
                this.LoadStart(this, e);
            }
        }

        internal void OnLoadEnd(CefFrame frame, int httpStatusCode)
        {
            if (frame.IsMain)
            {
                loading = false;
                loaded.Set();
            }

            if (this.LoadEnd != null)
            {
                var e = new LoadEndEventArgs(frame, httpStatusCode);
                this.LoadEnd(this, e);
            }
        }
        internal void OnLoadingStateChange(bool isLoading, bool canGoBack, bool canGoForward)
        {
            if (this.LoadingStateChange != null)
            {
                var e = new LoadingStateChangeEventArgs(isLoading, canGoBack, canGoForward);
                this.LoadingStateChange(this, e);
            }
        }
        internal void OnLoadError(CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
        {
            if (frame.IsMain)
            {
                loading = false;
                loaded.Set();
            }

            if (this.LoadError != null)
            {
                var e = new LoadErrorEventArgs(frame, errorCode, errorText, failedUrl);
                this.LoadError(this, e);
            }
        }

        #endregion

        #region Disposable

        ~SharpDXCefBrowser()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

                // TODO: What's the right way of disposing the browser instance?
                if (_browserHost != null)
                {
                    _browserHost.CloseBrowser(true);
                    _browserHost = null;
                }

                if (_browser != null)
                {
                    _browser.Dispose();
                    _browser = null;
                }
            }
        }

        #endregion

        public void Focus(bool focus = true)
        {
            this._browserHost.SetFocus(focus);
            this._browserHost.SendFocusEvent(focus);
        }

        public void DeleteCookies()
        {
            ThreadHelpers.PostTask(CefThreadId.IO, () =>
            {
                CefCookieManager.Global.DeleteCookies(null, null);
            });
        }

        public void SendMouseMoveEvent(int x, int y, bool leftDown)
        {
            this._browserHost.SendMouseMoveEvent(new CefMouseEvent
            {
                X = x,
                Y = y, 
                Modifiers = leftDown ? CefEventFlags.LeftMouseButton : CefEventFlags.None
            }, false);
        }

        public void SendMouseWheelEvent(int delta, int x, int y)
        {
            this._browserHost.SendMouseWheelEvent(new CefMouseEvent
            {
                X = x,
                Y = y
            }, 0, delta);
        }

        public void SendMouseButtonEvent(int x, int y, CefMouseButtonType buttonType, bool mouseUp)
        {
            this._browserHost.SendMouseClickEvent(new CefMouseEvent
            {
                X = x,
                Y = y
            }, buttonType, mouseUp, 1);
        }
        public void SendKeyEvent(CefKeyEvent keyEvent)
        {
            this._browserHost.SendKeyEvent(keyEvent);
        }


        #region Automation Helpers

        public object TryEvaluateScript(string script)
        {
            try
            {
                var result = this._cefClient.TryEval(script, _browser);

                return result;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public SharpDXCefBrowser WaitForPageLoad(int timeout = 10000)
        {
            System.Threading.Thread.Sleep(100);

            loaded.Reset();
            if (!loaded.WaitOne(timeout))
            {
                throw new TimeoutException();
            }

            return this;
        }

        public SharpDXCefBrowser NavigateTo(string url)
        {
            url = url.TrimStart();

            if (_browser != null)
                _browser.GetMainFrame().LoadUrl(url);

            return this;

            loading = true;
            return WaitForPageLoad();
        }

        public SharpDXCefBrowser SetValue(string cssSelector, string value)
        {
            TryEvaluateScript(string.Format("$('{0}').val('{1}').change(); void(0);", cssSelector, value));
            return this;
        }

        public string GetValue(string cssSelector)
        {
            var resultJSON = TryEvaluateScript(string.Format("$('{0}').val();", cssSelector));
            return (resultJSON ?? string.Empty).ToString();
        }

        public string GetText(string cssSelector)
        {
            var resultJSON = TryEvaluateScript(string.Format("$('{0}').text();", cssSelector));
            return (resultJSON ?? string.Empty).ToString();
        }

        public SharpDXCefBrowser Click(string cssSelector)
        {
            TryEvaluateScript(string.Format("$('{0}').trigger('click'); void(0);", cssSelector));
            TryEvaluateScript(string.Format("$('{0}')[0].click(); void(0);", cssSelector));

            return this;
        }

        #endregion

    }
}
