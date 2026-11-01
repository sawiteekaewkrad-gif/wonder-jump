using UnityEngine;

namespace WonderJump.Core
{
    /// <summary>
    /// Physics layer names used by every Wonder Jump system.
    /// Created by "Wonder Jump/Setup/Ensure Layers" (also run automatically by the generator).
    /// </summary>
    public static class WonderJumpLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Ground = "Ground";
        public const string Collectible = "Collectible";
        public const string Hazard = "Hazard";
        public const string Interactable = "Interactable";

        /// <summary>All required layers, in preferred slot order (slots 8..13 when free).</summary>
        public static readonly string[] All = { Player, Enemy, Ground, Collectible, Hazard, Interactable };

        /// <summary>Layer index, or -1 when the layer has not been created yet.</summary>
        public static int Index(string layerName)
        {
            return LayerMask.NameToLayer(layerName);
        }

        /// <summary>Bit mask for one layer, or 0 when the layer is missing.</summary>
        public static int Mask(string layerName)
        {
            int index = LayerMask.NameToLayer(layerName);
            return index < 0 ? 0 : 1 << index;
        }

        /// <summary>Surfaces the player can stand on.</summary>
        public static int GroundMask
        {
            get { return Mask(Ground); }
        }
    }
}
