using System.Linq;
using HollowCreek.Core;
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
    public class StoryRuntimeTests
    {
        FactDefinition a, b, c;
        GameState state;

        [SetUp]
        public void SetUp()
        {
            a = GameStateTests.MakeFact("a");
            b = GameStateTests.MakeFact("b");
            c = GameStateTests.MakeFact("c");
            state = new GameState();
            Services.Clear();
            Services.Register(state);
        }

        [TearDown]
        public void TearDown()
        {
            Services.Clear();
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        StoryRuleSet Rules(params (Condition when, FactDefinition grant)[] rules)
        {
            var set = ScriptableObject.CreateInstance<StoryRuleSet>();
            var so = new SerializedObject(set);
            var list = so.FindProperty("rules");
            list.arraySize = rules.Length;
            for (var i = 0; i < rules.Length; i++)
            {
                var p = list.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("when").managedReferenceValue = rules[i].when;
                var actions = p.FindPropertyRelative("actions");
                actions.arraySize = 1;
                actions.GetArrayElementAtIndex(0).managedReferenceValue = new GrantFactAction(rules[i].grant);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        [Test]
        public void Rules_FireOnceAndChain()
        {
            // a → b; b → c (второе правило срабатывает от результата первого).
            var set = Rules(
                (new AllOf(new HasFact(a), new Not(new HasFact(b))), b),
                (new AllOf(new HasFact(b), new Not(new HasFact(c))), c));
            var runner = new StoryRuleRunner(set, state);
            var fired = 0;
            runner.Fired += _ => fired++;
            state.FactGranted += _ => runner.Evaluate();

            runner.Evaluate();
            Assert.AreEqual(0, fired, "без факта a ничего не происходит");

            state.Grant(a);
            Assert.IsTrue(state.Has(b) && state.Has(c));
            Assert.AreEqual(2, fired);

            runner.Evaluate();
            Assert.AreEqual(2, fired, "повторно не срабатывают");
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Rules_WithoutGuardStopAfterLimit()
        {
            // Правило без «Не → Есть факт»: срабатывало бы вечно, если бы не ограничитель.
            var set = Rules((new HasFact(a), b));
            var runner = new StoryRuleRunner(set, state);
            state.Grant(a);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            runner.Evaluate();
            Assert.Pass("не зациклилось");
        }

        [Test]
        public void Objectives_RevealHintsInOrder_AndRoundTrip()
        {
            var set = ScriptableObject.CreateInstance<ObjectiveSet>();
            var so = new SerializedObject(set);
            var list = so.FindProperty("objectives");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "goal";
            list.GetArrayElementAtIndex(0).FindPropertyRelative("hints").arraySize = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            var tracker = new ObjectiveTracker(set, state);
            var goal = tracker.Current;
            Assert.AreEqual(0, tracker.RevealedHints(goal));
            tracker.RevealNextHint(goal);
            Assert.IsFalse(tracker.IsNextHintTheAnswer(goal));
            tracker.RevealNextHint(goal);
            Assert.IsTrue(tracker.IsNextHintTheAnswer(goal), "третья — ответ");
            tracker.RevealNextHint(goal);
            Assert.IsFalse(tracker.HasMoreHints(goal));
            Assert.IsFalse(tracker.RevealNextHint(goal));

            var restored = new ObjectiveTracker(set, state);
            restored.LoadHints(tracker.SaveHints().ToList());
            Assert.AreEqual(3, restored.RevealedHints(goal));
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Case_CorrectAccusationMarksSolved()
        {
            var culprit = ScriptableObject.CreateInstance<CharacterDefinition>();
            var caseDef = ScriptableObject.CreateInstance<CaseDefinition>();
            var so = new SerializedObject(caseDef);
            so.FindProperty("culprit").objectReferenceValue = culprit;
            so.FindProperty("solvedFact").objectReferenceValue = c;
            so.FindProperty("availableWhen").managedReferenceValue = new HasFact(a);
            so.FindProperty("acceptedMotives").arraySize = 1;
            so.FindProperty("acceptedMotives").GetArrayElementAtIndex(0).objectReferenceValue = a;
            so.FindProperty("acceptedLies").arraySize = 1;
            so.FindProperty("acceptedLies").GetArrayElementAtIndex(0).objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();

            var service = new CaseService(caseDef, state);
            Assert.IsFalse(service.IsAvailable);
            state.Grant(a);
            Assert.IsTrue(service.IsAvailable);

            Assert.AreEqual(Verdict.WrongLie, service.Accuse(culprit, a, a));
            Assert.IsFalse(service.IsSolved);
            var solvedEvents = 0;
            service.Solved += () => solvedEvents++;
            Assert.AreEqual(Verdict.Correct, service.Accuse(culprit, a, b));
            Assert.IsTrue(service.IsSolved);
            Assert.AreEqual(1, solvedEvents);

            Object.DestroyImmediate(caseDef);
            Object.DestroyImmediate(culprit);
        }
    }
}
