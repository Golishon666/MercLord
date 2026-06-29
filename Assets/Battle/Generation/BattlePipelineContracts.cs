using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Save;
using MercLord.Global.Cells;
using Scellecs.Morpeh;

namespace MercLord.Battle.Generation
{
    public interface IBattlePipeline
    {
        UniTask<BattleResult> RunBattleAsync(
            BattleGenerationRequest request,
            BattleArmyData attacker,
            BattleArmyData defender,
            CancellationToken cancellationToken);
    }

    public interface IBattleGenerationRequestFactory
    {
        BattleGenerationRequest Create(WorldCell sourceCell, int seed, bool nearSettlement);
    }

    public interface IBattleMapGenerator
    {
        BattleModel Generate(BattleGenerationRequest request, BattleArmyData attacker, BattleArmyData defender);
    }

    public interface IBattleWorldFactory
    {
        World CreateWorld(BattleModel model);
        void DisposeWorld(World world);
    }

    public interface IBattleResultApplier
    {
        void Apply(SaveModel saveModel, BattleResult result);
    }
}
