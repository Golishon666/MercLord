using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Trade Good", fileName = "TradeGoodConfig")]
    public sealed class TradeGoodConfig : IdentifiedConfig
    {
        [SerializeField] private int basePrice;
        [SerializeField] private LootRarity rarity;
        [SerializeField] private string iconAddress;
        [SerializeField] private string description;

        public int BasePrice => basePrice;
        public LootRarity Rarity => rarity;
        public string IconAddress => iconAddress;
        public string Description => description;
    }
}
