using HollowCreek.Core.Data;
using HollowCreek.Gameplay.Locations;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HollowCreek.Tests
{
    public sealed class SharedLocationTests
    {
        Scene scene;
        LocationDefinition street, diner;
        LocationRoot streetRoot, dinerRoot;

        [SetUp]
        public void SetUp()
        {
            scene=EditorSceneManager.NewPreviewScene();
            street=ScriptableObject.CreateInstance<LocationDefinition>();
            diner=ScriptableObject.CreateInstance<LocationDefinition>();
            streetRoot=Root("Street",street,null);
            dinerRoot=Root("Diner",diner,streetRoot.transform);
        }

        LocationRoot Root(string name,LocationDefinition definition,Transform parent)
        {
            var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);
            go.transform.SetParent(parent);
            var root=go.AddComponent<LocationRoot>();var serialized=new SerializedObject(root);
            serialized.FindProperty("location").objectReferenceValue=definition;serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        SpawnPoint Spawn(Transform parent,string id)
        {
            var go=new GameObject(id);go.transform.SetParent(parent,false);var point=go.AddComponent<SpawnPoint>();
            var serialized=new SerializedObject(point);serialized.FindProperty("id").stringValue=id;serialized.ApplyModifiedPropertiesWithoutUndo();return point;
        }

        [TearDown]
        public void TearDown()
        {
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(street);Object.DestroyImmediate(diner);
        }

        [Test]
        public void RequestedDefinitionSelectsNestedLocation()
        {
            Assert.That(LocationRoot.FindIn(scene,diner),Is.SameAs(dinerRoot));
            Assert.That(LocationRoot.FindIn(scene,street),Is.SameAs(streetRoot));
        }

        [Test]
        public void ParentIgnoresNestedSpawnWithSameId()
        {
            Spawn(dinerRoot.transform,"default");var own=Spawn(streetRoot.transform,"default");
            Assert.That(streetRoot.FindSpawnPoint(null),Is.SameAs(own));
        }

        [Test]
        public void ParentWithoutOwnSpawnsDoesNotTeleportIntoNestedRoom()
        {
            Spawn(dinerRoot.transform,"default");
            LogAssert.Expect(LogType.Warning,"[Location] В «Street» нет точки появления «default», используется первая.");
            Assert.That(streetRoot.FindSpawnPoint(null),Is.Null);
        }

        [Test]
        public void RelocatedRoomConvertsOldSaveOnceAndLeavesWorldCoordinatesIntact()
        {
            dinerRoot.transform.SetPositionAndRotation(new Vector3(23.15f,0,15.92f),Quaternion.Euler(0,-90,0));
            var serialized=new SerializedObject(dinerRoot);
            serialized.FindProperty("convertLegacySavedCoordinates").boolValue=true;
            serialized.FindProperty("legacySavedBounds").boundsValue=new Bounds(new Vector3(0,1.5f,5),new Vector3(13,7,11));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var oldPosition=new Vector3(0,0,1.2f);var position=oldPosition;float yaw=0;
            dinerRoot.ResolveSavedPose(ref position,ref yaw);
            Assert.That(Vector3.Distance(position,dinerRoot.transform.TransformPoint(oldPosition)),Is.LessThan(0.0001f));
            Assert.That(Mathf.DeltaAngle(yaw,-90),Is.EqualTo(0).Within(0.0001f));
            var converted=position;var convertedYaw=yaw;
            dinerRoot.ResolveSavedPose(ref position,ref yaw);
            Assert.That(position,Is.EqualTo(converted));Assert.That(yaw,Is.EqualTo(convertedYaw));
        }
    }
}
