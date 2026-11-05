using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Sizzle.Toolkits.Data;

namespace Sizzle.Toolkits.Tests.Data
{
    [TestFixture]
    public class TableParserTests
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

        public struct DummyTag
        {
            public string Value { get; }
            public DummyTag(string value) => Value = value;
        }

        public class TestCharacterModel
        {
            [CSVColumn("char_id", IsRequired = true)]
            public string Id;

            [CSVColumn("display_name")]
            public string Name { get; private set; }

            [CSVColumn("char_level", DefaultValue = 1)]
            private int m_level;

            [CSVColumn("tags")]
            public string[] Tags;

            public int Level => m_level;
        }

        public class TestCustomTagModel
        {
            [CSVColumn("id")]
            public int Id;

            [CSVColumn("custom_tag")]
            public DummyTag Tag;
        }

        public class TestCharacterTable : DataTable<string, TestCharacterModel>
        {
            protected override string GetKey(TestCharacterModel data) => data.Id;
            public override void Load() { }
        }

        [Test]
        public void Test01_CSVParser_AutoMapping_MatchesFields()
        {
            string csv = "Id,Name,Weight,IsEquipped,Type,Description\n" +
                         "101,Iron Sword,3.5,true,Weapon,A basic iron blade\n" +
                         "102,Steel Shield,5.0,false,Armor,Sturdy shield\n" +
                         "103,Health Potion,0.5,false,Consumable,Restores 50 HP\n";

            var result = CSVParser.Parse<TestItem>(csv);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(101, result[0].Id);
            Assert.AreEqual("Iron Sword", result[0].Name);
            Assert.IsTrue(Mathf.Approximately(result[0].Weight, 3.5f));
            Assert.IsTrue(result[0].IsEquipped);
            Assert.AreEqual(ItemType.Weapon, result[0].Type);
        }

        [Test]
        public void Test02_CSVParser_RFC4180_EscapesAndMultiline()
        {
            string rfcCsv = "Id,Name,Weight,IsEquipped,Type,Description\n" +
                            "201,\"Sword, Master\",4.2,true,Weapon,\"He said, \"\"Take this blade!\"\"\"\n" +
                            "202,Magic Cloak,1.0,false,Armor,\"Line 1\nLine 2\"\n";

            var result = CSVParser.Parse<TestItem>(rfcCsv);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Sword, Master", result[0].Name);
            Assert.AreEqual("He said, \"Take this blade!\"", result[0].Description);
            Assert.AreEqual("Line 1\nLine 2", result[1].Description);
        }

        [Test]
        public void Test03_CSVParser_LowLevelTokenizer_CustomParse()
        {
            string csv = "# Comment line\n" +
                         "// Another comment\n" +
                         "Id,Name,Weight\n" +
                         "301,Custom Axe,7.5\n";

            var rows = CSVParser.ParseCSVRows(csv);
            var items = new List<TestItem>();
            for (int i = 1; i < rows.Count; i++)
            {
                var tokens = rows[i];
                items.Add(new TestItem
                {
                    Id = int.Parse(tokens[0]),
                    Name = tokens[1],
                    Weight = float.Parse(tokens[2], System.Globalization.CultureInfo.InvariantCulture)
                });
            }

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(301, items[0].Id);
            Assert.AreEqual("Custom Axe", items[0].Name);
            Assert.IsTrue(Mathf.Approximately(items[0].Weight, 7.5f));
        }

        [Test]
        public void Test04_JSONTableParser_ArrayJson_ParsesCorrectly()
        {
            string json = "[{\"Id\":401,\"Name\":\"Dragon Bow\",\"Weight\":2.2,\"IsEquipped\":true,\"Type\":0,\"Description\":\"Legendary\"}]";
            var parser = new JSONTableParser<TestItem>();
            var items = parser.Parse(json).ToList();

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(401, items[0].Id);
            Assert.AreEqual("Dragon Bow", items[0].Name);
        }

