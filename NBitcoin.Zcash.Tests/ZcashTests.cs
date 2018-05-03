using Xunit;

namespace NBitcoin.Zcash.Tests
{
    public class ZcashTests
    {
        static ZcashTests()
        {
            ZcashNetworks.Instance.EnsureRegistered();
        }

        [Fact]
        public void ShouldGenerateAndParsePrivateKey()
        {
            var address = "tmXa2FBJhtVCLQthesaP73BBgKNpQJQZqsA";

            var key = Key.Parse("cVYbHbtGw95aZ4sh6Mk92sEZMwihQs9CaLk3QqDsUVnuLmPgGQ3g", ZcashNetworks.Instance.Testnet);

            Assert.Equal(ZcashNetworks.Instance.Testnet.Name, BitcoinAddress.Create(address).Network.Name);
            Assert.Equal(address, key.PubKey.GetAddress(ZcashNetworks.Instance.Testnet).ToString());
		}
    }
}
