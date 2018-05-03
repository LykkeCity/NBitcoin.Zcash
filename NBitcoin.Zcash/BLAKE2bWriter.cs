using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;

namespace NBitcoin.Zcash
{
    public class BLAKE2bWriter : BitcoinStream, IDisposable
    {
        private byte[] _personalization;

        public BLAKE2bWriter(char[] personalization) : this(Encoding.ASCII.GetBytes(personalization))
        {
        }

        public BLAKE2bWriter(byte[] personalization) : base(new MemoryStream(), true)
        {
            TransactionOptions = TransactionOptions.None;
            Type = SerializationType.Hash;
            _personalization = personalization;
        }

        public uint256 GetHash()
        {
            var blake2b = new Blake2bDigest(null, 32, null, _personalization);
            var hash = new byte[blake2b.GetDigestSize()];

            blake2b.BlockUpdate(((MemoryStream)Inner).ToArrayEfficient(), 0, (int)Inner.Length);
            blake2b.DoFinal(hash, 0);

            return new uint256(hash);
        }

        public void Dispose()
        {
            Inner.Dispose();
        }
    }
}
