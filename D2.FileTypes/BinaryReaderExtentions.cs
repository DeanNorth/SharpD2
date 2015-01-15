using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.FileTypes
{
    public static class BinaryReaderExtentions
    {
        public static string ReadNullTerminatedString(this BinaryReader br)
        {
            StringBuilder sb = new StringBuilder();
            Int32 nc;
            while ((nc = br.Read()) != -1)
            {
                Char c = (Char)nc;

                if (c == '\0') break;

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
