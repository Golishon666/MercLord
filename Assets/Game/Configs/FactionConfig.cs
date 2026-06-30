using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Faction", fileName = "FactionConfig")]
    public sealed class FactionConfig : IdentifiedConfig
    {
        [SerializeField] private Color color;
        [SerializeField] private int startingCredits;
        [SerializeField] private int startingStrength;
        [SerializeField] private int capitalCellId = -1;

        public Color Color => color;
        public int StartingCredits => startingCredits;
        public int StartingStrength => startingStrength;
        public int CapitalCellId => capitalCellId;
    }
}
