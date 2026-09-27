using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Puzzles
{
    /// <summary>
    /// Штриховка: на пустом листе остались вмятины от ручки. Игрок водит карандашом по строкам,
    /// и текст проступает. Решено, когда каждая строка заштрихована почти целиком.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Puzzles/Rubbing", fileName = "Rubbing", order = 42)]
    public sealed class RubbingPuzzle : PuzzleDefinition
    {
        [SerializeField, Tooltip("Строки, которые проступают на листе")]
        List<LocalizedString> lines = new();
        [SerializeField, Range(4, 40), Tooltip("На сколько участков делится строка по ширине")]
        int segmentsPerLine = 16;
        [SerializeField, Range(0.5f, 1f), Tooltip("Какую долю участков строки нужно заштриховать")]
        float requiredCoverage = 0.85f;

        public IReadOnlyList<LocalizedString> Lines => lines;
        public int SegmentsPerLine => segmentsPerLine;
        public float RequiredCoverage => requiredCoverage;
    }

    /// <summary>Какие участки строк уже заштрихованы. Только логика; координаты — доли листа от 0 до 1.</summary>
    public sealed class RubbingState
    {
        readonly bool[,] covered;
        readonly int[] coveredCount;
        readonly int segments;
        readonly float required;

        public RubbingState(int lines, int segmentsPerLine, float requiredCoverage)
        {
            covered = new bool[lines, segmentsPerLine];
            coveredCount = new int[lines];
            segments = segmentsPerLine;
            required = requiredCoverage;
        }

        public int LineCount => coveredCount.Length;
        public int Segments => segments;

        public bool IsCovered(int line, int segment) => covered[line, segment];

        /// <summary>Доля заштрихованных участков строки (0…1).</summary>
        public float Progress(int line) => (float)coveredCount[line] / segments;

        public bool IsLineComplete(int line) => Progress(line) >= required;

        public bool IsComplete
        {
            get
            {
                for (var i = 0; i < LineCount; i++)
                    if (!IsLineComplete(i)) return false;
                return true;
            }
        }

        /// <summary>
        /// Штрих карандаша по строке <paramref name="line"/> в точке <paramref name="x"/> (0…1 по ширине)
        /// с радиусом <paramref name="radius"/> (в тех же долях). Возвращает true, если что-то проступило.
        /// </summary>
        public bool Rub(int line, float x, float radius)
        {
            if (line < 0 || line >= LineCount) return false;
            var from = Mathf.Clamp(Mathf.FloorToInt((x - radius) * segments), 0, segments - 1);
            var to = Mathf.Clamp(Mathf.FloorToInt((x + radius) * segments), 0, segments - 1);
            var changed = false;
            for (var s = from; s <= to; s++)
            {
                if (covered[line, s]) continue;
                covered[line, s] = true;
                coveredCount[line]++;
                changed = true;
            }
            return changed;
        }
    }
}
