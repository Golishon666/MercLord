using MercLord.Battle.ECS.Components;

namespace MercLord.Battle.UI
{
    public readonly struct BattleCommandHudSnapshot
    {
        public BattleCommandHudSnapshot(
            bool hasPlayer,
            bool hasSquads,
            SquadOrderType currentOrder,
            int friendlySquadCount,
            int mixedOrderCount)
        {
            HasPlayer = hasPlayer;
            HasSquads = hasSquads;
            CurrentOrder = currentOrder;
            FriendlySquadCount = friendlySquadCount;
            MixedOrderCount = mixedOrderCount;
        }

        public bool HasPlayer { get; }
        public bool HasSquads { get; }
        public SquadOrderType CurrentOrder { get; }
        public int FriendlySquadCount { get; }
        public int MixedOrderCount { get; }
    }
}
