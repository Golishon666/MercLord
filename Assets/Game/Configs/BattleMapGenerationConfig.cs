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
    }
}
