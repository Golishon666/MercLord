using System;
using System.Collections.Generic;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using Scellecs.Morpeh;

namespace MercLord.Battle.ECS.Systems
{
    public interface IBattleAudioCuePlayer
    {
        void Tick(float deltaTime);
        void Play(BattleAudioCueComponent cue);
        void StopAll();
    }

    public sealed class SilentBattleAudioCuePlayer : IBattleAudioCuePlayer
    {
        public void Tick(float deltaTime)
        {
        }

        public void Play(BattleAudioCueComponent cue)
        {
        }

        public void StopAll()
        {
        }
    }

    public sealed class BattleAudioCueSystem : IBattleRuntimeSystem
    {
        public const int MaxCuesPerTick = 16;

        private readonly IBattleAudioCuePlayer cuePlayer;
        private readonly List<Entity> cueBuffer = new List<Entity>(MaxCuesPerTick * 2);

        private World world;
        private Filter filter;
        private Stash<BattleAudioCueComponent> cues;

        public BattleAudioCueSystem(IBattleAudioCuePlayer cuePlayer)
        {
            this.cuePlayer = cuePlayer ?? throw new ArgumentNullException(nameof(cuePlayer));
        }

        public void Initialize(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            world = session.World ?? throw new InvalidOperationException("BattleAudioCueSystem requires an active Morpeh world.");
            if (world.IsDisposed)
            {
                throw new InvalidOperationException("BattleAudioCueSystem cannot initialize on a disposed Morpeh world.");
            }

            filter = world.Filter
                .With<BattleAudioCueComponent>()
                .Build();
            cues = world.GetStash<BattleAudioCueComponent>();
        }

        public void Tick(float deltaTime)
        {
            if (world == null || world.IsDisposed || filter == null)
            {
                return;
            }

            cuePlayer.Tick(deltaTime);
            cueBuffer.Clear();
            foreach (var entity in filter)
            {
                cueBuffer.Add(entity);
            }

            var playedCount = 0;
            for (var cueIndex = 0; cueIndex < cueBuffer.Count; cueIndex++)
            {
                var entity = cueBuffer[cueIndex];
                if (playedCount < MaxCuesPerTick)
                {
                    cuePlayer.Play(cues.Get(entity));
                    playedCount++;
                }

                world.RemoveEntity(entity);
            }

            cueBuffer.Clear();
        }

        public void Dispose()
        {
            if (world != null && !world.IsDisposed && filter != null)
            {
                filter.Dispose();
            }

            cueBuffer.Clear();
            filter = null;
            world = null;
            cues = null;
            cuePlayer.StopAll();
        }
    }
}
