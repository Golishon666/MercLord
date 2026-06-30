using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Battle.ECS.Components;
using MercLord.Game.Save;
using MercLord.Global.Cells;
using Scellecs.Morpeh;

namespace MercLord.Battle.Generation
{
    public interface IBattlePipeline
    {
        UniTask<BattleSession> StartBattleAsync(
            BattleGenerationRequest request,
            BattleArmyData attacker,
            BattleArmyData defender,
            CancellationToken cancellationToken);

        void StopBattle(BattleSession session);
    }

    public interface IBattleSessionService
    {
        BattleSession Current { get; }
        void SetCurrent(BattleSession session);
        BattleSession ConsumeCurrent();
        void Clear();
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

    public interface IBattleEntityFactory
    {
        Entity CreateSquad(World world, BattleSquadSpawnRequest request);
        Entity CreateUnit(World world, BattleEntitySpawnRequest request);
        Entity CreateVehicle(World world, BattleVehicleEntitySpawnRequest request);
        WeaponStatsComponent CreateWeaponStats(MercLord.Game.Configs.WeaponConfig weapon);
        ArmorStatsComponent CreateArmorStats(MercLord.Game.Configs.ArmorConfig armor);
        AIStatsComponent CreateAIStats(MercLord.Game.Configs.AIConfig ai);
    }

    public interface IBattlePlayerSpawner
    {
        Entity SpawnPlayer(BattleSession session);
    }

    public interface IBattleVehicleSpawner
    {
        void SpawnVehicles(BattleSession session);
    }

    public interface IBattleResultApplier
    {
        void Apply(SaveModel saveModel, BattleResult result);
    }

    public interface IBattleResultBuilder
    {
        BattleResult Build(BattleSession session, BattleOutcome outcome, int winnerFactionId);
    }
}
