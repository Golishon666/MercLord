using MercLord.Game.Configs;
using UnityEngine;

namespace MercLord.Global.Rendering
{
    public enum GlobalMapOverlaySpriteId
    {
        SmallRoad,
        MediumRoad,
        LargeRoad,
        RoadJunction,
        ThinRiver,
        MediumRiver,
        WideRiver,
        RiverMouth,
        Coastline,
        RockyPatch,
        ForestCluster,
        WetlandPuddle,
        SnowDrift,
        DesertContour,
        RuinsDebris,
        AnomalyCrack
    }

    public enum GlobalMapIconSpriteId
    {
        Capital,
        City,
        Town,
        Village,
        HarborCity,
        RiverCity,
        MountainOutpost,
        ForestStation,
        ExpeditionCamp,
        HostileCamp,
        LogisticsStop,
        Mine,
        AncientRuins,
        IndustrialRuins,
        ResearchMonolith,
        Watchtower,
        CaveLair,
        AnomalyMarker,
        ToxicZone,
        BiomassResource,
        OreResource,
        FoodResource,
        WaterResource,
        RelicCache,
        Mission,
        Battle,
        Danger,
        Trade,
        BlueOutpost,
        PurpleOutpost,
        RedOutpost,
        NeutralPoint
    }

    [CreateAssetMenu(menuName = "MercLord/Global Map/Art Atlas", fileName = "GlobalMapArtAtlas")]
    public sealed class GlobalMapArtAtlas : ScriptableObject
    {
        [SerializeField] private Texture2D biomeAtlasTexture;
        [SerializeField] private Texture2D overlayAtlasTexture;
        [SerializeField] private Texture2D iconAtlasTexture;
        [SerializeField] private Sprite[] biomeSprites = new Sprite[0];
        [SerializeField] private Sprite[] extraBiomeSprites = new Sprite[0];
        [SerializeField] private Sprite[] overlaySprites = new Sprite[0];
        [SerializeField] private Sprite[] iconSprites = new Sprite[0];

        public Texture2D BiomeAtlasTexture => biomeAtlasTexture;
        public Texture2D OverlayAtlasTexture => overlayAtlasTexture;
        public Texture2D IconAtlasTexture => iconAtlasTexture;
        public Sprite[] BiomeSprites => biomeSprites;
        public Sprite[] ExtraBiomeSprites => extraBiomeSprites;
        public Sprite[] OverlaySprites => overlaySprites;
        public Sprite[] IconSprites => iconSprites;

        public bool TryGetBiomeSprite(BiomeType biome, out Sprite sprite)
        {
            var index = (int)biome;
            if (biomeSprites != null && index >= 0 && index < biomeSprites.Length)
            {
                sprite = biomeSprites[index];
                return sprite != null;
            }

            sprite = null;
            return false;
        }

        public bool TryGetOverlaySprite(GlobalMapOverlaySpriteId id, out Sprite sprite)
        {
            var index = (int)id;
            if (overlaySprites != null && index >= 0 && index < overlaySprites.Length)
            {
                sprite = overlaySprites[index];
                return sprite != null;
            }

            sprite = null;
            return false;
        }

        public bool TryGetIconSprite(GlobalMapIconSpriteId id, out Sprite sprite)
        {
            var index = (int)id;
            if (iconSprites != null && index >= 0 && index < iconSprites.Length)
            {
                sprite = iconSprites[index];
                return sprite != null;
            }

            sprite = null;
            return false;
        }
    }
}
