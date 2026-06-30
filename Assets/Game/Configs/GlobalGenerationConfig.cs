using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Global Generation", fileName = "GlobalGenerationConfig")]
    public sealed class GlobalGenerationConfig : IdentifiedConfig
    {
        public const int DefaultSeed = 1001;
        public const int DefaultTargetCellCount = 100000;
        public const int MinimumTargetCellCount = 64;
        public const int DefaultStartingDay = 1;
        public const int DefaultPlayerStartingCredits = 500;
        public const float DefaultStartingDominantInfluence = 100f;
        public const float MinimumStartingDominantInfluence = 0.01f;

        [SerializeField] private int seed = DefaultSeed;
        [SerializeField] private int targetCellCount = DefaultTargetCellCount;
        [SerializeField] private int startingDay = DefaultStartingDay;
        [SerializeField] private int playerStartCellId;
        [SerializeField] private int playerStartingCredits = DefaultPlayerStartingCredits;
        [SerializeField] private float startingDominantInfluence = DefaultStartingDominantInfluence;
        [SerializeField] private int roadStride;
        [SerializeField] private float defaultHeight;
        [SerializeField] private float defaultMoisture;
        [SerializeField] private float defaultTemperature;
        [SerializeField] private WorldTerrainGenerationSettings terrain = new WorldTerrainGenerationSettings();
        [SerializeField] private WorldNoiseSettings noise = new WorldNoiseSettings();
        [SerializeField] private WorldRiverGenerationSettings rivers = new WorldRiverGenerationSettings();
        [SerializeField] private WorldRoadGenerationSettings roads = new WorldRoadGenerationSettings();
        [SerializeField] private WorldFactionRegionGenerationSettings factionRegions = new WorldFactionRegionGenerationSettings();

        public int Seed => seed;
        public int TargetCellCount => targetCellCount >= MinimumTargetCellCount ? targetCellCount : DefaultTargetCellCount;
        public int StartingDay => startingDay > 0 ? startingDay : DefaultStartingDay;
        public int PlayerStartCellId => playerStartCellId;
        public int PlayerStartingCredits => playerStartingCredits > 0 ? playerStartingCredits : DefaultPlayerStartingCredits;
        public float StartingDominantInfluence => startingDominantInfluence > 0f ? startingDominantInfluence : DefaultStartingDominantInfluence;
        public int RoadStride => roadStride;
        public float DefaultHeight => defaultHeight;
        public float DefaultMoisture => defaultMoisture;
        public float DefaultTemperature => defaultTemperature;
        public WorldTerrainGenerationSettings Terrain => terrain ?? WorldTerrainGenerationSettings.Default;
        public WorldNoiseSettings Noise => noise ?? WorldNoiseSettings.Default;
        public WorldRiverGenerationSettings Rivers => rivers ?? WorldRiverGenerationSettings.Default;
        public WorldRoadGenerationSettings Roads => roads ?? WorldRoadGenerationSettings.Default;
        public WorldFactionRegionGenerationSettings FactionRegions => factionRegions ?? WorldFactionRegionGenerationSettings.Default;
    }
}
