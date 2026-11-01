using UnityEngine;
using WonderJump.Worlds;

namespace WonderJump.Enemies
{
    /// <summary>Attached to every generated enemy and boss prefab.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyIdentity : MonoBehaviour
    {
        [SerializeField] private string enemyId;
        [SerializeField] private WorldId world;
        [SerializeField] private bool isBoss;

        public string EnemyId { get { return enemyId; } }
        public WorldId World { get { return world; } }
        public bool IsBoss { get { return isBoss; } }

        /// <summary>Used by the editor generator only.</summary>
        public void Configure(string id, WorldId worldId, bool boss)
        {
            enemyId = id;
            world = worldId;
            isBoss = boss;
        }
    }

    /// <summary>Per-tick context handed to an enemy brain.</summary>
    public struct EnemyContext
    {
        public Transform Self;
        public float Time;
        /// <summary>Nearest player position, if any player is in range.</summary>
        public Vector3? NearestPlayer;
    }

    /// <summary>
    /// Enemy AI seam (Phase 5). Brains run ONLY where shared state is authoritative
    /// (offline or host, see WonderJump.Net.GameAuthority); clients just render replicated state.
    /// </summary>
    public interface IEnemyBrain
    {
        void Tick(EnemyContext context, float deltaTime);
    }
}
