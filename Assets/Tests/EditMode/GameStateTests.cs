using System;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollowCreek.Tests
{
    public class GameStateTests
    {
        FactDefinition a, b, c;
        GameState state;

        [SetUp]
        public void SetUp()
        {
            a = MakeFact("a");
            b = MakeFact("b");
            c = MakeFact("c");
            state = new GameState();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        internal static FactDefinition MakeFact(string id)
        {
            var fact = ScriptableObject.CreateInstance<FactDefinition>();
            fact.name = id;
            fact.AssignId(id);
            return fact;
        }

        [Test]
        public void Grant_AddsFactOnce_AndRaisesEventOnce()
        {
            var raised = 0;
            state.FactGranted += _ => raised++;

            Assert.IsTrue(state.Grant(a));
            Assert.IsFalse(state.Grant(a));

            Assert.IsTrue(state.Has(a));
            Assert.AreEqual(1, raised);
            Assert.AreEqual(1, state.Facts.Count);
        }

        [Test]
        public void Facts_KeepGrantOrder()
        {
            state.Grant(b);
            state.Grant(a);
            CollectionAssert.AreEqual(new[] { b, a }, state.Facts);
        }

        [Test]
        public void Grant_Throws_ForFactWithoutId()
        {
            var noId = ScriptableObject.CreateInstance<FactDefinition>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => state.Grant(noId));
            }
            finally
            {
                Object.DestroyImmediate(noId);
            }
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            state.Grant(a);
            state.Clear();
            Assert.IsFalse(state.Has(a));
            Assert.AreEqual(0, state.Facts.Count);
        }

        [Test]
        public void Conditions_EvaluateAgainstState()
        {
            state.Grant(a);
            state.Grant(b);

            Assert.IsTrue(new HasFact(a).Evaluate(state));
            Assert.IsFalse(new HasFact(c).Evaluate(state));
            Assert.IsTrue(new AllOf(new HasFact(a), new HasFact(b)).Evaluate(state));
            Assert.IsFalse(new AllOf(new HasFact(a), new HasFact(c)).Evaluate(state));
            Assert.IsTrue(new AnyOf(new HasFact(c), new HasFact(b)).Evaluate(state));
            Assert.IsFalse(new AnyOf(new HasFact(c)).Evaluate(state));
            Assert.IsTrue(new Not(new HasFact(c)).Evaluate(state));
        }

        [Test]
        public void EmptyConditions_BehaveSensibly()
        {
            Assert.IsTrue(Condition.IsMet(null, state), "нет условия — выполнено");
            Assert.IsTrue(new AllOf().Evaluate(state), "пустое «все» — выполнено");
            Assert.IsFalse(new AnyOf().Evaluate(state), "пустое «хотя бы одно» — не выполнено");
        }
    }
}
