using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin
{
    public class ZcashNetworks
	{
		private static Tuple<byte[], int>[] pnSeed6_main = {};
		private static Tuple<byte[], int>[] pnSeed6_test = {};
        private static Network _mainnet;
        private static Network _testnet;

        private static uint256 GetPoWHash(BlockHeader header)
        {
            var headerBytes = header.ToBytes();
            var h = SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
            return new uint256(h);
        }

        private static IEnumerable<NetworkAddress> ToSeed(Tuple<byte[], int>[] tuples)
        {
            return tuples
                .Select(t => new NetworkAddress(new IPAddress(t.Item1), t.Item2))
                .ToArray();
        }

        /// <summary>
        /// Registers the mainnet.
        /// </summary>
        /// <returns>The mainnet.</returns>
        private static Network RegisterMainnet()
        {
            return _mainnet = new NetworkBuilder()
                .SetConsensus(new Consensus()
                {
                    SubsidyHalvingInterval = 840000,
                    MajorityEnforceBlockUpgrade = 750,
                    MajorityRejectBlockOutdated = 950,
                    MajorityWindow = 4000,
                    BIP34Hash = new uint256("fa09d204a83a768ed5a7c8d441fa62f2043abf420cff1226c7b4329aeb9d51cf"),
                    PowLimit = new Target(new uint256("0007ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                    PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
                    PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
                    PowAllowMinDifficultyBlocks = false,
                    PowNoRetargeting = false,
                    RuleChangeActivationThreshold = 6048,
                    MinerConfirmationWindow = 8064,
                    CoinbaseMaturity = 100,
                    HashGenesisBlock = new uint256("0x00040fe8ec8471911baa1db1266ea15dd06b4a8a5c453883c000b031973dce08"),
                    GetPoWHash = GetPoWHash,
                    LitecoinWorkCalculation = true
                })
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x1C, 0xB8 })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x1C, 0xBD })
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0x80 })
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
                .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("zec"))
                .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("zec"))
                .SetMagic(0xdbb6c0fb)
                .SetPort(8233)
                .SetRPCPort(8232)
                .SetName("zcash-mainnet")
                .AddDNSSeeds(new[]
                {
                    new DNSSeedData("z.cash", "dnsseed.z.cash")
                })
                .SetGenesis(new Block(new BlockHeader()
                {
                    BlockTime = DateTimeOffset.FromUnixTimeSeconds(1477641360),
                    Nonce = new uint256("0x0000000000000000000000000000000000000000000000000000000000001257").GetLow32(),
                }))
                .AddSeeds(ToSeed(pnSeed6_main))
                .BuildAndRegister();
        }

        private static Network RegisterTestnet()
		{
			return _testnet = new NetworkBuilder()
                .SetConsensus(new Consensus()
			    {
				    SubsidyHalvingInterval = 840000,
				    MajorityEnforceBlockUpgrade = 51,
				    MajorityRejectBlockOutdated = 75,
				    MajorityWindow = 400,
				    PowLimit = new Target(new uint256("07ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				    PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
				    PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				    PowAllowMinDifficultyBlocks = true,
				    PowNoRetargeting = false,
				    RuleChangeActivationThreshold = 1512,
				    MinerConfirmationWindow = 2016,
				    CoinbaseMaturity = 100,
				    HashGenesisBlock = new uint256("f5ae71e26c74beacc88382716aced69cddf3dffff24f384e1808905e0188f68f"),
				    GetPoWHash = GetPoWHash,
				    LitecoinWorkCalculation = true
			    })
			    .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x1D, 0x25 })
			    .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x1C, 0xBA })
			    .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0xEF })
			    .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			    .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			    .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("taz"))
			    .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("taz"))
			    .SetMagic(0xf1c8d2fd)
			    .SetPort(18233)
			    .SetRPCPort(18232)
			    .SetName("zcash-testnet")
			    .AddDNSSeeds(new[]
			    {
				    new DNSSeedData("z.cash", "dnsseed.testnet.z.cash")
			    })
			    .AddSeeds(ToSeed(pnSeed6_test))
                .SetGenesis(new Block(new BlockHeader()
                {
                    BlockTime = DateTimeOffset.FromUnixTimeSeconds(1477648033),
                    Nonce = new uint256("0x0000000000000000000000000000000000000000000000000000000000000006").GetLow32(),
                }))
			    .BuildAndRegister();
		}

        public static void Register()
        {
            RegisterMainnet();
            RegisterTestnet();
        }

		public static Network Mainnet
		{
			get
			{
                return _mainnet ?? RegisterMainnet();
			}
		}

		public static Network Testnet
		{
			get
			{
                return _testnet ?? RegisterTestnet();
            }
        }
	}
}
