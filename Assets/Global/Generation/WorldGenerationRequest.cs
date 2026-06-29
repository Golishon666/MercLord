namespace MercLord.Global.Generation
{
    public readonly struct WorldGenerationRequest
    {
        public WorldGenerationRequest(int seed, int targetCellCount)
        {
            Seed = seed;
            TargetCellCount = targetCellCount;
        }

        public int Seed { get; }
        public int TargetCellCount { get; }
    }
}
