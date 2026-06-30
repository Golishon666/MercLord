using UnityEngine;

namespace MercLord.Game.Configs
{
    [System.Serializable]
    public struct DamageFormula
    {
        public int MinimumDamage;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Combat Balance", fileName = "CombatBalanceConfig")]
    public sealed class CombatBalanceConfig : IdentifiedConfig
    {
        [SerializeField] private DamageFormula damageFormula;

        public DamageFormula DamageFormula => damageFormula;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Faction", fileName = "FactionConfig")]
    public sealed class FactionConfig : IdentifiedConfig
    {
        [SerializeField] private Color color;
        [SerializeField] private int startingCredits;
        [SerializeField] private int startingStrength;
        [SerializeField] private int capitalCellId = -1;

        public Color Color => color;
        public int StartingCredits => startingCredits;
        public int StartingStrength => startingStrength;
        public int CapitalCellId => capitalCellId;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Culture", fileName = "CultureConfig")]
    public sealed class CultureConfig : IdentifiedConfig
    {
        [SerializeField] private int startingCellId;
        [SerializeField] private int startingCredits;
        [SerializeField] private WeaponConfig startingWeapon;
        [SerializeField] private ArmorConfig startingArmor;

        public int StartingCellId => startingCellId;
        public int StartingCredits => startingCredits;
        public WeaponConfig StartingWeapon => startingWeapon;
        public ArmorConfig StartingArmor => startingArmor;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Unit", fileName = "UnitConfig")]
    public sealed class UnitConfig : IdentifiedConfig
    {
        [SerializeField] private int factionId;
        [SerializeField] private UnitCategory category;
        [SerializeField] private int maxHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private AIConfig ai;
        [SerializeField] private string viewPrefabAddress;

