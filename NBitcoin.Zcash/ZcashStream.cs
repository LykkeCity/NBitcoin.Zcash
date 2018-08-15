using System;
using System.IO;
namespace NBitcoin.Zcash
{
    public class ZcashStream : BitcoinStream
    {
        public ZcashStream(Stream inner, bool serializing) : base(inner, serializing)
        {
        }

        public uint Version { get; set; }
        public bool Overwintered { get; set; }
    }
}
