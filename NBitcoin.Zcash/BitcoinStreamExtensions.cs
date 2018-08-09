using System;
using System.Collections;

namespace NBitcoin.Zcash
{
    public static class BitcoinStreamExtensions
    {
        public static void ReadWriteVersionEncoded(this BitcoinStream stream, ref uint version)
        {
            if (stream.Serializing)
            {
                if (version >= ZcashTransaction.OVERWINTER_VERSION)
                {
                    var versionData = new byte[4];
                    var bits = new BitArray(BitConverter.GetBytes(version));

                    bits.Set(31, true);
                    bits.CopyTo(versionData, 0);

                    stream.ReadWrite(BitConverter.ToUInt32(versionData, 0));
                }
                else
                {
                    stream.ReadWrite(version);
                }
            }
            else
            {
                stream.ReadWrite(ref version);

                var bits = new BitArray(BitConverter.GetBytes(version));

                if (bits.Get(31)) // Overwinter+
                {
                    var data = new byte[4];

                    bits.Set(31, false);
                    bits.CopyTo(data, 0);

                    version = BitConverter.ToUInt32(data, 0);
                }
            }
        }
    }
}