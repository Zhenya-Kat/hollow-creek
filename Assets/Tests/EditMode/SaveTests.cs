using System.IO;
using HollowCreek.Core.Save;
using HollowCreek.Core.State;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HollowCreek.Tests
{
    public class SaveTests
    {
        string path;
        SaveStore store;

        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Path.GetTempPath(), "hollowcreek-test-" + System.Guid.NewGuid() + ".json");
            store = new SaveStore(path);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void WriteThenRead_RoundTrips()
        {
            var data = new SaveData { episode = "ep", location = "loc", position = new Vector3(1, 2, 3), yaw = 45 };
            data.facts.Add("a");
            data.askedTopics.Add("npc/q");
            store.Write(data);
            store.Write(data); // повторная запись заменяет файл

            var read = store.Read();
            Assert.AreEqual("ep", read.episode);
            Assert.AreEqual("loc", read.location);
            Assert.AreEqual(new Vector3(1, 2, 3), read.position);
            CollectionAssert.AreEqual(new[] { "a" }, read.facts);
            CollectionAssert.AreEqual(new[] { "npc/q" }, read.askedTopics);
            Assert.IsNotEmpty(read.savedAt);
            Assert.IsFalse(File.Exists(path + ".tmp"), "временный файл убран");
        }

        [Test]
        public void LegacyPoseRemainsMarkedForMigrationAndNewWorldPoseRoundTrips()
        {
            File.WriteAllText(path, "{\"version\":1,\"location\":\"house\",\"position\":{\"x\":0,\"y\":0,\"z\":13}}");
            var legacy = store.Read();
            Assert.That(legacy.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(legacy.worldSpacePose, Is.False);
            Assert.That(legacy.position.z, Is.EqualTo(13));
            legacy.worldSpacePose = true;
            store.Write(legacy);
            Assert.That(store.Read().worldSpacePose, Is.True);
            Assert.That(store.Read().position.z, Is.EqualTo(13));
        }
        [Test]
        public void Read_ReturnsNull_WhenMissingOrCorrupted()
        {
            Assert.IsNull(store.Read());
            File.WriteAllText(path, "{ это не json");
            LogAssert.ignoreFailingMessages = true;
            Assert.IsNull(store.Read());
        }

        [Test]
        public void Read_RejectsNewerVersion()
        {
            File.WriteAllText(path, "{\"version\": " + (SaveData.CurrentVersion + 1) + ", \"episode\": \"ep\"}");
            LogAssert.ignoreFailingMessages = true;
            Assert.IsNull(store.Read());
        }

        [Test]
        public void GameStateRestore_ReplacesFactsWithoutGrantEvents()
        {
            var a = GameStateTests.MakeFact("a");
            var b = GameStateTests.MakeFact("b");
            var state = new GameState();
            state.Grant(a);
            var granted = 0;
            var restored = 0;
            state.FactGranted += _ => granted++;
            state.Restored += () => restored++;

            state.Restore(new[] { b, b, null });

            Assert.IsFalse(state.Has(a));
            Assert.IsTrue(state.Has(b));
            Assert.AreEqual(1, state.Facts.Count, "дубликаты и пустые пропущены");
            Assert.AreEqual(0, granted);
            Assert.AreEqual(1, restored);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}

