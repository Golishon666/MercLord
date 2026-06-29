namespace MercLord.Game.StateMachine
{
    public readonly struct GameStateContext
    {
        public GameStateContext(GameStateId to, GameStateId? from = null, object payload = null)
        {
            To = to;
            From = from;
            Payload = payload;
        }

        public GameStateId To { get; }
        public GameStateId? From { get; }
        public object Payload { get; }
    }
}
