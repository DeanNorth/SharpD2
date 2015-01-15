using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.FileTypes
{
    public class DS1File
    {
        public struct CELL_W_S
        {
            public byte prop1;
            public byte prop2;
            public byte prop3;
            public byte prop4;
            public byte orientation;
            public int  bt_idx;
            public byte flags;
        }

        public struct CELL_F_S
        {
            public byte prop1;
            public byte prop2;
            public byte prop3;
            public byte prop4;
            public int  bt_idx;
            public byte flags;
        }

        //typedef struct CELL_S_S // exactly the same struct for shadow as for the floor
        //{
        //   UBYTE prop1;
        //   UBYTE prop2;
        //   UBYTE prop3;
        //   UBYTE prop4;
        //   int   bt_idx;
        //   UBYTE flags;
        //} CELL_S_S;

        //typedef struct CELL_T_S
        //{
        //   // assume the data is 1 dword, and not 4 different bytes
        //   UDWORD num;
        //   UBYTE  flags;
        //} CELL_T_S;

        public int Width { get; set; }
        public int Height { get; set; }

        public List<List<CELL_F_S>> floors = new List<List<CELL_F_S>>();
        public List<List<CELL_W_S>> walls = new List<List<CELL_W_S>>();
        public List<List<CELL_W_S>> orientations = new List<List<CELL_W_S>>();
        public List<string> files = new List<string>();

        private byte[] dir_lookup = new byte[]{
                  0x00, 0x01, 0x02, 0x01, 0x02, 0x03, 0x03, 0x05, 0x05, 0x06,
                  0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
                  0x0F, 0x10, 0x11, 0x12, 0x14
               };

        public DS1File(Stream stream)
        {

            using (var br = new BinaryReader(stream))
            {

                int version = br.ReadInt32();

                Width = br.ReadInt32() + 1;
                Height = br.ReadInt32() + 1;

                int new_width = Width;
                int new_height = Height;

                int act = 1;
                if (version >= 8)
                {
                    act = br.ReadInt32();
                }

                int tagType = 0;
                if (version >= 10)
                {
                    tagType = br.ReadInt32();
                }

                int fileNum = 0;
                
                if (version >= 3)
                {
                    fileNum = br.ReadInt32();

                    for (int i = 0; i < fileNum; i++)
                    {
                        string file = br.ReadNullTerminatedString();
                        files.Add(file);
                    }
                }

                if ((version >= 9) && (version <= 13))
                {
                    br.ReadInt32();
                    br.ReadInt32();
                }

                int wallCount = 0;
                int floorCount = 0;
                int tagCount = 0;
                int shadowCount = 1;

                if (version >= 4)
                {
                    wallCount = br.ReadInt32();

                    if (version >= 16)
                    {
                        floorCount = br.ReadInt32();
                    }
                    else
                    {
                        floorCount = 1;
                    }
                }
                else
                {
                    wallCount = 1;
                    floorCount = 1;
                    tagCount = 1;
                }

                int[] lay_stream = new int[14];
                int layerCount = 0;
                if (version < 4)
                {
                    lay_stream[0] = 1; // wall 1
                    lay_stream[1] = 9; // floor 1
                    lay_stream[2] = 5; // orientation 1
                    lay_stream[3] = 12; // tag
                    lay_stream[4] = 11; // shadow
                    layerCount = 5;
                }
                else
                {
                    for (int x = 0; x < wallCount; x++)
                    {
                        lay_stream[layerCount++] = 1 + x; // wall x
                        lay_stream[layerCount++] = 5 + x; // orientation x
                    }
                    for (int x = 0; x < floorCount; x++)
                    {
                        lay_stream[layerCount++] = 9 + x; // floor x
                    }

                    if (shadowCount > 0)
                    {
                        lay_stream[layerCount++] = 11;    // shadow
                    }

                    if (tagCount > 0)
                    {
                        lay_stream[layerCount++] = 12;    // tag
                    }
                }

                int p;

                for (int i = 0; i < floorCount; i++)
                {
                    floors.Add(new List<CELL_F_S>());
                }

                for (int i = 0; i < wallCount; i++)
                {
                    walls.Add(new List<CELL_W_S>());
                }

                for (int i = 0; i < wallCount; i++)
                {
                    orientations.Add(new List<CELL_W_S>());
                }

                for (int n = 0; n < layerCount; n++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            switch (lay_stream[n])
                            {
                                // walls
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    if ((x < new_width) && (y < new_height))
                                    {
                                        p = lay_stream[n] - 1;
                                        CELL_W_S cell = new CELL_W_S();
                                        cell.prop1 = br.ReadByte();
                                        cell.prop2 = br.ReadByte();
                                        cell.prop3 = br.ReadByte();
                                        cell.prop4 = br.ReadByte();
                                        walls[p].Add(cell);
                                    }
                                    else
                                    {
                                        br.ReadInt32();
                                    }
                                    break;

                                // orientations
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                    if ((x < new_width) && (y < new_height))
                                    {
                                        p = lay_stream[n] - 5;
                                        CELL_W_S cell = new CELL_W_S();

                                        if (version < 7)
                                        {
                                            cell.orientation = dir_lookup[br.ReadByte()];
                                        }
                                        else
                                        {
                                            cell.orientation = br.ReadByte();;
                                        }

                                        orientations[p].Add(cell);
                                    }

                                    br.ReadByte();
                                    br.ReadByte();
                                    br.ReadByte();
                                    
                                    break;

                                // floors
                                case 9:
                                case 10:
                                    if ((x < new_width) && (y < new_height))
                                    {
                                        p = lay_stream[n] - 9;

                                        CELL_F_S cell = new CELL_F_S();
                                        cell.prop1 = br.ReadByte();
                                        cell.prop2 = br.ReadByte();
                                        cell.prop3 = br.ReadByte();
                                        cell.prop4 = br.ReadByte();
                                        floors[p].Add(cell);
                                    }
                                    else
                                    {
                                        br.ReadInt32();
                                    }
                                    break;

                                // shadow
                                case 11:
                                    //if ((x < new_width) && (y < new_height))
                                    //{
                                    //    p = lay_stream[n] - 11;
                                    //    s_ptr[p]->prop1 = *bptr;
                                    //    bptr++;
                                    //    s_ptr[p]->prop2 = *bptr;
                                    //    bptr++;
                                    //    s_ptr[p]->prop3 = *bptr;
                                    //    bptr++;
                                    //    s_ptr[p]->prop4 = *bptr;
                                    //    bptr++;
                                    //    s_ptr[p] += s_num;
                                    //}
                                    //else
                                    {
                                        br.ReadInt32();
                                    }
                                    break;

                                // tag
                                case 12:
                                    //if ((x < new_width) && (y < new_height))
                                    //{
                                    //    p = lay_stream[n] - 12;
                                    //    t_ptr[p]->num = (UDWORD) * ((UDWORD*)bptr);
                                    //    t_ptr[p] += t_num;
                                    //}
                                    br.ReadInt32();
                                    break;
                            }
                        }

                        //// in case of bigger width
                        //p = new_width - glb_ds1[ds1_idx].width;
                        //if (p > 0)
                        //{
                        //    switch (lay_stream[n])
                        //    {
                        //        // walls
                        //        case 1:
                        //        case 2:
                        //        case 3:
                        //        case 4:
                        //            w_ptr[lay_stream[n] - 1] += p * w_num;
                        //            break;

                        //        // orientations
                        //        case 5:
                        //        case 6:
                        //        case 7:
                        //        case 8:
                        //            o_ptr[lay_stream[n] - 5] += p * w_num;
                        //            break;

                        //        // floors
                        //        case 9:
                        //        case 10:
                        //            f_ptr[lay_stream[n] - 9] += p * f_num;
                        //            break;

                        //        // shadow
                        //        case 11:
                        //            s_ptr[lay_stream[n] - 11] += p * s_num;
                        //            break;

                        //        // tag
                        //        case 12:
                        //            t_ptr[lay_stream[n] - 12] += p * t_num;
                        //            break;
                        //    }
                        //}
                    }
                }

            }
        }

        
    }
}
