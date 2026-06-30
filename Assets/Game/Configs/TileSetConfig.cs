using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Tile Set", fileName = "TileSetConfig")]
    public sealed class TileSetConfig : IdentifiedConfig
    {
        [SerializeField] private BiomeType biomeType;

        public BiomeType BiomeType => biomeType;
    }
}