        public int FactionId => factionId;
        public UnitCategory Category => category;
        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public WeaponConfig Weapon => weapon;
        public ArmorConfig Armor => armor;
        public AIConfig AI => ai;
        public string ViewPrefabAddress => viewPrefabAddress;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Weapon", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : IdentifiedConfig
    {
        [SerializeField] private WeaponType type;
        [SerializeField] private DamageType damageType;
        [SerializeField] private int damage;
        [SerializeField] private float range;
        [SerializeField] private float cooldown;
        [SerializeField] private float projectileSpeed;
        [SerializeField] private bool isProjectile;
        [SerializeField] private bool usesParabolicTrajectory;
        [SerializeField] private float parabolicArcHeight;
        [SerializeField] private float explosionRadius;

        public WeaponType Type => type;
        public DamageType DamageType => damageType;
        public int Damage => damage;
        public float Range => range;
        public float Cooldown => cooldown;
        public float ProjectileSpeed => projectileSpeed;
        public bool IsProjectile => isProjectile;
        public bool UsesParabolicTrajectory => usesParabolicTrajectory;
        public float ParabolicArcHeight => parabolicArcHeight;
        public float ExplosionRadius => explosionRadius;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Armor", fileName = "ArmorConfig")]
    public sealed class ArmorConfig : IdentifiedConfig
    {
        [SerializeField] private int ballisticProtection;
        [SerializeField] private int energyProtection;
        [SerializeField] private int explosionProtection;

        public int BallisticProtection => ballisticProtection;
        public int EnergyProtection => energyProtection;
        public int ExplosionProtection => explosionProtection;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/AI", fileName = "AIConfig")]
    public sealed class AIConfig : IdentifiedConfig
    {
        [SerializeField] private AIType type;
        [SerializeField] private float thinkInterval;
        [SerializeField] private float targetSearchRadius;
        [SerializeField] private float preferredAttackDistance;
        [SerializeField] private float retreatHealthPercent;

        public AIType Type => type;
        public float ThinkInterval => thinkInterval;
        public float TargetSearchRadius => targetSearchRadius;
        public float PreferredAttackDistance => preferredAttackDistance;
        public float RetreatHealthPercent => retreatHealthPercent;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Vehicle", fileName = "VehicleConfig")]
    public sealed class VehicleConfig : IdentifiedConfig
    {
        [SerializeField] private int maxHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float enterRadius;
        [SerializeField] private float exitDistance;
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private string viewPrefabAddress;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public float EnterRadius => enterRadius;
        public float ExitDistance => exitDistance;
        public ArmorConfig Armor => armor;
        public WeaponConfig Weapon => weapon;
        public string ViewPrefabAddress => viewPrefabAddress;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Item", fileName = "ItemConfig")]
    public sealed class ItemConfig : IdentifiedConfig
    {
        [SerializeField] private ItemCategory category;
        [SerializeField] private int price;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private TradeGoodConfig tradeGood;

        public ItemCategory Category => category;
        public int Price => price;
        public WeaponConfig Weapon => weapon;
        public ArmorConfig Armor => armor;
        public TradeGoodConfig TradeGood => tradeGood;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Trade Good", fileName = "TradeGoodConfig")]
    public sealed class TradeGoodConfig : IdentifiedConfig
    {
        [SerializeField] private int basePrice;
        [SerializeField] private LootRarity rarity;
        [SerializeField] private string iconAddress;
        [SerializeField] private string description;

        public int BasePrice => basePrice;
        public LootRarity Rarity => rarity;
        public string IconAddress => iconAddress;
        public string Description => description;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Loot Table", fileName = "LootTableConfig")]
    public sealed class LootTableConfig : IdentifiedConfig
    {
        [SerializeField] private LootEntry[] entries = new LootEntry[0];

        public LootEntry[] Entries => entries;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Biome", fileName = "BiomeConfig")]
    public sealed class BiomeConfig : IdentifiedConfig
    {
        [SerializeField] private BiomeType biomeType;
        [SerializeField] private Color mapColor;
        [SerializeField] private int tileSetId;
        [SerializeField] private bool isPassableByDefault;

        public BiomeType BiomeType => biomeType;
        public Color MapColor => mapColor;
        public int TileSetId => tileSetId;
        public bool IsPassableByDefault => isPassableByDefault;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Tile Set", fileName = "TileSetConfig")]
    public sealed class TileSetConfig : IdentifiedConfig
    {
        [SerializeField] private BiomeType biomeType;

        public BiomeType BiomeType => biomeType;
    }

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

    [System.Serializable]
    public sealed class WorldTerrainGenerationSettings
    {
        public static readonly WorldTerrainGenerationSettings Default = new WorldTerrainGenerationSettings();

        [SerializeField] private int continentSalt = 11;
        [SerializeField] private int continentOctaves = 4;
        [SerializeField] private float continentFrequency = 2.15f;
        [SerializeField] private int detailSalt = 23;
        [SerializeField] private int detailOctaves = 4;
        [SerializeField] private float detailFrequency = 6.8f;
        [SerializeField] private int ridgeSalt = 37;
        [SerializeField] private int ridgeOctaves = 3;
        [SerializeField] private float ridgeFrequency = 9.4f;
        [SerializeField] private float continentWeight = 0.68f;
        [SerializeField] private float detailWeight = 0.22f;
        [SerializeField] private float ridgeWeight = 0.1f;
        [SerializeField] private float heightContrastPivot = 0.45f;
        [SerializeField] private float heightContrast = 1.45f;
        [SerializeField] private float heightOffset = 0.45f;
        [SerializeField] private int moistureSalt = 53;
        [SerializeField] private int moistureOctaves = 4;
        [SerializeField] private float moistureFrequency = 3.6f;
        [SerializeField] private float moistureNoiseWeight = 0.85f;
        [SerializeField] private float moistureLatitudeWeight = 0.08f;
        [SerializeField] private int climateLatitudeWarpSalt = 83;
        [SerializeField] private int climateLatitudeWarpOctaves = 2;
        [SerializeField] private float climateLatitudeWarpFrequency = 2.25f;
        [SerializeField] private float climateLatitudeWarpStrength = 0.24f;
        [SerializeField] private int temperatureSalt = 71;
        [SerializeField] private int temperatureOctaves = 2;
        [SerializeField] private float temperatureFrequency = 2.6f;
        [SerializeField] private float temperatureBase = 1f;
        [SerializeField] private float temperatureLatitudeWeight = 1.15f;
        [SerializeField] private float temperatureHeightWeight = 0.18f;
        [SerializeField] private float temperatureNoiseWeight = 0.22f;
        [SerializeField] private float targetWaterCoverage = 0.35f;
        [SerializeField] private float coastCoverage = 0.045f;
        [SerializeField] private float minWaterCoverage = 0.05f;
        [SerializeField] private float maxWaterCoverage = 0.85f;
        [SerializeField] private float oceanThreshold = 0.36f;
        [SerializeField] private float coastThreshold = 0.43f;
        [SerializeField] private float mountainHeightThreshold = 0.76f;
        [SerializeField] private float mountainRidgeMinHeight = 0.68f;
        [SerializeField] private float mountainRidgeThreshold = 0.74f;
        [SerializeField] private float impassableMountainThreshold = 0.84f;
        [SerializeField] private float snowTemperatureThreshold = 0.2f;
        [SerializeField] private float rustDesertMinHeight = 0.58f;
        [SerializeField] private float rustDesertMaxMoisture = 0.23f;
        [SerializeField] private float rustDesertMinTemperature = 0.56f;
        [SerializeField] private float rustDesertChanceThreshold = 0.75f;
        [SerializeField] private float ashWastesMinHeight = 0.58f;
        [SerializeField] private float ashWastesMaxMoisture = 0.18f;
        [SerializeField] private float ashWastesChanceThreshold = 0.84f;
        [SerializeField] private float swampMinMoisture = 0.76f;
        [SerializeField] private float swampMaxHeight = 0.53f;
        [SerializeField] private float toxicSwampMinTemperature = 0.34f;
        [SerializeField] private float toxicSwampChanceThreshold = 0.78f;
        [SerializeField] private float forestMinMoisture = 0.62f;
        [SerializeField] private float deadForestMaxTemperature = 0.25f;
        [SerializeField] private float deadForestChanceThreshold = 0.7f;
        [SerializeField] private float desertMaxMoisture = 0.28f;
        [SerializeField] private float desertMinTemperature = 0.4f;
        [SerializeField] private float industrialRuinsMinHeight = 0.62f;
        [SerializeField] private float industrialRuinsChanceThreshold = 0.86f;
        [SerializeField] private float demonScarMinHeight = 0.54f;
        [SerializeField] private float demonScarMaxMoisture = 0.33f;
        [SerializeField] private float demonScarChanceThreshold = 0.92f;

        public int ContinentSalt => continentSalt;
        public int ContinentOctaves => Positive(continentOctaves, Default.continentOctaves);
        public float ContinentFrequency => Positive(continentFrequency, Default.continentFrequency);
        public int DetailSalt => detailSalt;
        public int DetailOctaves => Positive(detailOctaves, Default.detailOctaves);
        public float DetailFrequency => Positive(detailFrequency, Default.detailFrequency);
        public int RidgeSalt => ridgeSalt;
        public int RidgeOctaves => Positive(ridgeOctaves, Default.ridgeOctaves);
        public float RidgeFrequency => Positive(ridgeFrequency, Default.ridgeFrequency);
        public float ContinentWeight => NonNegative(continentWeight);
        public float DetailWeight => NonNegative(detailWeight);
        public float RidgeWeight => NonNegative(ridgeWeight);
        public float HeightContrastPivot => heightContrastPivot;
        public float HeightContrast => Positive(heightContrast, Default.heightContrast);
        public float HeightOffset => heightOffset;
        public int MoistureSalt => moistureSalt;
        public int MoistureOctaves => Positive(moistureOctaves, Default.moistureOctaves);
        public float MoistureFrequency => Positive(moistureFrequency, Default.moistureFrequency);
        public float MoistureNoiseWeight => moistureNoiseWeight;
        public float MoistureLatitudeWeight => moistureLatitudeWeight;
        public int ClimateLatitudeWarpSalt => climateLatitudeWarpSalt;
        public int ClimateLatitudeWarpOctaves => Positive(climateLatitudeWarpOctaves, Default.climateLatitudeWarpOctaves);
        public float ClimateLatitudeWarpFrequency => Positive(climateLatitudeWarpFrequency, Default.climateLatitudeWarpFrequency);
        public float ClimateLatitudeWarpStrength => Clamp01(climateLatitudeWarpStrength);
        public int TemperatureSalt => temperatureSalt;
        public int TemperatureOctaves => Positive(temperatureOctaves, Default.temperatureOctaves);
        public float TemperatureFrequency => Positive(temperatureFrequency, Default.temperatureFrequency);
        public float TemperatureBase => temperatureBase;
        public float TemperatureLatitudeWeight => temperatureLatitudeWeight;
        public float TemperatureHeightWeight => temperatureHeightWeight;
        public float TemperatureNoiseWeight => temperatureNoiseWeight;
        public float TargetWaterCoverage => Clamp(TargetWaterCoverageRaw, MinWaterCoverage, MaxWaterCoverage);
        public float CoastCoverage => Clamp01(coastCoverage);
        public float MinWaterCoverage => Clamp01(minWaterCoverage);
        public float MaxWaterCoverage => Clamp(MaxWaterCoverageRaw, MinWaterCoverage, 0.95f);
        public float OceanThreshold => Clamp01(oceanThreshold);
        public float CoastThreshold => Clamp01(coastThreshold);
        public float MountainHeightThreshold => Clamp01(mountainHeightThreshold);
        public float MountainRidgeMinHeight => Clamp01(mountainRidgeMinHeight);
        public float MountainRidgeThreshold => Clamp01(mountainRidgeThreshold);
        public float ImpassableMountainThreshold => Clamp01(impassableMountainThreshold);
        public float SnowTemperatureThreshold => Clamp01(snowTemperatureThreshold);
        public float RustDesertMinHeight => Clamp01(rustDesertMinHeight);
        public float RustDesertMaxMoisture => Clamp01(rustDesertMaxMoisture);
        public float RustDesertMinTemperature => Clamp01(rustDesertMinTemperature);
        public float RustDesertChanceThreshold => Clamp01(rustDesertChanceThreshold);
        public float AshWastesMinHeight => Clamp01(ashWastesMinHeight);
        public float AshWastesMaxMoisture => Clamp01(ashWastesMaxMoisture);
        public float AshWastesChanceThreshold => Clamp01(ashWastesChanceThreshold);
        public float SwampMinMoisture => Clamp01(swampMinMoisture);
        public float SwampMaxHeight => Clamp01(swampMaxHeight);
        public float ToxicSwampMinTemperature => Clamp01(toxicSwampMinTemperature);
        public float ToxicSwampChanceThreshold => Clamp01(toxicSwampChanceThreshold);
        public float ForestMinMoisture => Clamp01(forestMinMoisture);
        public float DeadForestMaxTemperature => Clamp01(deadForestMaxTemperature);
        public float DeadForestChanceThreshold => Clamp01(deadForestChanceThreshold);
        public float DesertMaxMoisture => Clamp01(desertMaxMoisture);
        public float DesertMinTemperature => Clamp01(desertMinTemperature);
        public float IndustrialRuinsMinHeight => Clamp01(industrialRuinsMinHeight);
        public float IndustrialRuinsChanceThreshold => Clamp01(industrialRuinsChanceThreshold);
        public float DemonScarMinHeight => Clamp01(demonScarMinHeight);
        public float DemonScarMaxMoisture => Clamp01(demonScarMaxMoisture);
        public float DemonScarChanceThreshold => Clamp01(demonScarChanceThreshold);

        private static int Positive(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static float Positive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

        private static float NonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }

        private float TargetWaterCoverageRaw => Clamp01(targetWaterCoverage);

        private float MaxWaterCoverageRaw => Clamp01(maxWaterCoverage);

        private static float Clamp(float value, float min, float max)
        {
            if (max < min)
            {
                max = min;
            }

            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }

    [System.Serializable]
    public sealed class WorldNoiseSettings
    {
        public static readonly WorldNoiseSettings Default = new WorldNoiseSettings();

        [SerializeField] private float domainWarpFrequency = 1.7f;
        [SerializeField] private float domainWarpStrength = 0.38f;
        [SerializeField] private float octavePersistence = 0.5f;
        [SerializeField] private float octaveLacunarity = 2.03f;

        public float DomainWarpFrequency => domainWarpFrequency > 0f ? domainWarpFrequency : Default.domainWarpFrequency;
        public float DomainWarpStrength => domainWarpStrength;
        public float OctavePersistence => octavePersistence > 0f ? octavePersistence : Default.octavePersistence;
        public float OctaveLacunarity => octaveLacunarity > 0f ? octaveLacunarity : Default.octaveLacunarity;
    }

    [System.Serializable]
    public sealed class WorldRiverGenerationSettings
    {
        public static readonly WorldRiverGenerationSettings Default = new WorldRiverGenerationSettings();

        [SerializeField] private float sourceMinHeight = 0.68f;
        [SerializeField] private float sourceMinMoisture = 0.42f;
        [SerializeField] private int sourceNoiseSalt = 101;
        [SerializeField] private int sourceNoiseOctaves = 1;
        [SerializeField] private float sourceNoiseFrequency = 12f;
        [SerializeField] private float sourceNoiseWeight = 0.1f;
        [SerializeField] private int minRiverCount = 6;
        [SerializeField] private int riverCountDivisor = 120;
        [SerializeField] private int maxRiverCount = 140;
        [SerializeField] private int expectedEdgesPerRiver = 16;
        [SerializeField] private float sourceMinAngularDistance = 0.08f;
        [SerializeField] private int minRiverLength = 3;
        [SerializeField] private int maxTraceSteps = 80;
        [SerializeField] private int maxTraceCellDivisor = 4;
        [SerializeField] private int traceInitialCapacity = 32;
        [SerializeField] private float downhillMoistureWeight = 0.035f;
        [SerializeField] private int downhillNoiseSaltBase = 151;
        [SerializeField] private int downhillNoiseOctaves = 1;
        [SerializeField] private float downhillNoiseFrequency = 18f;
        [SerializeField] private float downhillNoiseWeight = 0.015f;
        [SerializeField] private float waterDestinationScoreBonus = 0.12f;
        [SerializeField] private float uphillStepPenalty = 18f;
        [SerializeField] private float downhillStepPenalty = 0.2f;
        [SerializeField] private float waterDistanceWeight = 0.018f;
        [SerializeField] private float existingRiverMergeBonus = 0.55f;
        [SerializeField] private float riverValleyMoistureBonus = 0.16f;
        [SerializeField] private float riverFlowMoistureBonusPerFlow = 0.01f;
        [SerializeField] private float riverFlowMoistureBonusMax = 0.12f;
        [SerializeField] private float nearWaterMoistureBonus = 0.06f;
        [SerializeField] private float mountainTracePenalty = 0.8f;
        [SerializeField] private float wetBiomeTraceDiscount = 0.12f;
        [SerializeField] private int mouthSearchCellDivisor = 3;
        [SerializeField] private int mouthSearchMinVisitedCells = 256;
        [SerializeField] private float flowBase = 1f;
        [SerializeField] private float flowPerStep = 0.05f;

        public float SourceMinHeight => Clamp01(sourceMinHeight);
        public float SourceMinMoisture => Clamp01(sourceMinMoisture);
        public int SourceNoiseSalt => sourceNoiseSalt;
        public int SourceNoiseOctaves => Positive(sourceNoiseOctaves, Default.sourceNoiseOctaves);
        public float SourceNoiseFrequency => Positive(sourceNoiseFrequency, Default.sourceNoiseFrequency);
        public float SourceNoiseWeight => sourceNoiseWeight;
        public int MinRiverCount => Positive(minRiverCount, Default.minRiverCount);
        public int RiverCountDivisor => Positive(riverCountDivisor, Default.riverCountDivisor);
        public int MaxRiverCount => Positive(maxRiverCount, Default.maxRiverCount);
        public int ExpectedEdgesPerRiver => Positive(expectedEdgesPerRiver, Default.expectedEdgesPerRiver);
        public float SourceMinAngularDistance => NonNegative(sourceMinAngularDistance);
        public int MinRiverLength => Positive(minRiverLength, Default.minRiverLength);
        public int MaxTraceSteps => Positive(maxTraceSteps, Default.maxTraceSteps);
        public int MaxTraceCellDivisor => Positive(maxTraceCellDivisor, Default.maxTraceCellDivisor);
        public int TraceInitialCapacity => Positive(traceInitialCapacity, Default.traceInitialCapacity);
        public float DownhillMoistureWeight => downhillMoistureWeight;
        public int DownhillNoiseSaltBase => downhillNoiseSaltBase;
        public int DownhillNoiseOctaves => Positive(downhillNoiseOctaves, Default.downhillNoiseOctaves);
        public float DownhillNoiseFrequency => Positive(downhillNoiseFrequency, Default.downhillNoiseFrequency);
        public float DownhillNoiseWeight => downhillNoiseWeight;
        public float WaterDestinationScoreBonus => waterDestinationScoreBonus;
        public float UphillStepPenalty => NonNegative(uphillStepPenalty);
        public float DownhillStepPenalty => NonNegative(downhillStepPenalty);
        public float WaterDistanceWeight => NonNegative(waterDistanceWeight);
        public float ExistingRiverMergeBonus => NonNegative(existingRiverMergeBonus);
        public float RiverValleyMoistureBonus => Clamp01(riverValleyMoistureBonus);
        public float RiverFlowMoistureBonusPerFlow => NonNegative(riverFlowMoistureBonusPerFlow);
        public float RiverFlowMoistureBonusMax => Clamp01(riverFlowMoistureBonusMax);
        public float NearWaterMoistureBonus => Clamp01(nearWaterMoistureBonus);
        public float MountainTracePenalty => NonNegative(mountainTracePenalty);
        public float WetBiomeTraceDiscount => NonNegative(wetBiomeTraceDiscount);
        public int MouthSearchCellDivisor => Positive(mouthSearchCellDivisor, Default.mouthSearchCellDivisor);
        public int MouthSearchMinVisitedCells => Positive(mouthSearchMinVisitedCells, Default.mouthSearchMinVisitedCells);
        public float FlowBase => flowBase;
        public float FlowPerStep => flowPerStep;

        private static int Positive(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static float Positive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

        private static float NonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }

    [System.Serializable]
    public sealed class WorldRoadGenerationSettings
    {
        public static readonly WorldRoadGenerationSettings Default = new WorldRoadGenerationSettings();

        [SerializeField] private int townMinCount = 8;
        [SerializeField] private int townCountDivisor = 120;
        [SerializeField] private int townMaxCount = 120;
        [SerializeField] private int activityMinCount = 24;
        [SerializeField] private int activityCountDivisor = 50;
        [SerializeField] private int activityMaxCount = 300;
        [SerializeField] private int townMinNetworkDistance = 2;
        [SerializeField] private int townMaxNetworkDistance = 9;
        [SerializeField] private int activityMinNetworkDistance = 1;
        [SerializeField] private int activityMaxNetworkDistance = 7;
        [SerializeField] private int graphDistanceReferenceCellCount = 1200;
        [SerializeField] private int angularDistanceReferenceCellCount = 1200;
        [SerializeField] private float satelliteMinAngularDistance = 0.045f;
        [SerializeField] private float satelliteMinimumScaledAngularDistance = 0.012f;
        [SerializeField] private float satelliteMaxHeight = 0.72f;
        [SerializeField] private float satellitePreferredTerrainScore = 1f;
        [SerializeField] private float satelliteFallbackTerrainScore = 0.35f;
        [SerializeField] private float satelliteDistanceScoreWeight = 1f;
        [SerializeField] private int satelliteNoiseSalt = 211;
        [SerializeField] private int satelliteNoiseOctaves = 2;
        [SerializeField] private float satelliteNoiseFrequency = 8f;
        [SerializeField] private float satelliteNoiseWeight = 0.45f;
        [SerializeField] private float satelliteRiverScoreBonus = 0.55f;
        [SerializeField] private float satelliteCoastScoreBonus = 0.35f;
        [SerializeField] private float satelliteNearWaterScoreBonus = 0.2f;
        [SerializeField] private float pathRiverPenalty = 4f;
        [SerializeField] private float pathMountainPenalty = 8f;
        [SerializeField] private float pathRiverValleyDiscount = 2f;
        [SerializeField] private float pathCoastDiscount = 1f;
        [SerializeField] private float pathUphillPenalty = 8f;
        [SerializeField] private int pathQueueMinCapacity = 16;
        [SerializeField] private int pathQueueCellDivisor = 4;

        public int TownMinCount => Positive(townMinCount, Default.townMinCount);
        public int TownCountDivisor => Positive(townCountDivisor, Default.townCountDivisor);
        public int TownMaxCount => Positive(townMaxCount, Default.townMaxCount);
        public int ActivityMinCount => Positive(activityMinCount, Default.activityMinCount);
        public int ActivityCountDivisor => Positive(activityCountDivisor, Default.activityCountDivisor);
        public int ActivityMaxCount => Positive(activityMaxCount, Default.activityMaxCount);
        public int TownMinNetworkDistance => Positive(townMinNetworkDistance, Default.townMinNetworkDistance);
        public int TownMaxNetworkDistance => Positive(townMaxNetworkDistance, Default.townMaxNetworkDistance);
        public int ActivityMinNetworkDistance => Positive(activityMinNetworkDistance, Default.activityMinNetworkDistance);
        public int ActivityMaxNetworkDistance => Positive(activityMaxNetworkDistance, Default.activityMaxNetworkDistance);
        public int GraphDistanceReferenceCellCount => Positive(graphDistanceReferenceCellCount, Default.graphDistanceReferenceCellCount);
        public int AngularDistanceReferenceCellCount => Positive(angularDistanceReferenceCellCount, Default.angularDistanceReferenceCellCount);
        public float SatelliteMinAngularDistance => NonNegative(satelliteMinAngularDistance);
        public float SatelliteMinimumScaledAngularDistance => NonNegative(satelliteMinimumScaledAngularDistance);
        public float SatelliteMaxHeight => Clamp01(satelliteMaxHeight);
        public float SatellitePreferredTerrainScore => satellitePreferredTerrainScore;
        public float SatelliteFallbackTerrainScore => satelliteFallbackTerrainScore;
        public float SatelliteDistanceScoreWeight => satelliteDistanceScoreWeight;
        public int SatelliteNoiseSalt => satelliteNoiseSalt;
        public int SatelliteNoiseOctaves => Positive(satelliteNoiseOctaves, Default.satelliteNoiseOctaves);
        public float SatelliteNoiseFrequency => Positive(satelliteNoiseFrequency, Default.satelliteNoiseFrequency);
        public float SatelliteNoiseWeight => satelliteNoiseWeight;
        public float SatelliteRiverScoreBonus => NonNegative(satelliteRiverScoreBonus);
        public float SatelliteCoastScoreBonus => NonNegative(satelliteCoastScoreBonus);
        public float SatelliteNearWaterScoreBonus => NonNegative(satelliteNearWaterScoreBonus);
        public float PathRiverPenalty => NonNegative(pathRiverPenalty);
        public float PathMountainPenalty => NonNegative(pathMountainPenalty);
        public float PathRiverValleyDiscount => NonNegative(pathRiverValleyDiscount);
        public float PathCoastDiscount => NonNegative(pathCoastDiscount);
        public float PathUphillPenalty => NonNegative(pathUphillPenalty);
        public int PathQueueMinCapacity => Positive(pathQueueMinCapacity, Default.pathQueueMinCapacity);
        public int PathQueueCellDivisor => Positive(pathQueueCellDivisor, Default.pathQueueCellDivisor);

        private static int Positive(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static float Positive(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

        private static float NonNegative(float value)
        {
            return value < 0f ? 0f : value;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }

    [System.Serializable]
    public sealed class WorldFactionRegionGenerationSettings
    {
        public static readonly WorldFactionRegionGenerationSettings Default = new WorldFactionRegionGenerationSettings();

        [SerializeField] private int fallbackFactionCount = 4;
        [SerializeField] private int fallbackFactionCredits = 1000;
        [SerializeField] private int fallbackFactionStrength = 100;
        [SerializeField] private float capitalMaxHeight = 0.74f;
        [SerializeField] private float capitalTargetHeight = 0.56f;
        [SerializeField] private float capitalHeightSuitabilityWeight = 0.05f;
        [SerializeField] private float capitalJitter = 0.002f;

        public int FallbackFactionCount => Positive(fallbackFactionCount, Default.fallbackFactionCount);
        public int FallbackFactionCredits => Positive(fallbackFactionCredits, Default.fallbackFactionCredits);
        public int FallbackFactionStrength => Positive(fallbackFactionStrength, Default.fallbackFactionStrength);
        public float CapitalMaxHeight => Clamp01(capitalMaxHeight);
        public float CapitalTargetHeight => Clamp01(capitalTargetHeight);
        public float CapitalHeightSuitabilityWeight => capitalHeightSuitabilityWeight;
        public float CapitalJitter => capitalJitter;

        private static int Positive(int value, int fallback)
        {
            return value > 0 ? value : fallback;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }

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

    [CreateAssetMenu(menuName = "MercLord/Configs/Battle Simulation", fileName = "BattleSimulationConfig")]
    public sealed class BattleSimulationConfig : IdentifiedConfig
    {
        [SerializeField] private float spatialHashCellSize;
        [SerializeField] private UnitConfig playerUnit;
        [SerializeField] private BattleSpawnSide playerSpawnSide;
        [SerializeField] private int playerSpawnPointIndex;
        [SerializeField] private float playerAimDotThreshold;
        [SerializeField] private BattleVehicleSpawnConfig[] vehicleSpawns = new BattleVehicleSpawnConfig[0];

        public float SpatialHashCellSize => spatialHashCellSize;
        public UnitConfig PlayerUnit => playerUnit;
        public BattleSpawnSide PlayerSpawnSide => playerSpawnSide;
        public int PlayerSpawnPointIndex => playerSpawnPointIndex;
        public float PlayerAimDotThreshold => playerAimDotThreshold;
        public BattleVehicleSpawnConfig[] VehicleSpawns => vehicleSpawns ?? System.Array.Empty<BattleVehicleSpawnConfig>();
    }

    [System.Serializable]
    public struct BattleVehicleSpawnConfig
    {
        [SerializeField] private VehicleConfig vehicle;
        [SerializeField] private BattleSpawnSide spawnSide;
        [SerializeField] private int spawnPointIndex;
        [SerializeField] private int factionId;
        [SerializeField] private VehicleSpawnControlMode controlMode;

        public VehicleConfig Vehicle => vehicle;
        public BattleSpawnSide SpawnSide => spawnSide;
        public int SpawnPointIndex => spawnPointIndex;
        public int FactionId => factionId;
        public VehicleSpawnControlMode ControlMode => controlMode;
    }

    [System.Serializable]
    public struct LootEntry
    {
        [SerializeField] private ItemConfig item;
        [SerializeField] private int minCount;
        [SerializeField] private int maxCount;
        [SerializeField] private float weight;

        public ItemConfig Item => item;
        public int MinCount => minCount;
        public int MaxCount => maxCount;
        public float Weight => weight;
    }
}
