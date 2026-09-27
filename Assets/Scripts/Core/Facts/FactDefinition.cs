using UnityEngine;

namespace HollowCreek.Core.Facts
{
    /// <summary>
    /// Факт — что-то, что игрок узнал или сделал: «сейф открыт», «говорил с Марой».
    /// Всё прохождение игры — это набор полученных фактов. Улики и предметы — тоже факты.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Fact", fileName = "Fact", order = 0)]
    public class FactDefinition : Data.GameDefinition
    {
        [SerializeField, TextArea, Tooltip("Заметка для разработчика: что означает этот факт. В игре не показывается.")]
        string developerNote;
    }
}
