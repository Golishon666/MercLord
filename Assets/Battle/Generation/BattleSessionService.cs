using System;

namespace MercLord.Battle.Generation
{
    public sealed class BattleSessionService : IBattleSessionService
    {
        public BattleSession Current { get; private set; }

        public void SetCurrent(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (Current != null && !ReferenceEquals(Current, session))
            {
                throw new InvalidOperationException("A battle session is already active.");
            }

            Current = session;
        }

        public BattleSession ConsumeCurrent()
        {
            if (Current == null)
            {
                throw new InvalidOperationException("No active battle session.");
            }

            var session = Current;
            Current = null;
            return session;
        }

        public void Clear()
        {
            Current = null;
        }
    }
}
