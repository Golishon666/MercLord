using MercLord.Battle.Generation;

namespace MercLord.Battle.ECS.Systems
{
    public interface IBattleRuntimeSystem
    {
        void Initialize(BattleSession session);
        void Tick(float deltaTime);
        void Dispose();
    }

    public interface IBattleSystemRunner
    {
        bool IsRunning { get; }
        void Start(BattleSession session);
        void Tick(float deltaTime);
        void Stop();
    }

    public sealed class BattleSystemRunner : IBattleSystemRunner
    {
        private readonly IBattleRuntimeSystem[] systems;

        public BattleSystemRunner(
            SpatialHashSystem spatialHashSystem,
            PlayerInputSystem playerInputSystem,
            VehicleInputSystem vehicleInputSystem,
            VehicleExitSystem vehicleExitSystem,
            VehicleEnterSystem vehicleEnterSystem,
            TargetSearchSystem targetSearchSystem,
            DecisionSystem decisionSystem,
            WeaponSystem weaponSystem,
            ProjectileSystem projectileSystem,
            ParabolicProjectileSystem parabolicProjectileSystem,
            MovementSystem movementSystem,
            DamageSystem damageSystem,
            ViewSyncSystem viewSyncSystem)
        {
            systems = new IBattleRuntimeSystem[]
            {
                spatialHashSystem,
                playerInputSystem,
                vehicleInputSystem,
                vehicleExitSystem,
                vehicleEnterSystem,
                targetSearchSystem,
                decisionSystem,
                weaponSystem,
                projectileSystem,
                parabolicProjectileSystem,
                movementSystem,
                damageSystem,
                viewSyncSystem
            };
        }

        public bool IsRunning { get; private set; }

        public void Start(BattleSession session)
        {
            if (IsRunning)
            {
                Stop();
            }

            for (var systemIndex = 0; systemIndex < systems.Length; systemIndex++)
            {
                systems[systemIndex].Initialize(session);
            }

            IsRunning = true;
            Tick(0f);
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            for (var systemIndex = 0; systemIndex < systems.Length; systemIndex++)
            {
                systems[systemIndex].Tick(deltaTime);
            }
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            for (var systemIndex = systems.Length - 1; systemIndex >= 0; systemIndex--)
            {
                systems[systemIndex].Dispose();
            }

            IsRunning = false;
        }
    }
}
