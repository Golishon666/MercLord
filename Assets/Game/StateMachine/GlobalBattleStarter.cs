using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MercLord.Game.Configs;
using MercLord.Game.Save;
using MercLord.Global.Cells;

namespace MercLord.Game.StateMachine
{
    public readonly struct GlobalBattleEncounter
    {
        public GlobalBattleEncounter(
            int cellId,
            int opponentArmyId,
            int playerFactionId,
            int opponentFactionId,
            int playerUnitCount,
            int opponentUnitCount)
        {
            CellId = cellId;
            OpponentArmyId = opponentArmyId;
            PlayerFactionId = playerFactionId;
            OpponentFactionId = opponentFactionId;
            PlayerUnitCount = playerUnitCount;
            OpponentUnitCount = opponentUnitCount;
        }

        public int CellId { get; }
        public int OpponentArmyId { get; }
        public int PlayerFactionId { get; }
        public int OpponentFactionId { get; }
        public int PlayerUnitCount { get; }
        public int OpponentUnitCount { get; }
    }

    public interface IGlobalBattleStarter
    {
        bool TransitionRequested { get; }
        bool TryGetPlayerBattleEncounter(int cellId, out GlobalBattleEncounter encounter);
        bool TryFindPlayerBattleOpponent(int cellId, out int opponentArmyId);
        UniTask<bool> TryStartPlayerBattleInCellAsync(
            int cellId,
            bool playerIsAttacker = true,
            CancellationToken cancellationToken = default);

        UniTask<bool> TryStartPlayerBattleAsync(
            int opponentArmyId,
            bool playerIsAttacker = true,
            CancellationToken cancellationToken = default);
    }

    public sealed class GlobalBattleStarter : IGlobalBattleStarter
    {
        private readonly ISaveService saveService;
        private readonly IBattleEncounterRequestFactory encounterRequestFactory;
        private readonly IGameStateMachine stateMachine;
        private readonly ConfigDatabase configDatabase;

        public GlobalBattleStarter(
            ISaveService saveService,
            IBattleEncounterRequestFactory encounterRequestFactory,
            IGameStateMachine stateMachine,
            ConfigDatabase configDatabase)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.encounterRequestFactory = encounterRequestFactory ?? throw new ArgumentNullException(nameof(encounterRequestFactory));
            this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            this.configDatabase = configDatabase ?? throw new ArgumentNullException(nameof(configDatabase));
        }

        public bool TransitionRequested { get; private set; }

        public bool TryFindPlayerBattleOpponent(int cellId, out int opponentArmyId)
        {
            if (TryGetPlayerBattleEncounter(cellId, out var encounter))
            {
                opponentArmyId = encounter.OpponentArmyId;
                return true;
            }

            opponentArmyId = WorldIds.None;
            return false;
        }

        public bool TryGetPlayerBattleEncounter(int cellId, out GlobalBattleEncounter encounter)
        {
            encounter = default;
            if (!TryGetWorldContext(out var worldModel, out var player) ||
                cellId == WorldIds.None ||
                player.CellId != cellId)
            {
                return false;
            }

            var playerFactionId = ResolvePlayerFactionId(player);
            var armies = worldModel.Armies ?? Array.Empty<ArmyData>();
            for (var armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                var army = armies[armyIndex];
                if (army.CellId == cellId && IsHostileToPlayer(army, playerFactionId))
                {
                    encounter = new GlobalBattleEncounter(
                        cellId,
                        army.Id,
                        playerFactionId,
                        army.FactionId,
                        CountUnits(player.Party),
                        CountUnits(army.Squads));
                    return true;
                }
            }

            return false;
        }

        public async UniTask<bool> TryStartPlayerBattleInCellAsync(
            int cellId,
            bool playerIsAttacker = true,
            CancellationToken cancellationToken = default)
        {
            if (!TryFindPlayerBattleOpponent(cellId, out var opponentArmyId))
            {
                return false;
            }

            return await TryStartPlayerBattleAsync(opponentArmyId, playerIsAttacker, cancellationToken);
        }

        public async UniTask<bool> TryStartPlayerBattleAsync(
            int opponentArmyId,
            bool playerIsAttacker = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TransitionRequested ||
                !TryGetWorldContext(out var worldModel, out var player) ||
                !CanFightPlayer(worldModel, player, opponentArmyId))
            {
                return false;
            }

            var request = encounterRequestFactory.CreatePlayerVsArmy(
                saveService.Current,
                opponentArmyId,
                playerIsAttacker);

            TransitionRequested = true;
            try
            {
                await stateMachine.ChangeStateAsync(GameStateId.EnterBattle, request, cancellationToken);
                return true;
            }
            catch
            {
                TransitionRequested = false;
                throw;
            }
        }

        private bool TryGetWorldContext(out WorldModel worldModel, out PlayerGlobalData player)
        {
            worldModel = saveService.Current?.World;
            player = worldModel?.Player;
            return worldModel != null && player != null && player.CellId != WorldIds.None;
        }

        private bool CanFightPlayer(WorldModel worldModel, PlayerGlobalData player, int opponentArmyId)
        {
            var playerFactionId = ResolvePlayerFactionId(player);
            var armies = worldModel.Armies ?? Array.Empty<ArmyData>();
            for (var armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                var army = armies[armyIndex];
                if (army.Id == opponentArmyId &&
                    army.CellId == player.CellId &&
                    IsHostileToPlayer(army, playerFactionId))
                {
                    return true;
                }
            }

            return false;
        }

        private int ResolvePlayerFactionId(PlayerGlobalData player)
        {
            if (player.FactionId != WorldIds.None &&
                configDatabase.TryGetFaction(player.FactionId, out _))
            {
                return player.FactionId;
            }

            var fallbackFactionId = configDatabase.BattleSimulation?.PlayerUnit?.FactionId ?? WorldIds.None;
            return fallbackFactionId != WorldIds.None &&
                   configDatabase.TryGetFaction(fallbackFactionId, out _)
                ? fallbackFactionId
                : player.FactionId;
        }

        private static bool IsHostileToPlayer(ArmyData army, int playerFactionId)
        {
            return army.FactionId != WorldIds.None && army.FactionId != playerFactionId;
        }

        private static int CountUnits(SquadData[] squads)
        {
            var total = 0;
            for (var squadIndex = 0; squads != null && squadIndex < squads.Length; squadIndex++)
            {
                if (squads[squadIndex].Count > 0)
                {
                    total += squads[squadIndex].Count;
                }
            }

            return total;
        }
    }
}
