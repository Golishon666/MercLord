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
        [SerializeField] private float explosionRadius;

        public WeaponType Type => type;
        public DamageType DamageType => damageType;
        public int Damage => damage;
        public float Range => range;
        public float Cooldown => cooldown;
        public float ProjectileSpeed => projectileSpeed;
        public bool IsProjectile => isProjectile;
        public bool UsesParabolicTrajectory => usesParabolicTrajectory;
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
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private string viewPrefabAddress;

        public int MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float RotationSpeed => rotationSpeed;
        public ArmorConfig Armor => armor;
        public WeaponConfig Weapon => weapon;
        public string ViewPrefabAddress => viewPrefabAddress;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Item", fileName = "ItemConfig")]
    public sealed class ItemConfig : IdentifiedConfig
    {
        [SerializeField] private ItemCategory category;
        [SerializeField] private int price;

        public ItemCategory Category => category;
        public int Price => price;
    }

    [CreateAssetMenu(menuName = "MercLord/Configs/Trade Good", fileName = "TradeGoodConfig")]
    public sealed class TradeGoodConfig : IdentifiedConfig
    {
        [SerializeField] private int basePrice;

        public int BasePrice => basePrice;
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
        [SerializeField] private int seed;
        [SerializeField] private int targetCellCount;
        [SerializeField] private int startingDay;
        [SerializeField] private int playerStartCellId;
        [SerializeField] private int playerStartingCredits;
        [SerializeField] private float startingDominantInfluence;
        [SerializeField] private int roadStride;
        [SerializeField] private float defaultHeight;
        [SerializeField] private float defaultMoisture;
        [SerializeField] private float defaultTemperature;

        public int Seed => seed;
        public int TargetCellCount => targetCellCount;
        public int StartingDay => startingDay;
        public int PlayerStartCellId => playerStartCellId;
        public int PlayerStartingCredits => playerStartingCredits;
        public float StartingDominantInfluence => startingDominantInfluence;
        public int RoadStride => roadStride;
        public float DefaultHeight => defaultHeight;
        public float DefaultMoisture => defaultMoisture;
        public float DefaultTemperature => defaultTemperature;
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
