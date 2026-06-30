using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public readonly struct BattleEntitySpawnRequest
    {
        public BattleEntitySpawnRequest(
            UnitConfig unitConfig,
            int factionId,
            BattleTeamType team,
            float2 position,
            bool playerControlled,
            int spawnIndex,
            int unitCount)
        {
            UnitConfig = unitConfig;
            FactionId = factionId;
            Team = team;
            Position = position;
            PlayerControlled = playerControlled;
            SpawnIndex = spawnIndex;
            UnitCount = unitCount;
        }

        public UnitConfig UnitConfig { get; }
        public int FactionId { get; }
        public BattleTeamType Team { get; }
        public float2 Position { get; }
        public bool PlayerControlled { get; }
        public int SpawnIndex { get; }
        public int UnitCount { get; }
    }

    public readonly struct BattleVehicleEntitySpawnRequest
    {
        public BattleVehicleEntitySpawnRequest(
            VehicleConfig vehicleConfig,
            int factionId,
            BattleTeamType team,
            float2 position,
            VehicleStateType state)
        {
            VehicleConfig = vehicleConfig;
            FactionId = factionId;
            Team = team;
            Position = position;
            State = state;
        }

        public VehicleConfig VehicleConfig { get; }
        public int FactionId { get; }
        public BattleTeamType Team { get; }
        public float2 Position { get; }
        public VehicleStateType State { get; }
    }

    public sealed class BattleEntityFactory : IBattleEntityFactory
    {
        public Entity CreateUnit(World world, BattleEntitySpawnRequest request)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.IsDisposed)
            {
                throw new InvalidOperationException("Cannot create a battle entity in a disposed Morpeh world.");
            }

            var unitConfig = request.UnitConfig
                ?? throw new InvalidOperationException("Battle entity spawn request requires UnitConfig.");

            var entity = world.CreateEntity();

            world.GetStash<BotComponent>().Set(entity, new BotComponent
            {
                UnitConfigId = unitConfig.Id
            });

            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = request.Team
            });

            world.GetStash<FactionComponent>().Set(entity, new FactionComponent
            {
                Value = request.FactionId
            });

            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = request.Position
            });

            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
            {
                Value = float2.zero
            });

            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = unitConfig.MaxHealth,
                Max = unitConfig.MaxHealth
            });

            world.GetStash<MovementStatsComponent>().Set(entity, new MovementStatsComponent
            {
                MoveSpeed = unitConfig.MoveSpeed,
                RotationSpeed = unitConfig.RotationSpeed
            });

            world.GetStash<WeaponStatsComponent>().Set(entity, CreateWeaponStats(unitConfig.Weapon));
            world.GetStash<ArmorStatsComponent>().Set(entity, CreateArmorStats(unitConfig.Armor));
            world.GetStash<AIStatsComponent>().Set(entity, CreateAIStats(unitConfig.AI));
            world.GetStash<AIThinkTimerComponent>().Set(
                entity,
                CreateAIThinkTimer(unitConfig.AI, request.SpawnIndex, request.UnitCount));

            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent
            {
                Value = 0f
            });

            world.GetStash<BotStateComponent>().Set(entity, new BotStateComponent
            {
                Value = BotStateType.Idle
            });

            if (request.PlayerControlled)
            {
                world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
                world.GetStash<PlayerInputComponent>().Set(entity, new PlayerInputComponent
                {
                    MoveDirection = float2.zero,
                    AimDirection = float2.zero,
                    FirePressed = false,
                    InteractPressed = false,
                    SelectedWeaponSlot = 0
                });
            }

            return entity;
        }

        public Entity CreateVehicle(World world, BattleVehicleEntitySpawnRequest request)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.IsDisposed)
            {
                throw new InvalidOperationException("Cannot create a vehicle entity in a disposed Morpeh world.");
            }

            var vehicleConfig = request.VehicleConfig
                ?? throw new InvalidOperationException("Vehicle spawn request requires VehicleConfig.");

            var entity = world.CreateEntity();

            world.GetStash<VehicleComponent>().Set(entity, new VehicleComponent
            {
                VehicleConfigId = vehicleConfig.Id,
                Driver = default,
                State = request.State
            });

            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = request.Team
            });

            world.GetStash<FactionComponent>().Set(entity, new FactionComponent
            {
                Value = request.FactionId
            });

            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = request.Position
            });

            world.GetStash<VelocityComponent>().Set(entity, new VelocityComponent
            {
                Value = float2.zero
            });

            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = vehicleConfig.MaxHealth,
                Max = vehicleConfig.MaxHealth
            });

            world.GetStash<MovementStatsComponent>().Set(entity, new MovementStatsComponent
            {
                MoveSpeed = vehicleConfig.MoveSpeed,
                RotationSpeed = vehicleConfig.RotationSpeed
            });

            world.GetStash<WeaponStatsComponent>().Set(entity, CreateWeaponStats(vehicleConfig.Weapon));
            world.GetStash<ArmorStatsComponent>().Set(entity, CreateArmorStats(vehicleConfig.Armor));
            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent
            {
                Value = 0f
            });

            return entity;
        }

        public WeaponStatsComponent CreateWeaponStats(WeaponConfig weapon)
        {
            if (weapon == null)
            {
                throw new InvalidOperationException("Battle entity references a missing WeaponConfig.");
            }

            return new WeaponStatsComponent
            {
                WeaponConfigId = weapon.Id,
                Type = weapon.Type,
                DamageType = weapon.DamageType,
                Damage = weapon.Damage,
                Range = weapon.Range,
                Cooldown = weapon.Cooldown,
                ProjectileSpeed = weapon.ProjectileSpeed,
                IsProjectile = weapon.IsProjectile,
                UsesParabolicTrajectory = weapon.UsesParabolicTrajectory,
                ParabolicArcHeight = weapon.ParabolicArcHeight,
                ExplosionRadius = weapon.ExplosionRadius
            };
        }

        public ArmorStatsComponent CreateArmorStats(ArmorConfig armor)
        {
            if (armor == null)
            {
                throw new InvalidOperationException("Battle entity references a missing ArmorConfig.");
            }

            return new ArmorStatsComponent
            {
                ArmorConfigId = armor.Id,
                BallisticProtection = armor.BallisticProtection,
                EnergyProtection = armor.EnergyProtection,
                ExplosionProtection = armor.ExplosionProtection
            };
        }

        public AIStatsComponent CreateAIStats(AIConfig ai)
        {
            if (ai == null)
            {
                throw new InvalidOperationException("Battle entity references a missing AIConfig.");
            }

            return new AIStatsComponent
            {
                AIConfigId = ai.Id,
                Type = ai.Type,
                ThinkInterval = ai.ThinkInterval,
                TargetSearchRadius = ai.TargetSearchRadius,
                PreferredAttackDistance = ai.PreferredAttackDistance,
                RetreatHealthPercent = ai.RetreatHealthPercent
            };
        }

        private static AIThinkTimerComponent CreateAIThinkTimer(AIConfig ai, int spawnIndex, int unitCount)
        {
            if (ai == null)
            {
                throw new InvalidOperationException("Battle entity references a missing AIConfig.");
            }

            var normalizedOffset = unitCount > 0
                ? (float)spawnIndex / unitCount
                : 0f;
            return new AIThinkTimerComponent
            {
                TimeUntilNextThink = ai.ThinkInterval * normalizedOffset
            };
        }
    }
}
