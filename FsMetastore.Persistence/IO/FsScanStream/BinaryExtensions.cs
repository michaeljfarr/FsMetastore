using System;
using System.IO;

namespace FsMetastore.Persistence.IO.FsScanStream
{
    static class BinaryExtensions
    {
        public static int Read7BitEncodedInt(this BinaryReader reader) {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new ApplicationException("");
 
                // ReadByte handles end of stream cases for us.
                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public static void Write7BitEncodedInt(this BinaryWriter writer, int value) {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint) value;   // support negative numbers
            while (v >= 0x80) {
                writer.Write((byte) (v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }

        public static long Read7BitEncodedLong(this BinaryReader reader) {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            long count = 0;
            int shift = 0;
            byte b;
            do {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 9 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new ApplicationException("");
 
                // ReadByte handles end of stream cases for us.
                b = reader.ReadByte();
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                count |= (b & 0x7F) << shift;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public static void Write7BitEncodedLong(this BinaryWriter writer, ulong value) {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            
            while (value >= 0x80) {
                writer.Write((byte) (value | 0x80));
                value >>= 7;
            }
            writer.Write((byte)value);
        }
    }
}
