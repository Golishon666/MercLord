using System;

namespace MercLord.Battle.Rendering
{
    [Serializable]
    public struct ViewTweenSettings
    {
        public float IdleBobDistance;
        public float IdleBobTime;
        public float MoveBobDistance;
        public float MoveBobTime;
        public float ShootRecoilDistance;
        public float ShootRecoilTime;
        public float HitShakeDistance;
        public float HitShakeTime;
        public float DeathCollapseTime;
    }
}
