using System;
using System.Globalization;
using System.Threading.Tasks;
using WonderJump.Worlds;

namespace WonderJump.Level
{
    /// <summary>
    /// Identifies one of the 25 levels. Canonical string form "W{world}_L{level}", e.g. "W1_L3".
    /// This string is used for scene names, save keys and network messages.
    /// </summary>
    [Serializable]
    public struct LevelId : IEquatable<LevelId>
    {
        public WorldId World;
        /// <summary>1..5</summary>
        public int Index;

        public LevelId(WorldId world, int index)
        {
            World = world;
            Index = index;
        }

        public bool IsValid
        {
            get
            {
                return World != WorldId.Shared
                    && (int)World >= 1 && (int)World <= WorldInfo.WorldCount
                    && Index >= 1 && Index <= WorldInfo.LevelsPerWorld;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "W{0}_L{1}", (int)World, Index);
        }

        public static bool TryParse(string text, out LevelId id)
        {
            id = default(LevelId);
            if (string.IsNullOrEmpty(text) || text.Length < 5 || text[0] != 'W') return false;
            int sep = text.IndexOf("_L", StringComparison.Ordinal);
            if (sep < 2) return false;
            int world, level;
            if (!int.TryParse(text.Substring(1, sep - 1), NumberStyles.None, CultureInfo.InvariantCulture, out world)) return false;
            if (!int.TryParse(text.Substring(sep + 2), NumberStyles.None, CultureInfo.InvariantCulture, out level)) return false;
            LevelId candidate = new LevelId((WorldId)world, level);
            if (!candidate.IsValid) return false;
            id = candidate;
            return true;
        }

        public bool Equals(LevelId other) { return World == other.World && Index == other.Index; }
        public override bool Equals(object obj) { return obj is LevelId && Equals((LevelId)obj); }
        public override int GetHashCode() { return ((int)World * 397) ^ Index; }
    }

    public struct LevelResult
    {
        public LevelId Level;
        public int Coins;
        public int Stars;
        public float TimeSeconds;
    }

    /// <summary>
    /// Level flow seam (Phase 4/6). Implementation loads the level scene additively into
    /// "Gameplay"; in co-op it uses NGO's network scene manager so both peers load the same level.
    /// </summary>
    public interface ILevelService
    {
        LevelId Current { get; }
        Task LoadAsync(LevelId level);
        /// <summary>Authoritative peer only.</summary>
        void Complete(LevelResult result);
        event Action<LevelResult> LevelCompleted;
    }
}
