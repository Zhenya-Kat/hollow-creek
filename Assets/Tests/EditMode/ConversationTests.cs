using System.Linq;
using HollowCreek.Core;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HollowCreek.Tests
{
    public class ConversationTests
    {
        FactDefinition clue, gate, reward;
        CharacterDefinition character;
        GameState state;
        DialogueLog log;

        [SetUp]
        public void SetUp()
        {
            clue = GameStateTests.MakeFact("clue");
            gate = GameStateTests.MakeFact("gate");
            reward = GameStateTests.MakeFact("reward");
            state = new GameState();
            log = new DialogueLog();
            Services.Clear();
            Services.Register(state);

            character = ScriptableObject.CreateInstance<CharacterDefinition>();
            ((Core.Data.GameDefinition)character).AssignId("npc");
            var so = new SerializedObject(character);
            var topics = so.FindProperty("topics");
            topics.arraySize = 2;
            topics.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "always";
            topics.GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "gated";
            topics.GetArrayElementAtIndex(1).FindPropertyRelative("availableWhen").managedReferenceValue = new HasFact(gate);
            var onAsked = topics.GetArrayElementAtIndex(0).FindPropertyRelative("onAsked");
            onAsked.arraySize = 1;
            onAsked.GetArrayElementAtIndex(0).managedReferenceValue = new GrantFactAction(reward);

            var reactions = so.FindProperty("reactions");
            reactions.arraySize = 1;
            reactions.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "clue";
            reactions.GetArrayElementAtIndex(0).FindPropertyRelative("evidence").objectReferenceValue = clue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Services.Clear();
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(clue);
            Object.DestroyImmediate(gate);
            Object.DestroyImmediate(reward);
        }

        Conversation Start() => new(character, state, log, null, firstMeeting: true);

        [Test]
        public void Topics_AppearWhenConditionMet()
        {
            var conversation = Start();
            CollectionAssert.AreEqual(new[] { "always" }, conversation.AvailableTopics.Select(t => t.Id));
            state.Grant(gate);
            CollectionAssert.AreEqual(new[] { "always", "gated" }, conversation.AvailableTopics.Select(t => t.Id));
        }

        [Test]
        public void Ask_MarksTopicAndRunsActions()
        {
            var conversation = Start();
            var topic = conversation.AvailableTopics.First();
            Assert.IsFalse(conversation.WasAsked(topic));

            conversation.Ask(topic);

            Assert.IsTrue(conversation.WasAsked(topic));
            Assert.IsTrue(state.Has(reward), "вопрос выдал улику");
            Assert.IsTrue(Start().WasAsked(topic), "отметка сохраняется между разговорами");
        }

        [Test]
        public void Present_UsesSpecificReactionOrDefault()
        {
            var conversation = Start();
            Assert.IsNotNull(conversation.FindReaction(clue));
            Assert.IsNull(conversation.FindReaction(gate), "для этой улики особой реакции нет");
            Assert.AreSame(character.DefaultReaction, conversation.Present(gate));
        }
    }
}
