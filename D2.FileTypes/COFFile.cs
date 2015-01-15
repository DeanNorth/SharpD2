using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.FileTypes
{
    public class COFFile
    {
        private byte[] data;

        string[] composit = new string[]{"HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8"};

        public enum DrawingMode
        {
            Trans75 = 0,
            Trans50,
            Trans25,
            AlphaBlending,
            Luminance,
            Normal,
            BrightAlphaBlending
        }

        public struct cof_comp
        {
            public DrawingMode drawingMode;
            public byte blend;
            public string composit;

            public bool   active;  // in ini
            public bool   present; // in cof
            public string name; // = new char[4]; // char  name[4];
            public string  wclass; // = new char[4];
            public string filepath; // = new char[30];
            public byte[] colormap; // = new byte[256]; // colormap to applied to this layer
            public int   palette;
            public string cmapname; // = new char[256];
        };

        public int Layers { get; set; }
        public int FramesPerDirection { get; set; }
        public int Directions { get; set; }

        public List<cof_comp> Comps = new List<cof_comp>();

        public byte[, ,] Composition;

        public COFFile(byte[] data)
        {
            this.Layers = data[0];
            this.FramesPerDirection = data[1];
            this.Directions = data[2];

            int coftbl = 28;
            for (int i = 0; i < Layers; i++)
            {
                var newcomp = new cof_comp();

                newcomp.present = true;

                newcomp.composit = composit[data[coftbl]];
                newcomp.blend = data[coftbl + 3];
                newcomp.drawingMode = (DrawingMode)data[coftbl + 4];
                
                newcomp.wclass = System.Text.Encoding.Default.GetString(data, coftbl + 5, 3);

                newcomp.filepath = string.Format(@"..\{0}\TP{0}litON{1}.dcc", newcomp.composit, newcomp.wclass);

                Comps.Add(newcomp);

                coftbl += 9;
            }

            coftbl += this.FramesPerDirection;

            Composition = new byte[Directions,FramesPerDirection, Layers];
            for (int d = 0; d < Directions; d++)
            {
                for (int f = 0; f < FramesPerDirection; f++)
                {
                    for (int l = 0; l < Layers; l++)
			        {
                        Composition[d, f, l] = data[coftbl++];
			        }
                }
            }

        }

    }
}
