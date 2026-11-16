using System;
using System.Collections.Generic;
using Sizzle.Toolkits.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sizzle.Toolkits.Editor.Animations
{
    /// <summary>
    /// 다중 선택된 스프라이트로부터 <see cref="SimpleSpriteAnimation"/> 오브젝트를 생성하는 에디터 유틸리티입니다.
    /// </summary>
    public static class SimpleSpriteAnimationEditorUtility
    {
        private const string AssetsMenuPath = "Assets/Sizzle/Create Simple Sprite Animation";
        private const string ToolsMenuPath = "Tools/Sizzle/Toolkits/Create Simple Sprite Animation";
        private const string UndoCreateMessage = "Create Simple Sprite Animation";
        private const string DefaultObjectName = "SimpleSpriteAnimation";

        [MenuItem(AssetsMenuPath, false, 2000)]
        [MenuItem(ToolsMenuPath, false, 200)]
        private static void CreateFromSelectedSprites()
        {
            List<Sprite> sprites = ExtractSprites(Selection.objects);
            if (sprites.Count == 0)
                return;

            GameObject targetParent = ResolveTargetParent();
            CreateAnimationObject(sprites, targetParent);
        }

        [MenuItem(AssetsMenuPath, true)]
        [MenuItem(ToolsMenuPath, true)]
        private static bool ValidateCreateFromSelectedSprites()
        {
            return HasAnySprite(Selection.objects);
        }

        /// <summary>
        /// 현재 에디터 상태에 따라 생성될 오브젝트의 부모를 결정합니다.
        /// 프리팹 모드라면 현재 프리팹의 루트 오브젝트를 반환하고, 일반 씬 모드라면 <c>null</c>을 반환합니다.
        /// </summary>
        public static GameObject ResolveTargetParent()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null ? prefabStage.prefabContentsRoot : null;
        }

        /// <summary>
        /// 전달된 오브젝트 목록에서 <see cref="Sprite"/>들을 추출하고 중복을 제거한 뒤 자연 정렬하여 반환합니다.
        /// <see cref="Sprite"/> 직접 선택과 <see cref="Texture2D"/> 에셋 선택(단일/슬라이스)을 모두 지원합니다.
        /// </summary>
        public static List<Sprite> ExtractSprites(IEnumerable<Object> objects)
        {
            var result = new List<Sprite>();
            if (objects == null)
                return result;

            var uniqueSprites = new HashSet<Sprite>();
            foreach (Object obj in objects)
                CollectSpritesFromObject(obj, result, uniqueSprites, stopAfterFirst: false);

            result.Sort(CompareSprites);
            return result;
        }

        /// <summary>
        /// 스프라이트 목록을 기반으로 새 게임 오브젝트와 <see cref="SimpleSpriteAnimation"/> 컴포넌트를 생성합니다.
        /// </summary>
        /// <param name="sprites">애니메이션 프레임으로 등록할 스프라이트 목록</param>
        /// <param name="parent">부모로 지정할 게임 오브젝트 (<c>null</c>이면 현재 스테이지 루트에 생성)</param>
        /// <param name="objectName">생성할 오브젝트 이름 (<c>null</c> 또는 공백이면 첫 스프라이트 이름에서 자동 추론)</param>
        /// <returns>생성된 <see cref="SimpleSpriteAnimation"/> 컴포넌트 (스프라이트가 비어있으면 <c>null</c>)</returns>
        public static SimpleSpriteAnimation CreateAnimationObject(
            IReadOnlyList<Sprite> sprites,
            GameObject parent = null,
            string objectName = null)
        {
            if (sprites == null || sprites.Count == 0)
                return null;

            string resolvedName = string.IsNullOrWhiteSpace(objectName)
                ? ResolveDefaultObjectName(sprites)
                : objectName.Trim();

            var gameObject = new GameObject(resolvedName);
            if (parent != null)
                GameObjectUtility.SetParentAndAlign(gameObject, parent);
            else
                StageUtility.PlaceGameObjectInCurrentStage(gameObject);

            GameObjectUtility.EnsureUniqueNameForSibling(gameObject);

            if (!gameObject.TryGetComponent(out SpriteRenderer spriteRenderer))
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            var spriteAnimation = gameObject.AddComponent<SimpleSpriteAnimation>();

            AssignSprites(spriteAnimation, spriteRenderer, sprites);

            Undo.RegisterCreatedObjectUndo(gameObject, UndoCreateMessage);
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);

            return spriteAnimation;
        }

        /// <summary>
        /// 첫 번째 유효한 스프라이트의 이름에서 프레임 번호 및 구분자를 제거해 기본 오브젝트 이름을 생성합니다.
        /// </summary>
        public static string ResolveDefaultObjectName(IReadOnlyList<Sprite> sprites)
        {
            Sprite firstSprite = FindFirstValidSprite(sprites);
            if (firstSprite == null || string.IsNullOrWhiteSpace(firstSprite.name))
                return DefaultObjectName;

            string rawName = firstSprite.name;
            int end = rawName.Length;
            while (end > 0 && char.IsDigit(rawName[end - 1]))
                end--;

            while (end > 0 && (rawName[end - 1] == '_' || rawName[end - 1] == '-' || rawName[end - 1] == ' ' || rawName[end - 1] == '.'))
                end--;

            string trimmed = rawName.Substring(0, end).Trim();
            return string.IsNullOrEmpty(trimmed) ? DefaultObjectName : trimmed;
        }

        /// <summary>
        /// 숫자가 포함된 문자열을 사람이 읽는 순서(예: frame_2 &lt; frame_10)로 비교합니다.
        /// </summary>
        public static int CompareNatural(string a, string b)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            int indexA = 0;
            int indexB = 0;
            int lengthA = a.Length;
            int lengthB = b.Length;

            while (indexA < lengthA && indexB < lengthB)
            {
                bool isDigitA = char.IsDigit(a[indexA]);
                bool isDigitB = char.IsDigit(b[indexB]);

                if (isDigitA && isDigitB)
                {
                    int numberCompare = CompareNumericRuns(a, ref indexA, b, ref indexB);
                    if (numberCompare != 0)
                        return numberCompare;

                    continue;
                }

                char charA = char.ToUpperInvariant(a[indexA]);
                char charB = char.ToUpperInvariant(b[indexB]);
                if (charA != charB)
                    return charA.CompareTo(charB);

                if (a[indexA] != b[indexB])
                    return a[indexA].CompareTo(b[indexB]);

                indexA++;
                indexB++;
            }

            return (lengthA - indexA).CompareTo(lengthB - indexB);
        }

        private static bool HasAnySprite(Object[] objects)
        {
            if (objects == null || objects.Length == 0)
                return false;

            for (int i = 0; i < objects.Length; i++)
            {
                if (CollectSpritesFromObject(objects[i], null, null, stopAfterFirst: true))
                    return true;
            }

            return false;
        }

        private static bool CollectSpritesFromObject(
            Object obj,
            List<Sprite> target,
            HashSet<Sprite> uniqueSprites,
            bool stopAfterFirst)
        {
            if (obj == null)
                return false;

            if (obj is Sprite sprite)
            {
                if (stopAfterFirst)
                    return true;

                if (uniqueSprites.Add(sprite))
                    target.Add(sprite);

                return true;
            }

            if (obj is Texture2D texture)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(assetPath))
                    return false;

                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool foundAny = false;

                for (int i = 0; i < subAssets.Length; i++)
                {
                    if (subAssets[i] is Sprite subSprite)
                    {
                        if (stopAfterFirst)
                            return true;

                        foundAny = true;
                        if (uniqueSprites.Add(subSprite))
                            target.Add(subSprite);
                    }
                }

                return foundAny;
            }

            return false;
        }

        private static Sprite FindFirstValidSprite(IReadOnlyList<Sprite> sprites)
        {
            if (sprites == null)
                return null;

            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] != null)
                    return sprites[i];
            }

            return null;
        }

        private static void AssignSprites(
            SimpleSpriteAnimation spriteAnimation,
            SpriteRenderer spriteRenderer,
            IReadOnlyList<Sprite> sprites)
        {
            spriteRenderer.sprite = FindFirstValidSprite(sprites);

            var serializedObject = new SerializedObject(spriteAnimation);
            serializedObject.Update();

            SerializedProperty rendererProperty = serializedObject.FindProperty("m_spriteRenderer");
            if (rendererProperty != null)
                rendererProperty.objectReferenceValue = spriteRenderer;

            SerializedProperty framesProperty = serializedObject.FindProperty("m_frames");
            if (framesProperty != null)
            {
                framesProperty.arraySize = sprites.Count;
                for (int i = 0; i < sprites.Count; i++)
                {
                    framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int CompareSprites(Sprite a, Sprite b)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            int nameComparison = CompareNatural(a.name, b.name);
            if (nameComparison != 0)
                return nameComparison;

            string pathA = AssetDatabase.GetAssetPath(a);
            string pathB = AssetDatabase.GetAssetPath(b);
            int pathComparison = string.Compare(pathA, pathB, StringComparison.OrdinalIgnoreCase);
            if (pathComparison != 0)
                return pathComparison;

            return a.GetEntityId().CompareTo(b.GetEntityId());
        }

        private static int CompareNumericRuns(string a, ref int indexA, string b, ref int indexB)
        {
            int startA = indexA;
            while (indexA < a.Length && char.IsDigit(a[indexA]))
                indexA++;

            int startB = indexB;
            while (indexB < b.Length && char.IsDigit(b[indexB]))
                indexB++;

            int nonZeroA = startA;
            while (nonZeroA < indexA && a[nonZeroA] == '0')
                nonZeroA++;

            int nonZeroB = startB;
            while (nonZeroB < indexB && b[nonZeroB] == '0')
                nonZeroB++;

            int significantLengthA = indexA - nonZeroA;
            int significantLengthB = indexB - nonZeroB;
            if (significantLengthA != significantLengthB)
                return significantLengthA.CompareTo(significantLengthB);

            for (int i = 0; i < significantLengthA; i++)
            {
                int digitCompare = a[nonZeroA + i].CompareTo(b[nonZeroB + i]);
                if (digitCompare != 0)
                    return digitCompare;
            }

            // 값이 동일하면 선행 0이 적은 쪽을 우선 정렬해 결정적 순서 유지
            int totalLengthA = indexA - startA;
            int totalLengthB = indexB - startB;
            return totalLengthA.CompareTo(totalLengthB);
        }
    }
}
