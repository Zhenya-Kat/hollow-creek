using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Puzzles
{
    /// <summary>
    /// Головоломка «расставь по порядку»: карточки (например, снимки) меняются местами попарно.
    /// Правильный порядок — порядок в списке <see cref="Cards"/>; начальный задаётся отдельно.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Puzzles/Sequence", fileName = "Sequence", order = 41)]
    public sealed class SequencePuzzle : PuzzleDefinition
    {
        [SerializeField, Tooltip("Карточки в ПРАВИЛЬНОМ порядке")]
        List<SequenceCard> cards = new();
        [SerializeField, Tooltip("Как карточки лежат в начале: номера из списка выше (с нуля)")]
        List<int> startOrder = new();
        [SerializeField, Tooltip("Что сказать, если порядок неверный")]
        LocalizedString wrongOrderMessage;

        public IReadOnlyList<SequenceCard> Cards => cards;
        public IReadOnlyList<int> StartOrder => startOrder;
        public LocalizedString WrongOrderMessage => wrongOrderMessage;
    }

    [Serializable]
    public sealed class SequenceCard
    {
        [SerializeField] Sprite image;
        [SerializeField, Tooltip("Описание для игроков, которые не видят картинку (и для отладки)")]
        LocalizedString caption;

        public Sprite Image => image;
        public LocalizedString Caption => caption;
    }

    /// <summary>Текущая раскладка карточек. Только логика.</summary>
    public sealed class SequenceState
    {
        readonly int[] order;

        /// <param name="count">Сколько карточек.</param>
        /// <param name="start">Начальная раскладка (номера карточек по позициям). Неверная — берётся по порядку.</param>
        public SequenceState(int count, IReadOnlyList<int> start)
        {
            order = new int[count];
            var valid = start != null && start.Count == count && IsPermutation(start, count);
            for (var i = 0; i < count; i++) order[i] = valid ? start[i] : i;
        }

        /// <summary>Какая карточка лежит на позиции.</summary>
        public int CardAt(int position) => order[position];

        public int Count => order.Length;

        public void Swap(int a, int b) => (order[a], order[b]) = (order[b], order[a]);

        public bool IsSolved
        {
            get
            {
                for (var i = 0; i < order.Length; i++)
                    if (order[i] != i) return false;
                return true;
            }
        }

        static bool IsPermutation(IReadOnlyList<int> values, int count)
        {
            var seen = new bool[count];
            foreach (var v in values)
            {
                if (v < 0 || v >= count || seen[v]) return false;
                seen[v] = true;
            }
            return true;
        }
    }
}
