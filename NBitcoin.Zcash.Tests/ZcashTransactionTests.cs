﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace NBitcoin.Zcash.Tests
{
    public class ZcashTransactionTests
    {
        public const string v1 = "0100000004ea24a0695ddcb70d80a7fca7af18bdf6f97ce74434195c71476fe95ba04e6e5e020000006b483045022100cf970ca333288830a2108019fa4e8c02f02745b1a8fc89f333879afe2ddc7a540220308d68d00b0a4a34eef22005977c84c0386601caa47ea943d7ca1efcce7599a8012103e3d923afef96d81456f6bf0f03c36ded44c22525440c34d9d431a678c8acd7a8ffffffff4fa6b584f4e151e2666a0bda343506dcb9b2e016fac3d06de5239e1a357fcaae020000006b483045022100ac6ad32bd25c2e807f3bb7b80acd79e9fca3c0540d63fb6d2705c261551c59090220623be6974658c21e19f2331ef81224a858c2325c6367cae3eeb90714277e3e23012103e3d923afef96d81456f6bf0f03c36ded44c22525440c34d9d431a678c8acd7a8ffffffffcaca8060a56fcf0ef0fe93923dee406976782ab8b01276e74a33daf02e1fbad3020000006b483045022100f143e63bf2bb7303c39ed927dc1af29e9defd462477fc5ccb73d707042e4d97302205e9b7d0f17ec5aee3ad29762e2e2f9d26529cd205b68daf7a90f6eba504cbf6f012103e3d923afef96d81456f6bf0f03c36ded44c22525440c34d9d431a678c8acd7a8ffffffff5ea1d091e2587a1627db9ce605a3850fbe4a3f0582ed22d7fc5cd2a121eac956020000006b483045022100af042676f1057a52c2cc99ee241212391bf6d5200083afccb30636379e4180cd0220397a4ee64ff1c6ec04d65ecba25e7cd1002bf43f7d4aa442a4520535af3c4557012103e3d923afef96d81456f6bf0f03c36ded44c22525440c34d9d431a678c8acd7a8ffffffff03449c3311000000001976a91411b5c0639fc5e742e7ab3b337f718316fc9e605788ac10270000000000001976a91438fef011da24095f9fcaf2a8be83af8d478c753b88ace43d9906000000001976a9148a89f8c3b1e537eccaece6b25c9434d720fb36e188ac00000000";
        public const string v2 = "0200000001972f62037b4f48efb442c24ea1b53c214ab16d3cad893c4d9e35402a2299b7a9000000006b48304502210083fd0093c8a2ee4a27b384ef9abef2e079ec1400979061c52cb059e43697e7d50220774868cb123db135f9bf38a0cccc8eddccc11e324ab613248a1f5b5a9d5b10a50121032040ff72baa56d439f8ac7779e13d34d50b150a48918b0e0e04d0f05843b2d9effffffff000000000001a04c9b3b0000000000000000000000007de030e26de6f0daec9d279b1330947393732b196610584edb87a26477140e77193dfc573ff2b10aa9452d2b98de6210173baf30e7b9f94b92968959e20ba8196c49255aaf896af80e325c6a017982a1f261f50e258ab34decb1dbbe836f7d1d6a9898c4ea21a4f80a7b888bfc2ac8fdc94944637e2c1433bcfc00c8614f02ef13d4db204b82d64a898d1ccf3b4622a2593906d518d427c9ff02d2c3b04b393fd27bce3ade405ce587d9d50487ed5d9ee90e7f8e6d930ba79489870d9f3ff509b9d46f86035e7e67dee21f98d05adae723a3ecf1a56287b5cbd80c5e02d12472bde9a3560ab3559b4acffba98d3fc14b23fc2272a8d3085a0a11a4551b0232e3b5c81718b0563dd4ed223e33e0d057075d5e8238fd77d5f157c4f3e79a12470b022bb5ebe13c6584b434b568cd0406975b2e39834a015b6e74a31df4d8547605e103126b4cde1acb32ee7bc7e01d4cf3e8b18242f8dddce16023dd8074a9d9b5dfd10a04be12930d8817b4b72cd1b136b21cf91b90766d6b5327d8bee405619346c92fc72750aa4a1b38dbce7ae5ca533e092557edd03da9d03a0f2473dfc52b589ac00200aa70bb3893afc278c0e66c4f981db19de4e7efa21761737624e10c73d3520c032d9aebcc226f5e85c0575bb293dc890998038be8ca44c5bad022b2d8fbaf739a022005306e3a75988fb3060fc8fb961736c3e6269b2e50eccc926ab9100564cb850320f85d5da8fdf182031df5be30bff1c0fb39620bbf404c9cbfe536d9dc6644a7021cc1f07b6c3221bf1ed2c2f7272344d24c0eb78e752d6f5bfcbcaa4351829790fff05d9144da6b0c3e887d7a7c5fd553864d9aaac8dccfefee782c287eca56cbb0e5f7f2daaf85724674a57957713a703f2df195416def0d20dfdf674440921a90934a53bc01ef70f7a8dca0c6e7953d8ee7d7fd5eb3122766094d28adf952fa2a0e17a5a1bcb73ed589d81f5b0c47b6a1d4fb8bc333abe19d5bb0d02ed07daa8c8dc11ec6a45c6abb4910d31e9952f7893e9eb85c22a59e6b7ce85b85fcd888ca955885192062313ad031f0b6f0b5d3d40662a4f7658f09be350dce620729c1a4068544129e232e7727b64b6556f91adc79c85a8d9583b9a3238cf0be19242505369a3e1159f9a87ba746902c9da64bd663fe5bd379fddf3dc8e05c029abc4cdc6d23efafd4ff00c0017c77c0094eb643295a7e3f3f627da2a1046fef38e7da726431ca7397ca5d084572f4861c4f2a29836f5d044300b96974a151c0aa2a1915918a861767c8d82fa31602312a40fc894ddbca3e888682b88302786e51e365baef4ba3930a3e291417da446da5661ecface91ac73a2bf245d9a6a2895991a32e9c14d348f5cab4609a9a4dc4c9be107308d24c33f12add4cae7fac2fe45f043510601280e3d43387c55eab475163c79251cf22dffc8c1a17027fb2a1c3c2e10e5929b174a38db0f4766c66599ad7f3176ac571e5be4e80350a2c3625838fd8a032e373dbf473b3dd634fefb247e907f0ee1b3ef5847b7f3750c4547233dbd5786b48deb031d987f9401c1086286ffecd81e63548eb85170e5065b1c73b6b1a4443d5f6981f9c685477b038c2e8178302edafd4da12e1885d188d7591da27d40ceb8f3612472d7c3a92031d35916b0f677c563ccb32753ae3cdfe58b3090666d88931d451bc8530113dcdd9e5ca75971a5a9194b1b4b9c6adfc75215e84de09aad2a46a260523adca24e0c9476c7c312b7c4136d74ff265f08d2174dff25acc570475bd74e7bfa42643c4813038d4e597ded3d65d32ac744453d4e58c13be3845036c41e88d9a51e45fbb2f89e93c8fb32a33808a3ef9f9227a022a320536ecb622c58dec30fa601535693539091ee69d18cc71e2015b49db16cc56059e04cace7e93c68394b2c71e41fc2bd45c77bf68a30641058e71be57610c653ec8d8354313f4fae88b7ae5a465e849f93778fca3b637ec044fa99547c56273f000f0b847089e7788c2c6fb2586be58af5a4a569b936ad18062c4f6ca89c781d994165ac4fa2d6bcfdc5b85f57113b595fb161e2c2784628177075d230dce662be9b477638a066f7b43206c3a268390070f0a17922d690be61de72a8b4b2b0f457c31afe028bbf91a2e844934d23f0c7543926e7de6194be22e7353d6b2ef9bec728df809af9662b00b1b74c2fadf97c5d1932f64f9d4da95a0b7809d198e70db1a94e6db02b6d25431566f6ea67be575974d725d93ab14bd520fe3c0110205f981bad15f2c8628432b18943acbf11d27664a7c49977466216c2f5e5307c9f9bd2a504a298b9ef00996fa7b34b32fd300060e9c08ba61ef19fa605ec48b6e25063488e45989818454ccf3ab1627aff2f3622a465175db2f57350e047a80e3c80b73bd28d47eb2a339d6898fbd46deeca5802eb832a47c23701a54c975b306c54748100f91ce4db8111a0cb857414bc97c4049fa21b80df8c8055678b9f85c067f9f07382eb4d64bfddff2e9a70b8777daa6d2697b14f104649017a0026373b398fb05a9ceb59608e116ee63d9b473db907448f34ea1a2d3e4b81fdbfb36427b98a0d3149c8f4a3bd36906f2695d3162641e85eb826b9935ce53355d824352b52f667de064c6dba6583e0514f8343fab05a08b685b03";
        public const string v3 = "030000807082c4030491871e4bb9d84c8286e17a4a26081d063838914d3312bb7f79c6f1e4acc9579e010000006a47304402206edb7536e314bfc484e012467dc56a46544e927c25747213d18cc93d2411b14902202576ede17b493a1f4aa3584fcb8733b57b50e2578c354b27e131f591d12f6d2e012102468f4f240d8c8e147f43b13a8aca82f3f8e685b5190a1adcd38f7732834d1566feffffff3b72f6b261e4094e23d97c897970f178260777e521cba0fc477d2389b839e8b6000000006a473044022041c140461bdadc9ab92190dc1681fc575f92236abf63134ee935f597eb0d98d7022013d7cc4a1696ad787b68360c015bfbd29e58c7237ac8dfc43b9041de35ba3d4a0121024db0c9955daea8b7c4e6d0afc0b5a64fdd455f02c4a1f2282f43e4cd4d00c00bfeffffffc4ed3fad5a80b6c59b1cdb999fd4ce12c06ea7ed5dae29205edd2981ec5a1a9a010000006b483045022100e0fa0b3fca8255a662a1da455e7dca4f46b426cbbfcaaa0135586e6a7729e1d4022047743d7e4704d9f791c7848d71e92462bccdc58bb0ca12de8f8898d161f050e9012103b9508ced99db991672d493be9dd795503e63962ea9b8b8085f669c402466a69dfeffffffe65350d1795f1c202644fd58345b9503c7fff5a286e548a4c4b5189162a85f52010000006a473044022038608866053c5a1f72765e9d50a424623a2b58bced8af91558ae1d7df73be5c00220138a4e6249b4e5a2ca0b77eee8a30b106dd537a7b903a1ff57c237785bb9f36f0121030f779053d6489568758f9e3a44a08fd6467e0ab9fd6943bf57cc51272187223ffeffffff0200a3e111000000001976a9144faeeb51bcd0b49f238b323e5f1c6c8bf11ae02a88acd451b003000000001976a914ce1a64eac3981eb300242ec415b9bc8373cab8d688ac6e7203008d72030000";
        public const string v4 = "0400008085202f89010763a91de5fff9ba149c65eedea8588e955cc7bd16a8298ae89750c0b78fabe70100000000ffffffff0240420f00000000001976a9141fecb553b1cff0364a7308ffd9ec8169495cf47288ac6fc11710000000001976a9147176f5d11e11c59a1248ef0bf0d6dadb2be1686188ac00000000c943ff020000000000000000000000";

        static ZcashTransactionTests()
        {
            ZcashNetworks.Instance.EnsureRegistered();
        }

        [Theory]
        [InlineData(v1)]
        [InlineData(v3)]
        [InlineData(v4)]
        public void ShouldDeserializeAndSerializeTransaction(string hex)
        {
            var trx = new ZcashTransaction(hex).ToHex();
            Assert.Equal(hex, trx);
        }

        [Fact]
        public void ShouldDeserializeV1()
        {
            var tx = new ZcashTransaction(v1);

            Assert.Equal("5c6ba844e1ca1c8083cd53e29971bd82f1f9eea1f86c1763a22dd4ca183ae061", tx.GetHash().ToString());
            Assert.Equal("5e6e4ea05be96f47715c193444e77cf9f6bd18afa7fca7800db7dc5d69a024ea", tx.Inputs[0].PrevOut.Hash.ToString());
            Assert.Equal(288595012L, tx.Outputs[0].Value.Satoshi);
        }

        [Fact]
        public void ShouldDeserializeV3()
        {
            var tx = new ZcashTransaction(v3);

            Assert.Equal("bd3772485a22944991d3e29fa775d1a90b26b5f052b7869a9f2b41a5112ea9c5", tx.GetHash().ToString());
            Assert.Equal("9e57c9ace4f1c6797fbb12334d913838061d08264a7ae186824cd8b94b1e8791", tx.Inputs[0].PrevOut.Hash.ToString());
            Assert.Equal(300000000L, tx.Outputs[0].Value.Satoshi);
        }

        [Fact]
        public void ShouldDeserializeV4()
        {
            var tx = new ZcashTransaction(v4);

            Assert.Equal("011bc69c232c8fa864d9647700a47452447f15c7913555c2291030809361172c", tx.GetHash().ToString());
            Assert.Equal("e7ab8fb7c05097e88a29a816bdc75c958e58a8deee659c14baf9ffe51da96307", tx.Inputs[0].PrevOut.Hash.ToString());
            Assert.Contains(tx.Outputs, x => x.Value.Satoshi == 001000000L);
            Assert.Contains(tx.Outputs, x => x.Value.Satoshi == 269992303L);
        }

        [Fact]
        public void ShouldThrow_IfTransactionIsShielded()
        {
            Assert.Throws<NotSupportedException>(() => new ZcashTransaction(v2));
        }

        [Fact]
        public void ShouldSignV1()
        {
            var prevTx = Transaction.Parse("01000000018d600e5b601607b9ba7d788830ce442893ba091520d0ac9706b3f5bce0670696010000006a47304402202d83e5f388d44fbf3d93a55de8eac7744edb04bfc5845e2392d59c51e7d90a28022043a95b0088bb09022ee519e8421eb5c347f5d8ace24dd4242364605754f171a2012103372413cbd5741751044d91b0d225715e783a3fce9f51dc5491f938634783dff1ffffffff015c9ce111000000001976a9144faeeb51bcd0b49f238b323e5f1c6c8bf11ae02a88ac00000000");
            var from = "tmGygFvgg1B35XeX3oC4e78VSiAyRGcCgME";
            var fromAddress = new BitcoinPubKeyAddress(from);
            var fromPrivateKey = "cTD2Ew71UHXkn2XTJLyfu6Rbo1os5zCF9sKZm4oiXshcYo6YPcKY";
            var fromKey = Key.Parse(fromPrivateKey);
            var to = "tmLaY2Ceabpd9TgMmPv6zfDfVGEwmAWuPKo";
            var toAddress = new BitcoinPubKeyAddress(to);

            var hex = new TransactionBuilder()
                .AddCoins(prevTx.Outputs.AsCoins())
                .Send(toAddress, Money.Coins(0.5m))
                .SetChange(fromAddress)
                .SendFees(Money.Coins(0.00002380m))
                .BuildTransaction(false)
                .ToHex();

            var tx = new ZcashTransaction(hex);

            tx.Sign(new[] { fromKey }, prevTx.Outputs.AsCoins().ToArray());

            Assert.Equal(
                "c749dbf380e287fb84190b4d6695b82e7eb4f91a1f79c68dc0cfb539fbdd45c4",
                tx.GetHash().ToString());

            Assert.Equal(
                "01000000016cf8b84870d09e65f5809772d8d56e7ec69801291fe3ae7f8e6e6446e80886fd000000006b483045022100f5a4d723f4dbd4b5c2c3e603ed67f5a8798c7a264198cf7035eb88d7e92dce4102204237d7e5af9a2fd64545f6bc8fbad0039510dca378795299454dc0253cd46785012103f9e72f0713a4d4a980309a14a2ba563e0b1125ad067818e77553a1eefbfc5be7ffffffff0290a2e60e000000001976a9144faeeb51bcd0b49f238b323e5f1c6c8bf11ae02a88ac80f0fa02000000001976a914772efac94ff91e33c6b2540e4b539fbbcd9b0ebb88ac00000000",
                tx.ToHex());
        }

        [Fact]
        public void ShouldSignV3()
        {
            var tx = new ZcashTransaction("030000807082c40301ac3e4e9435a8369e049b47906ddaa09601cd7e7cfe2f229e0bd305202a066f8e0100000000ffffffff0280969800000000001976a91415b6246e9b88867cdc3e14b9a5085813ca6d8b4888acc9180202000000001976a9144faeeb51bcd0b49f238b323e5f1c6c8bf11ae02a88ac000000005782030000");

            var privateKeys = new Key[]
            {
                Key.Parse("cTD2Ew71UHXkn2XTJLyfu6Rbo1os5zCF9sKZm4oiXshcYo6YPcKY", ZcashNetworks.Instance.Testnet)
            };

            var coins = new Coin[]
            {
                new Coin(
                    new OutPoint(uint256.Parse("8e6f062a2005d30b9e222ffe7c7ecd0196a0da6d90479b049e36a835944e3eac"), 1),
                    new TxOut(Money.Coins(0.43695697m), privateKeys[0].ScriptPubKey))
            };

            tx.Sign(privateKeys, coins);

            var txHex = tx.ToHex();

            Assert.Equal(
                "030000807082c40301ac3e4e9435a8369e049b47906ddaa09601cd7e7cfe2f229e0bd305202a066f8e010000006b483045022100ee41236b2550aa334948e1b750b8f9dd12f30ea15b879fe0bf7f9a86989bef230220336f17006636afc4165bef0a5fc1e79a1204f21ec1f1ad71c3c326e4091bc7fa012103f9e72f0713a4d4a980309a14a2ba563e0b1125ad067818e77553a1eefbfc5be7ffffffff0280969800000000001976a91415b6246e9b88867cdc3e14b9a5085813ca6d8b4888acc9180202000000001976a9144faeeb51bcd0b49f238b323e5f1c6c8bf11ae02a88ac000000005782030000",
                tx.ToHex());
        }

        [Fact]
        public void ShouldSignV4()
        {
            var tx = new ZcashTransaction(v4);

            var privateKeys = new Key[]
            {
                Key.Parse("cVWdihupzUy3GyP5bha15Dk1W1ejbETBngMV51xATMJzr4Z6fnRk", ZcashNetworks.Instance.Testnet)
            };

            var coins = new Coin[]
            {
                new Coin(
                    new OutPoint(uint256.Parse("e7ab8fb7c05097e88a29a816bdc75c958e58a8deee659c14baf9ffe51da96307"), 1),
                    new TxOut(Money.Coins(2.70996151m), privateKeys[0].ScriptPubKey))
            };

            tx.Sign(privateKeys, coins);

            var hex = tx.ToHex();

            Assert.Equal(
                "0400008085202f89010763a91de5fff9ba149c65eedea8588e955cc7bd16a8298ae89750c0b78fabe7010000006a473044022050b543d7c4f6b5abc78421cb09651789225a1ddd93edabc2ab430a9b1e3f2ce7022066af0db26f7b2406de2bf4e48e3d5bfbeebc7b09d109bbd9819f29405025215901210250b36ab2839e868e6c12c9cb252c3d7b71b61a7039e3c6a55a53cac6f8a1c17bffffffff0240420f00000000001976a9141fecb553b1cff0364a7308ffd9ec8169495cf47288ac6fc11710000000001976a9147176f5d11e11c59a1248ef0bf0d6dadb2be1686188ac00000000c943ff020000000000000000000000",
                tx.ToHex());
        }

        [Theory]
        [MemberData(nameof(GetTestVectors))]
        public void ShouldSignTestVectors(string rawTransaction, string script, int vin, long hashType, uint branchId, string expected)
        {
            try
            {
                var tx = new ZcashTransaction(rawTransaction);

                var actualBytesReversed = tx.GetSignatureHash(new Script(script), vin, (SigHash)hashType, Money.Zero, HashVersion.Original, null)
                    .ToBytes()
                    .Reverse()
                    .ToArray();

                Assert.Equal(expected, new uint256(actualBytesReversed).ToString());
            }
            catch (NotSupportedException) 
            {
                Console.WriteLine("Skipped shielded tx test");
            }
        }

        public static IEnumerable<object[]> GetTestVectors()
        {
            var vectors = JsonConvert.DeserializeObject<object[][]>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../sighash1.json")));
            for (int i = 1; i < vectors.Length; i++)
            {
                if ((long)vectors[i][3] >= 0)
                {
                    yield return vectors[i];
                }
            }
        }
    }
}
