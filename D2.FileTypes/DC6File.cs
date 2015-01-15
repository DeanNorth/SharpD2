using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace D2.FileTypes
{
    public struct DC6_Header_S
    {
        public int version;        // 0x00000006
        public int sub_version;    // 0x00000001
        public int zeros;          // 0x00000000
        public int termination;    // 0xEEEEEEEE or 0xCDCDCDCD //BYTE ARRAY!
        public int directions;     // 0x000000xx
        public int frames_per_dir; // 0x000000xx
    }

    public struct DC6_FRAME_HEADER_S
    {
        public int flip;
        public int width;
        public int height;
        public int offset_x;
        public int offset_y; // from bottom border, NOT upper border
        public int zeros;
        public int next_block;
        public int length;
    }

    public class D2Palette
    {
        private Bitmap current { get; set; }
        private int shift_color { get; set; }
        private string file { get; set; }

        private int[] dc6_frame_ptr;
        private DC6_Header_S dc6_header;
        private DC6_FRAME_HEADER_S[] dc6_frame_headers;

        private Color[] palette = new Color[256];
        private Color[] palette_shift = new Color[256];
        private byte[] cmap_grey_brown = null;
        private byte[] colormap = new byte[256];
        private byte[][,] dc6_indexed;

        private byte[] dc6_file;
        private byte[] act_file;
        private byte[] color_file;

        /// <summary>
        /// Initializes D2Palette Class
        /// Send byte arrays from embedded resource.
        /// Shift refers to a color index goes from black -> purple with reds/blues/greens inbetween
        /// </summary>
        /// <param name="byte[] dc6"></param>
        /// <param name="byte[] act"></param>
        /// <param name="byte[] color"></param>
        /// <param name="int shift"></param>      
        public D2Palette(byte[] dc6, byte[] act, byte[] color, int shift)
        {
            dc6_file = dc6;
            act_file = act;
            color_file = color;
            shift_color = shift;
        }

        /// <summary>
        /// Applies the transformation and returns an Image
        /// </summary>         
        public List<Image> Transform()
        {
            LoadHeader();
            IndexDC6();
            LoadPalette();
            //ShiftPalette();
            return ConstructBitmaps();
        }

        void LoadHeader()
        {
            long nb, s;

            int size = Marshal.SizeOf(typeof(DC6_Header_S));
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(dc6_file, 0, buffer, size);
                dc6_header = (DC6_Header_S)Marshal.PtrToStructure(buffer, typeof(DC6_Header_S));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            nb = dc6_header.directions * dc6_header.frames_per_dir;
            dc6_frame_ptr = new int[nb];
            dc6_frame_headers = new DC6_FRAME_HEADER_S[nb];

            s = sizeof(int) * nb;

            size = Marshal.SizeOf(s);
            buffer = Marshal.AllocHGlobal(size);
            try
            {
                //Marshal.Copy(dc6_file, Marshal.SizeOf(typeof(DC6_Header_S)), buffer, (int)s);
                //var dptr = (int)Marshal.PtrToStructure(buffer, typeof(int));


                for (int i = 0; i < nb; i++)
                {
                    var ptr = BitConverter.ToInt32(dc6_file, Marshal.SizeOf(typeof(DC6_Header_S)) + (i*4));
                    dc6_frame_ptr[i] = ptr;
                }
               
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }


            

            for (int i = 0; i < nb; i++)
            {
                size = Marshal.SizeOf(typeof(DC6_FRAME_HEADER_S));
                buffer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.Copy(dc6_file, dc6_frame_ptr[i], buffer, size);
                    var dc6_frame_header = (DC6_FRAME_HEADER_S)Marshal.PtrToStructure(buffer, typeof(DC6_FRAME_HEADER_S));

                    dc6_frame_headers[i] = dc6_frame_header;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            
        }

        void IndexDC6()
        {
            dc6_indexed = new byte[dc6_frame_headers.Length][,];
            for (int index = 0; index < dc6_frame_headers.Length; index++)
            {
                var dc6_frame_header = dc6_frame_headers[index];

                DC6_FRAME_HEADER_S fh;
                long i, i2, pos;
                byte c2;
                int c, x, y;

                fh = dc6_frame_header;

                dc6_indexed[index] = new byte[fh.width, fh.height];

                if ((fh.width <= 0) || (fh.height <= 0))
                {
                    return;
                }

                dc6_indexed[index] = new byte[(int)fh.width, (int)fh.height];

                pos = dc6_frame_ptr[index] + 32;

                x = 0;
                y = (int)fh.height - 1;

                for (i = 0; i < fh.length; i++)
                {
                    c = dc6_file[pos]; pos++;

                    if (c == 0x80)
                    {
                        x = 0;
                        y--;
                    }
                    else if ((c & 0x80) > 0)
                    {
                        x += c & 0x7F;
                    }
                    else
                    {
                        for (i2 = 0; i2 < c; i2++)
                        {
                            c2 = dc6_file[pos]; pos++;
                            i++;
                            dc6_indexed[index][x, y] = c2;
                            x++;
                        }
                    }
                }

            }
        }

        void LoadPalette()
        {
            for (int i = 0; i < 256; i++)
            {
                Color from_index = Color.FromArgb(255, act_file[i * 3 + 2], act_file[i * 3 + 1], act_file[i * 3]);
                palette[i] = from_index;
            }

            palette_shift = palette;
        }

        void ShiftPalette()
        {
            palette_shift = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                colormap[i] = color_file[shift_color * 256 + i];
                palette_shift[i] = palette[colormap[i]];
            }
        }

        List<Image> ConstructBitmaps()
        {
            var bitmaps = new List<Image>();
            for (int index = 0; index < dc6_frame_headers.Length; index++)
            {
                var dc6_frame_header = dc6_frame_headers[index];

                var b = new Bitmap(dc6_frame_header.width, dc6_frame_header.height);

                for (int y = 0; y < dc6_frame_header.height; y++)
                {
                    for (int x = 0; x < dc6_frame_header.width; x++)
                    {
                        b.SetPixel(x, y, palette_shift[dc6_indexed[index][x, y]]);
                    }
                }

                bitmaps.Add(b);
            }

            return bitmaps;
        }
    }
}