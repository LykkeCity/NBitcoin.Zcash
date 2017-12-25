using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using Xunit;

namespace NBitcoin.Zcash.Tests
{
    public class ZcashNetworksTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShouldGenerateAndParsePrivateKey(bool register)
        {
            if (register)
            {
                ZcashNetworks.Register();
            }

            var key = new Key();
            var privateKey = key.GetWif(ZcashNetworks.Testnet).ToString();
            var address = key.PubKey.GetAddress(ZcashNetworks.Testnet).ToString();

            Assert.Equal(ZcashNetworks.Testnet, BitcoinAddress.Create(address).Network);
            Assert.Equal(address, new BitcoinSecret(privateKey, ZcashNetworks.Testnet).GetAddress().ToString());
		}
    }
}
