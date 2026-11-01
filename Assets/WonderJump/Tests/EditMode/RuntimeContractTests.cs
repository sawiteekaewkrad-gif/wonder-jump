using NUnit.Framework;
using UnityEngine;
using WonderJump.Characters;
using WonderJump.Gameplay;
using WonderJump.Level;
using WonderJump.Net;
using WonderJump.Player;
using WonderJump.Runtime;
using WonderJump.Worlds;

namespace WonderJump.Tests.EditMode
{
    public class RuntimeContractTests
    {
        private sealed class CountingCollector : ICollector
        {
            public int Count;
            public bool CanCollect(Collectible c) { return true; }
            public void OnCollected(Collectible c) { Count++; }
        }

        [Test]
        public void CharacterIds_ParseExactNamesOnly()
        {
            CharacterId id;
            Assert.IsTrue(CharacterIds.TryParse("Flora", out id));
            Assert.AreEqual(CharacterId.Flora, id);
            Assert.IsFalse(CharacterIds.TryParse("flora", out id), "case-sensitive");
            Assert.IsFalse(CharacterIds.TryParse("3", out id), "numeric strings rejected");
            Assert.IsFalse(CharacterIds.TryParse(null, out id));
        }

        [Test]
        public void CharacterIdentity_ValidatesId()
        {
            var go = new GameObject("c");
            try
            {
                var identity = go.AddComponent<CharacterIdentity>();
                identity.characterId = "Riku";
                Assert.IsTrue(identity.IsValid);
                identity.characterId = "Mario";
                Assert.IsFalse(identity.IsValid);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LevelId_RoundTrips_AndRejectsInvalid()
        {
            var id = new LevelId(WorldId.CrystalCave, 5);
            Assert.AreEqual("W4_L5", id.ToString());
            LevelId parsed;
            Assert.IsTrue(LevelId.TryParse("W4_L5", out parsed));
            Assert.AreEqual(id, parsed);
            Assert.IsFalse(LevelId.TryParse("W6_L1", out parsed), "only 5 worlds");
            Assert.IsFalse(LevelId.TryParse("W1_L6", out parsed), "only 5 levels");
            Assert.IsFalse(LevelId.TryParse("W0_L1", out parsed), "Shared is not playable");
            Assert.IsFalse(LevelId.TryParse("garbage", out parsed));
        }

        [Test]
        public void MovementSettings_DeriveGravityAndJumpVelocity()
        {
            var s = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            try
            {
                s.jumpHeight = 3f;
                s.timeToApex = 0.5f;
                Assert.AreEqual(24f, s.Gravity, 1e-4f);
                Assert.AreEqual(12f, s.JumpVelocity, 1e-4f);
                // apex height from v^2 / 2g must equal the designed height
                Assert.AreEqual(3f, s.JumpVelocity * s.JumpVelocity / (2f * s.Gravity), 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void Collectible_TryCollect_SucceedsOnlyOnce()
        {
            var go = new GameObject("coin", typeof(SphereCollider));
            try
            {
                var item = go.AddComponent<Collectible>();
                var collector = new CountingCollector();
                int events = 0;
                item.Collected += (c, who) => events++;

                Assert.IsTrue(item.TryCollect(collector));
                Assert.IsFalse(item.TryCollect(collector));
                Assert.IsFalse(item.TryCollect(new CountingCollector()), "second player cannot collect it either");
                Assert.AreEqual(1, collector.Count);
                Assert.AreEqual(1, events);
                Assert.IsFalse(go.activeSelf);

                item.ResetForRespawn();
                Assert.IsTrue(go.activeSelf);
                Assert.IsTrue(item.TryCollect(collector));
                Assert.AreEqual(2, collector.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GameAuthority_DefaultsOffline_AndClientHasNoSharedAuthority()
        {
            GameAuthority.ResetToOffline();
            Assert.AreEqual(SessionRole.Offline, GameAuthority.Current.Role);
            Assert.IsTrue(GameAuthority.Current.HasSharedStateAuthority, "single player must not need a network");
            GameAuthority.Set(new FixedAuthorityContext(SessionRole.Client));
            Assert.IsFalse(GameAuthority.Current.HasSharedStateAuthority);
            GameAuthority.Set(new FixedAuthorityContext(SessionRole.Host));
            Assert.IsTrue(GameAuthority.Current.HasSharedStateAuthority);
            GameAuthority.ResetToOffline();
        }
    }
}
