using System;
using System.Collections.Generic;

namespace HollowCreek.Core.Data
{
    /// <summary>Элемент списка внутри ассета, у которого есть постоянный Id (для сохранений).</summary>
    public interface IHasStableId
    {
        string Id { get; set; }
    }

    public static class StableId
    {
        /// <summary>
        /// Выдаёт Id пустым элементам и исправляет повторы (они появляются, когда элемент дублируют в Inspector).
        /// Возвращает true, если что-то изменилось.
        /// </summary>
        public static bool EnsureUnique<T>(IList<T> items) where T : IHasStableId
        {
            if (items == null) return false;
            var changed = false;
            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (string.IsNullOrEmpty(item.Id) || !seen.Add(item.Id))
                {
                    item.Id = Guid.NewGuid().ToString("N").Substring(0, 12);
                    seen.Add(item.Id);
                    changed = true;
                }
            }
            return changed;
        }
    }
}
