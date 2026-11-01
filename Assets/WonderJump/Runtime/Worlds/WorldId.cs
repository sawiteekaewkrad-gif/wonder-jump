namespace WonderJump.Worlds
{
    /// <summary>
    /// The five worlds (5 levels each = 25 levels). Numeric values are FIXED (save data, network).
    /// <see cref="Shared"/> marks assets used by every world (characters, items, generic props).
    /// </summary>
    public enum WorldId
    {
        Shared = 0,
        GreenForest = 1,
        CandyKingdom = 2,
        OceanAdventure = 3,
        CrystalCave = 4,
        SkyCastle = 5
    }

    public static class WorldInfo
    {
        public const int WorldCount = 5;
        public const int LevelsPerWorld = 5;

        public static readonly WorldId[] PlayableWorlds =
        {
            WorldId.GreenForest, WorldId.CandyKingdom, WorldId.OceanAdventure,
            WorldId.CrystalCave, WorldId.SkyCastle
        };

        public static string DisplayName(WorldId world)
        {
            switch (world)
            {
                case WorldId.GreenForest: return "Green Forest";
                case WorldId.CandyKingdom: return "Candy Kingdom";
                case WorldId.OceanAdventure: return "Ocean Adventure";
                case WorldId.CrystalCave: return "Crystal Cave";
                case WorldId.SkyCastle: return "Sky Castle";
                default: return "Shared";
            }
        }

        /// <summary>Folder name used under generated/content asset folders.</summary>
        public static string FolderName(WorldId world)
        {
            return world.ToString();
        }
    }
}
