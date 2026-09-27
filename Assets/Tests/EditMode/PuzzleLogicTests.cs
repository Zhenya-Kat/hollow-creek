using HollowCreek.Core.Puzzles;
using NUnit.Framework;

namespace HollowCreek.Tests
{
    public class PuzzleLogicTests
    {
        [Test]
        public void CodeLock_AcceptsCorrectCode_AndResetsWrongOne()
        {
            var lockState = new CodeLockState("1031");
            foreach (var c in "1234") lockState.Press(c);
            Assert.IsTrue(lockState.IsFull);
            Assert.IsFalse(lockState.Press('5'), "лишняя цифра не добавляется");
            Assert.IsFalse(lockState.Submit());
            Assert.AreEqual(string.Empty, lockState.Entered, "неверный код сбрасывается");

            foreach (var c in "1031") lockState.Press(c);
            Assert.IsTrue(lockState.Submit());
        }

        [Test]
        public void CodeLock_EraseRemovesLastDigit()
        {
            var lockState = new CodeLockState("12");
            lockState.Press('1');
            lockState.Press('3');
            lockState.Erase();
            lockState.Press('2');
            Assert.IsTrue(lockState.Submit());
        }

        [Test]
        public void Sequence_SolvedWhenInCardOrder()
        {
            var state = new SequenceState(4, new[] { 1, 3, 0, 2 });
            Assert.IsFalse(state.IsSolved);
            state.Swap(0, 2); // 0 3 1 2
            state.Swap(1, 2); // 0 1 3 2
            state.Swap(2, 3); // 0 1 2 3
            Assert.IsTrue(state.IsSolved);
        }

        [Test]
        public void Sequence_InvalidStartFallsBackToIdentity()
        {
            var state = new SequenceState(3, new[] { 0, 0, 1 });
            Assert.AreEqual(0, state.CardAt(0));
            Assert.AreEqual(2, state.CardAt(2));
        }

        [Test]
        public void Rubbing_CompleteWhenEveryLineCovered()
        {
            var state = new RubbingState(lines: 2, segmentsPerLine: 10, requiredCoverage: 0.8f);
            for (var i = 0; i < 10; i++) state.Rub(0, 0.05f + i * 0.1f, 0.01f);
            Assert.IsTrue(state.IsLineComplete(0));
            Assert.IsFalse(state.IsComplete, "вторая строка ещё чистая");

            for (var i = 0; i < 7; i++) state.Rub(1, 0.05f + i * 0.1f, 0.01f);
            Assert.AreEqual(0.7f, state.Progress(1), 0.001f);
            Assert.IsFalse(state.IsComplete, "70% меньше требуемых 80%");

            state.Rub(1, 0.8f, 0.05f);
            Assert.IsTrue(state.IsComplete);
        }

        [Test]
        public void Rubbing_IgnoresOutOfRangeLines()
        {
            var state = new RubbingState(1, 8, 1f);
            Assert.IsFalse(state.Rub(5, 0.5f, 0.1f));
            Assert.IsFalse(state.Rub(-1, 0.5f, 0.1f));
        }
    }
}
