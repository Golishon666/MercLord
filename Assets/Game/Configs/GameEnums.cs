namespace MercLord.Game.Configs
{
    public enum UnitCategory
    {
        RangedInfantry,
        MeleeInfantry,
        Artillery,
        Vehicle,
        Player
    }

    public enum WeaponType
    {
        AutomaticRifle,
        Sword,
        Shield,
        ArtilleryCannon,
        TankCannon
    }

    public enum DamageType
    {
        Ballistic,
        Energy,
        Explosion
    }

    public enum AIType
    {
        Passive,
        Ranged,
        Melee,
        Artillery,
        Vehicle
    }

    public enum BiomeType
    {
        Ocean,
        Coast,
        Plains,
        Forest,
        Desert,
        Snow,
        Swamp,
        Mountains,
        AshWastes,
        RustDesert,
        DeadForest,
        IndustrialRuins,
        DemonScar,
        ToxicSwamp
    }

    public enum ItemCategory
    {
        Weapon,
        Armor,
        Helmet,
        Special,
        Consumable,
        TradeGood,
        Quest
    }

    public enum BattleSpawnSide
    {
        Attacker,
        Defender
    }
}
