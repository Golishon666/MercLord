using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MercLord.Battle.Generation
{
    public sealed class BattlePipeline : IBattlePipeline
    {
        private readonly IBattleMapGenerator mapGenerator;
        private readonly IBattleWorldFactory worldFactory;

        public BattlePipeline(
            IBattleMapGenerator mapGenerator,
            IBattleWorldFactory worldFactory)
        {
            this.mapGenerator = mapGenerator ?? throw new ArgumentNullException(nameof(mapGenerator));
            this.worldFactory = worldFactory ?? throw new ArgumentNullException(nameof(worldFactory));
        }

        public UniTask<BattleSession> StartBattleAsync(
            BattleGenerationRequest request,
            BattleArmyData attacker,
            BattleArmyData defender,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var model = mapGenerator.Generate(request, attacker, defender);
            cancellationToken.ThrowIfCancellationRequested();

            var world = worldFactory.CreateWorld(model);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(new BattleSession(request, model, world));
            }
            catch
            {
                worldFactory.DisposeWorld(world);
                throw;
            }
        }

        public void StopBattle(BattleSession session)
        {
            if (session == null)
            {
                return;
            }

            worldFactory.DisposeWorld(session.World);
        }
    }
}
