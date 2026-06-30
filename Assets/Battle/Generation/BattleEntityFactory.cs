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
            int unitCount,
            int squadId = -1,
            int squadSlotIndex = -1,
            int squadSize = 0,
            float2 formationLocalOffset = default)
        {
            UnitConfig = unitConfig;
            FactionId = factionId;
            Team = team;
            Position = position;
            PlayerControlled = playerControlled;
            SpawnIndex = spawnIndex;
            UnitCount = unitCount;
            SquadId = squadId;
            SquadSlotIndex = squadSlotIndex;
            SquadSize = squadSize;
            FormationLocalOffset = formationLocalOffset;
        }

        public UnitConfig UnitConfig { get; }
        public int FactionId { get; }
        public BattleTeamType Team { get; }
        public float2 Position { get; }
        public bool PlayerControlled { get; }
        public int SpawnIndex { get; }
        public int UnitCount { get; }
        public int SquadId { get; }
        public int SquadSlotIndex { get; }
        public int SquadSize { get; }
        public float2 FormationLocalOffset { get; }
    }

    public readonly struct BattleSquadSpawnRequest
    {
        public BattleSquadSpawnRequest(
            int squadId,
            int unitConfigId,
            int factionId,
            BattleTeamType team,
            int memberCount,
            float2 anchorPosition,
            float2 forwardDirection,
            SquadOrderType order,
            float2 targetPosition)
        {
            SquadId = squadId;
            UnitConfigId = unitConfigId;
            FactionId = factionId;
            Team = team;
            MemberCount = memberCount;
            AnchorPosition = anchorPosition;
            ForwardDirection = forwardDirection;
            Order = order;
            TargetPosition = targetPosition;
        }

        public int SquadId { get; }
        public int UnitConfigId { get; }
        public int FactionId { get; }
        public BattleTeamType Team { get; }
        public int MemberCount { get; }
        public float2 AnchorPosition { get; }
        public float2 ForwardDirection { get; }
        public SquadOrderType Order { get; }
        public float2 TargetPosition { get; }
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
        public Entity CreateSquad(World world, BattleSquadSpawnRequest request)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (world.IsDisposed)
            {
                throw new InvalidOperationException("Cannot create a battle squad entity in a disposed Morpeh world.");
            }

            if (request.SquadId < 0)
            {
                throw new InvalidOperationException("Battle squad spawn request requires a non-negative squad id.");
            }

            if (request.MemberCount <= 0)
            {
                throw new InvalidOperationException("Battle squad spawn request requires a positive member count.");
            }

            var entity = world.CreateEntity();
            world.GetStash<SquadComponent>().Set(entity, new SquadComponent
            {
                SquadId = request.SquadId,
                UnitConfigId = request.UnitConfigId,
                FactionId = request.FactionId,
                Team = request.Team,
                MemberCount = request.MemberCount
            });
            world.GetStash<SquadAnchorComponent>().Set(entity, new SquadAnchorComponent
            {
                Position = request.AnchorPosition,
                ForwardDirection = math.normalizesafe(request.ForwardDirection, new float2(1f, 0f))
            });
            world.GetStash<SquadOrderComponent>().Set(entity, new SquadOrderComponent
            {
                Value = request.Order,
                TargetPosition = request.TargetPosition
            });
            world.GetStash<SquadMoraleComponent>().Set(entity, new SquadMoraleComponent
            {
                Current = 100f,
                Max = 100f,
                RoutThreshold = 35f,
                IsRouted = false
            });

            return entity;
        }

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

            if (request.SquadId >= 0)
            {
                if (request.SquadSize <= 0 ||
                    request.SquadSlotIndex < 0 ||
                    request.SquadSlotIndex >= request.SquadSize)
                {
                    throw new InvalidOperationException("Battle entity squad membership requires a valid slot index and squad size.");
                }

                world.GetStash<SquadMemberComponent>().Set(entity, new SquadMemberComponent
                {
                    SquadId = request.SquadId,
                    SlotIndex = request.SquadSlotIndex,
                    SquadSize = request.SquadSize
                });
                world.GetStash<FormationSlotComponent>().Set(entity, new FormationSlotComponent
                {
                    LocalOffset = request.FormationLocalOffset
                });
            }

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
