using System;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Battle.Generation
{
    public sealed class ConfigDrivenBattleWorldFactory : IBattleWorldFactory
    {
        private readonly ConfigDatabase configDatabase;

        public ConfigDrivenBattleWorldFactory(ConfigDatabase configDatabase)
        {
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public World CreateWorld(BattleModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            ValidateModel(model);

            var world = World.Create();
            if (world == null)
            {
                throw new InvalidOperationException("Morpeh world could not be created.");
            }

            try
            {
                CreateArmy(world, model.Attacker, model.AttackerSpawnPoints, BattleTeamType.Attacker);
                CreateArmy(world, model.Defender, model.DefenderSpawnPoints, BattleTeamType.Defender);
                return world;
            }
            catch
            {
                DisposeWorld(world);
                throw;
            }
        }

        public void DisposeWorld(World world)
        {
            if (world == null || world.IsDisposed)
            {
                return;
            }

            world.Dispose();
        }

        private void CreateArmy(
            World world,
            BattleArmyData army,
            BattleSpawnPoint[] spawnPoints,
            BattleTeamType team)
        {
            if (army == null)
            {
                return;
            }

            ValidateArmy(army);

            var unitCount = CountUnits(army);
            if (unitCount == 0)
            {
                return;
            }

            if (spawnPoints == null || spawnPoints.Length < unitCount)
            {
                throw new InvalidOperationException(
                    $"{team} army requires {unitCount} spawn points, but battle map provides {spawnPoints?.Length ?? 0}.");
            }

            var spawnIndex = 0;
            for (var squadIndex = 0; squadIndex < army.Squads.Length; squadIndex++)
            {
                var squad = army.Squads[squadIndex];
                if (squad.Count == 0)
                {
                    continue;
                }

                var unitConfig = GetUnitConfig(squad.UnitConfigId);
                for (var unitIndex = 0; unitIndex < squad.Count; unitIndex++)
                {
                    CreateUnit(world, army.FactionId, team, unitConfig, spawnPoints[spawnIndex]);
                    spawnIndex++;
                }
            }
        }

        private void CreateUnit(
            World world,
            int factionId,
            BattleTeamType team,
            UnitConfig unitConfig,
            BattleSpawnPoint spawnPoint)
        {
            var entity = world.CreateEntity();

            world.GetStash<BotComponent>().Set(entity, new BotComponent
            {
                UnitConfigId = unitConfig.Id
            });

            world.GetStash<TeamComponent>().Set(entity, new TeamComponent
            {
                Value = team
            });

            world.GetStash<FactionComponent>().Set(entity, new FactionComponent
            {
                Value = factionId
            });

            world.GetStash<PositionComponent>().Set(entity, new PositionComponent
            {
                Value = new float2(spawnPoint.X, spawnPoint.Y)
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

            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent
            {
                Value = 0f
            });

            world.GetStash<BotStateComponent>().Set(entity, new BotStateComponent
            {
                Value = BotStateType.Idle
            });
        }

        private static WeaponStatsComponent CreateWeaponStats(WeaponConfig weapon)
        {
            if (weapon == null)
            {
                throw new InvalidOperationException("UnitConfig references a missing WeaponConfig.");
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
                ExplosionRadius = weapon.ExplosionRadius
            };
        }

        private static ArmorStatsComponent CreateArmorStats(ArmorConfig armor)
        {
            if (armor == null)
            {
                throw new InvalidOperationException("UnitConfig references a missing ArmorConfig.");
            }

            return new ArmorStatsComponent
            {
                ArmorConfigId = armor.Id,
                BallisticProtection = armor.BallisticProtection,
                EnergyProtection = armor.EnergyProtection,
                ExplosionProtection = armor.ExplosionProtection
            };
        }

        private static AIStatsComponent CreateAIStats(AIConfig ai)
        {
            if (ai == null)
            {
                throw new InvalidOperationException("UnitConfig references a missing AIConfig.");
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

        private UnitConfig GetUnitConfig(int unitConfigId)
        {
            if (!configDatabase.TryGetUnit(unitConfigId, out var unitConfig))
            {
                throw new InvalidOperationException($"UnitConfig id {unitConfigId} is not registered.");
            }

            return unitConfig;
        }

        private static void ValidateModel(BattleModel model)
        {
            if (model.Width <= 0 || model.Height <= 0)
            {
                throw new InvalidOperationException("BattleModel dimensions must be positive.");
            }

            if (model.Tiles == null || model.Tiles.Length != model.Width * model.Height)
            {
                throw new InvalidOperationException("BattleModel tiles must match map dimensions.");
            }
        }

        private void ValidateArmy(BattleArmyData army)
        {
            if (!configDatabase.TryGetFaction(army.FactionId, out _))
            {
                throw new InvalidOperationException($"FactionConfig id {army.FactionId} is not registered.");
            }

            if (army.Squads == null)
            {
                throw new InvalidOperationException("Battle army squads must not be null.");
            }

            for (var squadIndex = 0; squadIndex < army.Squads.Length; squadIndex++)
            {
                var squad = army.Squads[squadIndex];
                if (squad.Count < 0)
                {
                    throw new InvalidOperationException($"Squad {squadIndex} has negative unit count.");
                }

                if (squad.Count > 0)
                {
                    _ = GetUnitConfig(squad.UnitConfigId);
                }
            }
        }

        private static int CountUnits(BattleArmyData army)
        {
            var count = 0;
            for (var squadIndex = 0; squadIndex < army.Squads.Length; squadIndex++)
            {
                count += army.Squads[squadIndex].Count;
            }

            return count;
        }
    }
}
