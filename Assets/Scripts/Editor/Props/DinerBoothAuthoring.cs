using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class DinerBoothAuthoring
    {
        public const string PrefabPath = "Assets/Art/Environment/DinerInterior/Furniture/WindowBooth.prefab";

        [MenuItem("Hollow Creek/Реквизит/Большой стол возле Оуэна")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before placing the window booth.");
            var diner = GameObject.Find("[Location] Diner");
            var table = diner.transform.Find("Art/tableCross");
            var bench = diner.transform.Find("Art/benchCushion");
            var opposite = diner.GetComponentsInChildren<Transform>().First(t => t.name == "Mara opposite bench");
            var floor = diner.transform.Find("Geometry/Floor").GetComponent<Renderer>().bounds.max.y;
            var tableBounds = table.GetComponent<Renderer>().bounds;
            var offset = new Vector3(0, floor - tableBounds.min.y, 2.35f - tableBounds.center.z);
            var stage = new GameObject("Window booth near Owen");
            try
            {
                Copy(table, stage.transform, "Large booth table", offset);
                Copy(bench, stage.transform, "Booth bench by window", offset);
                Copy(opposite, stage.transform, "Booth bench toward aisle", offset);
                var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
                foreach (var old in diner.GetComponentsInChildren<Transform>(true)
                    .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                    Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = stage.name;
                instance.transform.SetParent(diner.transform.Find("Art"), false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                var owen = GameObject.Find("NPC Owen");
                // Right while facing the west windows is +Z. Keep his rotation and cup grip.
                var position = owen.transform.position;
                position.z = 4.35f;
                owen.transform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(owen.transform);
                var animator = owen.GetComponentInChildren<Animator>();
                if (animator && animator.runtimeAnimatorController)
                    animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle")).SampleAnimation(animator.gameObject, 0);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = null;
                SceneView.RepaintAll();
            }
            finally { Object.DestroyImmediate(stage); }
        }

        static void Copy(Transform source, Transform parent, string name, Vector3 offset)
        {
            var model = Object.Instantiate(source.gameObject, parent);
            model.name = name;
            model.transform.SetPositionAndRotation(source.position + offset, source.rotation);
            model.transform.localScale = source.lossyScale;
            if (!model.GetComponent<Collider>())
            {
                var bounds = model.GetComponent<MeshFilter>().sharedMesh.bounds;
                var box = model.AddComponent<BoxCollider>(); box.center = bounds.center; box.size = bounds.size;
            }
        }
    }
}
