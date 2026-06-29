using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.ECS.Components
{
    public enum BattleTeamType
    {
        Attacker,
        Defender
    }

    public enum BotStateType
    {
        Idle,
        Moving,
        Attacking,
        Retreating,
        Dead
    }

    public struct BotComponent : IComponent
    {
        public int UnitConfigId;
    }

    public struct PositionComponent : IComponent
    {
        public float2 Value;
    }

    public struct VelocityComponent : IComponent
    {
        public float2 Value;
    }

    public struct HealthComponent : IComponent
    {
        public int Current;
        public int Max;
    }

    public struct TeamComponent : IComponent
    {
        public BattleTeamType Value;
    }

    public struct FactionComponent : IComponent
    {
        public int Value;
    }

    public struct MovementStatsComponent : IComponent
    {
        public float MoveSpeed;
        public float RotationSpeed;
    }

    public struct WeaponStatsComponent : IComponent
    {
        public int WeaponConfigId;
        public WeaponType Type;
        public DamageType DamageType;
        public int Damage;
        public float Range;
        public float Cooldown;
        public float ProjectileSpeed;
        public bool IsProjectile;
        public bool UsesParabolicTrajectory;
        public float ParabolicArcHeight;
        public float ExplosionRadius;
    }

    public struct ArmorStatsComponent : IComponent
    {
        public int ArmorConfigId;
        public int BallisticProtection;
        public int EnergyProtection;
        public int ExplosionProtection;
    }

    public struct AIStatsComponent : IComponent
    {
        public int AIConfigId;
        public AIType Type;
        public float ThinkInterval;
        public float TargetSearchRadius;
        public float PreferredAttackDistance;
        public float RetreatHealthPercent;
    }

    public struct AIThinkTimerComponent : IComponent
    {
        public float TimeUntilNextThink;
    }

    public struct TargetComponent : IComponent
    {
        public Entity Target;
    }

    public struct AttackCooldownComponent : IComponent
    {
        public float Value;
    }

    public struct BotStateComponent : IComponent
    {
        public BotStateType Value;
    }

    public struct ViewRefComponent : IComponent
    {
        public int ViewId;
    }

    public struct PlayerControlledComponent : IComponent
    {
    }

    public struct PlayerInputComponent : IComponent
    {
        public float2 MoveDirection;
        public float2 AimDirection;
        public bool FirePressed;
        public bool InteractPressed;
        public int SelectedWeaponSlot;
    }

    public struct VehicleComponent : IComponent
    {
        public int VehicleConfigId;
        public Entity Driver;
    }

    public struct DriverComponent : IComponent
    {
        public Entity ControlledVehicle;
    }

    public struct ProjectileComponent : IComponent
    {
        public Entity Source;
        public int Damage;
        public DamageType DamageType;
        public float Speed;
    }

    public struct ParabolicProjectileComponent : IComponent
    {
        public float2 Start;
        public float2 Target;
        public float FlightTime;
        public float ElapsedTime;
        public float ArcHeight;
    }

    public struct ExplosionOnImpactComponent : IComponent
    {
        public float Radius;
    }

    public struct AttackRequestComponent : IComponent
    {
        public Entity Source;
        public Entity Target;
        public int WeaponConfigId;
    }

    public struct DamageRequestComponent : IComponent
    {
        public Entity Source;
        public Entity Target;
        public float2 HitPosition;
        public DamageType DamageType;
        public int Amount;
    }

    public struct DeadComponent : IComponent
    {
    }
}
