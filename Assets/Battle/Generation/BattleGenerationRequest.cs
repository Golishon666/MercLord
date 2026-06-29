using System;
using MercLord.Game.Configs;

namespace MercLord.Battle.Generation
{
    [Serializable]
    public struct BattleGenerationRequest
    {
        public int Seed;
        public int SourceCellId;
        public BiomeType Biome;
        public int DominantFactionId;
        public bool HasRoad;
        public bool NearSettlement;
        public float Height;
        public float Moisture;
        public float Temperature;
    }
}
