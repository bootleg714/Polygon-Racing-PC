#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing.Editor
{
    // Scenery pass v01 for the Forest Circuit: swaps the old box-and-cone pines for tiered
    // faceted pines (three variants) and the pyramid hills for rounded faceted mountains.
    // Positions, heights and materials are read from the existing objects; the route,
    // car and gameplay are untouched. (Coastal models update through menu 7.)
    public static class PolygonSceneryModelsSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string MeshPath="Assets/PolygonRacing/Generated/Scenery";

        [MenuItem("Polygon Racing/11 - Refresh Forest Scenery Models")]
        static void RefreshForest()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before refreshing scenery models.");return;}
            if(!File.Exists(ForestPath)){EditorUtility.DisplayDialog("Forest scenery","ForestCircuit.unity was not found. Nothing was changed.","OK");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(ForestPath,OpenSceneMode.Single);
            string backup="PolygonRacingBackups/ForestCircuit_before_scenery_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
            try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(ForestPath,backup,false);}
            catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up the Forest Circuit. It was not changed.");return;}

            EnsureFolder();
            var trunk=Save(PolygonSceneryMeshes.PineTrunk(),"Pine trunk");
            var crowns=new Mesh[PolygonSceneryMeshes.PineVariants.Length];
            for(int v=0;v<crowns.Length;v++)crowns[v]=Save(PolygonSceneryMeshes.PineCrown(v),"Pine crown "+v);
            var mountains=new[]{Save(PolygonSceneryMeshes.Mountain(40),"Mountain 0"),Save(PolygonSceneryMeshes.Mountain(41),"Mountain 1"),
                                Save(PolygonSceneryMeshes.Mountain(42),"Mountain 2")};

            int pines=0,hills=0;
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!t)continue;
                if(t.name=="Polygon pine"&&RebuildPine(t,trunk,crowns,pines))pines++;
                else if(t.name=="Distant faceted hill"&&RebuildHill(t,mountains,hills))hills++;
            }
            int garages=RebuildPitGarages();
            bool stand=RebuildGrandstand();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)){Debug.LogError("Unity could not save the Forest Circuit.");return;}
            Debug.Log($"Forest scenery refreshed: {pines} pines, {hills} mountains, {garages} pit garages{(stand?", grandstand":"")}. Backup: {backup}");
        }

        static bool RebuildPine(Transform pine,Mesh trunkMesh,Mesh[] crowns,int index)
        {
            // Variant comes from the position so a re-run picks the same one.
            var at=pine.position;
            int variant=Mathf.Abs(Mathf.RoundToInt(at.x*3.1f+at.z*1.7f))%crowns.Length;
            float variantScale=PolygonSceneryMeshes.PineVariants[variant].x;
            var oldTrunk=pine.Find("Trunk");var oldCrown=pine.Find("Crown");
            float height=0;Material bark=null,needles=null;
            if(oldTrunk){height=oldTrunk.localScale.y/.3f;bark=MaterialOf(oldTrunk);}
            if(oldCrown)needles=MaterialOf(oldCrown);
            // Already rebuilt: keep the stored height and materials.
            var newTrunk=pine.Find("Pine trunk");var newCrown=pine.Find("Pine crown");
            if(height<=0&&newTrunk){height=newTrunk.localScale.y/variantScale;bark=MaterialOf(newTrunk);}
            if(!needles&&newCrown)needles=MaterialOf(newCrown);
            if(height<=0||!bark||!needles)return false;

            for(int i=pine.childCount-1;i>=0;i--)Object.DestroyImmediate(pine.GetChild(i).gameObject);
            float scale=height*variantScale;
            pine.localRotation=Quaternion.Euler(0,Mathf.Abs(Mathf.RoundToInt(at.x*7+at.z*11))%360,0);
            Child(pine,"Pine trunk",trunkMesh,bark,scale);
            Child(pine,"Pine crown",crowns[variant],needles,scale);
            return true;
        }

        static bool RebuildHill(Transform hill,Mesh[] mountains,int index)
        {
            var old=hill.Find("Hill mesh");var current=hill.Find("Faceted mountain");
            float height;Material material;
            if(old)
            {
                var filter=old.GetComponent<MeshFilter>();material=MaterialOf(old);
                if(!filter||!filter.sharedMesh)return false;
                height=filter.sharedMesh.bounds.max.y;
            }
            else if(current)
            {
                height=current.localScale.y;material=MaterialOf(current);
            }
            else return false;
            if(!material||height<=0)return false;
            for(int i=hill.childCount-1;i>=0;i--)Object.DestroyImmediate(hill.GetChild(i).gameObject);
            var go=Child(hill,"Faceted mountain",mountains[index%mountains.Length],material,1);
            go.transform.localScale=new Vector3(95,height,95);
            go.transform.localRotation=Quaternion.Euler(0,index*37,0);
            return true;
        }

        // ------------------------------------------------------------ scenery v02: pit garages
        static int RebuildPitGarages()
        {
            var bodies=new List<Transform>();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(t&&t.name=="Pit garage")bodies.Add(t);
            if(bodies.Count==0)return 0;
            var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            int built=0;
            foreach(var body in bodies)
            {
                var parent=body.parent;var at=body.position;
                Material walls=MaterialOf(body),door=null,trim=null;
                foreach(var t in all)
                {
                    if(!t||t.parent!=parent||Mathf.Abs(t.position.x-at.x)>.5f)continue;
                    if(t.name=="Garage door"){door=MaterialOf(t);Object.DestroyImmediate(t.gameObject);}
                    else if(t.name=="Garage trim"){trim=MaterialOf(t);Object.DestroyImmediate(t.gameObject);}
                }
                if(!walls||!door||!trim)continue;
                var group=new GameObject("Pit garage building - faceted").transform;
                group.SetParent(parent,false);group.position=new Vector3(at.x,0,at.z);
                Object.DestroyImmediate(body.gameObject);
                var kit=new PolygonSceneryKit("Forest pit garage");
                // Box faces the road on its -z side; the canopy overhangs toward the pit lane.
                kit.Box(walls,new Vector3(0,3,1),new Vector3(17,6,10));
                kit.Box(walls,new Vector3(0,6.25f,-.5f),new Vector3(17.6f,.5f,13));
                kit.Box(trim,new Vector3(0,5.75f,-6.9f),new Vector3(17.6f,.7f,.3f));
                kit.Box(door,new Vector3(0,2.2f,-4.05f),new Vector3(12,4.4f,.2f));
                for(int k=0;k<9;k++)kit.Box(door,new Vector3(0,.45f+k*.48f,-4.2f),new Vector3(11.8f,.08f,.1f));
                foreach(int side in new[]{-1,1})
                {
                    kit.Box(trim,new Vector3(side*7.6f,3,-4.15f),new Vector3(.9f,6,.4f));
                    kit.Box(door,new Vector3(side*4.2f,7.0f,2.5f),new Vector3(2.6f,1.0f,2.2f));
                }
                kit.Box(trim,new Vector3(0,7.0f,-5.6f),new Vector3(3.2f,1.2f,.25f));
                kit.Emit(group);
                built++;
            }
            return built;
        }

        // ------------------------------------------------------------ scenery v02: grandstand
        static bool RebuildGrandstand()
        {
            Transform stand=null;
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(t&&t.name=="Grandstand landmark"){stand=t;break;}
            if(!stand)return false;
            Material blue=null,white=null,red=null,dark=null;
            foreach(Transform child in stand)
            {
                if(child.name=="Grandstand tier"){if(!blue)blue=MaterialOf(child);else if(!white&&MaterialOf(child)!=blue)white=MaterialOf(child);}
                else if(child.name=="Grandstand roof")red=MaterialOf(child);
                else if(child.name=="Roof support")dark=MaterialOf(child);
            }
            // Re-run: the rebuilt stand keeps its materials on the baked children.
            if(!blue||!white||!red||!dark)
            {
                var saved=stand.Find("Grandstand materials");
                if(!saved)return false;
                var r=saved.GetComponent<MeshRenderer>().sharedMaterials;
                if(r.Length<4)return false;
                blue=r[0];white=r[1];red=r[2];dark=r[3];
            }
            for(int i=stand.childCount-1;i>=0;i--)Object.DestroyImmediate(stand.GetChild(i).gameObject);
            var skins=new[]{Flat("PR_Crowd skin light",new Color(.96f,.78f,.62f)),Flat("PR_Crowd skin tan",new Color(.78f,.55f,.38f)),
                            Flat("PR_Crowd skin deep",new Color(.45f,.30f,.20f))};
            var hair=Flat("PR_Crowd hair",new Color(.16f,.11f,.08f));
            var pants=Flat("PR_Crowd denim",new Color(.18f,.24f,.36f));
            var shirts=new[]{blue,white,red,Flat("PR_Crowd shirt yellow",new Color(1,.78f,.1f)),Flat("PR_Crowd shirt green",new Color(.2f,.6f,.3f))};
            var kit=new PolygonSceneryKit("Forest grandstand");
            var facing=Vector3.back;
            for(int row=0;row<5;row++)
            {
                float z=row*1.9f,top=1.0f+row*.65f;
                kit.Box(row%2==0?blue:white,new Vector3(0,top/2,z),new Vector3(48,top,1.9f));
                kit.Box(dark,new Vector3(0,top+.225f,z+.45f),new Vector3(47.4f,.45f,.5f));
                for(int seat=0;seat<32;seat++)
                {
                    int id=row*32+seat;if(id%9==0||id%13==0)continue;
                    float x=-22.5f+seat*1.45f;
                    kit.Person(new Vector3(x,top,z+.45f),facing,.95f+(id%4)*.035f,shirts[(id*7+row)%shirts.Length],skins[id%3],
                        id%5==0?red:hair,pants,id%5==0,id%7==0);
                }
            }
            // Back wall, side walls, columns and a cantilever roof with trusses.
            kit.Box(white,new Vector3(0,3.6f,9.6f),new Vector3(48.6f,7.2f,.4f));
            foreach(int side in new[]{-1,1})
            {
                kit.Prism(white,new[]{new Vector3(side*24.2f,0,-1),new Vector3(side*24.2f,0,9.8f),new Vector3(side*24.2f,7.4f,9.8f),new Vector3(side*24.2f,2.2f,-1)},
                                new[]{new Vector3(side*24.6f,0,-1),new Vector3(side*24.6f,0,9.8f),new Vector3(side*24.6f,7.4f,9.8f),new Vector3(side*24.6f,2.2f,-1)});
            }
            for(int k=0;k<5;k++)
            {
                float x=-24+k*12;
                kit.Box(dark,new Vector3(x,4.0f,10.0f),new Vector3(.5f,8.0f,.5f));
                kit.Beam(dark,new Vector3(x,8.0f,10.0f),new Vector3(x,7.6f,-2.2f),.3f,.5f);
                kit.Beam(dark,new Vector3(x,5.6f,10.0f),new Vector3(x,7.5f,2.0f),.18f,.18f);
            }
            kit.Box(red,new Vector3(0,8.05f,3.9f),new Vector3(50,.3f,13),Quaternion.Euler(-2,0,0));
            kit.Box(white,new Vector3(0,7.75f,-2.55f),new Vector3(50.2f,.6f,.2f));
            kit.Emit(stand);
            // A hidden marker remembers the four palette materials for re-runs.
            var marker=new GameObject("Grandstand materials",typeof(MeshRenderer));
            marker.transform.SetParent(stand,false);
            var markerRenderer=marker.GetComponent<MeshRenderer>();
            markerRenderer.sharedMaterials=new[]{blue,white,red,dark};markerRenderer.enabled=false;
            return true;
        }

        static Material Flat(string name,Color color)
        {
            string path=$"{MeshPath}/{name}.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat)return mat;
            var shader=Resources.Load<Shader>(PolygonLook.FlatShaderName);
            if(!shader)shader=Shader.Find("Universal Render Pipeline/Simple Lit");
            mat=new Material(shader){name=name};mat.SetColor("_BaseColor",color);
            AssetDatabase.CreateAsset(mat,path);
            return mat;
        }

        static Material MaterialOf(Transform t)
        {
            var renderer=t?t.GetComponent<Renderer>():null;
            return renderer?renderer.sharedMaterial:null;
        }

        static GameObject Child(Transform parent,string name,Mesh mesh,Material material,float scale)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go;
        }

        static Mesh Save(Mesh mesh,string name)
        {
            string path=$"{MeshPath}/{name}.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing)
            {
                existing.Clear();
                existing.SetVertices(mesh.vertices);existing.SetTriangles(mesh.triangles,0);
                existing.RecalculateNormals();existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        static void EnsureFolder()
        {
            if(AssetDatabase.IsValidFolder(MeshPath))return;
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Scenery");
        }
    }
}
#endif
