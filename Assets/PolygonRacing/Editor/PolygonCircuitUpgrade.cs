#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PolygonRacing.Editor
{
    public static class PolygonCircuitUpgrade
    {
        [MenuItem("Polygon Racing/5 - Upgrade Long Straight v12")]
        static void Upgrade()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before upgrading the circuit.");return;}
            var player=Object.FindFirstObjectByType<PolygonDriver>();
            if(!player||!player.transform.parent){EditorUtility.DisplayDialog("Circuit upgrade","Open your saved ForestCircuit scene first.","OK");return;}
            var old=player.transform.parent.Find("Circuit - editable scenery");
            if(!old){EditorUtility.DisplayDialog("Circuit upgrade","Could not locate the existing circuit. Nothing changed.","OK");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=player.gameObject.scene;
            string backup="PolygonRacingBackups/ForestCircuit_before_v12_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
            Directory.CreateDirectory("PolygonRacingBackups");
            if(!EditorSceneManager.SaveScene(scene)||string.IsNullOrEmpty(scene.path)){Debug.LogError("Save the current scene before upgrading. Upgrade stopped.");return;}
            try{File.Copy(scene.path,backup,false);}
            catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not save a backup. Upgrade stopped.");return;}
            GameObject temporary=null;Transform scenery=null;
            try
            {
                temporary=PolygonWorld.Build();scenery=temporary.transform.Find("Circuit - editable scenery");
                AddSignsAndLandmarks(scenery);
                Directory.CreateDirectory("Assets/PolygonRacing/Generated/CircuitV12");AssetDatabase.Refresh();
                var seen=new HashSet<Object>();int index=0;
                foreach(var f in scenery.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=f.sharedMesh;if(!mesh||AssetDatabase.Contains(mesh)||!seen.Add(mesh))continue;
                    AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath($"Assets/PolygonRacing/Generated/CircuitV12/Mesh_{index++}.asset"));
                }
                foreach(var r in scenery.GetComponentsInChildren<Renderer>())foreach(var mat in r.sharedMaterials)
                {
                    if(!mat||AssetDatabase.Contains(mat)||!seen.Add(mat))continue;
                    AssetDatabase.CreateAsset(mat,AssetDatabase.GenerateUniqueAssetPath("Assets/PolygonRacing/Generated/CircuitV12/"+mat.name+".mat"));
                }
                scenery.SetParent(player.transform.parent,false);
                Object.DestroyImmediate(old.gameObject);
                // Move the saved player to the new start line; preserve all child objects and settings.
                player.transform.SetPositionAndRotation(PolygonWorld.Point(0),Quaternion.LookRotation(PolygonWorld.Tangent(0)));
                Object.DestroyImmediate(temporary);temporary=null;
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject=player.gameObject;
                Debug.Log($"Forest Circuit v12 saved: {PolygonCircuit.Length:0} metres. Your player car and cockpit settings were preserved. Scene backup: {backup}. Press Play to choose Race or Time Trial.");
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);Debug.LogError("Upgrade stopped. A pre-upgrade scene copy is in PolygonRacingBackups beside Assets. If needed, restore it while Unity is closed.");
            }
            finally{if(temporary)Object.DestroyImmediate(temporary);}
        }
        static Transform TrackObject(Transform root,string name,float meters,float lateral)
        {
            float t=PolygonCircuit.AngleAtDistance(meters);var g=new GameObject(name);g.transform.SetParent(root,false);
            g.transform.position=PolygonWorld.Point(t)+PolygonWorld.Side(t)*lateral;
            g.transform.rotation=Quaternion.LookRotation(PolygonWorld.Tangent(t));return g.transform;
        }
        static void Sign(Transform root,string message,float meters,float lateral,float width=2.8f,float size=.06f)
        {
            var t=TrackObject(root,"Track sign - "+message,meters,lateral);
            var dark=PolygonWorld.Paint("Sign charcoal",new Color(.04f,.06f,.09f));var white=PolygonWorld.Paint("Sign white",Color.white);
            PolygonWorld.Box("Post",t,new Vector3(0,.85f,0),new Vector3(.12f,1.7f,.12f),dark);
            PolygonWorld.Box("Sign face",t,new Vector3(0,1.8f,0),new Vector3(width,1.3f,.12f),white);
            PolygonWorld.Label("Sign lettering",t,message,new Vector3(0,1.8f,-.075f),size,new Color(.04f,.08f,.12f));
        }
        static float KnotDistance(float knot)
        {
            float fraction=PolygonCircuit.ProjectLap(PolygonWorld.Point(knot/PolygonCircuit.KnotCount*Mathf.PI*2),out _);return fraction*PolygonCircuit.Length;
        }
        static void AddSignsAndLandmarks(Transform root)
        {
            var red=PolygonWorld.Paint("Marker red",new Color(.9f,.07f,.035f));
            var white=PolygonWorld.Paint("Marker white",new Color(.95f,.95f,.9f));
            var dark=PolygonWorld.Paint("Stand charcoal",new Color(.07f,.1f,.14f));
            var blue=PolygonWorld.Paint("Landmark blue",new Color(.035f,.28f,.63f));
            // Two braking zones. Distances are measured along the track, not in a straight line.
            foreach(float corner in new[]{KnotDistance(4),KnotDistance(11.7f)})
                foreach(int meters in new[]{150,100,50})Sign(root,meters.ToString(),corner-meters,-12.5f,1.8f,.12f);
            Sign(root,"PINE\nSTRAIGHT",45,-13.5f,3,.055f);
            Sign(root,"S-BEND",KnotDistance(5.5f),-13.5f,3,.07f);
            Sign(root,"CANYON\nTURN",KnotDistance(10.9f),-13.5f,3,.055f);
            foreach(var mark in new[]{new Vector2(KnotDistance(4),1),new Vector2(KnotDistance(6),-1),new Vector2(KnotDistance(7.4f),1),new Vector2(KnotDistance(11.7f),1)})
            {
                for(int i=0;i<3;i++)Sign(root,mark.y>0?">": "<",mark.x-8+i*9,-12.5f,1.6f,.15f);
            }
            var stands=new GameObject("Grandstand landmark").transform;stands.SetParent(root,false);stands.position=new Vector3(-25,0,35);
            for(int row=0;row<5;row++)PolygonWorld.Box("Grandstand tier",stands,new Vector3(0,.5f+row*.65f,row*1.9f),new Vector3(48,1,2),row%2==0?blue:white);
            PolygonWorld.Box("Grandstand roof",stands,new Vector3(0,7,5),new Vector3(52,.3f,15),red);
            foreach(int x in new[]{-23,23})PolygonWorld.Box("Roof support",stands,new Vector3(x,3.5f,5),new Vector3(.3f,7,.3f),dark);
            var tower=new GameObject("Blue timing tower landmark").transform;tower.SetParent(root,false);tower.position=new Vector3(490,0,-55);
            PolygonWorld.Box("Tower base",tower,new Vector3(0,8,0),new Vector3(7,16,7),white);
            PolygonWorld.Box("Observation room",tower,new Vector3(0,17,0),new Vector3(13,5,13),blue);
            PolygonWorld.Box("Tower roof",tower,new Vector3(0,20,0),new Vector3(15,.6f,15),red);
            // Bright posts emphasize the direction of the switchback without cluttering the racing lane.
            for(int i=0;i<10;i++)
            {
                float d=KnotDistance(5.5f)+i*11;var t=TrackObject(root,"Switchback marker post",d,12.8f);
                PolygonWorld.Box("Post",t,new Vector3(0,.8f,0),new Vector3(.3f,1.6f,.3f),i%2==0?red:white);
            }
        }
    }
}
#endif
