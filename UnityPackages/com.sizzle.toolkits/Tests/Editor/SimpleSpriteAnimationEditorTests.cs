using System.Collections.Generic;
using NUnit.Framework;
using Sizzle.Toolkits.Animations;
using Sizzle.Toolkits.Editor.Animations;
using UnityEditor;
using UnityEngine;

namespace Sizzle.Toolkits.Tests.Animations
{
    [TestFixture]
    public class SimpleSpriteAnimationEditorTests
    {
        private readonly List<Object> m_createdObjects = new List<Object>();
        private Texture2D m_testTexture;

        [SetUp]
        public void SetUp()
        {
            m_testTexture = new Texture2D(4, 4);
            m_createdObjects.Add(m_testTexture);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_createdObjects.Count - 1; i >= 0; i--)
            {
                if (m_createdObjects[i] != null)
                    Object.DestroyImmediate(m_createdObjects[i]);
            }

            m_createdObjects.Clear();
            m_testTexture = null;
        }

        [Test]
        public void CompareNatural_SortsNumericSuffixesInHumanOrder()
        {
            var names = new List<string> { "Walk_10", "Walk_2", "Walk_1", "Walk_20", "Walk_03" };
            names.Sort(SimpleSpriteAnimationEditorUtility.CompareNatural);

            CollectionAssert.AreEqual(
                new[] { "Walk_1", "Walk_2", "Walk_03", "Walk_10", "Walk_20" },
                names);
        }

        [Test]
        public void ResolveDefaultObjectName_StripsTrailingFrameIndexAndSeparators()
        {
            Sprite s1 = CreateTestSprite("Hero_Attack_01");
            Sprite s2 = CreateTestSprite("Run-12");
            Sprite s3 = CreateTestSprite("001");
            Sprite s4 = CreateTestSprite("Idle");

            Assert.AreEqual("Hero_Attack", SimpleSpriteAnimationEditorUtility.ResolveDefaultObjectName(new[] { s1 }));
            Assert.AreEqual("Run", SimpleSpriteAnimationEditorUtility.ResolveDefaultObjectName(new[] { s2 }));
            Assert.AreEqual("SimpleSpriteAnimation", SimpleSpriteAnimationEditorUtility.ResolveDefaultObjectName(new[] { s3 }));
            Assert.AreEqual("Idle", SimpleSpriteAnimationEditorUtility.ResolveDefaultObjectName(new[] { s4 }));
        }

        [Test]
        public void ExtractSprites_DeduplicatesAndSortsSelectedSprites()
        {
            Sprite s10 = CreateTestSprite("Flame_10");
            Sprite s2 = CreateTestSprite("Flame_2");
            Sprite s1 = CreateTestSprite("Flame_1");
            var nonSprite = new GameObject("Dummy");
            m_createdObjects.Add(nonSprite);

            List<Sprite> extracted = SimpleSpriteAnimationEditorUtility.ExtractSprites(
                new Object[] { s10, null, nonSprite, s2, s10, s1 });

            Assert.AreEqual(3, extracted.Count);
            Assert.AreSame(s1, extracted[0]);
            Assert.AreSame(s2, extracted[1]);
            Assert.AreSame(s10, extracted[2]);
        }

        [Test]
        public void CreateAnimationObject_WithoutParent_CreatesAtRootWithAllSprites()
        {
            Sprite s0 = CreateTestSprite("Spark_0");
            Sprite s1 = CreateTestSprite("Spark_1");
            Sprite s2 = CreateTestSprite("Spark_2");

            SimpleSpriteAnimation animation = SimpleSpriteAnimationEditorUtility.CreateAnimationObject(
                new[] { s0, s1, s2 });

            Assert.IsNotNull(animation);
            m_createdObjects.Add(animation.gameObject);

            Assert.IsNull(animation.transform.parent);
            Assert.AreEqual("Spark", animation.gameObject.name);
            Assert.AreEqual(3, animation.FrameCount);

            var renderer = animation.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreSame(s0, renderer.sprite);

            var serializedObject = new SerializedObject(animation);
            Assert.AreSame(renderer, serializedObject.FindProperty("m_spriteRenderer").objectReferenceValue);

            SerializedProperty framesProp = serializedObject.FindProperty("m_frames");
            Assert.AreEqual(3, framesProp.arraySize);
            Assert.AreSame(s0, framesProp.GetArrayElementAtIndex(0).objectReferenceValue);
            Assert.AreSame(s1, framesProp.GetArrayElementAtIndex(1).objectReferenceValue);
            Assert.AreSame(s2, framesProp.GetArrayElementAtIndex(2).objectReferenceValue);
        }

        [Test]
        public void CreateAnimationObject_WithParent_CreatesUnderSpecifiedParent()
        {
            var parentRoot = new GameObject("PrefabRootMock");
            m_createdObjects.Add(parentRoot);

            Sprite s0 = CreateTestSprite("Hit_0");
            Sprite s1 = CreateTestSprite("Hit_1");

            SimpleSpriteAnimation animation = SimpleSpriteAnimationEditorUtility.CreateAnimationObject(
                new[] { s0, s1 },
                parentRoot);

            Assert.IsNotNull(animation);
            Assert.AreSame(parentRoot.transform, animation.transform.parent);
            Assert.AreEqual(Vector3.zero, animation.transform.localPosition);
            Assert.AreEqual(2, animation.FrameCount);
        }

        private Sprite CreateTestSprite(string spriteName)
        {
            Sprite sprite = Sprite.Create(m_testTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            sprite.name = spriteName;
            m_createdObjects.Add(sprite);
            return sprite;
        }
    }
}
