using System;
using UnityEngine;

namespace HollowCreek.Core.Logic
{
    /// <summary>
    /// Ставится на поле с <see cref="SerializeReference"/>: в Inspector появляется выпадающий список,
    /// из которого можно выбрать конкретный тип (например, какое условие или действие создать).
    /// </summary>
    public sealed class SubclassSelectorAttribute : PropertyAttribute { }

    /// <summary>Понятное название типа в выпадающем списке <see cref="SubclassSelectorAttribute"/>.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SelectorLabelAttribute : Attribute
    {
        public string Label { get; }

        public SelectorLabelAttribute(string label) => Label = label;
    }
}
