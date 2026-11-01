using UnityEngine;

namespace WonderJump.Runtime
{
    /// <summary>
    /// Identifies which of the six characters a prefab represents.
    /// Kept in the original namespace and with the original string fields so that
    /// prefabs and scripts written against Asset Pack v1 keep working.
    /// Use <see cref="TryGetId"/> for a typed, validated ID.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterIdentity : MonoBehaviour
    {
        public string characterId;
        public string displayName;

        public bool TryGetId(out WonderJump.Characters.CharacterId id)
        {
            return WonderJump.Characters.CharacterIds.TryParse(characterId, out id);
        }

        public bool IsValid
        {
            get
            {
                WonderJump.Characters.CharacterId unused;
                return TryGetId(out unused);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(characterId) && !IsValid)
            {
                Debug.LogWarning("[WonderJump] '" + characterId + "' on " + name +
                    " is not one of the six character IDs (Leon, Elin, Miyu, Koko, Riku, Flora).", this);
            }
        }
#endif
    }
}
