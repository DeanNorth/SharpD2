using D2.FileTypes;
using SharpDX.Toolkit.Content;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace D2.Game
{
    public class Diablo2ReaderFactory : IContentReaderFactory, IContentReader
    {
        public static SynchronizationContext Context;

        public IContentReader TryCreate(Type type)
        {
            if (type == typeof(byte[]))
            {
                return this;
            }

            if (type == typeof(DT1Texture))
            {
                return this;
            }

            if (type == typeof(DS1File))
            {
                return this;
            }

            return type.IsSubclassOf(typeof(Texture)) ? this : null;
        }

        public static byte[] BitmapToByteArray(System.Drawing.Bitmap bitmap)
        {

            BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int numbytes = bmpdata.Stride * bitmap.Height;
            byte[] bytedata = new byte[numbytes];
            IntPtr ptr = bmpdata.Scan0;

            Marshal.Copy(ptr, bytedata, 0, numbytes);

            bitmap.UnlockBits(bmpdata);

            return bytedata;

        }

        public object ReadContent(IContentManager contentManager, ref ContentReaderParameters parameters)
        {
            var service = contentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
            if (service == null)
                throw new InvalidOperationException("Unable to retrieve a IGraphicsDeviceService service provider");

            if (service.GraphicsDevice == null)
                throw new InvalidOperationException("GraphicsDevice is not initialized");

            string asset = parameters.AssetName;

            if (Path.GetExtension(asset).ToLower() == ".dat")
            {
                return D2.FileTypes.StreamHelper.ReadToEnd(parameters.Stream);
            }

            if (Path.GetExtension(asset).ToLower() == ".ds1")
            {
                return new DS1File(parameters.Stream);
            }

            if (Path.GetExtension(asset).ToLower() == ".dt1")
            {
                byte[] palette = null;

                Context.Send(_ =>
                {
                    palette = contentManager.Load<byte[]>(@"data\global\palette\act1\pal.dat");
                }, null);

                var dt1Texture = new DT1Texture();
                dt1Texture.File = new DT1File(parameters.Stream, palette);

                int floorCount = dt1Texture.File.FloorHeaders.Count;
                if (floorCount > 0)
                {
                    Context.Send(_ =>
                    {
                        dt1Texture.FloorsTexture = Texture2D.New(service.GraphicsDevice, 128, 128, SharpDX.Toolkit.Graphics.PixelFormat.B8G8R8A8.UNorm, arraySize: floorCount);
                    }, null);

                    for (int i = 0; i < floorCount; i++)
                    {
                        //var ms = new MemoryStream();
                        //dt1Texture.File.GetFloorImage(i).Save(ms, ImageFormat.Bmp);
                        //var data = ms.ToArray().Skip(54).ToArray();

                        var data = BitmapToByteArray(dt1Texture.File.GetFloorImage(i) as Bitmap);

                        Context.Send(_ =>
                        {
                            dt1Texture.FloorsTexture.SetData(service.GraphicsDevice, data, i, 0, null);
                        }, null);
                    }
                }

                int wallCount = dt1Texture.File.WallHeaders.Count;
                if (wallCount > 0)
                {
                    Context.Send(_ =>
                    {
                        dt1Texture.WallsTexture = Texture2D.New(service.GraphicsDevice, 160, 960, SharpDX.Toolkit.Graphics.PixelFormat.B8G8R8A8.UNorm, arraySize: wallCount);
                    }, null);

                    for (int i = 0; i < wallCount; i++)
                    {
                        //var ms = new MemoryStream();
                        //dt1Texture.File.GetWallImage(i).Save(ms, ImageFormat.Bmp);
                        //var data = ms.ToArray().Skip(54).ToArray();

                        var data = BitmapToByteArray(dt1Texture.File.GetWallImage(i) as Bitmap);

                        Context.Send(_ =>
                        {
                            dt1Texture.WallsTexture.SetData(service.GraphicsDevice, data, i, 0, null);
                        }, null);
                    }
                }

                return dt1Texture;
            }

            if (Path.GetExtension(asset).ToLower() == ".dc6")
            {
                byte[] palette = null;
                Context.Send(_ =>
                {
                    palette = contentManager.Load<byte[]>(@"data\global\palette\loading\pal.dat");
                }, null);
                var dc6File = new DC6File(D2.FileTypes.StreamHelper.ReadToEnd(parameters.Stream), palette);

                int maxWidth = 0;
                int maxHeight = 0;

                var frames = dc6File.Transform();

                foreach (var frame in frames)
                {
                    maxWidth = Math.Max(frame.Width, maxWidth);
                    maxHeight = Math.Max(frame.Height, maxHeight);
                }

                Texture2D dc6Texture = null;

                Context.Send(_ =>
                {
                    dc6Texture = Texture2D.New(service.GraphicsDevice, maxWidth, maxHeight, SharpDX.Toolkit.Graphics.PixelFormat.B8G8R8A8.UNorm, arraySize: frames.Count);
                }, null);

                int i = 0;
                foreach (var frame in frames)
                {
                    //var ms = new MemoryStream();
                    //frame.Save(ms, ImageFormat.Bmp);
                    //var data = ms.ToArray().Skip(54).ToArray();

                    var data = BitmapToByteArray(frame as Bitmap);

                    Context.Send(_ =>
                    {
                        dc6Texture.SetData(service.GraphicsDevice, data, i++, 0, null);
                    }, null);
                }

                return dc6Texture;
            }

            var texture = Texture.Load(service.GraphicsDevice, parameters.Stream);
            if (texture != null)
            {
                texture.Name = parameters.AssetName;
            }

            return texture;

        }
    }
}
