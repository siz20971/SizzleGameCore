#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Sizzle.Toolkits.Data;

namespace Sizzle.Toolkits.Editor.Data
{
    /// <summary>
    /// ITableParser, CSVTableParser, JSONTableParser, DataTable 연동 검증용 에디터 테스트 스크립트입니다.
    /// Unity 메뉴: Tools > Sizzle > Run Table Parser Tests
    /// </summary>
    public static class TableParserTests
    {
        public enum ItemType
        {
            Weapon,
            Armor,
            Consumable
        }

        [Serializable]
        public class TestItem
        {
            public int Id;
            public string Name;
            public float Weight;
            public bool IsEquipped;
            public ItemType Type;
            public string Description;
        }

        public class TestItemTable : DataTable<int, TestItem>
        {
            protected override int GetKey(TestItem data) => data.Id;
            public override void Load() { }
        }

        [MenuItem("Tools/Sizzle/Run Table Parser Tests")]
        public static void RunTests()
        {
            int passCount = 0;
            int failCount = 0;

            // Test 1: CSV Auto Mapping
            try
            {
                string csv = "Id,Name,Weight,IsEquipped,Type,Description\n" +
                             "101,Iron Sword,3.5,true,Weapon,A basic iron blade\n" +
                             "102,Steel Shield,5.0,false,Armor,Sturdy shield\n" +
                             "103,Health Potion,0.5,false,Consumable,Restores 50 HP\n";

                var parser = new CSVTableParser<TestItem>();
                var items = parser.Parse(csv).ToList();

                if (items.Count == 3 && items[0].Id == 101 && items[0].Name == "Iron Sword" &&
                    Mathf.Approximately(items[0].Weight, 3.5f) && items[0].IsEquipped && items[0].Type == ItemType.Weapon)
                {
                    passCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogError("[TableParserTests] Test 1 Failed: Item fields do not match expected values.");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Debug.LogError($"[TableParserTests] Test 1 Exception: {ex}");
            }

            // Test 2: RFC 4180 Escapes & Multiline
            try
            {
                string rfcCsv = "Id,Name,Weight,IsEquipped,Type,Description\n" +
                                "201,\"Sword, Master\",4.2,true,Weapon,\"He said, \"\"Take this blade!\"\"\"\n" +
                                "202,Magic Cloak,1.0,false,Armor,\"Line 1\nLine 2\"\n";

                var parser = new CSVTableParser<TestItem>();
                var items = parser.Parse(rfcCsv).ToList();

                if (items.Count == 2 &&
                    items[0].Name == "Sword, Master" &&
                    items[0].Description == "He said, \"Take this blade!\"" &&
                    items[1].Description == "Line 1\nLine 2")
                {
                    passCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogError("[TableParserTests] Test 2 Failed: RFC 4180 parsing mismatch.");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Debug.LogError($"[TableParserTests] Test 2 Exception: {ex}");
            }

            // Test 3: Custom Row Converter
            try
            {
                string csv = "# Comment line\n" +
                             "// Another comment\n" +
                             "Id,Name,Weight\n" +
                             "301,Custom Axe,7.5\n";

                var parser = new CSVTableParser<TestItem>(tokens => new TestItem
                {
                    Id = int.Parse(tokens[0]),
                    Name = tokens[1],
                    Weight = float.Parse(tokens[2], System.Globalization.CultureInfo.InvariantCulture)
                });

                var items = parser.Parse(csv).ToList();
                if (items.Count == 1 && items[0].Id == 301 && items[0].Name == "Custom Axe" && Mathf.Approximately(items[0].Weight, 7.5f))
                {
                    passCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogError("[TableParserTests] Test 3 Failed: Custom converter mismatch.");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Debug.LogError($"[TableParserTests] Test 3 Exception: {ex}");
            }

            // Test 4: JSON Array Parsing (with JsonUtility)
            try
            {
                string json = "[{\"Id\":401,\"Name\":\"Dragon Bow\",\"Weight\":2.2,\"IsEquipped\":true,\"Type\":0,\"Description\":\"Legendary\"}]";
                var parser = new JSONTableParser<TestItem>();
                var items = parser.Parse(json).ToList();

                if (items.Count == 1 && items[0].Id == 401 && items[0].Name == "Dragon Bow")
                {
                    passCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogError("[TableParserTests] Test 4 Failed: JSON parsing mismatch.");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Debug.LogError($"[TableParserTests] Test 4 Exception: {ex}");
            }

            // Test 5: DataTable Integration
            try
            {
                string csv = "Id,Name,Weight,IsEquipped,Type,Description\n" +
                             "501,Item A,1.0,false,0,First\n" +
                             "502,Item B,2.0,true,1,Second\n";

                var table = new TestItemTable();
                table.Load(csv, new CSVTableParser<TestItem>());

                if (table.IsLoaded && table.Count == 2 && table.ContainsKey(501) && table[502].Name == "Item B" &&
                    table.TryGet(501, out var a) && a.Name == "Item A")
                {
                    passCount++;
                }
                else
                {
                    failCount++;
                    Debug.LogError("[TableParserTests] Test 5 Failed: DataTable load/query mismatch.");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                Debug.LogError($"[TableParserTests] Test 5 Exception: {ex}");
            }

            if (failCount == 0)
            {
                Debug.Log($"<color=green><b>[TableParserTests] All {passCount} tests PASSED successfully!</b></color>");
            }
            else
            {
                Debug.LogError($"[TableParserTests] {failCount} tests FAILED out of {passCount + failCount}.");
            }
        }
    }
}
#endif
