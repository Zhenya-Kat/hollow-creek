using System;
using HollowCreek.Core;
using HollowCreek.Gameplay.Modals;
using HollowCreek.Gameplay.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCreek.Gameplay.Menus
{
    /// <summary>
    /// Пауза: по Esc во время игры и автоматически, когда окно игры теряет фокус.
    /// Отсюда же — выход в главное меню и из игры (с сохранением).
    /// </summary>
    [DefaultExecutionOrder(-830)]
    public sealed class PauseService : MonoBehaviour
    {
        ModalStack modals;

        public bool IsPaused { get; private set; }

        /// <summary>Можно ли ставить паузу (не в главном меню и не во время загрузки).</summary>
        public bool Enabled { get; set; }

        public event Action Paused;
        public event Action Resumed;

        void Awake()
        {
            modals = Services.Get<ModalStack>();
            modals.BackPressedInGameplay += Pause;
            Services.Register(this);
        }

        void OnDestroy()
        {
            modals.BackPressedInGameplay -= Pause;
            Services.Unregister(this);
            Time.timeScale = 1f;
        }

        // В редакторе не ставим паузу при потере фокуса: окно редактора теряет фокус постоянно.
        void OnApplicationFocus(bool focused)
        {
            if (!focused && !Application.isEditor && modals.IsEmpty) Pause();
        }

        public void Pause()
        {
            if (IsPaused || !Enabled) return;
            IsPaused = true;
            Time.timeScale = 0f;
            Paused?.Invoke();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            Resumed?.Invoke();
        }

        /// <summary>Сохранить и вернуться в главное меню (сцена Bootstrap загружается заново).</summary>
        public void QuitToMainMenu()
        {
            SaveProgress();
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        public void QuitGame()
        {
            SaveProgress();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void SaveProgress()
        {
            if (Services.TryGet<SaveService>(out var saves)) saves.SaveNow();
        }
    }
}
