using UnityEditor;
using UnityEditor.SceneManagement;

namespace HollowCreek.Editor
{
    /// <summary>
    /// Последняя открытая сцена хранится в папке Library, которой нет в git. Поэтому после клонирования
    /// Unity открывает пустую сцену «Untitled». В этом случае при открытии проекта сами открываем Bootstrap.
    /// </summary>
    [InitializeOnLoad]
    static class OpenStartScene
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string DoneKey = "HollowCreek.StartSceneChecked";

        static OpenStartScene() => EditorApplication.delayCall += Open;

        static void Open()
        {
            // Только один раз за запуск редактора, не в режиме игры и не поверх чужой работы.
            if (SessionState.GetBool(DoneKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(DoneKey, true);
            var active = EditorSceneManager.GetActiveScene();
            if (EditorSceneManager.sceneCount > 1 || !string.IsNullOrEmpty(active.path) || active.isDirty) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
