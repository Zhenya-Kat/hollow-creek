using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using HollowCreek.Core.Story;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollowCreek.Tests
{
    public class StoryLogicTests
    {
        FactDefinition a, b;
        GameState state;

        [SetUp]
        public void SetUp()
        {
            a = GameStateTests.MakeFact("a");
            b = GameStateTests.MakeFact("b");
            state = new GameState();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void Objectives_CurrentIsFirstActiveAndIncomplete()
        {
            var set = ScriptableObject.CreateInstance<ObjectiveSet>();
            var so = new SerializedObject(set);
            var list = so.FindProperty("objectives");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("completeWhen").managedReferenceValue = new HasFact(a);
            list.GetArrayElementAtIndex(1).FindPropertyRelative("activeWhen").managedReferenceValue = new HasFact(b);
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreSame(set.Objectives[0], set.Current(state), "первая цель открыта с начала");
            state.Grant(a);
            Assert.IsNull(set.Current(state), "первая выполнена, вторая ещё не активна");
            state.Grant(b);
            Assert.AreSame(set.Objectives[1], set.Current(state));
            Assert.IsFalse(set.Objectives[1].IsComplete(state), "цель без условия выполнения не завершается");

            Object.DestroyImmediate(set);
        }

        [Test]
        public void Case_JudgesSuspectThenMotiveThenLie()
        {
            var culprit = ScriptableObject.CreateInstance<CharacterDefinition>();
            var innocent = ScriptableObject.CreateInstance<CharacterDefinition>();
            var caseDef = ScriptableObject.CreateInstance<CaseDefinition>();
            var so = new SerializedObject(caseDef);
            so.FindProperty("culprit").objectReferenceValue = culprit;
            var motives = so.FindProperty("acceptedMotives");
            motives.arraySize = 1;
            motives.GetArrayElementAtIndex(0).objectReferenceValue = a;
            var lies = so.FindProperty("acceptedLies");
            lies.arraySize = 1;
            lies.GetArrayElementAtIndex(0).objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(Verdict.Correct, caseDef.Judge(culprit, a, b));
            Assert.AreEqual(Verdict.WrongSuspect, caseDef.Judge(innocent, a, b));
            Assert.AreEqual(Verdict.WrongMotive, caseDef.Judge(culprit, b, b));
            Assert.AreEqual(Verdict.WrongLie, caseDef.Judge(culprit, a, a));
            Assert.AreEqual(Verdict.WrongMotive, caseDef.Judge(culprit, null, null));

            Object.DestroyImmediate(caseDef);
            Object.DestroyImmediate(culprit);
            Object.DestroyImmediate(innocent);
        }
    }
}
