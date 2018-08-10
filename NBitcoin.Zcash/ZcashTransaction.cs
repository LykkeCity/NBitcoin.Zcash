using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.Protocol;

namespace NBitcoin.Zcash
{
    public class ZcashTransaction : Transaction
    {
        public const uint JOIN_SPLIT_VERSION = 2;
        public const uint OVERWINTER_BRANCH_ID = 0x5ba81b19;
        public const uint OVERWINTER_VERSION = 3;
        public const uint OVERWINTER_VERSION_GROUP_ID = 0x03C48270;
        public const uint SAPLING_BRANCH_ID = 0x76b809bb;
        public const uint SAPLING_VERSION = 4;
        public const uint SAPLING_VERSION_GROUP_ID = 0x892f2085;
        public const uint GROTH_PROOF_SIZE = 192;                   // https://github.com/zcash/zcash/blob/871e1726c6d8ebb940f0a51260a00aea0a496bce/src/zcash/JoinSplit.hpp#L18
        public const uint ZC_SAPLING_ENCCIPHERTEXT_SIZE = 580;      // https://github.com/zcash/zcash/blob/d86f60f3823b98a6d0f87ad9f4ae09e4db299929/src/zcash/Zcash.h#L27
        public const uint ZC_SAPLING_OUTCIPHERTEXT_SIZE = 80;       // https://github.com/zcash/zcash/blob/d86f60f3823b98a6d0f87ad9f4ae09e4db299929/src/zcash/Zcash.h#L28

        public class ZcashSpendDescription : IBitcoinSerializable
        {
            public uint256 cv;
            public uint256 anchor;
            public uint256 nullifier;
            public uint256 rk;
            public byte[] zkproof = new byte[GROTH_PROOF_SIZE];
            public byte[] spendAuthSig = new byte[64];

            public void ReadWrite(BitcoinStream stream)
            {
                stream.ReadWrite(ref cv);
                stream.ReadWrite(ref anchor);
                stream.ReadWrite(ref nullifier);
                stream.ReadWrite(ref rk);
                stream.ReadWrite(ref zkproof);
                stream.ReadWrite(ref spendAuthSig);
            }
        }

        public class ZcashOutputDescription : IBitcoinSerializable
        {
            public uint256 cv;
            public uint256 cm;
            public uint256 ephemeralKey;
            public byte[] encCiphertext = new byte[ZC_SAPLING_ENCCIPHERTEXT_SIZE];
            public byte[] outCiphertext = new byte[ZC_SAPLING_OUTCIPHERTEXT_SIZE];
            public byte[] zkproof = new byte[GROTH_PROOF_SIZE];

            public void ReadWrite(BitcoinStream stream)
            {
                stream.ReadWrite(ref cv);
                stream.ReadWrite(ref cm);
                stream.ReadWrite(ref ephemeralKey);
                stream.ReadWrite(ref encCiphertext);
                stream.ReadWrite(ref outCiphertext);
                stream.ReadWrite(ref zkproof);
            }
        }

        public class JSDescription : IBitcoinSerializable
        {
            private long vpub_old;
            private long vpub_new;
            private uint256 anchor;
            private uint256[] nullifiers = new uint256[2];
            private uint256[] commitments = new uint256[2];
            private uint256 ephemeralKey;
            private byte[][] ciphertexts = new byte[2][] { new byte[601], new byte[601] };
            private uint256 randomSeed = uint256.Zero;
            private uint256[] macs = new uint256[2];
            libzcash::SproutProof proof;

        }

        private readonly char[] ZCASH_PREVOUTS_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','P','r','e','v','o','u','t','H','a','s','h'};
        private readonly char[] ZCASH_SEQUENCE_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','S','e','q','u','e','n','c','H','a','s','h'};
        private readonly char[] ZCASH_OUTPUTS_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','O','u','t','p','u','t','s','H','a','s','h'};

