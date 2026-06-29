using System.Collections.Generic;
using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Config Database", fileName = "ConfigDatabase")]
    public sealed class ConfigDatabase : ScriptableObject
    {
        [SerializeField] private FactionConfig[] factions = new FactionConfig[0];
        [SerializeField] private CultureConfig[] cultures = new CultureConfig[0];
        [SerializeField] private UnitConfig[] units = new UnitConfig[0];
        [SerializeField] private WeaponConfig[] weapons = new WeaponConfig[0];
        [SerializeField] private ArmorConfig[] armors = new ArmorConfig[0];
        [SerializeField] private AIConfig[] aiConfigs = new AIConfig[0];
        [SerializeField] private VehicleConfig[] vehicles = new VehicleConfig[0];
        [SerializeField] private ItemConfig[] items = new ItemConfig[0];
        [SerializeField] private TradeGoodConfig[] tradeGoods = new TradeGoodConfig[0];
        [SerializeField] private LootTableConfig[] lootTables = new LootTableConfig[0];
        [SerializeField] private BiomeConfig[] biomes = new BiomeConfig[0];
        [SerializeField] private TileSetConfig[] tileSets = new TileSetConfig[0];
        [SerializeField] private CombatBalanceConfig combatBalance;
        [SerializeField] private GlobalGenerationConfig globalGeneration;
        [SerializeField] private BattleMapGenerationConfig battleMapGeneration;

        public IReadOnlyList<FactionConfig> Factions => factions;
        public IReadOnlyList<CultureConfig> Cultures => cultures;
        public IReadOnlyList<UnitConfig> Units => units;
        public IReadOnlyList<WeaponConfig> Weapons => weapons;
        public IReadOnlyList<ArmorConfig> Armors => armors;
        public IReadOnlyList<AIConfig> AIConfigs => aiConfigs;
        public IReadOnlyList<VehicleConfig> Vehicles => vehicles;
        public IReadOnlyList<ItemConfig> Items => items;
        public IReadOnlyList<TradeGoodConfig> TradeGoods => tradeGoods;
        public IReadOnlyList<LootTableConfig> LootTables => lootTables;
        public IReadOnlyList<BiomeConfig> Biomes => biomes;
        public IReadOnlyList<TileSetConfig> TileSets => tileSets;
        public CombatBalanceConfig CombatBalance => combatBalance;
        public GlobalGenerationConfig GlobalGeneration => globalGeneration;
        public BattleMapGenerationConfig BattleMapGeneration => battleMapGeneration;

        public bool TryGetFaction(int id, out FactionConfig config) => TryFind(factions, id, out config);
        public bool TryGetCulture(int id, out CultureConfig config) => TryFind(cultures, id, out config);
        public bool TryGetUnit(int id, out UnitConfig config) => TryFind(units, id, out config);
        public bool TryGetWeapon(int id, out WeaponConfig config) => TryFind(weapons, id, out config);
        public bool TryGetArmor(int id, out ArmorConfig config) => TryFind(armors, id, out config);
        public bool TryGetAI(int id, out AIConfig config) => TryFind(aiConfigs, id, out config);
        public bool TryGetVehicle(int id, out VehicleConfig config) => TryFind(vehicles, id, out config);
        public bool TryGetItem(int id, out ItemConfig config) => TryFind(items, id, out config);
        public bool TryGetTradeGood(int id, out TradeGoodConfig config) => TryFind(tradeGoods, id, out config);
        public bool TryGetLootTable(int id, out LootTableConfig config) => TryFind(lootTables, id, out config);
        public bool TryGetBiome(int id, out BiomeConfig config) => TryFind(biomes, id, out config);
        public bool TryGetTileSet(int id, out TileSetConfig config) => TryFind(tileSets, id, out config);

        private static bool TryFind<T>(IReadOnlyList<T> configs, int id, out T config)
            where T : class, IIdentifiedConfig
        {
            for (var i = 0; i < configs.Count; i++)
            {
                var candidate = configs[i];
                if (candidate != null && candidate.Id == id)
                {
                    config = candidate;
                    return true;
                }
            }

            config = null;
            return false;
        }
    }
}
