using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.ECS.Systems;
using MercLord.Battle.Generation;
using NUnit.Framework;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace MercLord.Editor.Tests
{
    public sealed class BattleAudioCueSystemTests
    {
        [Test]
        public void BattleAudioCueSystemPlaysOnlyBudgetedCuesAndRemovesAll()
        {
            var world = World.Create();
            var player = new FakeAudioCuePlayer();
            var system = new BattleAudioCueSystem(player);

            try
            {
                var cueCount = BattleAudioCueSystem.MaxCuesPerTick + 3;
                for (var cueIndex = 0; cueIndex < cueCount; cueIndex++)
                {
                    CreateCue(world, cueIndex);
                }

                world.Commit();

                system.Initialize(CreateSession(world));
                system.Tick(0f);
                world.Commit();

                Assert.AreEqual(BattleAudioCueSystem.MaxCuesPerTick, player.Played.Count);
                Assert.AreEqual(1, player.TickCount);
                Assert.AreEqual(0, CountCues(world));
            }
            finally
            {
                system.Dispose();
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

        private static void CreateCue(World world, int index)
        {
            var entity = world.CreateEntity();
            world.GetStash<BattleAudioCueComponent>().Set(entity, new BattleAudioCueComponent
            {
                Type = BattleAudioCueType.HitscanShot,
                Position = new float2(index, 0f),
                Volume = 1f,
                Pitch = 1f
            });
        }

        private static int CountCues(World world)
        {
            var filter = world.Filter
                .With<BattleAudioCueComponent>()
                .Build();
            var count = 0;
            foreach (var entity in filter)
            {
                count++;
            }

            filter.Dispose();
            return count;
        }

        private static void DisposeWorld(World world)
        {
            if (world != null && !world.IsDisposed)
            {
                world.Dispose();
            }
        }

        private sealed class FakeAudioCuePlayer : IBattleAudioCuePlayer
        {
            public List<BattleAudioCueComponent> Played { get; } = new List<BattleAudioCueComponent>();
            public int TickCount { get; private set; }
            public int StopCount { get; private set; }

            public void Tick(float deltaTime)
            {
                TickCount++;
            }

            public void Play(BattleAudioCueComponent cue)
            {
                Played.Add(cue);
            }

            public void StopAll()
            {
                StopCount++;
            }
        }
    }
}
