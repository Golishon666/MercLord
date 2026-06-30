using UnityEngine;

namespace MercLord.Game.Configs
{
    [CreateAssetMenu(menuName = "MercLord/Configs/Biome", fileName = "BiomeConfig")]
    public sealed class BiomeConfig : IdentifiedConfig
    {
        [SerializeField] private BiomeType biomeType;
        [SerializeField] private Color mapColor;
        [SerializeField] private int tileSetId;
        [SerializeField] private bool isPassableByDefault;

        public BiomeType BiomeType => biomeType;
        public Color MapColor => mapColor;
        public int TileSetId => tileSetId;
        public bool IsPassableByDefault => isPassableByDefault;
    }
}
