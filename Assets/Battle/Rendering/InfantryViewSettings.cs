using UnityEngine;

namespace MercLord.Battle.Rendering
{
    public sealed class InfantryViewSettings : MonoBehaviour
    {
        [SerializeField] private float selectionRadius;
        [SerializeField] private Vector2 hitPointOffset;
        [SerializeField] private ViewTweenSettings tweenSettings;

        public float SelectionRadius => selectionRadius;
        public Vector2 HitPointOffset => hitPointOffset;
        public ViewTweenSettings TweenSettings => tweenSettings;
    }
}
