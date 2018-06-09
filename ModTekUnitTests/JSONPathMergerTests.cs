using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModTek;
using Newtonsoft.Json.Linq;

namespace ModTekUnitTests
{
    [TestClass]
    public class JSONPathMergerTests
    {
        private JObject root;

        [TestInitialize]
        public void Initialize()
        {
            root = JObject.Parse(
@"
{
    ""objectkey1"": ""value1"",
    ""objectkey2"": [
        ""arrayvalue1"",
        {
            ""objectkey3"": ""value4"",
            ""objectkey4"": [
                ""arrayvalue2""
            ]
        },
        {
            ""objectkey5"": [
                ""arrayvalue3""
            ],
            ""objectkey6"": ""value5""
        },
        ""arrayvalue4""
    ]
}
");
        }

        [TestMethod]
        public void Test()
        {
            var a = root.SelectToken("$.objectkey1");
            var b = a.Parent;
            b.Remove();
            var test = root["objectkey1"];
            Assert.IsNull(test);
        }

        [TestMethod]
        public void RemoveFromObject()
        {
            Assert.AreEqual("value1", root["objectkey1"]);
            JSONPathMerger.ProcessReplacements(root,
@"
[
    {
        ""JSONPath"": ""$.objectkey1"",
        ""Action"": ""Remove""
    }
]
");
            Assert.IsNull(root["objectkey1"]);
        }

        [TestMethod]
        public void ReplaceInObject()
        {
            Assert.AreNotEqual("newvalue", root["objectkey2"]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2"",
        ""Action"": ""Replace"",
        ""Value"": ""newvalue""
    }
]
");
            Assert.AreEqual("newvalue", root["objectkey2"]);
        }

        [TestMethod]
        public void RemoveFromArray()
        {
            Assert.AreEqual("arrayvalue1", root["objectkey2"][0]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2[0]"",
        ""Action"": ""Remove""
    }
]
");
            Assert.AreNotEqual("arrayvalue1", root["objectkey2"][0]);
        }

        [TestMethod]
        public void ReplaceInArray()
        {
            Assert.AreNotEqual("newvalue", root["objectkey2"][0]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2[0]"",
        ""Action"": ""Replace"",
        ""Value"": ""newvalue""
    }
]
");
            Assert.AreEqual("newvalue", root["objectkey2"][0]);
        }

        [TestMethod]
        public void AddBeforeInArray()
        {
            var oldFirst = root["objectkey2"][0];
            Assert.AreNotEqual("newvalue", root["objectkey2"][0]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2[0]"",
        ""Action"": ""AddBefore"",
        ""Value"": ""newvalue""
    }
]
");
            Assert.AreEqual("newvalue", root["objectkey2"][0]);
            Assert.AreEqual(oldFirst, root["objectkey2"][1]);
        }

        [TestMethod]
        public void AddAfterInArray()
        {
            var currentFirst = root["objectkey2"][0];
            Assert.AreNotEqual("newvalue", root["objectkey2"][1]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2[0]"",
        ""Action"": ""AddAfter"",
        ""Value"": ""newvalue""
    }
]
");
            Assert.AreEqual(currentFirst, root["objectkey2"][0]);
            Assert.AreEqual("newvalue", root["objectkey2"][1]);
        }

        [TestMethod]
        public void AddBeforeInArrayWithArray()
        {
            var oldFirst = root["objectkey2"][0];
            Assert.IsNotNull(oldFirst);

            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2[0]"",
        ""Action"": ""AddBefore"",
        ""Value"": [""newvalue1"", ""newvalue2""]
    }
]
");
            Assert.AreEqual(new JArray("newvalue1", "newvalue2").ToString(), root["objectkey2"][0].ToString());
            Assert.AreEqual(oldFirst, root["objectkey2"][1]);
        }

        [TestMethod]
        public void AddInArrayWithArray()
        {
            var oldLast = root.SelectToken("$.objectkey2[-1:]").ToString();
            Assert.IsNotNull(oldLast);

            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2"",
        ""Action"": ""Add"",
        ""Value"": [""newvalue1"", ""newvalue2""]
    }
]
");
            Assert.AreEqual(oldLast, root.SelectToken("$.objectkey2[-2:-1:]").ToString());
            Assert.AreEqual(new JArray("newvalue1", "newvalue2").ToString(), root.SelectToken("$.objectkey2[-1:]").ToString());
        }

        [TestMethod]
        public void ConactInArrayWithArray()
        {
            var oldLast = root.SelectToken("$.objectkey2[-1:]").ToString();
            Assert.IsNotNull(oldLast);

            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2"",
        ""Action"": ""Concat"",
        ""Value"": [""newvalue1"", ""newvalue2""]
    }
]
");
            Assert.AreEqual(oldLast, root.SelectToken("$.objectkey2[-3:-2:]").ToString());
            Assert.AreEqual("newvalue1", root.SelectToken("$.objectkey2[-2:-1:]").ToString());
            Assert.AreEqual("newvalue2", root.SelectToken("$.objectkey2[-1:]").ToString());
        }

        [TestMethod]
        public void MergeRootObject()
        {
            Assert.IsNull(root["newobjectkey"]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$"",
        ""Action"": ""Merge"",
        ""Value"": { ""newobjectkey"": ""newvalue"" }
    }
]
");
            Assert.AreEqual("newvalue", root["newobjectkey"]);
        }

        [TestMethod]
        public void MergeNestedObject()
        {
            Assert.IsNull(root["objectkey2"][1]["newobjectkey"]);
            JSONPathMerger.ProcessReplacements(root,
                @"
[
    {
        ""JSONPath"": ""$.objectkey2[1]"",
        ""Action"": ""Merge"",
        ""Value"": { ""newobjectkey"": ""newvalue"" }
    }
]
");
            Assert.AreEqual("newvalue", root["objectkey2"][1]["newobjectkey"]);
        }
    }
}
