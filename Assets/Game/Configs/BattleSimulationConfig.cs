using UnityEngine;

namespace MercLord.Game.Configs
{
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
}
