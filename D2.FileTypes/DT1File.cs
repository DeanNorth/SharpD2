using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace D2.FileTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DT1_TILE_HEADER
    {
        public int 	 Direction;	//	 'General' orientation
        public Int16 	 RoofHeight;	//	 In pixels
        public byte 	 SoundIndex;	//	
        public byte 	 Animated;	//	 Flag
        public int 	 Height; 	//	 in pixels, always power of 32,	 always a negative number 
        public int 	 Width; 	//	 in pixels, always power of 32 
        public int 	 Zeros0;	//	 Unused
        public int 	 Orientation;	//	 The 3 indexes that identify a Tile 
        public int 	 MainIndex;	//	
        public int 	 SubIndex;	//	
        public int 	 RarityFrameIndex;	//	 Only Frame index in an Animated Floor Tile 
        public byte 	 Unknown1;	//	 Seems to always be the same for
        public byte 	 Unknown2;	//	 all the Tiles of the DT1 
        public byte 	 Unknown3;	//	
        public byte 	 Unknown4;	//	

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        public byte[] SubTilesFlags;	//25	 Left to Right, and Bottom to Up

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] Zeros1;	//	 Unused
        public int 	 BlockHeadersPointer; 	//	 Pointer in file to Block Headers for this Tile 
        public int 	 BlockDatasLength;	//	 Block Headers + Block Datas of this Tile 
        public int 	 BlockCount;	//	
        public int 	 Zeros2;	//	 Unused
        public int 	 Zeros3;	//	 Unused
        public int 	 Zeros4;	//	 Unused
    }

    public struct DT1_TILE_HEADER_WITH_INDEX
    {
        public DT1_TILE_HEADER tile;
        public int TileIndex;
        public int FloorWallIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DT1_BLOCK_HEADER
    {
        public Int16 X;
        public Int16 Y;
        public Int16 Zeros0;
        public byte GridX;
        public byte GridY;
        public Int16 Format;
        public int Length;
        public Int16 Zeros1;
        public int Offset;
    }

    public class DT1File
    {
        private byte[] data;
        private byte[] paletteData;
        private Color[] palette = new Color[256];

        public int Version1 { get; set; }
        public int Version2 { get; set; }

        public int TileCount { get; set; }

        public DT1_TILE_HEADER[] TileHeaders;

        public List<DT1_TILE_HEADER_WITH_INDEX> FloorHeaders = new List<DT1_TILE_HEADER_WITH_INDEX>();
        public List<DT1_TILE_HEADER_WITH_INDEX> WallHeaders = new List<DT1_TILE_HEADER_WITH_INDEX>();

        Dictionary<int, DT1_BLOCK_HEADER[]> BlockHeaders = new Dictionary<int,DT1_BLOCK_HEADER[]>();

        public DT1File(Stream stream, byte[] paletteData) : this(StreamHelper.ReadToEnd(stream), paletteData)
        {
        }

        public DT1File(byte[] data, byte[] paletteData)
        {
            this.data = data;
            this.paletteData = paletteData;

            Version1 = BitConverter.ToInt32(data, 0);
            Version2 = BitConverter.ToInt32(data, 4);
            TileCount = BitConverter.ToInt32(data, 268);

            TileHeaders = new DT1_TILE_HEADER[TileCount];
            
            int firstTileHeader = BitConverter.ToInt32(data, 272);

            int offset = firstTileHeader;
            int size = Marshal.SizeOf(typeof(DT1_TILE_HEADER));
            for (int i = 0; i < TileCount; i++)
            {
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.Copy(data, offset, buffer, size);
                    var tileHeader = (DT1_TILE_HEADER)Marshal.PtrToStructure(buffer, typeof(DT1_TILE_HEADER));
                    TileHeaders[i] = tileHeader;

                    if (tileHeader.Orientation == 0)
                    {
                        FloorHeaders.Add(new DT1_TILE_HEADER_WITH_INDEX() { tile = tileHeader, TileIndex = i, FloorWallIndex = FloorHeaders.Count });
                    }
                    else
                    {
                        WallHeaders.Add(new DT1_TILE_HEADER_WITH_INDEX() { tile = tileHeader, TileIndex = i, FloorWallIndex = WallHeaders.Count });
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                offset += size;
            }

            size = Marshal.SizeOf(typeof(DT1_BLOCK_HEADER));

            for (int i = 0; i < TileCount; i++)
			{
                var tileHeader = TileHeaders[i];

                var blockHeaders = new List<DT1_BLOCK_HEADER>();
                offset = tileHeader.BlockHeadersPointer;

                for (int b = 0; b < tileHeader.BlockCount; b++)
                {
                    IntPtr buffer = Marshal.AllocHGlobal(size);
                    try
                    {
                        Marshal.Copy(data, offset, buffer, size);
                        var blockHeader = (DT1_BLOCK_HEADER)Marshal.PtrToStructure(buffer, typeof(DT1_BLOCK_HEADER));
                        blockHeaders.Add(blockHeader);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }

                    offset += size;
                }

                BlockHeaders[i] = blockHeaders.ToArray();
            }
        }

        void LoadPalette()
        {
            for (int i = 0; i < 256; i++)
            {
                Color from_index = Color.FromArgb(255, paletteData[i * 3 + 2], paletteData[i * 3 + 1], paletteData[i * 3]);
                palette[i] = from_index;
            }
        }

        public Image GetFloorImage(int tileIndex)
        {
            LoadPalette();

            Bitmap result;

            var tileWithIndex = FloorHeaders[tileIndex];
            var tile = tileWithIndex.tile;

            if (tile.Width != 0 && tile.Height != 0)
            {
                result = new Bitmap(Math.Abs(tile.Width), Math.Abs(tile.Height));
                foreach (var blockHeader in BlockHeaders[tileWithIndex.TileIndex])
                {
                    if (blockHeader.Format == 1) // ISO
                    {
                        DrawBlockIsometric(result, blockHeader.X, blockHeader.Y, data, tile.BlockHeadersPointer + blockHeader.Offset, blockHeader.Length);
                    }
                    else // RLE
                    {
                        if (tile.Orientation == 0)
                        {
                            DrawBlockNormal(result, blockHeader.X, Math.Abs(blockHeader.Y), data, tile.BlockHeadersPointer + blockHeader.Offset, blockHeader.Length);
                        }
                        else
                        {
                            DrawBlockNormal(result, blockHeader.X, Math.Abs(tile.Height) - Math.Abs(blockHeader.Y), data, tile.BlockHeadersPointer + blockHeader.Offset, blockHeader.Length);
                        }
                    }
                }
            }
            else
            {
                result = new Bitmap(128, 128);
            }
            

            return RotateImage(StretchImage(result));
        }

        public Image GetWallImage(int tileIndex)
        {
            LoadPalette();

            Bitmap result;

            var tileWithIndex = WallHeaders[tileIndex];
            var tile = tileWithIndex.tile;

            if (tile.Width != 0 && tile.Height != 0)
            {
                result = new Bitmap(160, 960);
                foreach (var blockHeader in BlockHeaders[tileWithIndex.TileIndex])
                {
                    if (blockHeader.Format == 1) // ISO
                    {
                        DrawBlockIsometric(result, blockHeader.X, blockHeader.Y, data, tile.BlockHeadersPointer + blockHeader.Offset, blockHeader.Length);
                    }
                    else // RLE
                    {
                        if (tile.Orientation == 0)
                        {
                            DrawBlockNormal(result, blockHeader.X, Math.Abs(blockHeader.Y), data, tile.BlockHeadersPointer + blockHeader.Offset, blockHeader.Length);
                        }
                        else
                        {
                            DrawBlockNormal(result, blockHeader.X, 960 - Math.Abs(blockHeader.Y), data, tile.BlockHeadersPointer + blockHeader.Offset, blockHeader.Length);
                        }
                    }
                }
            }
            else
            {
                result = new Bitmap(160, 960);
            }

            return result;
        }

        public static Image StretchImage(Image img)
        {
            //create an empty Bitmap image
            Bitmap bmp = new Bitmap(img.Width, img.Width);

            //turn the Bitmap into a Graphics object
            Graphics gfx = Graphics.FromImage(bmp);

            gfx.ScaleTransform(1, 2);



            //set the InterpolationMode to HighQualityBicubic so to ensure a high
            //quality image once it is transformed to the specified size
            gfx.InterpolationMode = InterpolationMode.NearestNeighbor; //HighQualityBicubic;

            //now draw our new image onto the graphics object
            gfx.DrawImage(img, new Point(0, 0));

            //dispose of our Graphics object
            gfx.Dispose();

            //return the image
            return bmp;
        }

        public static Image RotateImage(Image img)
        {
            //create an empty Bitmap image
            Bitmap bmp = new Bitmap(128, 128);

            //turn the Bitmap into a Graphics object
            Graphics gfx = Graphics.FromImage(bmp);

            //now we set the rotation point to the center of our image
            gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);

            //now rotate the image
            gfx.RotateTransform(45);

            gfx.ScaleTransform(1.2f, 1.2f);

            gfx.TranslateTransform(-(float)img.Width / 2, -(float)img.Height / 2);

            //set the InterpolationMode to HighQualityBicubic so to ensure a high
            //quality image once it is transformed to the specified size
            gfx.InterpolationMode = InterpolationMode.NearestNeighbor; //HighQualityBicubic;

            //now draw our new image onto the graphics object
            gfx.DrawImage(img, new Point(0, 0));

            //dispose of our Graphics object
            gfx.Dispose();

            bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);

            //return the image
            return bmp;
        }

        public void DrawBlockIsometric(Bitmap dst, int x0, int y0, byte[] data, int offset, int length)
        {
            int x, y = 0;
            int n;
            int[] xjump =  new int[] {14, 12, 10, 8, 6, 4, 2, 0, 2, 4, 6, 8, 10, 12, 14};
            int[] nbpix = new int[] {4, 8, 12, 16, 20, 24, 28, 32, 28, 24, 20, 16, 12, 8, 4}; 

            // 3d-isometric subtile is 256 bytes, no more, no less 
            if (length != 256) 
                return;

            // draw 
            while (length > 0) 
            { 
                x = xjump[y]; 
                n = nbpix[y]; 
                length -= n; 
                while (n > 0) 
                {
                    dst.SetPixel(x0 + x, y0 + y, palette[data[offset]]);
                    offset++;
                    x++; 
                    n--; 
                } 
                y++; 
            } 
        }

        public void DrawBlockNormal(Bitmap dst, int x0, int y0, byte[] data, int offset, int length)
        {
            int x = 0;
            int y = 0;
            byte b1, b2;
            // draw 
            while (length > 0)
            {
                b1 = data[offset++];
                b2 = data[offset++];
                
                length -= 2;
                if (b1 > 0 || b2 > 0)
                {
                    x += b1;
                    length -= b2;
                    while (b2 > 0)
                    {
                        if (y0 + y < dst.Height) // Not sure why this is required
                        {
                            dst.SetPixel(x0 + x, y0 + y, palette[data[offset]]);
                        }
                        offset++;
                        x++;
                        b2--;
                    }
                }
                else
                {
                    x = 0;
                    y++;
                }
            } 
        }

    }
}
