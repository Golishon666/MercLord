using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Loot Table", fileName = "LootTableConfig")]
    public sealed class LootTableConfig : IdentifiedConfig
    {
        [SerializeField] private LootEntry[] entries = new LootEntry[0];

        public LootEntry[] Entries => entries;
    }
}
