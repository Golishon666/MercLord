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

        public bool TryGetPayload<TPayload>(out TPayload payload)
        {
            if (Payload is TPayload typedPayload)
            {
                payload = typedPayload;
                return true;
            }

            payload = default;
            return false;
        }

        public TPayload GetPayloadOrDefault<TPayload>(TPayload defaultValue = default)
        {
            return TryGetPayload(out TPayload payload) ? payload : defaultValue;
        }
    }
}
