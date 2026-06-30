using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Item", fileName = "ItemConfig")]
    public sealed class ItemConfig : IdentifiedConfig
    {
        [SerializeField] private ItemCategory category;
        [SerializeField] private int price;
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private ArmorConfig armor;
        [SerializeField] private TradeGoodConfig tradeGood;

        public ItemCategory Category => category;
        public int Price => price;
        public WeaponConfig Weapon => weapon;
        public ArmorConfig Armor => armor;
        public TradeGoodConfig TradeGood => tradeGood;
    }
}
