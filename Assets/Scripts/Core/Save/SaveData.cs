using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCreek.Core.Save
{
    /// <summary>
    /// Содержимое сохранения. Хранятся только Id (факты, локация, вопросы) — сами данные берутся из ассетов,
    /// поэтому правка текстов и ассетов не ломает старые сохранения.
    /// Если меняется структура — увеличьте <see cref="CurrentVersion"/> и добавьте шаг в <see cref="SaveStore"/>.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string episode;
        public string location;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public List<string> facts = new();
        public List<string> askedTopics = new();
        /// <summary>Сколько подсказок открыто по каждой цели: «id:число».</summary>
        public List<string> hints = new();
        /// <summary>Когда сохранено (ISO 8601, UTC) — для меню «Продолжить».</summary>
        public string savedAt;
    }
}
