using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;

namespace HollowCreek
{
    public static class ReleaseBuild
    {
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Windows", "HollowCreek.exe");
        public static void Linux() => Build(BuildTarget.StandaloneLinux64, "Linux", "HollowCreek.x86_64");

        static void Build(BuildTarget target, string platform, string executable)
        {
            var root = Path.GetFullPath("Builds/Release/" + platform);
            Directory.CreateDirectory(root);
            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != target)
                    throw new InvalidOperationException("Start Unity with -buildTarget Win64 or Linux64 before invoking the release builder.");
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                    throw new InvalidOperationException("Missing build support for " + target);
                // Windows uses the installed native IL2CPP player; Linux ships Mono.
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,
                    target == BuildTarget.StandaloneWindows64 ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0 || scenes[0] != "Assets/Scenes/Bootstrap.unity")
                    throw new InvalidOperationException("Bootstrap must be the first enabled scene.");
                AddressableAssetSettings.BuildPlayerContent(out UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult content);
                if (!string.IsNullOrEmpty(content.Error)) throw new Exception("Addressables: " + content.Error);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(root, executable),
                    target = target,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                var details = new BuildDetails
                {
                    platform = platform, version = PlayerSettings.bundleVersion,
                    result = summary.result.ToString(), errors = summary.totalErrors,
                    warnings = summary.totalWarnings, bytes = summary.totalSize,
                    seconds = summary.totalTime.TotalSeconds
                };
                File.WriteAllText(Path.Combine(root, "build-report.json"), JsonUtility.ToJson(details, true));
                if (summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + summary.result);
                Debug.Log("RELEASE_BUILD_SUCCEEDED " + platform + " " + root);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                File.WriteAllText(Path.Combine(root, "build-error.txt"), error.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Serializable]
        class BuildDetails
        {
            public string platform, version, result;
            public int errors, warnings;
            public ulong bytes;
            public double seconds;
        }
    }
}
