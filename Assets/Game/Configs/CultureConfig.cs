using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Culture", fileName = "CultureConfig")]
    public sealed class CultureConfig : IdentifiedConfig
    {
        [SerializeField] private int startingCellId;
        [SerializeField] private int startingCredits;
        [SerializeField] private WeaponConfig startingWeapon;
        [SerializeField] private ArmorConfig startingArmor;

        public int StartingCellId => startingCellId;
        public int StartingCredits => startingCredits;
        public WeaponConfig StartingWeapon => startingWeapon;
        public ArmorConfig StartingArmor => startingArmor;
    }
}
