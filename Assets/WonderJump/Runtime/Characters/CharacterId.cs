using System;

namespace WonderJump.Characters
{
    /// <summary>
    /// The six playable characters. IDs and numeric values are FIXED:
    /// they are written to save files and sent over the network. Never renumber.
    /// </summary>
    public enum CharacterId
    {
        Leon = 1,
        Elin = 2,
        Miyu = 3,
        Koko = 4,
        Riku = 5,
        Flora = 6
    }

    public static class CharacterIds
    {
        public static readonly CharacterId[] All =
        {
            CharacterId.Leon, CharacterId.Elin, CharacterId.Miyu,
            CharacterId.Koko, CharacterId.Riku, CharacterId.Flora
        };

        /// <summary>Exact, case-sensitive match against the six IDs. Numeric strings are rejected.</summary>
        public static bool TryParse(string value, out CharacterId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (string.Equals(All[i].ToString(), value, StringComparison.Ordinal))
                {
                    id = All[i];
                    return true;
                }
            }
            id = default(CharacterId);
            return false;
        }
    }
}
