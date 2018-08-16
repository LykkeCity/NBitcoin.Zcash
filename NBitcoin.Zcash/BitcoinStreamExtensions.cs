using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Zcash
{
    public static class BitcoinStreamExtensions
    {
        static uint512.MutableUint512 _mutableUint512 = new uint512.MutableUint512(uint512.Zero);

        public static void ReadWriteVersionEncoded(this BitcoinStream stream, ref uint version, ref bool overwintered)
        {
            if (stream.Serializing)
            {
                if (overwintered)
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

                if (overwintered = bits.Get(31)) // Overwinter+
                {
                    var data = new byte[4];

                    bits.Set(31, false);
                    bits.CopyTo(data, 0);

                    version = BitConverter.ToUInt32(data, 0);
                }
            }
        }

        public static void ReadWrite(this BitcoinStream stream, ref uint512 value)
        {
            value = value ?? uint512.Zero;
            _mutableUint512.Value = value;
            stream.ReadWrite(ref _mutableUint512);
            value = _mutableUint512.Value;
        }
    
        public static void ReadWriteArray(this BitcoinStream stream, ref uint512[] value)
        {
            if (stream.Serializing)
            {
                var list = value?.Select(v => v.AsBitcoinSerializable()).ToArray();
                stream.ReadWrite(ref list);
            }
            else
            {
                List<uint512.MutableUint512> list = null;
                stream.ReadWrite(ref list);
                value = list.Select(l => l.Value).ToArray();
            }
        }

        public static void ReadWriteArray(this BitcoinStream stream, ref uint256[] value)
        {
            if (stream.Serializing)
            {
                var list = value?.Select(v => v.AsBitcoinSerializable()).ToArray();
                stream.ReadWrite(ref list);
            }
            else
            {
                List<uint256.MutableUint256> list = null;
                stream.ReadWrite(ref list);
                value = list.Select(l => l.Value).ToArray();
            }
        }
    }
}