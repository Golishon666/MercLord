using MercLord.Battle.Rendering;
using UnityEngine;

namespace MercLord.Battle.Vehicles
{
    public sealed class VehicleViewSettings : MonoBehaviour
    {
        [SerializeField] private float selectionRadius;
        [SerializeField] private float enterRadius;
        [SerializeField] private ViewTweenSettings tweenSettings;

        public float SelectionRadius => selectionRadius;
        public float EnterRadius => enterRadius;
        public ViewTweenSettings TweenSettings => tweenSettings;
    }
}
