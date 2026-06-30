using MercLord.Game.Configs;

namespace MercLord.Global.Cells
{
    public static class WorldMovementCosts
    {
        public const int ImpassableCost = 1000000;

        public static int Calculate(BiomeType biome, RoadType roadType, bool hasRiver)
        {
            if (biome == BiomeType.Ocean)
            {
                return ImpassableCost;
            }

            var baseCost = GetBiomeCost(biome) + (hasRiver ? 5 : 0);
            if (roadType == RoadType.None)
            {
                return baseCost;
            }

            var roadCost = GetRoadCost(roadType) + (hasRiver ? 1 : 0);
            return roadCost < baseCost ? roadCost : baseCost;
        }

        public static int GetBiomeCost(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Plains:
                case BiomeType.Coast:
                    return 10;
                case BiomeType.Forest:
                case BiomeType.DeadForest:
                    return 15;
                case BiomeType.Desert:
                case BiomeType.RustDesert:
                case BiomeType.Snow:
                case BiomeType.AshWastes:
                case BiomeType.IndustrialRuins:
                    return 18;
                case BiomeType.Swamp:
                case BiomeType.ToxicSwamp:
                case BiomeType.DemonScar:
                    return 22;
                case BiomeType.Mountains:
                    return 28;
                case BiomeType.Ocean:
                    return ImpassableCost;
                default:
                    return 14;
            }
        }

        public static int GetRoadCost(RoadType roadType)
        {
            switch (roadType)
            {
                case RoadType.Large:
                    return 4;
                case RoadType.Medium:
                    return 6;
                case RoadType.Small:
                    return 8;
                default:
                    return int.MaxValue;
            }
        }
    }
}
