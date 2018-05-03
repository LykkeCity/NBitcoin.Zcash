namespace NBitcoin.Zcash
{
    public class ZcashConsensusFactory : ConsensusFactory
    {
        private ZcashConsensusFactory()
        {
        }

        public static ZcashConsensusFactory Instance { get; } = new ZcashConsensusFactory();

        public override Transaction CreateTransaction()
        {
            return new ZcashTransaction();
        }
    }
}
