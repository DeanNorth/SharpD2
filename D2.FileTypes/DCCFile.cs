using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace D2.FileTypes
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct DCC_Header_S
    {
        public byte signature;      //0x74 
        public byte version;        //0x06
        public byte directions;
        public int frames_per_dir;
        public int one;
        public int totalSizeCoded;
    }

    public struct DCC_PB_ENTRY_S // PixelBuffer entry
    {
       //public byte B;
       //public byte G;
       //public byte R;
       //public byte A;
        public byte[] val;
        public int frame;
        public int frame_cell_index;
    }

    public struct DCC_CELL_S
    {
        public int x0, y0;                   // for frame cells in stage 2
        public int w, h;

        public int last_w, last_h;           // width & size of the last frame cell that used
                                            // this buffer cell (for stage 2)
        public int last_x0, last_y0;

        public Bitmap bmp;                   // sub-bitmap in the buffer bitmap
    }                                       // maybe I'll make 2 kind of cells, 1 for frame-buffer cells,
                                            // the other for the frame cells...


    public struct DCC_BOX_S
    {
        public int xmin;
        public int ymin;
        public int xmax;
        public int ymax;
        public int width;
        public int height;
    }

    public struct DCC_DIRECTION_HEADER_S
    {
        public int OutSizeCoded;
        public byte CompressionFlags;

        public uint Variable0Bits;
        public uint WidthBits;
        public uint HeightBits;
        public uint XOffsetBits;
        public uint YOffsetBits;
        public uint OptionalDataBits;
        public uint CodedBytesBits;

        public DCC_FRAME_HEADER_S[] frame_headers;

        public BitArray PixelValuesKey;

        public uint EqualCellsBitstreamSize;
        public uint PixelMaskBitstreamSize;
        public uint EncodingTypeBitsreamSize;
        public uint RawPixelCodesBitstreamSize;

        public byte[] EqualCellsBitstream;
        public byte[] PixelMaskBitstream;
        public byte[] EncodingTypeBitsream;
        public byte[] RawPixelCodesBitstream;
        public byte[] PixelCodesAndDisplacementBitstream;

        public int pco;

        public byte[] PixelValues;

        public DCC_BOX_S box;

        public DCC_CELL_S[] cells;
        public int nb_cells_w;
        public int nb_cells_h;

        public DCC_PB_ENTRY_S[] pixel_buffer;
        public Bitmap bmp;
    }

    public struct DCC_FRAME_HEADER_S
    {
        public uint Variable0;
        public uint width;
        public uint height;
        public int offset_x;
        public int offset_y;
        public uint optionalData;
        public uint codedBytes;

        public bool FrameBottomUp;

        public DCC_BOX_S box;

        public DCC_CELL_S[] cells;
        public int nb_cells_w;
        public int nb_cells_h;
    }

    public class DCCFile
    {
        const int DCC_MAX_DIR    = 32;
        const int DCC_MAX_FRAME  = 256;
        const int DCC_MAX_PB_ENTRY = 85000;

        private Bitmap current { get; set; }
        private int shift_color { get; set; }
        private string file { get; set; }

        private int[] dcc_direction_ptr;
        private DCC_Header_S dcc_header;
        public DCC_DIRECTION_HEADER_S[] dcc_direction_headers;

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
        public DCCFile(byte[] dc6, byte[] act, byte[] color, int shift)
        {
            dc6_file = dc6;
            act_file = act;
            color_file = color;
            shift_color = shift;

            LoadHeader();
            LoadPalette();
        }

        /// <summary>
        /// Applies the transformation and returns an Image
        /// </summary>         
        public List<Image> Transform(int directionIndex)
        {
            
            //IndexDC6();
            
            //ShiftPalette();
            return ConstructBitmaps(directionIndex);
        }

        void LoadHeader()
        {
            long nb, s;

            int size = Marshal.SizeOf(typeof(DCC_Header_S));
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(dc6_file, 0, buffer, size);
                dcc_header = (DCC_Header_S)Marshal.PtrToStructure(buffer, typeof(DCC_Header_S));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            //nb = dcc_header.directions; // *dcc_header.frames_per_dir;
            dcc_direction_ptr = new int[dcc_header.directions];
            dcc_direction_headers = new DCC_DIRECTION_HEADER_S[dcc_header.directions];

            s = sizeof(int) * dcc_header.directions;

            size = Marshal.SizeOf(s);
            buffer = Marshal.AllocHGlobal(size);
            try
            {
                //Marshal.Copy(dc6_file, Marshal.SizeOf(typeof(DC6_Header_S)), buffer, (int)s);
                //var dptr = (int)Marshal.PtrToStructure(buffer, typeof(int));


                for (int i = 0; i < dcc_header.directions; i++)
                {
                    var ptr = BitConverter.ToInt32(dc6_file, Marshal.SizeOf(typeof(DCC_Header_S)) + (i*4));
                    dcc_direction_ptr[i] = ptr;
                }
               
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }


            int bitOffset = (int)((Marshal.SizeOf(typeof(DCC_Header_S)) + ((dcc_header.directions) * 4)) * 8);
            BitArray bits = new BitArray(dc6_file);

            uint[] widthTable = { 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 32 };

            for (int i = 0; i < dcc_header.directions; i++)
            {
                int offsetStart = bitOffset;
                int directionBufferSize;
                if (i == dcc_header.directions - 1)
                {
                    directionBufferSize = 8 * (dc6_file.Length - dcc_direction_ptr[i]);
                }
                else
                {
                    directionBufferSize = 8 * (dcc_direction_ptr[i + 1] - dcc_direction_ptr[i]);
                }

                var directionHeader = new DCC_DIRECTION_HEADER_S();
                directionHeader.PixelValues = new byte[256];
                directionHeader.frame_headers = new DCC_FRAME_HEADER_S[dcc_header.frames_per_dir];

                directionHeader.OutSizeCoded = bits.ToInt32(ref bitOffset);

                directionHeader.CompressionFlags = bits.ToByte(ref bitOffset, 2);

                directionHeader.Variable0Bits = bits.ToUInt32(ref bitOffset, 4);
                directionHeader.WidthBits = bits.ToUInt32(ref bitOffset, 4);
                directionHeader.HeightBits = bits.ToUInt32(ref bitOffset, 4);
                directionHeader.XOffsetBits = bits.ToUInt32(ref bitOffset, 4);
                directionHeader.YOffsetBits = bits.ToUInt32(ref bitOffset, 4);
                directionHeader.OptionalDataBits = bits.ToUInt32(ref bitOffset, 4);
                directionHeader.CodedBytesBits = bits.ToUInt32(ref bitOffset, 4);

                // Calculate box sizes
                // init direction box min & max (NOT ZERO !)
                directionHeader.box.xmin = directionHeader.box.ymin = int.MaxValue;
                directionHeader.box.xmax = directionHeader.box.ymax = int.MinValue;


                for (int j = 0; j < dcc_header.frames_per_dir; j++)
                {
                    var frameHeader = new DCC_FRAME_HEADER_S();
                    frameHeader.Variable0 = bits.ToUInt32(ref bitOffset, widthTable[directionHeader.Variable0Bits]);
                    frameHeader.width = bits.ToUInt32(ref bitOffset, widthTable[directionHeader.WidthBits]);
                    frameHeader.height = bits.ToUInt32(ref bitOffset, widthTable[directionHeader.HeightBits]);
                    frameHeader.offset_x = bits.ToInt32(ref bitOffset, widthTable[directionHeader.XOffsetBits]);
                    frameHeader.offset_y = bits.ToInt32(ref bitOffset, widthTable[directionHeader.YOffsetBits]);
                    frameHeader.optionalData = bits.ToUInt32(ref bitOffset, widthTable[directionHeader.OptionalDataBits]);
                    frameHeader.codedBytes = bits.ToUInt32(ref bitOffset, widthTable[directionHeader.CodedBytesBits]);

                    frameHeader.FrameBottomUp = bits.ToBool(ref bitOffset);

                    // frame box
                    frameHeader.box.xmin = frameHeader.offset_x;
                    frameHeader.box.xmax = (int)(frameHeader.box.xmin + frameHeader.width - 1);

                    if (frameHeader.FrameBottomUp) // bottom-up
                    {
                        frameHeader.box.ymin = frameHeader.offset_y;
                        frameHeader.box.ymax = (int)(frameHeader.box.ymin + frameHeader.height - 1);
                    }
                    else // top-down
                    {
                        frameHeader.box.ymax = frameHeader.offset_y;
                        frameHeader.box.ymin = (int)(frameHeader.box.ymax - frameHeader.height + 1);
                    }
                    frameHeader.box.width = frameHeader.box.xmax - frameHeader.box.xmin + 1;
                    frameHeader.box.height = frameHeader.box.ymax - frameHeader.box.ymin + 1;


                    // direction box
                    if (frameHeader.box.xmin < directionHeader.box.xmin)
                        directionHeader.box.xmin = frameHeader.box.xmin;

                    if (frameHeader.box.ymin < directionHeader.box.ymin)
                        directionHeader.box.ymin = frameHeader.box.ymin;

                    if (frameHeader.box.xmax > directionHeader.box.xmax)
                        directionHeader.box.xmax = frameHeader.box.xmax;

                    if (frameHeader.box.ymax > directionHeader.box.ymax)
                        directionHeader.box.ymax = frameHeader.box.ymax;

                    directionHeader.frame_headers[j] = frameHeader;
                }


                directionHeader.box.width = directionHeader.box.xmax - directionHeader.box.xmin + 1;
                directionHeader.box.height = directionHeader.box.ymax - directionHeader.box.ymin + 1;



                //TODO OptionalData
                if (directionHeader.OptionalDataBits > 0)
                {
                    throw new NotImplementedException();
                }

                if ((directionHeader.CompressionFlags & 0x02) == 0x02)
                {
                    directionHeader.EqualCellsBitstreamSize = bits.ToUInt32(ref bitOffset, 20);
                }

                directionHeader.PixelMaskBitstreamSize = bits.ToUInt32(ref bitOffset, 20);

                if ((directionHeader.CompressionFlags & 0x01) == 0x01)
                {
                    directionHeader.EncodingTypeBitsreamSize = bits.ToUInt32(ref bitOffset, 20);
                    directionHeader.RawPixelCodesBitstreamSize = bits.ToUInt32(ref bitOffset, 20);
                }

                directionHeader.PixelValuesKey = bits.ToBitArray(ref bitOffset, 256);

                int pi = 0;
                for (int p = 0; p < 256; p++)
                {
                    if (directionHeader.PixelValuesKey[p])
                    {
                        directionHeader.PixelValues[pi++] = (byte)p;
                    }
                }

                directionHeader.EqualCellsBitstream = bits.ToBytes(ref bitOffset, directionHeader.EqualCellsBitstreamSize);
                directionHeader.PixelMaskBitstream = bits.ToBytes(ref bitOffset, directionHeader.PixelMaskBitstreamSize);
                directionHeader.EncodingTypeBitsream = bits.ToBytes(ref bitOffset, directionHeader.EncodingTypeBitsreamSize);
                directionHeader.RawPixelCodesBitstream = bits.ToBytes(ref bitOffset, directionHeader.RawPixelCodesBitstreamSize);


                directionHeader.PixelCodesAndDisplacementBitstream = bits.ToBytes(ref bitOffset, (uint)(directionBufferSize - (bitOffset - offsetStart)));

                dcc_direction_headers[i] = directionHeader;
     
                //size = Marshal.SizeOf(typeof(DC6_FRAME_HEADER_S));
                //buffer = Marshal.AllocHGlobal(size);
                //try
                //{
                //    Marshal.Copy(dc6_file, dcc_frame_ptr[i], buffer, size);
                //    var dc6_frame_header = (DC6_FRAME_HEADER_S)Marshal.PtrToStructure(buffer, typeof(DC6_FRAME_HEADER_S));

                //    dcc_frame_headers[i] = dc6_frame_header;
                //}
                //finally
                //{
                //    Marshal.FreeHGlobal(buffer);
                //}
            }
            
        }

        void IndexDC6()
        {
            //dc6_indexed = new byte[dc6_frame_headers.Length][,];
            //for (int index = 0; index < dc6_frame_headers.Length; index++)
            //{
            //    var dc6_frame_header = dc6_frame_headers[index];

            //    DC6_FRAME_HEADER_S fh;
            //    long i, i2, pos;
            //    byte c2;
            //    int c, x, y;

            //    fh = dc6_frame_header;

            //    dc6_indexed[index] = new byte[fh.width, fh.height];

            //    if ((fh.width <= 0) || (fh.height <= 0))
            //    {
            //        return;
            //    }

            //    dc6_indexed[index] = new byte[(int)fh.width, (int)fh.height];

            //    pos = dc6_frame_ptr[index] + 32;

            //    x = 0;
            //    y = (int)fh.height - 1;

            //    for (i = 0; i < fh.length; i++)
            //    {
            //        c = dc6_file[pos]; pos++;

            //        if (c == 0x80)
            //        {
            //            x = 0;
            //            y--;
            //        }
            //        else if ((c & 0x80) > 0)
            //        {
            //            x += c & 0x7F;
            //        }
            //        else
            //        {
            //            for (i2 = 0; i2 < c; i2++)
            //            {
            //                c2 = dc6_file[pos]; pos++;
            //                i++;
            //                dc6_indexed[index][x, y] = c2;
            //                x++;
            //            }
            //        }
            //    }

            //}
        }



        int dcc_prepare_buffer_cells(int directionIndex)
        {
            int buffer_w, buffer_h, tmp, nb_cell_w, nb_cell_h, nb_cell, i, x0, y0, x, y;
            int[] cell_w, cell_h;

            buffer_w = dcc_direction_headers[directionIndex].box.width;
            buffer_h = dcc_direction_headers[directionIndex].box.height;


            tmp = buffer_w - 1;
            nb_cell_w = 1 + (tmp / 4);
      
            tmp = buffer_h - 1;
            nb_cell_h = 1 + (tmp / 4);

            nb_cell = nb_cell_w * nb_cell_h;

            dcc_direction_headers[directionIndex].cells = new DCC_CELL_S[nb_cell];
            cell_w = new int[nb_cell_w];
            cell_h = new int[nb_cell_h];


            if (nb_cell_w == 1)
            {
                cell_w[0] = buffer_w;
            }
            else
            {
                for (i = 0; i < (nb_cell_w - 1); i++)
                {
                    cell_w[i] = 4;
                }

                cell_w[nb_cell_w - 1] = buffer_w - (4 * (nb_cell_w - 1));
            }

            if (nb_cell_h == 1)
            {
                cell_h[0] = buffer_h;
            }
            else
            {
                for (i = 0; i < (nb_cell_h - 1); i++)
                {
                    cell_h[i] = 4;
                }

                cell_h[nb_cell_h - 1] = buffer_h - (4 * (nb_cell_h - 1));
            }

            dcc_direction_headers[directionIndex].nb_cells_w = nb_cell_w;
            dcc_direction_headers[directionIndex].nb_cells_h = nb_cell_h;

            y0 = 0;
            for (y = 0; y < nb_cell_h; y++)
            {
                x0 = 0;
                for (x = 0; x < nb_cell_w; x++)
                {
                    var cell = new DCC_CELL_S();
                    cell.w = cell_w[x];
                    cell.h = cell_h[y];
                    cell.bmp = new Bitmap(cell.w, cell.h); //x0, y0, 

                    using (var g = Graphics.FromImage(cell.bmp))
                    {
                        g.Clear(Color.Blue);
                    }

                    dcc_direction_headers[directionIndex].cells[x + (y * nb_cell_w)] = cell;

                    x0 += 4;
                }
                y0 += 4;
            }

            return 0;
        }

        // ==========================================================================
        // make the cells of 1 frame, called during stage 1, but used mainly
        // for stage 2
        // return 0 on success, non-zero if error
        int dcc_prepare_frame_cells(int d, int f)
        {
            int             frame_w, frame_h, w, h, tmp,
                            nb_cell_w, nb_cell_h, nb_cell,
                            i, x0, y0, x, y;
            int[] cell_w, cell_h;

            frame_w = dcc_direction_headers[d].frame_headers[f].box.width;
            frame_h = dcc_direction_headers[d].frame_headers[f].box.height;

            // width (in # of pixels) in 1st column
            w = 4 - ((dcc_direction_headers[d].frame_headers[f].box.xmin - dcc_direction_headers[d].box.xmin) % 4);
   
            if ((frame_w - w) <= 1) // if 2nd column is 0 or 1 pixel width
                nb_cell_w = 1;
            else
            {
                // so, we have minimum 2 pixels behind 1st column
                tmp = frame_w - w - 1; // tmp is minimum 1, can't be 0
                nb_cell_w = 2 + (tmp / 4);
                if ((tmp % 4) == 0)
                    nb_cell_w--;
            }

            h = 4 - ((dcc_direction_headers[d].frame_headers[f].box.ymin - dcc_direction_headers[d].box.ymin) % 4);
            

            if ((frame_h - h) <= 1)
                nb_cell_h = 1;
            else
            {
                tmp = frame_h - h - 1;
                nb_cell_h = 2 + (tmp / 4);
                if ((tmp % 4) == 0)
                    nb_cell_h--;
            }

            nb_cell = nb_cell_w * nb_cell_h;

            dcc_direction_headers[d].frame_headers[f].cells = new DCC_CELL_S[nb_cell];
            cell_w = new int[nb_cell_w];
            cell_h = new int[nb_cell_h];
            

            if (nb_cell_w == 1)
                cell_w[0] = frame_w;
            else
            {
                cell_w[0] = w;
                for (i=1; i < (nb_cell_w - 1); i++)
                    cell_w[i] = 4;
                cell_w[nb_cell_w - 1] = frame_w - w - (4 * (nb_cell_w - 2));
            }

            if (nb_cell_h == 1)
                cell_h[0] = frame_h;
            else
            {
                cell_h[0] = h;
                for (i=1; i < (nb_cell_h - 1); i++)
                    cell_h[i] = 4;
                cell_h[nb_cell_h - 1] = frame_h - h - (4 * (nb_cell_h - 2));
            }

            dcc_direction_headers[d].frame_headers[f].nb_cells_w = nb_cell_w;
            dcc_direction_headers[d].frame_headers[f].nb_cells_h = nb_cell_h;

            y0 = dcc_direction_headers[d].frame_headers[f].box.ymin - dcc_direction_headers[d].box.ymin;
            DCC_CELL_S cell = new DCC_CELL_S();
            for (y=0; y < nb_cell_h; y++)
            {
                x0 = dcc_direction_headers[d].frame_headers[f].box.xmin - dcc_direction_headers[d].box.xmin;
               
                for (x=0; x < nb_cell_w; x++)
                {
                    cell = new DCC_CELL_S();
                    cell.x0  = x0;
                    cell.y0  = y0;
                    cell.w   = cell_w[x];
                    cell.h   = cell_h[y];
                    cell.bmp = new Bitmap(cell.w, cell.h); //x0, y0, 

                    using (var g = Graphics.FromImage(cell.bmp))
                    {
                        g.Clear(Color.Yellow);
                    }

                    dcc_direction_headers[d].frame_headers[f].cells[x + (y * nb_cell_w)] = cell;

                    x0 += cell.w;
                }
                y0 += cell.h;
            }

            return 0;
        }

        // ==========================================================================
        // stage 1 of the decompression of the direction frames
        // return 0 on success, non-zero if error
        int dcc_fill_pixel_buffer(int d)
        {
            Nullable<DCC_PB_ENTRY_S> old_entry;
            DCC_PB_ENTRY_S new_entry;
            int            cell0_x, cell0_y, cell_w, cell_h;
            uint          tmp, pixel_mask=0, encoding_type, last_pixel,pix_displ;
            uint[] read_pixel = new uint[4];

            int             nb_cell, size;
            int             i, curr_cell_x, curr_cell_y, curr_cell;
            int[]             nb_pix_table = new int[16] {0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4};
            int             x, y, nb_pix, next_cell, curr_idx, pb_idx = -1; // current entry of pixel_buffer
            Nullable<DCC_PB_ENTRY_S>[] cell_buffer;
            int             buff_w, buff_h, decoded_pix;
            //char[]            add_error[256];


            // create & init pixel buffer
            dcc_direction_headers[d].pixel_buffer = new DCC_PB_ENTRY_S[DCC_MAX_PB_ENTRY];
            for (i=0; i < DCC_MAX_PB_ENTRY; i++)
            {
                dcc_direction_headers[d].pixel_buffer[i].frame            = -1;
                dcc_direction_headers[d].pixel_buffer[i].frame_cell_index = -1;
            }

            // frame buffer, will be simply called as "buffer" for now
            dcc_direction_headers[d].bmp = new Bitmap(dcc_direction_headers[d].box.width, dcc_direction_headers[d].box.height);

            using (var g = Graphics.FromImage(dcc_direction_headers[d].bmp))
            {
                g.Clear(Color.Magenta);
            }

            // create sub-bitmaps into this dir->bmp, 1 for each cells
            if (dcc_prepare_buffer_cells(d) > 0)
            {
                return 1;
            }

            // create & init cell_buffer (for stage 1 only)
            // this buffer is a table of pointer to the current pixelbuffer entry
            // of 1 buffer cell
            buff_w  = dcc_direction_headers[d].nb_cells_w;
            buff_h  = dcc_direction_headers[d].nb_cells_h;
            nb_cell = buff_w * buff_h;

            cell_buffer = new Nullable<DCC_PB_ENTRY_S>[nb_cell];

            BitArray equalCellsBits = new BitArray(dcc_direction_headers[d].EqualCellsBitstream);
            int equalCellsBitsOffset = 0;

            BitArray pixelMaskBits = new BitArray(dcc_direction_headers[d].PixelMaskBitstream);
            int pixelMaskBitsOffset = 0;

            BitArray encodingTypeBits = new BitArray(dcc_direction_headers[d].EncodingTypeBitsream);
            int encodingTypeBitsOffset = 0;

            BitArray rawPixelCodesBits = new BitArray(dcc_direction_headers[d].RawPixelCodesBitstream);
            int rawPixelCodesBitsOffset = 0;

            BitArray pixelCodesAndDisplacementBits = new BitArray(dcc_direction_headers[d].PixelCodesAndDisplacementBitstream);
            int pixelCodesAndDisplacementBitsOffset = 0;

            // for all frames of this direction
            for (int f=0; f < dcc_header.frames_per_dir; f++)
            {
                // make cells of this frame
                if (dcc_prepare_frame_cells(d, f) > 0)
                {
                    return 1;
                }

                cell_w  = dcc_direction_headers[d].frame_headers[f].nb_cells_w;
                cell_h  = dcc_direction_headers[d].frame_headers[f].nb_cells_h;
                var fbox = dcc_direction_headers[d].frame_headers[f].box;
                var dbox = dcc_direction_headers[d].box;
                cell0_x = (fbox.xmin - dbox.xmin) / 4;
                cell0_y = (fbox.ymin - dbox.ymin) / 4;
      
                // for each cells of this frame
                for (y=0; y < cell_h; y++)
                {
                    curr_cell_y = cell0_y + y;
                    for (x=0; x < cell_w; x++)
                    {
                        curr_cell_x = cell0_x + x;
                        curr_cell   = curr_cell_x + (curr_cell_y * buff_w);
                        if (curr_cell >= nb_cell)
                        {
                            //sprintf(dcc_error, "dcc_fill_pixel_buffer() : "
                            //    "can't check the cell %i in cell_buffer,\n"
                            //    "run-time max cell is %i\n"
                            //    "dir %i, frm %i, x=%i, y=%i\n",
                            //    curr_cell, nb_cell,
                            //    d, f, x, y);

                            return 1;
                        }

                        // check if this cell need a new entry in pixel_buffer
                        next_cell = 0;
                        if (cell_buffer[curr_cell] != null)
                        {
                            if (dcc_direction_headers[d].EqualCellsBitstreamSize > 0)
                            {
                                tmp = equalCellsBits.ToUInt32(ref equalCellsBitsOffset, 1);
                                //if (dcc_read_bits( & dir->equal_cell_bitstream,
                                //                    1, FALSE, & tmp))
                                //{
                                //    //sprintf(add_error, "dcc_fill_pixel_buffer() : "
                                //    //"equal_cell bitstream\ndirection %i, frame %i, "
                                //    //"curr_cell %i\n",
                                //    //    d, f, curr_cell);
                                //    //strcat(dcc_error, add_error);
                                //    return 1;
                                //}
                            }
                            else
                            {
                                tmp = 0;
                            }

                            if (tmp == 0)
                            {
                                pixel_mask = pixelMaskBits.ToUInt32(ref pixelMaskBitsOffset, 4);

                                //if (dcc_read_bits( & dir->pixel_mask_bitstream,
                                //                    4, FALSE, & pixel_mask))
                                //{
                                //    sprintf(add_error, "dcc_fill_pixel_buffer() : "
                                //    "pixel_mask bitstream\n"
                                //    "direction %i, frame %i, curr_cell %i\n"
                                //    "x=%i, y=%i\n",
                                //        d, f, curr_cell, x, y);
                                //    strcat(dcc_error, add_error);
                                //    free(cell_buffer);
                                //    return 1;
                                //}

                            }
                            else
                            {
                                next_cell = 1;
                            }
                        }
                        else
                        {
                            pixel_mask = 0x0F;
                        }

                        if (next_cell == 0)
                        {
                            // decode the pixels

                            // read_pixel[] is a stack, where we "push" the pixel code
                            read_pixel[0] = read_pixel[1] = 0;
                            read_pixel[2] = read_pixel[3] = 0;
                            last_pixel    = 0;
                            nb_pix        = nb_pix_table[pixel_mask];
                            if (nb_pix > 0 && dcc_direction_headers[d].EncodingTypeBitsreamSize > 0)
                            {
                                encoding_type = encodingTypeBits.ToUInt32(ref encodingTypeBitsOffset, 1);

                                //if (dcc_read_bits( & dir->encoding_type_bitstream,
                                //                    1, FALSE, & encoding_type))
                                //{
                                //    sprintf(add_error, "dcc_fill_pixel_buffer() :\n   "
                                //    "encoding_type bitstream, direction %i, frame %i, "
                                //    "curr_cell %i\n"
                                //    "   nb_pix = %i, curr_cell_x y = %i %i\n",
                                //        d, f, curr_cell, nb_pix, curr_cell_x, curr_cell_y);
                                //    strcat(dcc_error, add_error);
                                //    free(cell_buffer);
                                //    return 1;
                                //}
                            }
                            else
                            {
                                encoding_type = 0;
                            }

                            decoded_pix = 0;
                            for (i=0; i < nb_pix; i++)
                            {
                                if (encoding_type == 1)
                                {
                                    read_pixel[i] = rawPixelCodesBits.ToUInt32(ref rawPixelCodesBitsOffset, 8);

                                    //if (dcc_read_bits( & dir->raw_pixel_bitstream,
                                    //                8, FALSE, & read_pixel[i]))
                                    //{
                                    //sprintf(add_error, "dcc_fill_pixel_buffer() :\n   "
                                    //    "raw_pixel bitstream, direction %i, frame %i, "
                                    //    "curr_cell %i\n"
                                    //    "   nb_pix = %i, curr_cell_x y = %i %i\n",
                                    //    d, f, curr_cell, nb_pix, curr_cell_x, curr_cell_y);
                                    //strcat(dcc_error, add_error);
                                    //free(cell_buffer);
                                    //return 1;
                                    //}
                                }
                                else
                                {
                                    read_pixel[i] = last_pixel;

                                    pix_displ = pixelCodesAndDisplacementBits.ToUInt32(ref pixelCodesAndDisplacementBitsOffset, 4);

                                    //if (dcc_read_bits(
                                    //& dir->pixel_code_and_displacment_bitstream,
                                    //4, FALSE, & pix_displ))
                                    //{
                                    //    sprintf(add_error, "dcc_fill_pixel_buffer() :\n   "
                                    //        "pixel_code_and_displacment bitstream, direction %i, frame %i, "
                                    //        "curr_cell %i\n",
                                    //        d, f, curr_cell);
                                    //    strcat(dcc_error, add_error);
                                    //    free(cell_buffer);
                                    //    return 1;
                                    //}

                                    read_pixel[i] += pix_displ;
                                    while (pix_displ == 15)
                                    {
                                        pix_displ = pixelCodesAndDisplacementBits.ToUInt32(ref pixelCodesAndDisplacementBitsOffset, 4);

                                        //if (dcc_read_bits(
                                        //    & dir->pixel_code_and_displacment_bitstream,
                                        //    4, FALSE, & pix_displ))
                                        //{
                                        //    sprintf(add_error, "dcc_fill_pixel_buffer() :\n   "
                                        //        "pixel_code_and_displacment bitstream, direction %i, frame %i, "
                                        //        "curr_cell %i\n",
                                        //        d, f, curr_cell);
                                        //    strcat(dcc_error, add_error);
                                        //    free(cell_buffer);
                                        //    return 1;
                                        //}
                                        read_pixel[i] += pix_displ;
                                    }
                                }

                                if (read_pixel[i] == last_pixel)
                                {
                                    read_pixel[i] = 0; // discard this pixel
                                    i = nb_pix;        // stop the decoding of pixels
                                }
                                else
                                {
                                    last_pixel = read_pixel[i];
                                    decoded_pix++;
                                }
                            }
               
                            // we have the 4 pixels code for the new entry in pixel_buffer
                            old_entry = cell_buffer[curr_cell];
                            pb_idx++;
                            if (pb_idx >= DCC_MAX_PB_ENTRY)
                            {
                                //sprintf(dcc_error, "dcc_fill_pixel_buffer() : "
                                //    "can't add a new entry in pixel buffer,\nmax is %i\n"
                                //    "direction %i, frame %i, curr_cell %i\n"
                                //    "   nb_pix = %i, curr_cell_x y = %i %i\n",
                                //    DCC_MAX_PB_ENTRY, d, f, curr_cell, nb_pix,
                                //    curr_cell_x, curr_cell_y);
                                //free(cell_buffer);
                                return 1;
                            }

                            new_entry = dcc_direction_headers[d].pixel_buffer[pb_idx];
                            new_entry.val = new byte[4];
                            curr_idx  = decoded_pix - 1; // we'll "pop" them

                            for (i=0; i<4; i++)
                            {
                                if (((int)pixel_mask & (1 << i)) == (1 << i))
                                {
                                    if (curr_idx >= 0) // if stack is not empty, pop it
                                    new_entry.val[i] = (byte)read_pixel[curr_idx--];
                                    else // else pop a 0
                                    new_entry.val[i] = 0;
                                }
                                else{
                                    new_entry.val[i] = old_entry.Value.val[i];
                                }
                            }

                            cell_buffer[curr_cell]      = new_entry;
                            new_entry.frame            = f;
                            new_entry.frame_cell_index = x + (y * cell_w);

                            dcc_direction_headers[d].pixel_buffer[pb_idx] = new_entry;
                        }
                    }
                }
            }

            // prepare the stage 2
            //    replace pixel codes in pixel_buffer by their true values
            for (i=0; i <= pb_idx; i++)
            {
                for (x=0; x<4; x++)
                {
                    y = dcc_direction_headers[d].pixel_buffer[i].val[x];
                    dcc_direction_headers[d].pixel_buffer[i].val[x] = dcc_direction_headers[d].PixelValues[y];
                }
            }

            // end
            //dcc_direction_headers[d].pb_nb_entry = pb_idx + 1;
            dcc_direction_headers[d].pco = pixelCodesAndDisplacementBitsOffset;

            return 0;
        }


        // ==========================================================================
        // stage 2 of the decompression of the direction frames
        // return 0 on success, non-zero if error
        List<Image> dcc_make_frame(int d)
        {
            List<Image> bitmaps = new List<Image>();

            DCC_DIRECTION_HEADER_S dir = dcc_direction_headers[d];
            int pbei = 0;
            DCC_PB_ENTRY_S pbe = dir.pixel_buffer[pbei++];
            

            DCC_CELL_S buff_cell, cell;
            uint          pix;
            int             nb_cell, nb_bit,
                            cell_x, cell_y, cell_idx,
                            x, y, c;
            //Bitmap frm_bmp;
            //char            add_error[256];

            BitArray pixelCodesAndDisplacementBits = new BitArray(dcc_direction_headers[d].PixelCodesAndDisplacementBitstream);
            int pixelCodesAndDisplacementBitsOffset = dcc_direction_headers[d].pco;

            // initialised the last_w & last_h of the buffer cells
            for (c=0; c < dir.nb_cells_w * dir.nb_cells_h; c++)
            {
                dir.cells[c].last_w  = -1;
                dir.cells[c].last_h  = -1;
            }

            
            
      
            // for all frames
            for (int f=0; f < dcc_header.frames_per_dir; f++)
            {
                // create the temp frame bitmap (size = current direction box)
                //DCC_FRAME_HEADER_S frame = dir.frame_headers[f];

                var frm_bmp = new Bitmap(dir.box.width, dir.box.height);
                using (var g = Graphics.FromImage(frm_bmp))
                {
                    g.Clear(Color.Transparent);
                }

                //clear(frm_bmp); // clear the final frame bitmap (to index 0)

                nb_cell = dir.frame_headers[f].nb_cells_w * dir.frame_headers[f].nb_cells_h;

                // for all cells of this frame
                for (c=0; c < nb_cell; c++)
                {
                    // frame cell
                    cell = dir.frame_headers[f].cells[c];

                    // buffer cell
                    cell_x    = cell.x0 / 4;
                    cell_y    = cell.y0 / 4;
                    cell_idx  = cell_x + (cell_y * (dir.nb_cells_w - 1));

                    buff_cell = dir.cells[cell_idx];
         
                    // equal cell checks
                    if ((pbe.frame != f) || (pbe.frame_cell_index != c))
                    {
                        // this buffer cell have an equalcell bit set to 1
                        //    so either copy the frame cell or clear it
            
                        if ((cell.w != buff_cell.last_w) || (cell.h != buff_cell.last_h))
                        {
                            // different sizes
                            //clear(cell.bmp); // set all pixels of the frame cell to 0

                            using (var g = Graphics.FromImage(dir.frame_headers[f].cells[c].bmp))
                            {
                                g.Clear(Color.Magenta);
                            }
                        }
                        else
                        {
                            // same sizes
               
                            //// copy the old frame cell into its new position
                            //blit(dir.bmp, dir.bmp,
                            //    buff_cell.last_x0, buff_cell.last_y0,
                            //    cell.x0, cell.y0,
                            //    cell.w, cell.h
                            //);

                            using (var fg = Graphics.FromImage(dcc_direction_headers[d].bmp))
                            {
                                fg.DrawImage(dcc_direction_headers[d].bmp, cell.x0, cell.y0);
                            }
               
                            //// copy it again, into the final frame bitmap
                            //blit(cell.bmp, frm_bmp,
                            //    0, 0,
                            //    cell.x0, cell.y0,
                            //    cell.w, cell.h
                            //);

                            using (var fg = Graphics.FromImage(frm_bmp))
                            {
                                fg.DrawImage(dir.frame_headers[f].cells[c].bmp, cell.x0, cell.y0);
                            }

                        }
                    }
                    else
                    {
                        // fill the frame cell with pixels
                        if (pbe.val[0] == pbe.val[1])
                        {
                            // fill FRAME cell to color val[0]
                            //clear_to_color(cell.bmp, pbe.val[0]);

                            byte color = pbe.val[0];
                            using (var g = Graphics.FromImage(dir.frame_headers[f].cells[c].bmp))
                            {
                                g.Clear(palette[color]);
                            }
                        }
                        else
                        {
                            if (pbe.val[1] == pbe.val[2])
                            {
                                nb_bit = 1;
                            }
                            else
                            {
                                nb_bit = 2;
                            }

                            //// fill FRAME cell with pixels
                            for (y=0; y < cell.h; y++)
                            {
                                for (x=0; x < cell.w; x++)
                                {
                                    //if (dcc_read_bits(
                                    //& dcc.direction[d].pixel_code_and_displacment_bitstream,
                                    //nb_bit, FALSE, & pix))
                                    //{
                                    //sprintf(add_error, "dcc_make_frame() :\n   "
                                    //    "pixel_code_and_displacment bitstream, direction %i, frame %i, "
                                    //    "curr_cell %i\n",
                                    //d, pbe.frame, pbe.frame_cell_index);
                                    //strcat(dcc_error, add_error);
                                    //return 1;
                                    //}

                                    pix = pixelCodesAndDisplacementBits.ToUInt32(ref pixelCodesAndDisplacementBitsOffset, (uint)nb_bit);

                                    byte color = pbe.val[pix];
                                    //if (pix < 3)
                                    {
                                        dir.frame_headers[f].cells[c].bmp.SetPixel(x, y, palette[color]);
                                    }

                                    //putpixel(cell.bmp, x, y, pbe.val[pix]);
                                }
                            }
                        }

                        //// copy the frame cell into the frame bitmap
                        //blit(cell.bmp, frm_bmp,
                        //        0, 0,
                        //        cell.x0, cell.y0,
                        //        cell.w, cell.h
                        //);

                        using (var fg = Graphics.FromImage(frm_bmp))
                        {
                            fg.DrawImage(dir.frame_headers[f].cells[c].bmp, cell.x0, cell.y0);
                        }

                        // next pixelbuffer entry
                        pbe = dir.pixel_buffer[pbei++];
                    }

                    // for the buffer cell that was used by this frame cell,
                    // save the width & size of the current frame cell
                    // (needed for further tests about equalcell)
                    buff_cell.last_w  = cell.w;
                    buff_cell.last_h  = cell.h;

                    // and save its origin, for further copy when equalcell
                    buff_cell.last_x0 = cell.x0;
                    buff_cell.last_y0 = cell.y0;
                }

                // save frame
                bitmaps.Add(frm_bmp);
                //return frm_bmp;
            }

            return bitmaps;
        }


        void LoadPalette()
        {
            for (int i = 0; i < 256; i++)
            {
                Color from_index = Color.FromArgb(255, act_file[i * 3 + 2], act_file[i * 3 + 1], act_file[i * 3]);
                palette[i] = from_index;
            }

            palette[0] = Color.FromArgb(0, palette[0]);

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

        List<Image> ConstructBitmaps(int directionIndex)
        {
            //var bitmaps = new List<Image>();
            //for (int index = 0; index < dc6_frame_headers.Length; index++)
            //{
            //    var dc6_frame_header = dc6_frame_headers[index];

            //    var b = new Bitmap(dc6_frame_header.width, dc6_frame_header.height);

            //    for (int y = 0; y < dc6_frame_header.height; y++)
            //    {
            //        for (int x = 0; x < dc6_frame_header.width; x++)
            //        {
            //            b.SetPixel(x, y, palette_shift[dc6_indexed[index][x, y]]);
            //        }
            //    }

            //    bitmaps.Add(b);
            //}

            dcc_fill_pixel_buffer(directionIndex);

            return dcc_make_frame(directionIndex);

            //return bitmaps;
        }
    }
}