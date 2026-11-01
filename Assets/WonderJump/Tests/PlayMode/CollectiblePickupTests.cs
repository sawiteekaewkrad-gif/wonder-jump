using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WonderJump.Gameplay;
using WonderJump.Net;

namespace WonderJump.Tests.PlayMode
{
    /// <summary>
    /// Real physics: a kinematic capsule (same setup as generated characters) moves through a
    /// trigger SphereCollider (same setup as generated items).
    /// </summary>
    public class CollectiblePickupTests
    {
        private sealed class TestCollector : MonoBehaviour, ICollector
        {
            public int Count;
            public bool CanCollect(Collectible c) { return true; }
            public void OnCollected(Collectible c) { Count++; }
        }

        private GameObject item;
        private GameObject playerA;
        private GameObject playerB;

        [SetUp]
        public void SetUp()
        {
            GameAuthority.ResetToOffline();
        }

        [TearDown]
        public void TearDown()
        {
            if (item != null) Object.Destroy(item);
            if (playerA != null) Object.Destroy(playerA);
            if (playerB != null) Object.Destroy(playerB);
            GameAuthority.ResetToOffline();
        }

        private static GameObject MakeItem(Vector3 position)
        {
            var go = new GameObject("Coin");
            go.transform.position = position;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.3f;
            go.AddComponent<Collectible>();
            return go;
        }

        private static GameObject MakePlayer(Vector3 position)
        {
            var go = new GameObject("Player");
            go.transform.position = position;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.32f;
            capsule.center = new Vector3(0f, 1f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<TestCollector>();
            return go;
        }

        private static IEnumerator FixedSteps(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Player_EnteringTrigger_CollectsExactlyOnce()
        {
            item = MakeItem(new Vector3(0f, 1f, 0f));
            playerA = MakePlayer(new Vector3(-3f, 0f, 0f));
            var collector = playerA.GetComponent<TestCollector>();
            var body = playerA.GetComponent<Rigidbody>();
            yield return FixedSteps(2);

            body.MovePosition(new Vector3(0f, 0f, 0f));
            yield return FixedSteps(3);
            Assert.AreEqual(1, collector.Count, "collected once on enter");
            Assert.IsFalse(item.activeSelf);

            body.MovePosition(new Vector3(3f, 0f, 0f));
            yield return FixedSteps(3);
            body.MovePosition(new Vector3(0f, 0f, 0f));
            yield return FixedSteps(3);
            Assert.AreEqual(1, collector.Count, "re-entering does not collect again");
        }

        [UnityTest]
        public IEnumerator TwoPlayers_SameFrame_OnlyOneCollects()
        {
            item = MakeItem(new Vector3(0f, 1f, 0f));
            playerA = MakePlayer(new Vector3(-3f, 0f, 0f));
            playerB = MakePlayer(new Vector3(3f, 0f, 0f));
            yield return FixedSteps(2);

            playerA.GetComponent<Rigidbody>().MovePosition(new Vector3(-0.05f, 0f, 0f));
            playerB.GetComponent<Rigidbody>().MovePosition(new Vector3(0.05f, 0f, 0f));
            yield return FixedSteps(3);

            int total = playerA.GetComponent<TestCollector>().Count + playerB.GetComponent<TestCollector>().Count;
            Assert.AreEqual(1, total);
        }

        [UnityTest]
        public IEnumerator Client_DoesNotResolvePickupsLocally()
        {
            GameAuthority.Set(new FixedAuthorityContext(SessionRole.Client));
            item = MakeItem(new Vector3(0f, 1f, 0f));
            playerA = MakePlayer(new Vector3(-3f, 0f, 0f));
            yield return FixedSteps(2);

            playerA.GetComponent<Rigidbody>().MovePosition(Vector3.zero);
            yield return FixedSteps(3);
            Assert.AreEqual(0, playerA.GetComponent<TestCollector>().Count, "host decides shared events");
            Assert.IsTrue(item.activeSelf);
        }
    }
}
