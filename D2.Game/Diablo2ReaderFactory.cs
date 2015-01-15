using D2.FileTypes;
using SharpDX.Toolkit.Content;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.Game
{
    public class Diablo2ReaderFactory : IContentReaderFactory, IContentReader
    {
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
                //try
                //{
                    var palette = contentManager.Load<byte[]>(@"data\global\palette\act1\pal.dat");

                    var dt1Texture = new DT1Texture();
                    dt1Texture.File = new DT1File(parameters.Stream, palette);

                    int floorCount = dt1Texture.File.FloorHeaders.Count;
                    if (floorCount > 0)
                    {
                        dt1Texture.FloorsTexture = Texture2D.New(service.GraphicsDevice, 128, 128, SharpDX.Toolkit.Graphics.PixelFormat.B8G8R8A8.UNorm, arraySize: floorCount);

                        for (int i = 0; i < floorCount; i++)
                        {
                            var ms = new MemoryStream();
                            dt1Texture.File.GetFloorImage(i).Save(ms, ImageFormat.Bmp);
                            var data = ms.ToArray().Skip(54).ToArray();

                            dt1Texture.FloorsTexture.SetData(service.GraphicsDevice, data, i, 0, null);
                        }
                    }

                    int wallCount = dt1Texture.File.WallHeaders.Count;
                    if (wallCount > 0)
                    {
                        dt1Texture.WallsTexture = Texture2D.New(service.GraphicsDevice, 160, 960, SharpDX.Toolkit.Graphics.PixelFormat.B8G8R8A8.UNorm, arraySize: wallCount);

                        for (int i = 0; i < wallCount; i++)
                        {
                            var ms = new MemoryStream();
                            dt1Texture.File.GetWallImage(i).Save(ms, ImageFormat.Bmp);
                            var data = ms.ToArray().Skip(54).ToArray();

                            dt1Texture.WallsTexture.SetData(service.GraphicsDevice, data, i, 0, null);
                        }
                    }

                    return dt1Texture;
                //}
                //catch (Exception ex)
                //{
                    
                //    throw;
                //}

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