        [Test]
        public void Test05_DataTable_CSVParserResult_LoadsAndQueries()
        {
            string csv = "Id,Name,Weight,IsEquipped,Type,Description\n" +
                         "501,Item A,1.0,false,0,First\n" +
                         "502,Item B,2.0,true,1,Second\n";

            var table = new TestItemTable();
            var result = CSVParser.Parse<TestItem>(csv);
            table.Load(result);

            Assert.IsTrue(table.IsLoaded);
            Assert.AreEqual(2, table.Count);
            Assert.IsTrue(table.ContainsKey(501));
            Assert.AreEqual("Item B", table[502].Name);
            Assert.IsTrue(table.TryGet(501, out var a));
            Assert.AreEqual("Item A", a.Name);
        }

        [Test]
        public void Test06_CSVParser_ColumnAttribute_BindsPrivateAndProperties()
        {
            string csv = "char_id,display_name,char_level,tags\n" +
                         "C01,Knight,10,Warrior|Tank\n" +
                         "C02,Wizard,,Mage|DPS\n"; // C02 level is empty -> DefaultValue 1 applied

            var result = CSVParser.Parse<TestCharacterModel>(csv);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(2, result.SuccessCount);
            Assert.AreEqual("C01", result[0].Id);
            Assert.AreEqual("Knight", result[0].Name);
            Assert.AreEqual(10, result[0].Level);
            Assert.AreEqual(2, result[0].Tags.Length);

            // Default value for empty cell
            Assert.AreEqual("C02", result[1].Id);
            Assert.AreEqual("Wizard", result[1].Name);
            Assert.AreEqual(1, result[1].Level);
        }

        [Test]
        public void Test07_CSVParser_Result_DiagnosticsAndQueries()
        {
            string errorCsv = "char_id,display_name,char_level\n" +
                              "C03,Rogue,25\n" +
                              ",InvalidHero,30\n"; // Missing required char_id

            var result = CSVParser.Parse<TestCharacterModel>(errorCsv);
            var dict = result.ToDictionary(x => x.Id);
            var found = result.Find(x => x.Level == 25);

            Assert.IsTrue(result.HasErrors);
            Assert.AreEqual(1, result.FailedCount);
            Assert.AreEqual(1, result.SuccessCount);
            Assert.IsTrue(dict.ContainsKey("C03"));
            Assert.IsNotNull(found);
            Assert.AreEqual("Rogue", found.Name);
        }

        [Test]
        public void Test08_CSVParser_CustomConverter_Via_CSVParseConfig()
        {
            string csv = "id,custom_tag\n" +
                         "1,Player.State.Alive\n" +
                         "2,Player.State.Dead\n";

            // 전역 상태 없이 CSVParseConfig 객체에 커스텀 변환 람다를 등록하여 주입
            var config = new CSVParseConfig()
                .AddConverter<DummyTag>(raw => new DummyTag("Converted:" + raw));

            var result = CSVParser.Parse<TestCustomTagModel>(csv, config);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(2, result.SuccessCount);
            Assert.AreEqual("Converted:Player.State.Alive", result[0].Tag.Value);
            Assert.AreEqual("Converted:Player.State.Dead", result[1].Tag.Value);
        }

        [Test]
        public void Test09_DataTable_LoadCSVParseResult_DirectBinding()
        {
            string csv = "char_id,display_name,char_level\n" +
                         "C10,Archer,12\n" +
                         "C11,Cleric,15\n";

            var parseResult = CSVParser.Parse<TestCharacterModel>(csv);
            var table = new TestCharacterTable();
            table.Load(parseResult, x => x.Id);

            Assert.IsTrue(table.IsLoaded);
            Assert.AreEqual(2, table.Count);
            Assert.IsTrue(table.ContainsKey("C10"));
            Assert.AreEqual("Cleric", table["C11"].Name);
        }

        [Test]
        public void Test10_CSVParseConfig_Isolation_And_AutoStringCtor_Fallback()
        {
            string csv = "id,custom_tag\n" +
                         "1,AutoTag.Value\n";

            // 커스텀 컨버터가 없는 기본 Config인 경우, 단일 string 생성자(DummyTag(string))가 자동 감지되어 동작
            var defaultConfig = CSVParseConfig.Default;
            var result = CSVParser.Parse<TestCustomTagModel>(csv, defaultConfig);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("AutoTag.Value", result[0].Tag.Value);
        }
    }
}
