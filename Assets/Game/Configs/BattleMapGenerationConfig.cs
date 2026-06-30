using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Battle Map Generation", fileName = "BattleMapGenerationConfig")]
    public sealed class BattleMapGenerationConfig : IdentifiedConfig
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int defaultMoveCost;
        [SerializeField] private int roadMoveCost;
        [SerializeField] private int defaultCover;
        [SerializeField] private int settlementCover;
        [SerializeField] private int maxTileHeight;
        [SerializeField] private int roadColumn;
        [SerializeField] private int roadWidth;
        [SerializeField] private int attackerSpawnColumns;
        [SerializeField] private int defenderSpawnColumns;
        [SerializeField] private Vector2 unitSpawnOffset;
        [SerializeField] private float unitSpawnJitterRadius;
        [SerializeField] private int plainsCoverPatchCount;
        [SerializeField] private int plainsCoverPatchRadius;
        [SerializeField] private int forestCoverPatchCount;
        [SerializeField] private int forestCoverPatchRadius;
        [SerializeField] private int forestObstaclePatchCount;
        [SerializeField] private int forestObstaclePatchRadius;

        public int Width => width;
        public int Height => height;
        public int DefaultMoveCost => defaultMoveCost;
        public int RoadMoveCost => roadMoveCost;
        public int DefaultCover => defaultCover;
        public int SettlementCover => settlementCover;
        public int MaxTileHeight => maxTileHeight;
        public int RoadColumn => roadColumn;
        public int RoadWidth => roadWidth;
        public int AttackerSpawnColumns => attackerSpawnColumns;
        public int DefenderSpawnColumns => defenderSpawnColumns;
        public Vector2 UnitSpawnOffset => unitSpawnOffset;
        public float UnitSpawnJitterRadius => unitSpawnJitterRadius;
        public int PlainsCoverPatchCount => plainsCoverPatchCount;
        public int PlainsCoverPatchRadius => plainsCoverPatchRadius;
        public int ForestCoverPatchCount => forestCoverPatchCount;
        public int ForestCoverPatchRadius => forestCoverPatchRadius;
        public int ForestObstaclePatchCount => forestObstaclePatchCount;
        public int ForestObstaclePatchRadius => forestObstaclePatchRadius;
    }
}
