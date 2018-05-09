using System;
using System.Collections.Generic;
using System.Linq;
using ModTek;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModTekUnitTests
{
    [TestClass]
    public class LoadOrderTests
    {
        List<ModDef> mods;

        [TestInitialize]
        public void Initialize()
        {
            mods = new List<ModDef>();
        }

        private void TestLoadOrder(List<ModDef> mods, string[] expectedLoadOrder, string[] expectedSkippedMods)
        {
            var skippedMods = new List<string>();
            var dict = new Dictionary<string, ModDef>();
            mods.ForEach(m => dict.Add(m.Name, m));
            var loadOrder = ModTek.ModTek.GetLoadOrder(dict, out skippedMods);
            
            Assert.AreEqual(0, skippedMods.Intersect(expectedLoadOrder).Count());
            Assert.AreEqual(expectedSkippedMods.Length, skippedMods.Union(expectedSkippedMods).Count());

            foreach (var expectedModName in expectedLoadOrder)
            {
                Assert.AreEqual(expectedModName, loadOrder.Dequeue().Name);
            }
        }

        [TestMethod]
        public void SimpleMods()
        {
            {
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            {
                var modDef = new ModDef();
                modDef.Name = "modB";
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA", "modB" }, new string[]{});
        }

        [TestMethod]
        public void SimpleDependency()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modA");
                mods.Add(modDef);
            }
            
            TestLoadOrder(mods, new[] { "modA", "modB" }, new string[] { });
        }

        [TestMethod]
        public void SimpleDependencyReverseOrder()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                modDef.DependsOn.Add("modB");
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modB";
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modB", "modA" }, new string[] { });
        }


        [TestMethod]
        public void SimpleTransitiveDependency()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modA");
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modC";
                modDef.DependsOn.Add("modB");
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA", "modB", "modC" }, new string[] { });
        }

        [TestMethod]
        public void ConflictsAreRemoved()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modA");
                mods.Add(modDef);
            }
            { // mod with DependsOn and ConflictsWith
                var modDef = new ModDef();
                modDef.Name = "modC";
                modDef.DependsOn.Add("modA");
                modDef.ConflictsWith.Add("modB");
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA", "modB" }, new [] { "modC" });
        }
        
        [TestMethod]
        public void AllConflictsAreRemovedEvenIfEarlier()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn and ConflictsWith
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modA");
                modDef.ConflictsWith.Add("modC");
                mods.Add(modDef);
            }
            { // mod with DependsOn and ConflictsWith
                var modDef = new ModDef();
                modDef.Name = "modC";
                modDef.DependsOn.Add("modA");
                modDef.ConflictsWith.Add("modB");
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA" }, new[] { "modB", "modC" });
        }

        [TestMethod]
        public void AllConflictsAreRemovedEvenWithMissingDependencies()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn and ConflictsWith
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modA");
                modDef.ConflictsWith.Add("modC");
                mods.Add(modDef);
            }
            { // mod with DependsOn and ConflictsWith
                var modDef = new ModDef();
                modDef.Name = "modC";
                modDef.DependsOn.Add("modX");
                modDef.ConflictsWith.Add("modB");
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA" }, new[] { "modB", "modC" });
        }

        [TestMethod]
        public void MissingDependencies()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modX");
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA" }, new[] { "modB" });
        }
        
        [TestMethod]
        public void CyclicDependencies()
        {
            { // simple mod
                var modDef = new ModDef();
                modDef.Name = "modA";
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modB";
                modDef.DependsOn.Add("modC");
                mods.Add(modDef);
            }
            { // mod with DependsOn
                var modDef = new ModDef();
                modDef.Name = "modC";
                modDef.DependsOn.Add("modB");
                mods.Add(modDef);
            }

            TestLoadOrder(mods, new[] { "modA" }, new[] { "modB", "modC" });
        }
    }
}