        private uint nVersionGroupId = 0;
        private uint nExpiryHeight = 0;
        private long valueBalance = 0;
        private VarInt nJoinSplit = new VarInt();
        private List<ZcashSpendDescription> vShieldedSpend = new List<ZcashSpendDescription>();
        private List<ZcashOutputDescription> vShieldedOutput = new List<ZcashOutputDescription>();
        private uint256 joinSplitPubKey;
        private byte[] joinSplitSig = new byte[64];
        private byte[] bindingSig = new byte[64];

        public ZcashTransaction()
        {
            Version = SAPLING_VERSION;
            VersionGroupId = SAPLING_VERSION_GROUP_ID;
        }

        public ZcashTransaction(string hex) : base(hex)
        {
        }

        public uint VersionGroupId
        {
            get => nVersionGroupId;
            set => nVersionGroupId = value;
        }

        public uint ExpiryHeight
        {
            get => nExpiryHeight;
            set => nExpiryHeight = value;
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return ZcashConsensusFactory.Instance;
        }

        public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount, 
            HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
        {
            if (Version >= OVERWINTER_VERSION)
            {
                uint256 hashPrevouts = uint256.Zero;
                uint256 hashSequence = uint256.Zero;
                uint256 hashOutputs = uint256.Zero;
                uint256 hashJoinSplits = uint256.Zero;
                uint256 hashShieldedSpends = uint256.Zero;
                uint256 hashShieldedOutputs = uint256.Zero;

                if ((nHashType & SigHash.AnyoneCanPay) == 0)
                {
                    hashPrevouts = precomputedTransactionData == null ?
                                   GetHashPrevouts() : precomputedTransactionData.HashPrevouts;
                }

                if ((nHashType & SigHash.AnyoneCanPay) == 0 && ((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
                {
                    hashSequence = precomputedTransactionData == null ?
                                   GetHashSequence() : precomputedTransactionData.HashSequence;
                }

                if (((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
                {
                    hashOutputs = precomputedTransactionData == null ?
                                  GetHashOutputs() : precomputedTransactionData.HashOutputs;
                }
                else if (((uint)nHashType & 0x1f) == (uint)SigHash.Single && nIn < this.Outputs.Count)
                {
                    using (var ss = new BLAKE2bWriter(ZCASH_OUTPUTS_HASH_PERSONALIZATION))
                    {
                        ss.ReadWrite(Outputs[nIn]);
                        hashOutputs = ss.GetHash();
                    }
                }

                var branchId =
                    Version == OVERWINTER_VERSION ? OVERWINTER_BRANCH_ID :
                    Version == SAPLING_VERSION ? SAPLING_BRANCH_ID :
                    0;

                var branchIdData = BitConverter.IsLittleEndian ? 
                    BitConverter.GetBytes(branchId) : 
                    BitConverter.GetBytes(branchId).Reverse().ToArray();

                var personal = Encoding.ASCII.GetBytes("ZcashSigHash")
                    .Concat(branchIdData)
                    .ToArray();

                using (var ss = new BLAKE2bWriter(personal))
                {
                    // Version
                    var nVersion = Version;
                    ss.ReadWriteVersionEncoded(ref nVersion);
                    ss.ReadWrite(VersionGroupId);
                    // Input prevouts/nSequence (none/all, depending on flags)
                    ss.ReadWrite(hashPrevouts);
                    ss.ReadWrite(hashSequence);
                    // Outputs (none/one/all, depending on flags)
                    ss.ReadWrite(hashOutputs);
                    // JoinSplits
                    ss.ReadWrite(hashJoinSplits);
                    
                    if (Version >= SAPLING_VERSION)
                    {
                        // Spend descriptions
                        ss.ReadWrite(hashShieldedSpends);
                        // Output descriptions
                        ss.ReadWrite(hashShieldedOutputs);
                    }

                    // Locktime
                    ss.ReadWriteStruct(LockTime);
                    // Expiry height
                    ss.ReadWrite(ExpiryHeight);
                    
                    if (Version >= SAPLING_VERSION)
                    {
                        // Sapling value balance
                        ss.ReadWrite(valueBalance);
                    }

                    // Sighash type
                    ss.ReadWrite((int)nHashType);

                    // Shielded transactions are not supported, condition below is always true.
                    // See https://github.com/zcash/zcash/blob/0753a0e8a91fec42e0ab424452909d3b02da1afa/src/script/interpreter.cpp#L1189 for original code:
                    if (nIn != int.MaxValue)
                    {
                        // The input being signed (replacing the scriptSig with scriptCode + amount)
                        // The prevout may already be contained in hashPrevout, and the nSequence
                        // may already be contained in hashSequence.
                        ss.ReadWrite(Inputs[nIn].PrevOut);
                        ss.ReadWrite(scriptCode);
                        ss.ReadWrite(amount.Satoshi);
                        ss.ReadWrite(Inputs[nIn].Sequence.Value);
                    }

                    return ss.GetHash();
                }
            }
            else
            {
                return base.GetSignatureHash(scriptCode, nIn, nHashType, amount, sigversion, precomputedTransactionData);
            }
        }

        public override void ReadWrite(BitcoinStream stream)
        {
            // we can't use "ref" keyword with properties,
            // so copy base class properties to new variables for value types,
            // and get references to reference types
            var nVersion = Version;
            var nLockTime = LockTime;
            var vin = Inputs;
            var vout = Outputs;

            stream.ReadWriteVersionEncoded(ref nVersion);

            if (nVersion >= OVERWINTER_VERSION)
            {
                stream.ReadWrite(ref nVersionGroupId);
            }

            stream.ReadWrite<TxInList, TxIn>(ref vin);
            stream.ReadWrite<TxOutList, TxOut>(ref vout);
            stream.ReadWriteStruct<LockTime>(ref nLockTime);

            if (nVersion >= OVERWINTER_VERSION)
            {
                stream.ReadWrite(ref nExpiryHeight);
            }

            if (nVersion >= SAPLING_VERSION)
            {
                stream.ReadWrite(ref valueBalance);
                stream.ReadWrite(ref vShieldedSpend);
                stream.ReadWrite(ref vShieldedOutput);
            }

            if (nVersion >= JOIN_SPLIT_VERSION)
            {
                stream.ReadWrite(ref nJoinSplit);

                if (nJoinSplit.ToLong() > 0)
                {
                    stream.ReadWrite(ref joinSplitPubKey);
                    stream.ReadWrite(ref joinSplitSig);

                    throw new NotSupportedException($"Shielded (z-) transactions are not supported.");
                }
            }

            if (nVersion >= SAPLING_VERSION && (vShieldedSpend.Any() || vShieldedOutput.Any()))
            {
                stream.ReadWrite(ref bindingSig);
            }

            // update base class properties for value types
            if (!stream.Serializing)
            {
                Version = nVersion;
                LockTime = nLockTime;
            }
        }

        private uint256 GetHashOutputs()
        {
            using (var ss = new BLAKE2bWriter(ZCASH_OUTPUTS_HASH_PERSONALIZATION))
            {
                foreach (var txout in Outputs)
                {
                    ss.ReadWrite(txout);
                }

                return ss.GetHash();
            }
        }

        private uint256 GetHashSequence()
        {
            using (var ss = new BLAKE2bWriter(ZCASH_SEQUENCE_HASH_PERSONALIZATION))
            {
                foreach (var input in Inputs)
                {
                    ss.ReadWrite((uint)input.Sequence);
                }

                return ss.GetHash();
            }
        }

        private uint256 GetHashPrevouts()
        {
            using (var ss = new BLAKE2bWriter(ZCASH_PREVOUTS_HASH_PERSONALIZATION))
            {
                foreach (var input in Inputs)
                {
                    ss.ReadWrite(input.PrevOut);
                }

                return ss.GetHash();
            }
        }
    }
}
