using D2.FileTypes;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.Game
{
    public class DT1Texture
    {
        public Texture2D WallsTexture { get; set; }
        public Texture2D FloorsTexture { get; set; }
        public DT1File File { get; set; }
    }
}
