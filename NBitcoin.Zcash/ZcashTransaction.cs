using System;
using System.Collections;
using System.Collections.Generic;
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

        // for the next network upgrade
        public const uint SAPLING_BRANCH_ID = 0x76b809bb;
        public const uint SAPLING_VERSION = 4;

        private readonly char[] ZCASH_PREVOUTS_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','P','r','e','v','o','u','t','H','a','s','h'};
        private readonly char[] ZCASH_SEQUENCE_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','S','e','q','u','e','n','c','H','a','s','h'};
        private readonly char[] ZCASH_OUTPUTS_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','O','u','t','p','u','t','s','H','a','s','h'};
        private readonly char[] ZCASH_JOINSPLITS_HASH_PERSONALIZATION = new char[] {'Z','c','a','s','h','J','S','p','l','i','t','s','H','a','s','h'};

        public ZcashTransaction()
        {
            Version = OVERWINTER_VERSION;
            VersionGroupId = OVERWINTER_VERSION_GROUP_ID;
        }

        public ZcashTransaction(string hex) : base(hex)
        {
        }

        public uint VersionGroupId
        {
            get;
            set;
        }

        public uint ExpiryHeight
        {
            get;
            set;
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

                // Shielded transactions are not supported, pseudocode below is for reference only.
                // See https://github.com/zcash/zcash/blob/0753a0e8a91fec42e0ab424452909d3b02da1afa/src/script/interpreter.cpp#L1159 for original code:
                //
                // if (JoinSplits.Any())
                // {
                //     hashJoinSplits = precomputedTransactionData == null ?
                //                      GetHashJoinSplits() : precomputedTransactionData.HashJoinSplits;
                // }

                var branchId =
                    Version == OVERWINTER_VERSION ? OVERWINTER_BRANCH_ID :
                    Version == SAPLING_BRANCH_ID ? SAPLING_BRANCH_ID :
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
                    ss.ReadWrite(VersionEncoded());
                    ss.ReadWrite(VersionGroupId);
                    // Input prevouts/nSequence (none/all, depending on flags)
                    ss.ReadWrite(hashPrevouts);
                    ss.ReadWrite(hashSequence);
                    // Outputs (none/one/all, depending on flags)
                    ss.ReadWrite(hashOutputs);
                    // JoinSplits
                    ss.ReadWrite(hashJoinSplits);
                    // Locktime
                    ss.ReadWriteStruct(LockTime);
                    // Expiry height
                    ss.ReadWrite(ExpiryHeight);
                    // Sighash type
                    ss.ReadWrite((uint)nHashType);

                    // Shielded transactions are not supported, condition below is always true.
                    // See https://github.com/zcash/zcash/blob/0753a0e8a91fec42e0ab424452909d3b02da1afa/src/script/interpreter.cpp#L1189 for original code:
                    if (nIn != uint.MaxValue)
                    {
                        // The input being signed (replacing the scriptSig with scriptCode + amount)
                        // The prevout may already be contained in hashPrevout, and the nSequence
                        // may already be contain in hashSequence.
                        ss.ReadWrite(Inputs[nIn].PrevOut);
                        ss.ReadWrite(scriptCode);
                        ss.ReadWrite(amount.Satoshi);
                        ss.ReadWrite((uint)Inputs[nIn].Sequence);
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
            if (!stream.Serializing)
            {
                uint nVersion = 0;
                uint nVersionGroupId = 0;
                uint nExpiryHeight = 0;
                VarInt nJoinSplit = new VarInt();
                TxInList vin = new TxInList();
                TxOutList vout = new TxOutList();
                LockTime nLockTime = default(LockTime);

                stream.ReadWrite(ref nVersion);

                var bits = new BitArray(BitConverter.GetBytes(nVersion));

                if (bits.Get(31)) // Overwinter+
                {
                    var data = new byte[4];

                    bits.Set(31, false);
                    bits.CopyTo(data, 0);

                    nVersion = BitConverter.ToUInt32(data, 0);

                    stream.ReadWrite(ref nVersionGroupId);

                    if (nVersion != OVERWINTER_VERSION ||
                        nVersionGroupId != OVERWINTER_VERSION_GROUP_ID)
                    {
                        throw new FormatException($"Unknown transaction format. Version: {nVersion}, VersionGroupId: {nVersionGroupId}");
                    }
                }

                stream.ReadWrite<TxInList, TxIn>(ref vin);
                stream.ReadWrite<TxOutList, TxOut>(ref vout);
                stream.ReadWriteStruct<LockTime>(ref nLockTime);

                if (nVersion >= OVERWINTER_VERSION)
                {
                    stream.ReadWrite(ref nExpiryHeight);
                }

                if (nVersion >= JOIN_SPLIT_VERSION &&
                    stream.Inner.Length > stream.Inner.Position + 1)
                {
                    stream.ReadWrite(ref nJoinSplit);

                    if (nJoinSplit.ToLong() > 0)
                    {
                        throw new FormatException($"Shielded (z-) transactions are not supported.");
                    }
                }

                Version = nVersion;
                VersionGroupId = nVersionGroupId;
                ExpiryHeight = nExpiryHeight;
                Inputs.AddRange(vin);
                Outputs.AddRange(vout);
                LockTime = nLockTime;
            }
            else
            {
                stream.ReadWrite(VersionEncoded());

                if (Version >= OVERWINTER_VERSION)
                { 
                    stream.ReadWrite(VersionGroupId);
                }

                var vin = Inputs;
                stream.ReadWrite<TxInList, TxIn>(ref vin);

                var vout = Outputs;
                stream.ReadWrite<TxOutList, TxOut>(ref vout);

                stream.ReadWriteStruct(LockTime);

                if (Version >= OVERWINTER_VERSION)
                {
                    stream.ReadWrite(ExpiryHeight);
                }

                if (Version >= JOIN_SPLIT_VERSION)
                {
                    stream.ReadWrite(new VarInt(0));
                }
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

        private uint VersionEncoded()
        {
            if (Version >= OVERWINTER_VERSION)
            {
                var nVersionData = new byte[4];
                var bits = new BitArray(BitConverter.GetBytes(Version));

                bits.Set(31, true);
                bits.CopyTo(nVersionData, 0);

                return BitConverter.ToUInt32(nVersionData, 0);
            }
            else
            {
                return Version;
            }
        }
    }
}
