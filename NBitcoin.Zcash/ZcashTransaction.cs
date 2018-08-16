using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Zcash
{
    public class ZcashTransaction : Transaction
    {
        private const uint SPROUT_VERSION = 1;
        private const uint JOIN_SPLIT_VERSION = 2;
        private const uint OVERWINTER_BRANCH_ID = 0x5ba81b19;
        private const uint OVERWINTER_VERSION = 3;
        private const uint OVERWINTER_VERSION_GROUP_ID = 0x03C48270;
        private const uint SAPLING_BRANCH_ID = 0x76b809bb;
        private const uint SAPLING_VERSION = 4;
        private const uint SAPLING_VERSION_GROUP_ID = 0x892f2085;
        private const uint GROTH_PROOF_SIZE = 192;                   // https://github.com/zcash/zcash/blob/871e1726c6d8ebb940f0a51260a00aea0a496bce/src/zcash/JoinSplit.hpp#L18
        private const uint ZC_SAPLING_ENCCIPHERTEXT_SIZE = 580;      // https://github.com/zcash/zcash/blob/d86f60f3823b98a6d0f87ad9f4ae09e4db299929/src/zcash/Zcash.h#L27
        private const uint ZC_SAPLING_OUTCIPHERTEXT_SIZE = 80;       // https://github.com/zcash/zcash/blob/d86f60f3823b98a6d0f87ad9f4ae09e4db299929/src/zcash/Zcash.h#L28
        private const byte G1_PREFIX_MASK = 0x02;
        private const byte G2_PREFIX_MASK = 0x0a;
        private const uint NOT_AN_INPUT = uint.MaxValue;
        private const uint TX_EXPIRY_HEIGHT_THRESHOLD = 500000000;

        private class ZcashSpendDescription : IBitcoinSerializable
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

        private class ZcashOutputDescription : IBitcoinSerializable
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

        private class JSDescription : IBitcoinSerializable
        {
            public long vpub_old;
            public long vpub_new;
            public uint256 anchor;
            public uint256[] nullifiers = { uint256.Zero, uint256.Zero };
            public uint256[] commitments = { uint256.Zero, uint256.Zero };
            public uint256 ephemeralKey;
            public byte[][] ciphertexts = { new byte[601], new byte[601] };
            public uint256 randomSeed = uint256.Zero;
            public uint256[] macs = { uint256.Zero, uint256.Zero };
            public byte[] grothProof = new byte[GROTH_PROOF_SIZE];
            public PHGRProof phgrProof = new PHGRProof();

            public void ReadWrite(BitcoinStream stream)
            {
                stream.ReadWrite(ref vpub_old);
                stream.ReadWrite(ref vpub_new);
                stream.ReadWrite(ref anchor);

                stream.ReadWrite(ref nullifiers[0]);
                stream.ReadWrite(ref nullifiers[1]);
                stream.ReadWrite(ref commitments[0]);
                stream.ReadWrite(ref commitments[1]);
                stream.ReadWrite(ref ephemeralKey);
                stream.ReadWrite(ref randomSeed);
                stream.ReadWrite(ref macs[0]);
                stream.ReadWrite(ref macs[1]);

                if (((ZcashStream)stream).Overwintered &&
                    ((ZcashStream)stream).Version >= SAPLING_VERSION)
                {
                    stream.ReadWrite(ref grothProof);  
                }
                else
                {
                    stream.ReadWrite(ref phgrProof);
                }

                stream.ReadWrite(ref ciphertexts[0]);
                stream.ReadWrite(ref ciphertexts[1]);
            }
        }

        private class PHGRProof : IBitcoinSerializable
        {
            public CompressedG1 g_A;
            public CompressedG1 g_A_prime;
            public CompressedG2 g_B;
            public CompressedG1 g_B_prime;
            public CompressedG1 g_C;
            public CompressedG1 g_C_prime;
            public CompressedG1 g_K;
            public CompressedG1 g_H;

            public void ReadWrite(BitcoinStream stream)
            {
                stream.ReadWrite(ref g_A);
                stream.ReadWrite(ref g_A_prime);
                stream.ReadWrite(ref g_B);
                stream.ReadWrite(ref g_B_prime);
                stream.ReadWrite(ref g_C);
                stream.ReadWrite(ref g_C_prime);
                stream.ReadWrite(ref g_K);
                stream.ReadWrite(ref g_H);
            }
        }

        private class CompressedG1 : IBitcoinSerializable
        {
            public bool y_lsb;
            public uint256 x;

            public void ReadWrite(BitcoinStream stream)
            {
                byte leadingByte = G1_PREFIX_MASK;

                if (y_lsb)
                {
                    leadingByte |= 1;
                }

                stream.ReadWrite(ref leadingByte);

                if ((leadingByte & (~1)) != G1_PREFIX_MASK)
                {
                    throw new InvalidOperationException("lead byte of G1 point not recognized");
                }

                y_lsb = (leadingByte & 1) == 1;

                stream.ReadWrite(ref x);
            }
        }

        private class CompressedG2 : IBitcoinSerializable
        {
            public bool y_gt;
            public uint512 x;

            public void ReadWrite(BitcoinStream stream)
            {
                byte leadingByte = G2_PREFIX_MASK;

                if (y_gt)
                {
                    leadingByte |= 1;
                }

                stream.ReadWrite(ref leadingByte);

                if ((leadingByte & (~1)) != G2_PREFIX_MASK)
                {
                    throw new InvalidOperationException("lead byte of G2 point not recognized");
                }

                y_gt = (leadingByte & 1) == 1;

                BitcoinStreamExtensions.ReadWrite(stream, ref x);
            }
        }

        private static readonly char[] ZCASH_PREVOUTS_HASH_PERSONALIZATION = {'Z','c','a','s','h','P','r','e','v','o','u','t','H','a','s','h'};
        private static readonly char[] ZCASH_SEQUENCE_HASH_PERSONALIZATION = {'Z','c','a','s','h','S','e','q','u','e','n','c','H','a','s','h'};
        private static readonly char[] ZCASH_OUTPUTS_HASH_PERSONALIZATION = {'Z','c','a','s','h','O','u','t','p','u','t','s','H','a','s','h'};
        private static readonly char[] ZCASH_JOINSPLITS_HASH_PERSONALIZATION = {'Z','c','a','s','h','J','S','p','l','i','t','s','H','a','s','h'};
        private static readonly char[] ZCASH_SHIELDED_SPENDS_HASH_PERSONALIZATION = {'Z','c','a','s','h','S','S','p','e','n','d','s','H','a','s','h'};
        private static readonly char[] ZCASH_SHIELDED_OUTPUTS_HASH_PERSONALIZATION = {'Z','c','a','s','h','S','O','u','t','p','u','t','H','a','s','h'};

        private bool fOverwintered = false;
        private uint nVersionGroupId = 0;
        private uint? nBranchId = 0;
        private uint nExpiryHeight = 0;
        private long valueBalance = 0;
        private List<ZcashSpendDescription> vShieldedSpend = new List<ZcashSpendDescription>();
        private List<ZcashOutputDescription> vShieldedOutput = new List<ZcashOutputDescription>();
        private List<JSDescription> vjoinsplit = new List<JSDescription>();
        private uint256 joinSplitPubKey;
        private byte[] joinSplitSig = new byte[64];
        private byte[] bindingSig = new byte[64];

        public ZcashTransaction()
        {
            Version = SAPLING_VERSION;
            nVersionGroupId = SAPLING_VERSION_GROUP_ID;
        }

        public ZcashTransaction(string hex, uint? branchId = null) : base(hex)
        {
            nBranchId = branchId;
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return ZcashConsensusFactory.Instance;
        }

        public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount, 
            HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
        {
            if (nIn >= Inputs.Count && (uint)nIn != NOT_AN_INPUT) // second condition is always true, NBitcoin restricts us to transparent txs only, left to match the original code
            {
                throw new InvalidOperationException("Input index is out of range");
            }
            
            if (fOverwintered && (nVersionGroupId == OVERWINTER_VERSION_GROUP_ID || nVersionGroupId == SAPLING_VERSION_GROUP_ID))
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

                if (vjoinsplit.Any())
                {
                    hashJoinSplits = GetJoinSplitsHash();
                }

                if (vShieldedSpend.Any())
                {
                    hashShieldedSpends = GetShieldedSpendsHash();
                }

                if (vShieldedOutput.Any())
                {
                    hashShieldedOutputs = GetShieldedOutputsHash();
                }

                var branchId = 
                    nBranchId.HasValue ? nBranchId.Value :
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
                    ss.ReadWriteVersionEncoded(ref nVersion, ref fOverwintered);
                    ss.ReadWrite(nVersionGroupId);
                    // Input prevouts/nSequence (none/all, depending on flags)
                    ss.ReadWrite(hashPrevouts);
                    ss.ReadWrite(hashSequence);
                    // Outputs (none/one/all, depending on flags)
                    ss.ReadWrite(hashOutputs);
                    // JoinSplits
                    ss.ReadWrite(hashJoinSplits);
                    
                    if (nVersionGroupId == SAPLING_VERSION_GROUP_ID)
                    {
                        // Spend descriptions
                        ss.ReadWrite(hashShieldedSpends);
                        // Output descriptions
                        ss.ReadWrite(hashShieldedOutputs);
                    }

                    // Locktime
                    ss.ReadWriteStruct(LockTime);
                    // Expiry height
                    ss.ReadWrite(nExpiryHeight);
                    
                    if (nVersionGroupId == SAPLING_VERSION_GROUP_ID)
                    {
                        // Sapling value balance
                        ss.ReadWrite(valueBalance);
                    }

                    // Sighash type
                    ss.ReadWrite((uint)nHashType);


                    // If this hash is for a transparent input signature
                    // (i.e. not for txTo.joinSplitSig):
                    if ((uint)nIn != NOT_AN_INPUT) // always true, NBitcoin restricts us to transparent txs only, left to match the original code
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
                // Check for invalid use of SIGHASH_SINGLE
                if (((uint)nHashType & 0x1f) == (uint)SigHash.Single)
                {
                    if (nIn >= Outputs.Count)
                    {
                        throw new InvalidOperationException("No matching output for SIGHASH_SINGLE");
                    }
                }

                var scriptCopy = new Script(scriptCode.ToOps().Where(op => op.Code != OpcodeType.OP_CODESEPARATOR));
                var txCopy = new ZcashTransaction(this.ToHex());

                //Set all TxIn script to empty string
                foreach (var txin in txCopy.Inputs)
                {
                    txin.ScriptSig = new Script();
                }

                //Copy subscript into the txin script you are checking
                txCopy.Inputs[nIn].ScriptSig = scriptCopy;

                if (nHashType == SigHash.None)
                {
                    //The output of txCopy is set to a vector of zero size.
                    txCopy.Outputs.Clear();

                    //All other inputs aside from the current input in txCopy have their nSequence index set to zero
                    foreach (var input in txCopy.Inputs.Where((x, i) => i != nIn))
                        input.Sequence = 0;
                }
                else if (nHashType == SigHash.Single)
                {
                    //The output of txCopy is resized to the size of the current input index+1.
                    txCopy.Outputs.RemoveRange(nIn + 1, txCopy.Outputs.Count - (nIn + 1));

                    //All other txCopy outputs aside from the output that is the same as the current input index are set to a blank script and a value of (long) -1.
                    for (var i = 0; i < txCopy.Outputs.Count; i++)
                    {
                        if (i == nIn)
                            continue;
                        txCopy.Outputs[i] = new TxOut();
                    }

                    //All other txCopy inputs aside from the current input are set to have an nSequence index of zero.
                    foreach (var input in txCopy.Inputs.Where((x, i) => i != nIn))
                        input.Sequence = 0;
                }

                if ((nHashType & SigHash.AnyoneCanPay) != 0)
                {
                    //The txCopy input vector is resized to a length of one.
                    var script = txCopy.Inputs[nIn];
                    txCopy.Inputs.Clear();
                    txCopy.Inputs.Add(script);
                    //The subScript (lead in by its length as a var-integer encoded!) is set as the first and only member of this vector.
                    txCopy.Inputs[0].ScriptSig = scriptCopy;
                }

                // clean JS signature
                // see https://github.com/zcash/zcash/blob/e868f8247faea8cc74aef69262d93bdeacc82c53/src/script/interpreter.cpp#L1053
                txCopy.joinSplitSig = new byte[64];

                //Serialize TxCopy, append 4 byte hashtypecode
                using (var hs = CreateSignatureHashStream())
                {
                    BitcoinStream stream = new BitcoinStream(hs, true);
                    stream.Type = SerializationType.Hash;
                    stream.TransactionOptions = sigversion == HashVersion.Original ? TransactionOptions.None : TransactionOptions.Witness;
                    txCopy.ReadWrite(stream);
                    stream.ReadWrite((uint)nHashType);
                    return hs.GetHash();
                }
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
            var zStream = new ZcashStream(stream.Inner, stream.Serializing);

            zStream.ReadWriteVersionEncoded(ref nVersion, ref fOverwintered);

            if (fOverwintered)
            {
                zStream.ReadWrite(ref nVersionGroupId);
            }

            var isSprout = nVersion == SPROUT_VERSION;
            var isJs = nVersion == JOIN_SPLIT_VERSION;
            var isOverwinter = nVersion == OVERWINTER_VERSION && nVersionGroupId == OVERWINTER_VERSION_GROUP_ID;
            var isSapling = nVersion == SAPLING_VERSION && nVersionGroupId == SAPLING_VERSION_GROUP_ID;

            if (!isSprout && !isJs && !(fOverwintered && (isOverwinter || isSapling)))
            {
                throw new NotSupportedException("Unknown tx format");
            }

            zStream.ReadWrite<TxInList, TxIn>(ref vin);
            zStream.ReadWrite<TxOutList, TxOut>(ref vout);
            zStream.ReadWriteStruct(ref nLockTime);

            if (fOverwintered)
            {
                zStream.ReadWrite(ref nExpiryHeight);
            }

            if (nExpiryHeight >= TX_EXPIRY_HEIGHT_THRESHOLD)
            {
                throw new NotSupportedException($"Expiry height must be less than {TX_EXPIRY_HEIGHT_THRESHOLD}");
            }

            if (isSapling)
            {
                zStream.ReadWrite(ref valueBalance);
                zStream.ReadWrite(ref vShieldedSpend);
                zStream.ReadWrite(ref vShieldedOutput);
            }

            if (nVersion >= JOIN_SPLIT_VERSION)
            {
                // provide version info to joinSplit serializer
                zStream.Version = nVersion;
                zStream.Overwintered = fOverwintered;

                zStream.ReadWrite(ref vjoinsplit);

                if (vjoinsplit.Count > 0)
                {
                    zStream.ReadWrite(ref joinSplitPubKey);
                    zStream.ReadWrite(ref joinSplitSig);
                }
            }

            if (isSapling && (vShieldedSpend.Any() || vShieldedOutput.Any()))
            {
                zStream.ReadWrite(ref bindingSig);
            }

            // update base class properties for value types
            if (!zStream.Serializing)
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

        private uint256 GetJoinSplitsHash() {
            using (var ss = new BLAKE2bWriter(ZCASH_JOINSPLITS_HASH_PERSONALIZATION))
            {
                // provide version info to joinSplit serializer
                ss.Version = Version;
                ss.Overwintered = fOverwintered;

                foreach (var js in vjoinsplit)
                {
                    ss.ReadWrite(js);
                }

                ss.ReadWrite(joinSplitPubKey);

                return ss.GetHash();
            }
        }

        private uint256 GetShieldedSpendsHash()
        {
            using (var ss = new BLAKE2bWriter(ZCASH_SHIELDED_SPENDS_HASH_PERSONALIZATION))
            {
                foreach (var spend in vShieldedSpend)
                {
                    ss.ReadWrite(spend.cv);
                    ss.ReadWrite(spend.anchor);
                    ss.ReadWrite(spend.nullifier);
                    ss.ReadWrite(spend.rk);
                    ss.ReadWrite(ref spend.zkproof);
                }

                return ss.GetHash();
            }
        }

        private uint256 GetShieldedOutputsHash()
        {
            using (var ss = new BLAKE2bWriter(ZCASH_SHIELDED_OUTPUTS_HASH_PERSONALIZATION))
            {
                foreach (var sout in vShieldedOutput)
                {
                    ss.ReadWrite(sout);
                }

                return ss.GetHash();
            }
        }
    }
}
