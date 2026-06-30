namespace MercLord.Battle.ECS.Systems
{
    internal struct BattleCadenceTimer
    {
        private readonly float interval;
        private float timeUntilNextTick;

        public BattleCadenceTimer(float interval)
        {
            this.interval = interval;
            timeUntilNextTick = 0f;
        }

        public bool Consume(float deltaTime)
        {
            if (interval <= 0f)
            {
                return true;
            }

            timeUntilNextTick -= deltaTime > 0f ? deltaTime : 0f;
            if (timeUntilNextTick > 0f)
            {
                return false;
            }

            timeUntilNextTick += interval;
            if (timeUntilNextTick < 0f)
            {
                timeUntilNextTick = 0f;
            }

            return true;
        }
    }
}
