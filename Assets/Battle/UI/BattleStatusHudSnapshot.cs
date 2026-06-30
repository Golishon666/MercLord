using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;

namespace MercLord.Battle.UI
{
    public readonly struct BattleTeamHudSnapshot
    {
        public BattleTeamHudSnapshot(int total, int alive)
        {
            Total = total;
            Alive = alive;
            Lost = total > alive ? total - alive : 0;
        }

        public int Total { get; }
        public int Alive { get; }
        public int Lost { get; }
    }

    public readonly struct BattleStatusHudSnapshot
    {
        public BattleStatusHudSnapshot(
            BattleTeamHudSnapshot attacker,
            BattleTeamHudSnapshot defender,
            bool hasPlayer,
            BattleTeamType playerTeam,
            int playerCurrentHealth,
            int playerMaxHealth,
            bool isCompleted,
            BattleOutcome outcome)
        {
            Attacker = attacker;
            Defender = defender;
            HasPlayer = hasPlayer;
            PlayerTeam = playerTeam;
            PlayerCurrentHealth = playerCurrentHealth;
            PlayerMaxHealth = playerMaxHealth;
            IsCompleted = isCompleted;
            Outcome = outcome;
        }

        public BattleTeamHudSnapshot Attacker { get; }
        public BattleTeamHudSnapshot Defender { get; }
        public bool HasPlayer { get; }
        public BattleTeamType PlayerTeam { get; }
        public int PlayerCurrentHealth { get; }
        public int PlayerMaxHealth { get; }
        public bool IsCompleted { get; }
        public BattleOutcome Outcome { get; }
    }
}
