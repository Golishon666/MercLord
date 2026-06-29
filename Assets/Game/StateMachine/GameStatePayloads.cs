using MercLord.Battle.Generation;
using MercLord.Global.Cells;

namespace MercLord.Game.StateMachine
{
    public readonly struct NewGameRequest
    {
        public NewGameRequest(int? seed = null, int cultureId = WorldIds.None)
        {
            Seed = seed;
            CultureId = cultureId;
        }

        public int? Seed { get; }
        public int CultureId { get; }
    }

    public readonly struct LoadGlobalRequest
    {
        public LoadGlobalRequest(bool loadScene)
        {
            LoadScene = loadScene;
        }

        public bool LoadScene { get; }
    }

    public readonly struct EnterBattleRequest
    {
        public EnterBattleRequest(
            int sourceCellId,
            BattleArmyData attacker,
            BattleArmyData defender,
            int? seed = null,
            bool nearSettlement = false,
            bool loadScene = true)
        {
            SourceCellId = sourceCellId;
            Attacker = attacker;
            Defender = defender;
            Seed = seed;
            NearSettlement = nearSettlement;
            LoadScene = loadScene;
        }

        public int SourceCellId { get; }
        public BattleArmyData Attacker { get; }
        public BattleArmyData Defender { get; }
        public int? Seed { get; }
        public bool NearSettlement { get; }
        public bool LoadScene { get; }
    }

    public readonly struct BattleStateRequest
    {
        public BattleStateRequest(BattleSession session)
        {
            Session = session;
        }

        public BattleSession Session { get; }
    }

    public readonly struct ExitBattleRequest
    {
        public ExitBattleRequest(BattleResult result, bool loadGlobalScene = true)
        {
            Result = result;
            LoadGlobalScene = loadGlobalScene;
        }

        public BattleResult Result { get; }
        public bool LoadGlobalScene { get; }
    }
}
