using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.UI;
using MercLord.Game.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace MercLord.Editor.Tests
{
    public sealed class BattlePlayerHudViewTests
    {
        [Test]
        public void RuntimeViewRendersPlayerCombatSnapshot()
        {
            var world = World.Create();
            var view = BattlePlayerHudView.CreateRuntime();

            try
            {
                var target = CreateTarget(world);
                CreatePlayer(world, target);
                world.Commit();

                view.Bind(CreateSession(world));

                StringAssert.Contains("Player 1001", view.TitleLabel.text);
                StringAssert.Contains("HP 30/50", view.HealthLabel.text);
                StringAssert.Contains("Slot 2", view.WeaponLabel.text);
                StringAssert.Contains("W77", view.WeaponLabel.text);
                StringAssert.Contains("Rifle", view.WeaponLabel.text);
                StringAssert.Contains("Cooldown", view.CooldownLabel.text);
                StringAssert.Contains("Armor B4 E2 X1", view.ArmorLabel.text);
                StringAssert.Contains("Target 3001", view.TargetLabel.text);
                StringAssert.Contains("HP 9/20", view.TargetLabel.text);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
                DisposeWorld(world);
            }
        }

        private static BattleSession CreateSession(World world)
        {
            return new BattleSession(
                new BattleGenerationRequest(),
                new BattleModel
                {
                    Width = 1,
                    Height = 1
                },
                world);
        }

        private static void CreatePlayer(World world, Entity target)
        {
            var entity = world.CreateEntity();
            world.GetStash<PlayerControlledComponent>().Set(entity, new PlayerControlledComponent());
            world.GetStash<BotComponent>().Set(entity, new BotComponent
            {
                UnitConfigId = 1001
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 30,
                Max = 50
            });
            world.GetStash<WeaponStatsComponent>().Set(entity, new WeaponStatsComponent
            {
                WeaponConfigId = 77,
                Type = WeaponType.AutomaticRifle,
                Cooldown = 1.5f
            });
            world.GetStash<AttackCooldownComponent>().Set(entity, new AttackCooldownComponent
            {
                Value = 0.5f
            });
            world.GetStash<ArmorStatsComponent>().Set(entity, new ArmorStatsComponent
            {
                BallisticProtection = 4,
                EnergyProtection = 2,
                ExplosionProtection = 1
            });
            world.GetStash<PlayerInputComponent>().Set(entity, new PlayerInputComponent
            {
                MoveDirection = float2.zero,
                AimDirection = new float2(1f, 0f),
                FirePressed = false,
                InteractPressed = false,
                SelectedWeaponSlot = 1
            });
            world.GetStash<TargetComponent>().Set(entity, new TargetComponent
            {
                Target = target
            });
        }

        private static Entity CreateTarget(World world)
        {
            var entity = world.CreateEntity();
            world.GetStash<BotComponent>().Set(entity, new BotComponent
            {
                UnitConfigId = 3001
            });
            world.GetStash<HealthComponent>().Set(entity, new HealthComponent
            {
                Current = 9,
                Max = 20
            });
            return entity;
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }
    }
}
