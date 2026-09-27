using HollowCreek.Gameplay.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HollowCreek.Editor
{
    /// <summary>
    /// Кнопка Play всегда запускает игру со сцены Bootstrap, даже если открыта сцена локации.
    /// Открытая сцена запоминается, и Bootstrap загружает именно её, — так любую локацию можно проверить сразу.
    /// </summary>
    [InitializeOnLoad]
    static class PlayModeBootstrap
    {
        const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        static PlayModeBootstrap()
        {
            EditorApplication.delayCall += AssignStartScene;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void AssignStartScene() =>
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            AssignStartScene();
            SessionState.SetString(GameBootstrapper.EditorStartSceneKey, EditorSceneManager.GetActiveScene().name);
        }
    }
}
