using Microsoft.Win32.SafeHandles;
using SharpDX.Direct3D11;
using SharpDX.Toolkit.Graphics;
using System;
using System.Threading;
using System.Windows.Input;
using Xilium.CefGlue;

namespace SharpDX.Toolkit.CefGlue
{
    internal sealed class SharpDXCefRenderHandler : CefRenderHandler
    {
        private readonly SharpDXCefBrowser _owner;

        private int _height = 768;
        private int _width = 1024;

        private SharpDX.Toolkit.Graphics.Texture2D _offscreenBuffer;
        private SharpDX.Toolkit.Graphics.Texture2D _mainTexture;
        private GraphicsDevice GraphicsDevice;

        private SynchronizationContext synchronization;
        static object loadLock = new object();

        public SharpDX.Toolkit.Graphics.Texture2D Texture
        {
            get
            {
                return _offscreenBuffer;
            }
        }

        public SharpDXCefRenderHandler(SharpDXCefBrowser owner, GraphicsDevice GraphicsDevice)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            _owner = owner;

            this.GraphicsDevice = GraphicsDevice;

            Resize(_owner.Width, _owner.Height);

            synchronization = SynchronizationContext.Current;
        }

        protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
        {
            return GetViewRect(browser, ref rect);
        }

        protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
        {
            rect.X = 0;
            rect.Y = 0;
            rect.Width = _width;
            rect.Height = _height;
            return true;
        }

        protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return true;
        }

        protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
        {
            screenInfo.Depth = 32;
            screenInfo.DepthPerComponent = 8;
            screenInfo.AvailableRectangle = new CefRectangle(0, 0, _width, _height);
            screenInfo.Rectangle = new CefRectangle(0, 0, _width, _height);
            return false;
        }

        protected override void OnPopupShow(CefBrowser browser, bool show)
        {

        }

        protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
        {

        }

        protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
        {
            synchronization.Post((_) =>
            {
                if (type == CefPaintElementType.View)
                {

                    if (this.Resize(width, height))
                    {
                        ((Device)this.GraphicsDevice).ImmediateContext.UpdateSubresource(this._offscreenBuffer, 0, null, buffer, this._width * 4, 0);
                    }
                    else
                    {
                        for (int i = 0; i < dirtyRects.Length; i++)
                        {
                            CefRectangle cefRectangle = dirtyRects[i];
                            ((Device)this.GraphicsDevice).ImmediateContext.UpdateSubresource(this._offscreenBuffer, 0, new ResourceRegion?(new ResourceRegion(cefRectangle.X, cefRectangle.Y, 0, cefRectangle.X + cefRectangle.Width, cefRectangle.Y + cefRectangle.Height, 1)), buffer + cefRectangle.X * 4 + cefRectangle.Y * width * 4, this._width * 4, 0);
                        }
                    }

                    ////try
                    ////{
                    ////    var Pointer = new DataPointer(buffer, this._windowWidth * this._windowHeight * 4);
                    ////    this._offscreenBuffer.SetData(Pointer);
                    ////}
                    ////catch (Exception)
                    ////{

                    ////}


                    //for (int i = 0; i < dirtyRects.Length; i++)
                    //{
                    //    CefRectangle cefRectangle = dirtyRects[i];

                    //    var region = new ResourceRegion(cefRectangle.X, cefRectangle.Y, 0, cefRectangle.X + cefRectangle.Width, cefRectangle.Y + cefRectangle.Height, 1);
                    //    var pointer = new DataPointer(buffer + cefRectangle.X * 4 + cefRectangle.Y * width * 4, cefRectangle.Width * cefRectangle.Height * 4);
                    //    this._offscreenBuffer.SetData(pointer, 0, 0, region);
                    //}

                }

                ((Device)this.GraphicsDevice).ImmediateContext.Flush();

            }, null);
        }
        

        internal bool Resize(int width, int height)
        {
            if (this._width != width || this._height != height || this._offscreenBuffer == null)
            {
                this._width = width;
                this._height = height;
                //if (this._mainTexture != null)
                //{
                //    this._mainTexture.Dispose();
                //}
                if (this._offscreenBuffer != null)
                {
                    this._offscreenBuffer.Dispose();
                }

                this._offscreenBuffer = SharpDX.Toolkit.Graphics.Texture2D.New(this.GraphicsDevice, width, height, SharpDX.Toolkit.Graphics.PixelFormat.B8G8R8A8.UNorm, TextureFlags.ShaderResource, 1, ResourceUsage.Default);

                //this._offscreenBuffer = new SharpDX.Direct3D11.Texture2D((Device)this.GraphicsDevice, new Texture2DDescription
                //{
                //    ArraySize = 1,
                //    BindFlags = BindFlags.ShaderResource,
                //    CpuAccessFlags = CpuAccessFlags.None,
                //    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb,
                //    Width = width,
                //    Height = height,
                //    MipLevels = 1,
                //    OptionFlags = ResourceOptionFlags.Shared,
                //    Usage = ResourceUsage.Default,
                //    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
                //});
                //using (SharpDX.DXGI.Resource resource = ((SharpDX.Direct3D11.Texture2D)this._offscreenBuffer).QueryInterface<SharpDX.DXGI.Resource>())
                //{
                //    this._mainTexture = SharpDX.Toolkit.Graphics.Texture2D.New(this.GraphicsDevice, ((Device)this.GraphicsDevice).OpenSharedResource<SharpDX.Direct3D11.Texture2D>(resource.SharedHandle));
                //}
                return true;
            }
            return false;
        }

        protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle)
        {

        }

        protected override void OnScrollOffsetChanged(CefBrowser browser)
        {
        }
    }
}
