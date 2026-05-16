using Sizzle.GameTagSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sizzle.AbilitySystem.Editor
{
    public class AbilityGameTagCodeGenerator : EditorWindow
    {
        private const string CLASS_NAME = "AbilityTags";

        public const string GENERATED_FILE_NAME = "AbilityTags_Generated";
        public const string GENERATED_FILE_PATH = "Assets/Scripts/Generated/" + GENERATED_FILE_NAME + ".cs";
        public const string CUSTOM_CLASS_NAME = "AbilityTags";
        public const string CUSTOM_FILE_PATH = "Assets/Scripts/" + CUSTOM_CLASS_NAME + ".cs";

        [MenuItem(PATHS.MENUITEM_ROOT + "Generate AbilityTags Define Script", priority = 100)]
        public static void AskGenerate()
        {
            string title = "AbilityTags 정의 스크립트 생성";
            string message =
                "다음 작업을 수행합니다:\n\n" +
                "· 프로젝트 내의 모든 'Ability' 애셋을 스캔하여 사용된 고유 태그를 수집합니다.\n" +
                "· 수집된 태그를 기반으로 자동 생성 파일을 만듭니다.\n\n" +
                "생성 결과:\n" +
                "· 생성 파일: " + GENERATED_FILE_PATH + "\n" +
                "· 포함 항목: Ability.TagSet에 정의된 MainTag, CancelAbilitiesWithTag, BlockAbilitiesWithTag,\n" +
                "              ActivationOwnedTags, ActivationRequiredTags, ActivationBlockedTags\n\n" +
                "주의사항:\n" +
                "· 이미 존재하는 '" + CUSTOM_CLASS_NAME + ".cs' 파일에 정의된 커스텀 태그는 유지되며, 자동 생성 파일에는 주석 처리됩니다.\n" +
                "· 자동 생성 파일은 덮어써집니다. 커스텀으로 유지하고 싶은 태그는 반드시 '" + CUSTOM_FILE_PATH + "'에 직접 추가하세요.\n\n" +
                "계속 진행하시겠습니까?";

            if (EditorUtility.DisplayDialog(title, message, "생성", "취소"))
            {
                Generate();
                GameTagUtils.CacheGameTagsInAbilityAssets();
            }
        }

        public static void Generate()
        {
            HashSet<GameTag> uniqueTags = new HashSet<GameTag>();

            string[] guids = AssetDatabase.FindAssets("t:Ability");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Ability ability = AssetDatabase.LoadAssetAtPath<Ability>(path);

                if (ability == null || ability.TagSet == null)
                    continue;

                AbilityGameTagSet tagSet = ability.TagSet;
                uniqueTags.Add(tagSet.MainTag);

                foreach (var tag in tagSet.CancelAbilitiesWithTag)
                    uniqueTags.Add(tag);

                foreach (var tag in tagSet.BlockAbilitiesWithTag)
                    uniqueTags.Add(tag);

                foreach (var tag in tagSet.ActivationOwnedTags)
                    uniqueTags.Add(tag);

                foreach (var tag in tagSet.ActivationRequiredTags)
                    uniqueTags.Add(tag);

                foreach (var tag in tagSet.ActivationBlockedTags)
                    uniqueTags.Add(tag);

                Resources.UnloadAsset(ability);
            }

            if (uniqueTags.Count == 0)
            {
                Debug.LogWarning("No unique Ability.MainTag values found.");
                return;
            }

            if (!Directory.Exists(Path.GetDirectoryName(GENERATED_FILE_PATH)))
                Directory.CreateDirectory(Path.GetDirectoryName(GENERATED_FILE_PATH));

            if (!File.Exists(CUSTOM_FILE_PATH))
                File.WriteAllText(CUSTOM_FILE_PATH, GenerateCustomClassContent(), Encoding.UTF8);

            var customTags = GetReadonlyGameTagsFromFile(CUSTOM_FILE_PATH);

            File.WriteAllText(GENERATED_FILE_PATH, 
                GenerateClassContent(uniqueTags, customTags), 
                Encoding.UTF8);

            AssetDatabase.Refresh();

            Debug.Log("AbilityTags_Generated.cs 파일이 성공적으로 생성되었습니다. 생성 결로 : " + GENERATED_FILE_PATH);

            global::System.GC.Collect();
        }

        private static string GenerateClassContent(HashSet<GameTag> tags, List<GameTag> ignoreTags)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// ------------------------------------------------------------------------------");
            sb.AppendLine("// 이 파일은 자동으로 생성되었습니다.");
            sb.AppendLine("//");
            sb.AppendLine("// * 프로젝트 내의 Ability 애셋들 내부에 정의된 Tag들에 의해 수집된 태그 목록입니다.");
            sb.AppendLine("// * 이 파일을 수동으로 수정하지 마십시오.");
            sb.AppendLine("// * 변경이 필요한 경우 원본 소스 또는 코드 생성기를 업데이트하십시오.");
            sb.AppendLine("//");
            sb.AppendLine("// 수동 수정은 예기치 않은 동작을 유발할 수 있습니다.");
            sb.AppendLine("//");
            sb.AppendLine("// 이 코드는 'AbilityGameTagCodeGenerator.cs'에 의해 생성되었습니다.");
            sb.AppendLine("// ------------------------------------------------------------------------------");
            sb.AppendLine("using Sizzle.GameTagSystem;");
            sb.AppendLine();
            sb.AppendLine("public static partial class " + CLASS_NAME);
            sb.AppendLine("{");

            char firstChar = char.MinValue;
            foreach (var tag in tags.OrderBy(t => t.TagName))
            {
                if (string.IsNullOrEmpty(tag.TagName))
                    continue;
                string tagName = tag.TagName;

                if (tagName[0] != firstChar)
                {
                    if (firstChar != char.MinValue)
                        sb.AppendLine();

                    firstChar = tagName[0];
                }

                string constantName = ConvertToConstantName(tagName);

                if (!ignoreTags.Contains(tag))
                    sb.AppendLine($"    public static readonly GameTag {constantName} = new GameTag(\"{tag}\");");
                else
                    sb.AppendLine($"    // public static readonly GameTag {constantName} = new GameTag(\"{tag}\"); // Commented out because it's defined in custom tags.");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateCustomClassContent()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// ------------------------------------------------------------------------------");
            sb.AppendLine("// 이 파일은 Ability 애셋들에 직접 작성된 태그가 아닌 커스텀 태그를 정의하는 파일입니다.");
            sb.AppendLine("// 프로젝트에 이 파일이 없을 경우에만, 빈 클래스로 파일이 추가됩니다.");
            sb.AppendLine("//");
            sb.AppendLine("// * 자유롭게 태그를 추가해주세요.");
            sb.AppendLine("// * 경로를 변경할 경우, GameTag의 선택 리스트에서 정의된 태그들이 보여지지 않습니다.");
            sb.AppendLine("// * public static readonly GameTag [TAG_NAME] = \"TagValue\" 형식을 읽어들입니다.");
            sb.AppendLine("//");
            sb.AppendLine("// 이 코드는 'AbilityGameTagCodeGenerator.cs'에 의해 최초 생성되었습니다.");
            sb.AppendLine("// ------------------------------------------------------------------------------");
            sb.AppendLine("public static partial class " + CLASS_NAME);
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string ConvertToConstantName(string tag)
        {
            return tag.ToUpper().Replace(GameTag.SEPARATOR.ToString(), "_");
        }

        // param으로 받은 파일경로의 파일에 GameTag 상수들을 읽어들여 리스트로 반환합니다.
        private static List<GameTag> GetReadonlyGameTagsFromFile(string filePath)
        {
            List<GameTag> tags = new List<GameTag>();
            if (!File.Exists(filePath))
                return tags;
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("public static readonly GameTag "))
                {
                    int equalsIndex = trimmedLine.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        string tagValuePart = trimmedLine.Substring(equalsIndex + 1).Trim().TrimEnd(';').Trim();
                        if (tagValuePart.StartsWith("new GameTag(\"") && tagValuePart.EndsWith("\")"))
                        {
                            string tagName = tagValuePart.Substring(13, tagValuePart.Length - 15);
                            tags.Add(new GameTag(tagName));
                        }
                    }
                }
            }
            return tags;
        }
    }
}