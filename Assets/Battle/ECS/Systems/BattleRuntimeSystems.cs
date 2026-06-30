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
            SpatialHashDebugViewSystem spatialHashDebugViewSystem,
            BattleLodSystem battleLodSystem,
            PlayerInputSystem playerInputSystem,
            PlayerSquadCommandSystem playerSquadCommandSystem,
            VehicleInputSystem vehicleInputSystem,
            VehicleExitSystem vehicleExitSystem,
            VehicleEnterSystem vehicleEnterSystem,
            VehicleAISystem vehicleAISystem,
            TargetSearchSystem targetSearchSystem,
            DecisionSystem decisionSystem,
            SquadMovementSystem squadMovementSystem,
            LocalSeparationSystem localSeparationSystem,
            WeaponSystem weaponSystem,
            BattleAudioCueSystem battleAudioCueSystem,
            ProjectileSystem projectileSystem,
            ParabolicProjectileSystem parabolicProjectileSystem,
            ProjectileViewSystem projectileViewSystem,
            ArtilleryWarningSystem artilleryWarningSystem,
            HitscanTraceSystem hitscanTraceSystem,
            MovementSystem movementSystem,
            DamageSystem damageSystem,
            BattleObjectiveSystem battleObjectiveSystem,
            SquadMoraleSystem squadMoraleSystem,
            BattleVictorySystem battleVictorySystem,
            ArtilleryWarningViewSystem artilleryWarningViewSystem,
            HitscanTraceViewSystem hitscanTraceViewSystem,
            ViewSyncSystem viewSyncSystem,
            BattleCameraFollowSystem battleCameraFollowSystem,
            BattleCameraShakeSystem battleCameraShakeSystem)
        {
            systems = new IBattleRuntimeSystem[]
            {
                spatialHashSystem,
                spatialHashDebugViewSystem,
                battleLodSystem,
                playerInputSystem,
                playerSquadCommandSystem,
                vehicleInputSystem,
                vehicleExitSystem,
                vehicleEnterSystem,
                vehicleAISystem,
                targetSearchSystem,
                decisionSystem,
                squadMovementSystem,
                localSeparationSystem,
                weaponSystem,
                battleAudioCueSystem,
                projectileSystem,
                parabolicProjectileSystem,
                projectileViewSystem,
                artilleryWarningSystem,
                hitscanTraceSystem,
                movementSystem,
                damageSystem,
                battleObjectiveSystem,
                squadMoraleSystem,
                battleVictorySystem,
                artilleryWarningViewSystem,
                hitscanTraceViewSystem,
                viewSyncSystem,
                battleCameraFollowSystem,
                battleCameraShakeSystem
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
