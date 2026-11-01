using System;
using UnityEngine;
using WonderJump.Net;

namespace WonderJump.Gameplay
{
    public enum CollectibleType
    {
        Coin = 0,
        Star = 1,
        Heart = 2,
        CrystalGem = 3,
        Key = 4,
        PowerUp = 5
    }

    /// <summary>Anything that can pick up collectibles (the player). Found on the collider's Rigidbody or parents.</summary>
    public interface ICollector
    {
        bool CanCollect(Collectible collectible);
        void OnCollected(Collectible collectible);
    }

    /// <summary>
    /// Trigger-based pickup with a single-collection guarantee:
    /// once collected, further trigger events and TryCollect calls are ignored until ResetForRespawn().
    /// Only the authoritative peer (offline or host) resolves pickups; in co-op the client's
    /// request path is added in Phase 2/4, so two players can never both collect one item.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleType type = CollectibleType.Coin;
        [SerializeField, Min(0)] private int value = 1;

        public CollectibleType Type { get { return type; } }
        public int Value { get { return value; } }
        public bool IsCollected { get; private set; }

        /// <summary>Raised once per collection, after the collector was notified.</summary>
        public event Action<Collectible, ICollector> Collected;

        /// <summary>Used by the editor generator only.</summary>
        public void Configure(CollectibleType collectibleType, int collectibleValue)
        {
            type = collectibleType;
            value = collectibleValue;
        }

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsCollected) return;
            if (!GameAuthority.Current.HasSharedStateAuthority) return;

            ICollector collector = FindCollector(other);
            if (collector != null) TryCollect(collector);
        }

        /// <summary>Returns true only for the first successful collection.</summary>
        public bool TryCollect(ICollector collector)
        {
            if (IsCollected || collector == null) return false;
            if (!collector.CanCollect(this)) return false;

            IsCollected = true; // set first: re-entrant calls from callbacks are rejected
            collector.OnCollected(this);
            Action<Collectible, ICollector> handler = Collected;
            if (handler != null) handler(this, collector);
            gameObject.SetActive(false);
            return true;
        }

        /// <summary>Re-arms the item (checkpoint reload, object pooling, tests).</summary>
        public void ResetForRespawn()
        {
            IsCollected = false;
            gameObject.SetActive(true);
        }

        private static ICollector FindCollector(Collider other)
        {
            if (other == null) return null;
            Rigidbody body = other.attachedRigidbody;
            GameObject source = body != null ? body.gameObject : other.gameObject;
            return source.GetComponentInParent<ICollector>();
        }
    }
}
