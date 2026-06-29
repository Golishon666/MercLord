using MercLord.Global.Cells;

namespace MercLord.Battle.Generation
{
    public sealed class BattleGenerationRequestFactory : IBattleGenerationRequestFactory
    {
        public BattleGenerationRequest Create(WorldCell sourceCell, int seed, bool nearSettlement)
        {
            return new BattleGenerationRequest
            {
                Seed = seed,
                SourceCellId = sourceCell.Id,
                Biome = sourceCell.Biome,
                DominantFactionId = sourceCell.DominantFactionId,
                HasRoad = sourceCell.HasRoad,
                NearSettlement = nearSettlement,
                Height = sourceCell.Height,
                Moisture = sourceCell.Moisture,
                Temperature = sourceCell.Temperature
            };
        }
    }
}
