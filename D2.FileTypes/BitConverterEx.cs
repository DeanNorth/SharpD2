using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.FileTypes
{
    public static class BitArrayExtentions
    {
        public static int ToInt32(this BitArray bits, ref int bitOffset, uint bitsToRead = 32)
        {
            BitArray result = new BitArray(32);

            for (int i = 0; i < 32; i++)
            {
                result[i] = true;
            }

            for (int i = 0; i < bitsToRead; i++)
            {
                result[i] = bits[bitOffset + i];
            }

            byte[] resultBytes = new byte[4];
            result.CopyTo(resultBytes, 0);

            bitOffset += (int)bitsToRead;

            return BitConverter.ToInt32(resultBytes, 0);
        }

        public static uint ToUInt32(this BitArray bits, ref int bitOffset, uint bitsToRead = 32)
        {
            BitArray result = new BitArray((int)bitsToRead);

            for (int i = 0; i < bitsToRead; i++)
            {
                result[i] = bits[bitOffset + i];
            }

            byte[] resultBytes = new byte[4];
            result.CopyTo(resultBytes, 0);

            bitOffset += (int)bitsToRead;

            return BitConverter.ToUInt32(resultBytes, 0);
        }

        public static byte ToByte(this BitArray bits, ref int bitOffset, int bitsToRead = 8)
        {
            BitArray result = new BitArray(bitsToRead);

            for (int i = 0; i < bitsToRead; i++)
            {
                result[i] = bits[bitOffset + i];
            }

            byte[] resultBytes = new byte[1];
            result.CopyTo(resultBytes, 0);

            bitOffset += bitsToRead;

            return resultBytes[0];
        }

        public static bool ToBool(this BitArray bits, ref int bitOffset)
        {
            bool result = bits[bitOffset];
            bitOffset += 1;

            return result;
        }

        public static BitArray ToBitArray(this BitArray bits, ref int bitOffset, uint bitsToRead)
        {
            BitArray result = new BitArray((int)bitsToRead);

            for (int i = 0; i < bitsToRead; i++)
            {
                result[i] = bits[bitOffset + i];
            }

            bitOffset += (int)bitsToRead;

            return result;
        }

        public static byte[] ToBytes(this BitArray bits, ref int bitOffset, uint bitsToRead)
        {
            BitArray result = new BitArray((int)bitsToRead);

            for (int i = 0; i < bitsToRead; i++)
            {
                result[i] = bits[bitOffset + i];
            }

            bitOffset += (int)bitsToRead;

            byte[] resultBytes = new byte[(int)Math.Ceiling(bitsToRead / 8m)];
            result.CopyTo(resultBytes, 0);

            return resultBytes;
        }
    }
}
