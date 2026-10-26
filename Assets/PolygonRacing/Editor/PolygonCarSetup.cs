#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolygonRacing.Editor
{
    // Car and cockpit pass: replaces the player car's box body with a faceted open-wheel
    // body, steering wheel, tyres and rims. Keeps the car root, PolygonDriver settings,
    // seated tracking space/camera, tuned Dashboard box and instrument text, steering-wheel
    // and wheel pivots (so steering, wheel spin and rival copies keep working).
    public static class PolygonCarSetup
    {
        const string ForestPath="Assets/PolygonRacing/Scenes/ForestCircuit.unity";
        const string CoastalPath="Assets/PolygonRacing/Scenes/CoastalExpressway.unity";
        const string CarPath="Assets/PolygonRacing/Generated/Car";
        const string BodyName="Car body - faceted",WheelName="Steering wheel - faceted",RimName="Wheel rim - faceted";

        // Submesh / material slots.
        const int Red=0,Ivory=1,Carbon=2,Metal=3,Yellow=4,Mirror=5,Cockpit=6,Green=7,Screen=8,Weave=9,SlotCount=10;
        const string UprightName="Wheel upright - faceted",ColumnName="Steering column - tilted";
        const float WheelTilt=15;   // degrees; top of the wheel leans away from the driver

        static readonly HashSet<string> OldBodyParts=new HashSet<string>{
            "Wedge nose","Nose stripe","Chassis","Side pod","White side stripe","Cockpit rim","Seat back","Seat cushion",
            "Engine cover","Front wing","Rear wing","Rear wing support","Wing endplate","Suspension arm",BodyName};
        static readonly HashSet<string> OldWheelParts=new HashSet<string>{
            "Wheel top","Wheel lower","Wheel grip","Wheel hub","Wheel center stripe",WheelName};

        [MenuItem("Polygon Racing/10 - Rebuild Player Car (Faceted)")]
        static void RebuildBothCircuits()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before rebuilding the car.");return;}
            if(!Resources.Load<Shader>(PolygonLook.FlatShaderName))
            {
                EditorUtility.DisplayDialog("Polygon Racing car","PolygonFlat.shader is missing from Assets/PolygonRacing/Resources. Nothing was changed.","OK");
                return;
            }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EnsureFolder();
            var materials=CarMaterials();
            var meshes=BuildMeshes();
            int done=0;
            foreach(var path in new[]{ForestPath,CoastalPath})
            {
                if(!File.Exists(path)){Debug.LogWarning("Skipped missing scene "+path);continue;}
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                string backup="PolygonRacingBackups/"+Path.GetFileNameWithoutExtension(path)+"_before_car_"+
                    System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
                try{Directory.CreateDirectory("PolygonRacingBackups");File.Copy(path,backup,false);}
                catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Could not back up "+path+". It was not changed.");continue;}
                var driver=Object.FindFirstObjectByType<PolygonDriver>();
                if(!driver){Debug.LogError("No PolygonDriver in "+path+". It was not changed.");continue;}
                ApplyToCar(driver,materials,meshes);
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene)){Debug.LogError("Unity could not save "+path+".");continue;}
                done++;
                Debug.Log($"Faceted car applied in {path}. Backup: {backup}");
            }
            AssetDatabase.SaveAssets();
            if(done>0)Debug.Log("Player car rebuilt in "+done+" circuit(s). Seat, tracking, dashboard and handling are unchanged. Rivals copy the new body automatically.");
        }

        // Applies only to the currently open PC scene. The original shared car meshes and
        // tuned materials remain untouched, so other circuits and menu 13 are independent.
        [MenuItem("Polygon Racing/PC Visuals/Refresh PC Car Detail - Open Scene")]
        static void RefreshPcCarDetail()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play Mode before refreshing PC car detail.");return;}
            var scene=EditorSceneManager.GetActiveScene();
            if(!scene.IsValid()||string.IsNullOrEmpty(scene.path)){Debug.LogError("Open and save a circuit scene first.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            PolygonDriver driver=null;
            foreach(var candidate in Object.FindObjectsByType<PolygonDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(candidate.gameObject.scene==scene){if(driver){Debug.LogError("More than one player car in the open scene.");return;}driver=candidate;}
            if(!driver){Debug.LogError("No player car in the open scene.");return;}
            var body=driver.transform.Find(BodyName);
            var bodyRenderer=body?body.GetComponent<MeshRenderer>():null;
            var bodyFilter=body?body.GetComponent<MeshFilter>():null;
            var shader=Resources.Load<Shader>("PolygonCarbonPC");
            var texture=Resources.Load<Texture2D>("PolygonCarbonWeave");
            if(!bodyRenderer||!bodyFilter||bodyRenderer.sharedMaterials.Length<Weave||!bodyRenderer.sharedMaterials[Carbon]||!shader||!texture)
            {Debug.LogError("PC car detail needs the existing faceted car, carbon material, shader and weave texture.");return;}
            if(driver.wheels==null||driver.wheels.Length!=4){Debug.LogError("Expected four existing wheel transforms; nothing was rebuilt.");return;}
            foreach(var tire in driver.wheels)
                if(!tire||!tire.parent||!tire.Find(RimName)||!tire.parent.Find(UprightName))
                {Debug.LogError("Existing wheel rim or upright missing; nothing was rebuilt.");return;}

            // Protect both the current saved scene and every generated car asset before mutation.
            string backup=Path.Combine("PolygonRacingBackups","PC_car_before_refresh_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            try
            {
                Directory.CreateDirectory(backup);
                File.Copy(scene.path,Path.Combine(backup,Path.GetFileName(scene.path)),false);
                var carBackup=Path.Combine(backup,"Car");Directory.CreateDirectory(carBackup);
                foreach(var file in Directory.GetFiles(CarPath))File.Copy(file,Path.Combine(carBackup,Path.GetFileName(file)),false);
            }
            catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("Car backup failed; nothing was rebuilt.");return;}

            try
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if(!importer)throw new System.InvalidOperationException("Could not configure the carbon texture importer.");
                if(!importer.mipmapEnabled||importer.filterMode!=FilterMode.Trilinear||importer.anisoLevel!=8||importer.wrapMode!=TextureWrapMode.Repeat)
                {
                    importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
                    importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
                }
                texture=Resources.Load<Texture2D>("PolygonCarbonWeave");
                string materialPath=CarPath+"/PR_Car PC cockpit weave.mat";
                var woven=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!woven)
                {
                    woven=new Material(bodyRenderer.sharedMaterials[Carbon]){name="PR_Car PC cockpit weave"};
                    woven.shader=shader;woven.SetTexture("_BaseMap",texture);
                    AssetDatabase.CreateAsset(woven,materialPath);
                }
                else {woven.shader=shader;woven.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(woven);}
                var meshes=BuildMeshes(true);
                SetPcMesh(bodyFilter,bodyRenderer,meshes.Body,woven);
                foreach(var tire in driver.wheels)
                {
                    float side=driver.transform.InverseTransformPoint(tire.position).x<0?-1:1;
                    bool front=driver.frontSteering!=null&&System.Array.IndexOf(driver.frontSteering,tire.parent)>=0;
                    var rim=tire.Find(RimName);var upright=tire.parent.Find(UprightName);
                    SetPcMesh(rim.GetComponent<MeshFilter>(),rim.GetComponent<MeshRenderer>(),side<0?meshes.RimLeft:meshes.RimRight,woven);
                    SetPcMesh(upright.GetComponent<MeshFilter>(),upright.GetComponent<MeshRenderer>(),front?
                        (side<0?meshes.UprightFrontLeft:meshes.UprightFrontRight):
                        (side<0?meshes.UprightRearLeft:meshes.UprightRearRight),woven);
                }
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the scene.");
                Debug.Log("PC car detail refreshed in "+scene.path+". Backup: "+backup+". Rivals copy these visuals at race start.");
            }
            catch(System.Exception ex){Debug.LogException(ex);Debug.LogError("PC car refresh failed; backup: "+backup);}
        }

        static void SetPcMesh(MeshFilter filter,MeshRenderer renderer,Mesh mesh,Material woven)
        {
            if(!filter||!renderer)throw new System.InvalidOperationException("Existing car mesh renderer is missing.");
            var existing=renderer.sharedMaterials;
            if(existing.Length<Weave)throw new System.InvalidOperationException("Existing car material slots are incomplete.");
            var materials=new Material[SlotCount];System.Array.Copy(existing,materials,Weave);materials[Weave]=woven;
            filter.sharedMesh=mesh;renderer.sharedMaterials=materials;
            EditorUtility.SetDirty(filter);EditorUtility.SetDirty(renderer);
        }

        // Scenery v02: static copies of the player car parked in the coastal paddock garages.
        // Called by the coastal create/refresh menus after the scenery is built.
        public static void FillParkedCarSlots(Transform scenery)
        {
            Transform slots=null;
            foreach(var t in scenery.GetComponentsInChildren<Transform>(true))
                if(t.name=="Paddock parked car slots"){slots=t;break;}
            if(!slots||!Resources.Load<Shader>(PolygonLook.FlatShaderName))return;
            EnsureFolder();
            var shared=CarMaterials();var meshes=BuildMeshes();
            var liveries=new[]{new Color(.98f,.32f,.32f),new Color(.025f,.43f,.48f),new Color(.98f,.65f,.39f)};
            for(int i=0;i<slots.childCount;i++)
            {
                var slot=slots.GetChild(i);
                for(int c=slot.childCount-1;c>=0;c--)Object.DestroyImmediate(slot.GetChild(c).gameObject);
                var materials=(Material[])shared.Clone();
                var livery=CarMaterial("PR_Paddock car livery "+(i%liveries.Length+1),liveries[i%liveries.Length],shared[Carbon].shader);
                materials[Red]=livery;
                var car=new GameObject("Parked team car",typeof(MeshFilter),typeof(MeshRenderer));
                car.transform.SetParent(slot,false);
                Assign(car,meshes.Body,materials);
                var column=new GameObject(ColumnName).transform;column.SetParent(car.transform,false);
                column.localPosition=new Vector3(0,.68f,.03f);column.localRotation=Quaternion.Euler(WheelTilt,0,0);
                var wheel=new GameObject(WheelName,typeof(MeshFilter),typeof(MeshRenderer));
                wheel.transform.SetParent(column,false);Assign(wheel,meshes.Steering,materials);
                foreach(var (zc,xw) in WheelCentres)foreach(float side in new[]{-1f,1f})
                {
                    var pivot=new GameObject("Parked wheel").transform;pivot.SetParent(car.transform,false);
                    pivot.localPosition=new Vector3(side*xw,.39f,zc);
                    var tire=new GameObject("Tire",typeof(MeshFilter),typeof(MeshRenderer));
                    tire.transform.SetParent(pivot,false);Assign(tire,meshes.Tire,materials);
                    var rim=new GameObject(RimName,typeof(MeshFilter),typeof(MeshRenderer));
                    rim.transform.SetParent(pivot,false);Assign(rim,side<0?meshes.RimLeft:meshes.RimRight,materials);
                    var upright=new GameObject(UprightName,typeof(MeshFilter),typeof(MeshRenderer));
                    upright.transform.SetParent(pivot,false);
                    Assign(upright,zc>0?(side<0?meshes.UprightFrontLeft:meshes.UprightFrontRight)
                                       :(side<0?meshes.UprightRearLeft:meshes.UprightRearRight),materials);
                }
            }
        }

        struct CarMeshes{public Mesh Body,Steering,Tire,RimLeft,RimRight,UprightFrontLeft,UprightFrontRight,UprightRearLeft,UprightRearRight;}

        static void ApplyToCar(PolygonDriver driver,Material[] materials,CarMeshes meshes)
        {
            var car=driver.transform;
            for(int i=car.childCount-1;i>=0;i--)
            {
                var child=car.GetChild(i);
                if(OldBodyParts.Contains(child.name))Object.DestroyImmediate(child.gameObject);
            }
            var body=new GameObject(BodyName,typeof(MeshFilter),typeof(MeshRenderer));
            body.transform.SetParent(car,false);
            body.transform.SetSiblingIndex(0);
            Assign(body,meshes.Body,materials);

            if(driver.steeringWheel)
            {
                var wheel=driver.steeringWheel;
                // Tilt via a parent column so PolygonDriver's steering rotation still turns the
                // wheel about its own (tilted) axis.
                var column=wheel.parent;
                if(!column||column.name!=ColumnName)
                {
                    column=new GameObject(ColumnName).transform;
                    column.SetParent(car,false);
                    column.localPosition=wheel.localPosition;
                    column.SetSiblingIndex(wheel.GetSiblingIndex());
                    wheel.SetParent(column,false);
                    wheel.localPosition=Vector3.zero;
                }
                column.localRotation=Quaternion.Euler(WheelTilt,0,0);
                for(int i=wheel.childCount-1;i>=0;i--)
                    if(OldWheelParts.Contains(wheel.GetChild(i).name))Object.DestroyImmediate(wheel.GetChild(i).gameObject);
                var go=new GameObject(WheelName,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(wheel,false);
                Assign(go,meshes.Steering,materials);
            }

            if(driver.wheels!=null)
                foreach(var tire in driver.wheels)
                {
                    if(!tire)continue;
                    for(int i=tire.childCount-1;i>=0;i--)
                    {
                        var child=tire.GetChild(i);
                        if(child.name=="Wheel hub"||child.name==RimName)Object.DestroyImmediate(child.gameObject);
                    }
                    var filter=tire.GetComponent<MeshFilter>();if(!filter)filter=tire.gameObject.AddComponent<MeshFilter>();
                    var renderer=tire.GetComponent<MeshRenderer>();if(!renderer)renderer=tire.gameObject.AddComponent<MeshRenderer>();
                    filter.sharedMesh=meshes.Tire;
                    renderer.sharedMaterials=materials;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    float side=car.InverseTransformPoint(tire.position).x<0?-1:1;
                    var rim=new GameObject(RimName,typeof(MeshFilter),typeof(MeshRenderer));
                    rim.transform.SetParent(tire,false);
                    Assign(rim,side<0?meshes.RimLeft:meshes.RimRight,materials);
                    // Upright on the wheel's pivot: it steers with the front wheels but does not spin.
                    var pivot=tire.parent;
                    if(pivot&&pivot!=car)
                    {
                        for(int i=pivot.childCount-1;i>=0;i--)
                            if(pivot.GetChild(i).name==UprightName)Object.DestroyImmediate(pivot.GetChild(i).gameObject);
                        bool front=driver.frontSteering!=null&&System.Array.IndexOf(driver.frontSteering,pivot)>=0;
                        var upright=new GameObject(UprightName,typeof(MeshFilter),typeof(MeshRenderer));
                        upright.transform.SetParent(pivot,false);
                        upright.transform.localPosition=tire.localPosition;
                        Assign(upright,front?(side<0?meshes.UprightFrontLeft:meshes.UprightFrontRight)
                                            :(side<0?meshes.UprightRearLeft:meshes.UprightRearRight),materials);
                    }
                    EditorUtility.SetDirty(tire.gameObject);
                }
            EditorUtility.SetDirty(car.gameObject);
        }

        static void Assign(GameObject go,Mesh mesh,Material[] materials)
        {
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();
            renderer.sharedMaterials=materials;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }

        // ------------------------------------------------------------ materials
        static Material[] CarMaterials()
        {
            var shader=Resources.Load<Shader>(PolygonLook.FlatShaderName);
            var livery=new Color(.9f,.055f,.045f);
            var existing=AssetDatabase.LoadAssetAtPath<Material>("Assets/PolygonRacing/Generated/PR_Vermilion.mat");
            if(existing&&existing.HasProperty("_BaseColor"))livery=existing.GetColor("_BaseColor");
            // The livery name must start with PR_Vermilion: rivals recolour those materials.
            return new[]{
                CarMaterial("PR_Vermilion car body",livery,shader),
                CarMaterial("PR_Car ivory",new Color(.94f,.95f,.9f),shader),
                CarMaterial("PR_Car carbon",new Color(.055f,.07f,.09f),shader),
                CarMaterial("PR_Car metal",new Color(.30f,.35f,.40f),shader),
                CarMaterial("PR_Car yellow",new Color(1,.78f,.05f),shader),
                CarMaterial("PR_Car mirror",new Color(.78f,.86f,.92f),shader),
                CarMaterial("PR_Car cockpit",new Color(.16f,.19f,.24f),shader),
                CarMaterial("PR_Car LED green",new Color(.20f,.85f,.35f),shader),
                CarMaterial("PR_Car display",new Color(.05f,.25f,.28f),shader)};
        }

        static Material CarMaterial(string name,Color color,Shader shader)
        {
            string path=$"{CarPath}/{name}.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created=!mat;
            if(created){mat=new Material(shader){name=name};AssetDatabase.CreateAsset(mat,path);}
            mat.shader=shader;
            if(created)mat.SetColor("_BaseColor",color);   // keep any colour tuned in the Inspector
            mat.SetColor("_EmissionColor",Color.black);
            mat.SetFloat("_Cull",0);                         // double-sided: open panels, wings and walls
            mat.SetColor("_SkyAmbient",new Color(.60f,.74f,.86f));mat.SetColor("_GroundAmbient",new Color(.50f,.42f,.33f));
            mat.SetFloat("_AmbientStrength",.75f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------ meshes
        static CarMeshes BuildMeshes(bool pcDetail=false)
        {
            var body=new CarMesh(pcDetail);BuildBody(body,pcDetail);
            var steering=new CarMesh(pcDetail);BuildSteering(steering);
            var tire=new CarMesh(pcDetail);BuildTire(tire);
            var rimLeft=new CarMesh(pcDetail);BuildRim(rimLeft,-1,pcDetail);
            var rimRight=new CarMesh(pcDetail);BuildRim(rimRight,1,pcDetail);
            var ufl=new CarMesh(pcDetail);BuildUpright(ufl,-1,true,pcDetail);
            var ufr=new CarMesh(pcDetail);BuildUpright(ufr,1,true,pcDetail);
            var url=new CarMesh(pcDetail);BuildUpright(url,-1,false,pcDetail);
            var urr=new CarMesh(pcDetail);BuildUpright(urr,1,false,pcDetail);
            string suffix=pcDetail?" PC detail":"";
            return new CarMeshes{
                Body=SaveMesh(body,"Car body"+suffix),Steering=SaveMesh(steering,"Steering wheel"+suffix),
                Tire=SaveMesh(tire,"Tire"+suffix),RimLeft=SaveMesh(rimLeft,"Rim left"+suffix),RimRight=SaveMesh(rimRight,"Rim right"+suffix),
                UprightFrontLeft=SaveMesh(ufl,"Upright front left"+suffix),UprightFrontRight=SaveMesh(ufr,"Upright front right"+suffix),
                UprightRearLeft=SaveMesh(url,"Upright rear left"+suffix),UprightRearRight=SaveMesh(urr,"Upright rear right"+suffix)};
        }

        static Mesh SaveMesh(CarMesh source,string name)
        {
            string path=$"{CarPath}/{name}.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}
            source.Fill(mesh);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static Vector2[] NoseRing(float w,float y0,float h,float ridge)
        {
            return new[]{
                new Vector2(.70f*w,y0),new Vector2(w,y0+.30f*h),new Vector2(.92f*w,y0+.75f*h),new Vector2(.50f*w,y0+h),
                new Vector2(0,y0+h+ridge),
                new Vector2(-.50f*w,y0+h),new Vector2(-.92f*w,y0+.75f*h),new Vector2(-w,y0+.30f*h),new Vector2(-.70f*w,y0)};
        }
        static readonly int[] NoseMats={Red,Red,Red,Ivory,Ivory,Red,Red,Red,Carbon};

        static Vector2[] Pod(float xi,float xo,float yb,float yt)
        {
            return new[]{
                new Vector2(xi,yb),new Vector2(xo-.06f,yb),new Vector2(xo,yb+.25f*(yt-yb)),
                new Vector2(xo-.02f,yt-.08f),new Vector2(xo-.10f,yt),new Vector2(xi,yt+.02f)};
        }

        static Vector2[] Flip(Vector2[] points)
        {
            var result=new Vector2[points.Length];
            for(int i=0;i<points.Length;i++)result[i]=new Vector2(-points[i].x,points[i].y);
            return result;
        }

        // Body section data: {z, half width, bottom, height, ridge} and sidepods {z, inner x, outer x, bottom, top}.
        static readonly float[,] NoseRows={{.48f,.42f,.14f,.56f,.06f},{1.00f,.38f,.15f,.45f,.05f},{1.60f,.32f,.17f,.35f,.04f},
                                          {2.20f,.25f,.19f,.25f,.03f},{2.70f,.17f,.21f,.17f,.02f},{3.05f,.10f,.24f,.10f,.01f}};
        static readonly float[,] EngineRows={{-0.95f,.44f,.14f,.70f,.10f},{-1.35f,.40f,.14f,.78f,.14f},
                                            {-1.80f,.30f,.14f,.55f,.08f},{-2.25f,.16f,.16f,.25f,.03f}};
        static readonly float[,] PodRows={{-1.95f,.34f,.44f,.20f,.30f},{-1.50f,.40f,.66f,.18f,.42f},{-0.90f,.42f,.90f,.18f,.56f},
                                         {-0.10f,.42f,.95f,.18f,.62f},{0.30f,.40f,.86f,.18f,.58f}};

        // Outermost body surface (right side) at a given z and height, from the nose, engine cover
        // and sidepod sections. Suspension arms end just inside it so they visibly join the body.
        static float BodyHalfWidth(float z,float y)
        {
            float best=-1;
            var lofts=new[]{Sections(NoseRows),Sections(EngineRows),PodSections()};
            foreach(var sections in lofts)
                for(int k=0;k<sections.Count-1;k++)
                {
                    var (za,ra)=sections[k];var (zb,rb)=sections[k+1];
                    if(z<Mathf.Min(za,zb)||z>Mathf.Max(za,zb))continue;
                    float t=Mathf.Approximately(za,zb)?0:(z-za)/(zb-za);
                    for(int i=0;i<ra.Length;i++)
                    {
                        int j=(i+1)%ra.Length;
                        var p0=Vector2.Lerp(ra[i],rb[i],t);var p1=Vector2.Lerp(ra[j],rb[j],t);
                        if((p0.y-y)*(p1.y-y)>0||Mathf.Approximately(p0.y,p1.y))continue;
                        float x=p0.x+(p1.x-p0.x)*(y-p0.y)/(p1.y-p0.y);
                        best=Mathf.Max(best,x);
                    }
                    break;
                }
            return best>0?best:.30f;
        }

        static List<(float,Vector2[])> PodSections()
        {
            var list=new List<(float,Vector2[])>();
            for(int i=0;i<PodRows.GetLength(0);i++)list.Add((PodRows[i,0],Pod(PodRows[i,1],PodRows[i,2],PodRows[i,3],PodRows[i,4])));
            return list;
        }

        static void BuildBody(CarMesh b,bool pcDetail=false)
        {
            // Nose and monocoque front, from the cockpit bulkhead (z .48) to the tip (z 3.05).
            b.LoftZ(Sections(NoseRows),NoseMats,Carbon,Red,true);

            // Cockpit tub side walls (open profile), floor, bulkhead and seat.
            var wall=new[]{new Vector2(.30f,.20f),new Vector2(.31f,.72f),new Vector2(.37f,.79f),
                           new Vector2(.44f,.74f),new Vector2(.46f,.45f),new Vector2(.31f,.14f)};
            var wallMats=new[]{Cockpit,pcDetail?Weave:Cockpit,Red,Red,Carbon};
            foreach(var profile in new[]{wall,Flip(wall)})
                b.LoftZ(new List<(float,Vector2[])>{(-1.0f,profile),(-0.2f,profile),(0.62f,profile)},wallMats,-1,-1,false);
            if(pcDetail)
                foreach(float side in new[]{-1f,1f})
                    b.Tube(new Vector3(side*.305f,.718f,-.94f),new Vector3(side*.305f,.718f,.55f),.005f,Carbon);
            b.Box(new Vector3(0,.16f,-.19f),new Vector3(.62f,.04f,1.62f),Cockpit);
            b.Quad(new Vector3(-.31f,.14f,-1.0f),new Vector3(.31f,.14f,-1.0f),new Vector3(.31f,.86f,-1.0f),new Vector3(-.31f,.86f,-1.0f),Carbon);
            b.Box(new Vector3(0,.58f,-.90f),new Vector3(.56f,.62f,.08f),Carbon);
            b.Box(new Vector3(0,.26f,-.62f),new Vector3(.56f,.10f,.50f),Carbon);

            // Engine cover and airbox behind the driver.
            b.LoftZ(Sections(EngineRows),NoseMats,Carbon,Carbon,true);

            // Sidepods with dark intakes, shaped and tapering toward the rear.
            var pods=PodRows;
            var podMats=new[]{Carbon,Red,Red,Ivory,Red,Carbon};
            foreach(float s in new[]{1f,-1f})
            {
                var sections=new List<(float,Vector2[])>();
                for(int i=0;i<pods.GetLength(0);i++)
                {
                    var ring=Pod(pods[i,1],pods[i,2],pods[i,3],pods[i,4]);
                    sections.Add((pods[i,0],s>0?ring:Flip(ring)));
                }
                b.LoftZ(sections,podMats,Red,Carbon,true);
            }

            // Floor, diffuser and strakes.
            b.Box(new Vector3(0,.12f,-.55f),new Vector3(1.84f,.05f,3.2f),Carbon);
            b.Quad(new Vector3(-.62f,.145f,-2.15f),new Vector3(.62f,.145f,-2.15f),new Vector3(.62f,.32f,-2.50f),new Vector3(-.62f,.32f,-2.50f),Carbon);
            foreach(float x in new[]{-.4f,0f,.4f})
                b.ExtrudeX(ZY(-2.10f,.145f,-2.52f,.145f,-2.52f,.34f),x-.015f,x+.015f,Metal);

            // Front wing: ivory main plane, livery flap, carbon endplates and nose pylons.
            b.ExtrudeX(ZY(2.95f,.13f,2.80f,.165f,2.55f,.17f,2.35f,.15f,2.55f,.115f,2.80f,.11f),-1.33f,1.33f,Ivory);
            b.ExtrudeX(ZY(2.55f,.20f,2.40f,.27f,2.30f,.29f,2.38f,.24f,2.52f,.185f),-1.25f,1.25f,Red);
            var frontPlate=ZY(2.98f,.08f,2.98f,.20f,2.70f,.34f,2.28f,.34f,2.28f,.08f);
            foreach(float s in new[]{1f,-1f})
            {
                b.ExtrudeX(frontPlate,s*1.33f,s*1.37f,Carbon);
                b.Box(new Vector3(s*.11f,.19f,2.60f),new Vector3(.03f,.08f,.30f),Carbon);
            }

            // Rear wing: livery main plane, ivory flap, carbon endplates, metal swan-neck pylons.
            b.ExtrudeX(ZY(-2.05f,.98f,-2.15f,1.02f,-2.40f,1.03f,-2.55f,1.00f,-2.40f,.97f,-2.15f,.96f),-.95f,.95f,Red);
            b.ExtrudeX(ZY(-2.38f,1.08f,-2.48f,1.15f,-2.58f,1.18f,-2.55f,1.13f,-2.45f,1.08f),-.95f,.95f,Ivory);
            var rearPlate=ZY(-2.0f,.62f,-2.0f,1.10f,-2.25f,1.24f,-2.65f,1.24f,-2.65f,.62f);
            var pylon=ZY(-1.95f,.35f,-2.10f,.35f,-2.30f,.97f,-2.18f,.97f);
            foreach(float s in new[]{1f,-1f})
            {
                b.ExtrudeX(rearPlate,s*.95f,s*.99f,Carbon);
                b.ExtrudeX(pylon,s*.10f,s*.13f,Metal);
            }
            b.ExtrudeX(ZY(-2.45f,.45f,-2.65f,.47f,-2.65f,.50f,-2.45f,.49f),-.55f,.55f,Carbon);

            // Flat aero wishbones from the body surface to each wheel upright, plus a pushrod.
            // Upright faces sit 0.25 m inboard of the wheel centre (see BuildUpright).
            foreach(var (zc,xw) in WheelCentres)
                foreach(float s in new[]{1f,-1f})
                {
                    var upper=new Vector3(s*(xw-.25f),.52f,zc);var lower=new Vector3(s*(xw-.25f),.26f,zc);
                    foreach(float dz in new[]{.25f,-.25f})
                    {
                        float zi=zc+dz;
                        b.FlatTube(new Vector3(s*(BodyHalfWidth(zi,.55f)-.03f),.55f,zi),upper,.032f,.011f,Carbon);
                        b.FlatTube(new Vector3(s*(BodyHalfWidth(zi,.24f)-.03f),.24f,zi),lower,.032f,.011f,Carbon);
                    }
                    if(pcDetail)
                    {
                        foreach(float dz in new[]{.25f,-.25f})foreach(float y in new[]{.24f,.55f})
                        {
                            float zi=zc+dz, x=BodyHalfWidth(zi,y);
                            b.Box(new Vector3(s*(x+.006f),y,zi),new Vector3(.035f,.055f,.065f),Metal);
                        }
                    }
                    float zp=zc-.2f;
                    b.Tube(new Vector3(s*(xw-.25f),.28f,zc-.02f),new Vector3(s*(BodyHalfWidth(zp,.58f)-.03f),.58f,zp),.016f,Metal);
                }

            // Mirrors on short stalks, glass facing the driver.
            foreach(float s in new[]{1f,-1f})
            {
                b.Tube(new Vector3(s*.44f,.74f,.25f),new Vector3(s*.58f,.84f,.20f),.015f,Carbon);
                if(!pcDetail)b.Box(new Vector3(s*.64f,.86f,.19f),new Vector3(.17f,.075f,.06f),Red,Mirror);
                else
                {
                    float x=s*.64f;
                    b.PrismXY(XY(x-.075f,.8225f,x+.075f,.8225f,x+.085f,.835f,x+.085f,.885f,
                        x+.075f,.8975f,x-.075f,.8975f,x-.085f,.885f,x-.085f,.835f),.16f,.22f,Red);
                    b.Box(new Vector3(x,.86f,.158f),new Vector3(.138f,.049f,.004f),Mirror);
                }
            }
        }

        // Wheel centres (z, |x|) in car space; matches the existing wheel pivots.
        static readonly (float,float)[] WheelCentres={(1.40f,1.13f),(-1.45f,1.13f)};

        static void BuildUpright(CarMesh b,float s,bool front,bool pcDetail=false)
        {
            // In pivot space (the wheel centre). A tapered metal block just inboard of the tyre.
            float x=-s*.25f;
            var rows=new[]{(-.17f,.07f),(0f,.09f),(.17f,.06f)};
            for(int r=0;r<rows.Length-1;r++)
            {
                var (y0,w0)=rows[r];var (y1,w1)=rows[r+1];
                var a=new[]{new Vector3(x-.04f,y0,-w0/2-.02f),new Vector3(x+.04f,y0,-w0/2-.02f),new Vector3(x+.04f,y0,w0/2+.02f),new Vector3(x-.04f,y0,w0/2+.02f)};
                var c=new[]{new Vector3(x-.04f,y1,-w1/2-.02f),new Vector3(x+.04f,y1,-w1/2-.02f),new Vector3(x+.04f,y1,w1/2+.02f),new Vector3(x-.04f,y1,w1/2+.02f)};
                for(int i=0;i<4;i++){int j=(i+1)%4;b.Quad(a[i],a[j],c[j],c[i],Metal);}
                b.Poly(a,Metal);b.Poly(c,Metal);
            }
            if(pcDetail)
            {
                foreach(float y in new[]{-.13f,.13f})
                    b.Box(new Vector3(x,y,0),new Vector3(.10f,.055f,.14f),Metal);
            }
            if(front)b.Tube(new Vector3(x,.02f,.05f),new Vector3(x-s*.07f,.02f,.16f),.014f,Metal);   // steering arm
        }

        static void BuildSteering(CarMesh b)
        {
            // Smooth swept rim: one continuous tube around a flat-bottomed oval, thicker grips
            // where the hands go. Thickness depends only on |cos t| so both sides match.
            const int samples=40,sides=6;
            var centres=new Vector3[samples];var grips=new float[samples];
            for(int k=0;k<samples;k++)
            {
                float t=k*Mathf.PI*2/samples;
                centres[k]=new Vector3(.195f*Mathf.Cos(t),Mathf.Max(.125f*Mathf.Sin(t),-.112f),0);
                float g=Mathf.Clamp01((Mathf.Abs(Mathf.Cos(t))-.72f)/.2f);g=g*g*(3-2*g);
                if(centres[k].y<-.1f)g*=.4f;
                grips[k]=g;
            }
            var rings=new Vector3[samples][];
            for(int k=0;k<samples;k++)
            {
                var tangent=(centres[(k+1)%samples]-centres[(k+samples-1)%samples]).normalized;
                var outward=new Vector3(tangent.y,-tangent.x,0);
                float radius=.014f+.012f*grips[k],depth=1.15f+.35f*grips[k];
                rings[k]=new Vector3[sides];
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides;
                    rings[k][j]=centres[k]+outward*(Mathf.Cos(a)*radius)+Vector3.forward*(Mathf.Sin(a)*radius*depth);
                }
            }
            for(int k=0;k<samples;k++)
            {
                var ra=rings[k];var rb=rings[(k+1)%samples];
                for(int j=0;j<sides;j++){int i2=(j+1)%sides;b.Quad(ra[j],ra[i2],rb[i2],rb[j],Carbon);}
            }

            // Centre plate, display and LED strip (the LEDs are static until the tachometer pass).
            b.PrismXY(XY(-.10f,-.07f,-.12f,-.04f,-.12f,.05f,-.09f,.075f,.09f,.075f,.12f,.05f,.12f,-.04f,.10f,-.07f),-.012f,.014f,Carbon);
            b.PrismXY(XY(-.075f,-.005f,.075f,-.005f,.075f,.055f,-.075f,.055f),-.016f,-.012f,Screen);
            for(int i=0;i<9;i++)
            {
                float x=-.064f+i*.016f;
                b.PrismXY(XY(x-.006f,.061f,x+.006f,.061f,x+.006f,.069f,x-.006f,.069f),-.016f,-.012f,i<3?Green:i<6?Yellow:Red);
            }
            // Spokes to the grips and the flat bottom.
            foreach(float s in new[]{1f,-1f})
                b.PrismXY(XY(s*.115f,-.03f,s*.182f,-.045f,s*.182f,.015f,s*.115f,.03f),-.006f,.010f,Carbon);
            b.PrismXY(XY(-.03f,-.07f,.03f,-.07f,.025f,-.105f,-.025f,-.105f),-.006f,.010f,Carbon);
            // Octagonal buttons in two columns, two rotary knobs with ivory pointers.
            var buttons=new[]{(-.095f,.03f,Yellow),(-.095f,0f,Mirror),(-.095f,-.03f,Red),(.095f,.03f,Green),(.095f,0f,Ivory),(.095f,-.03f,Mirror)};
            foreach(var (bx,by,m) in buttons)b.PrismXY(Octagon(bx,by,.009f),-.020f,-.012f,m);
            foreach(float kx in new[]{-.045f,.045f})
            {
                b.PrismXY(Octagon(kx,-.04f,.013f),-.024f,-.012f,Metal);
                b.PrismXY(XY(kx-.002f,-.04f,kx+.002f,-.04f,kx+.002f,-.03f,kx-.002f,-.03f),-.0255f,-.024f,Ivory);
            }
            // Shift paddles behind the rim and a yellow top-centre marker.
            foreach(float s in new[]{1f,-1f})
                b.PrismXY(XY(s*.07f,.02f,s*.19f,.05f,s*.21f,-.02f,s*.08f,-.04f),.016f,.024f,Carbon);
            b.PrismXY(XY(-.012f,.108f,.012f,.108f,.012f,.145f,-.012f,.145f),-.02f,.02f,Yellow);
        }

        static Vector2[] Octagon(float cx,float cy,float r)
        {
            var points=new Vector2[8];
            for(int k=0;k<8;k++){float a=k*Mathf.PI/4+Mathf.PI/8;points[k]=new Vector2(cx+r*Mathf.Cos(a),cy+r*Mathf.Sin(a));}
            return points;
        }

        // Pairs of (x, y) values -> polygon points.
        static Vector2[] XY(params float[] xy)
        {
            var points=new Vector2[xy.Length/2];
            for(int i=0;i<points.Length;i++)points[i]=new Vector2(xy[i*2],xy[i*2+1]);
            return points;
        }

        static void BuildTire(CarMesh b)
        {
            // 18-sided tyre around the X axle with chamfered shoulders and a metal barrel.
            const int sides=18;
            var profile=new[]{new Vector2(-.20f,.26f),new Vector2(-.20f,.345f),new Vector2(-.17f,.39f),
                              new Vector2(.17f,.39f),new Vector2(.20f,.345f),new Vector2(.20f,.26f)};
            for(int k=0;k<sides;k++)
            {
                float a0=k*Mathf.PI*2/sides,a1=(k+1)*Mathf.PI*2/sides;
                for(int i=0;i<profile.Length-1;i++)
                    b.Quad(Axle(profile[i],a0),Axle(profile[i],a1),Axle(profile[i+1],a1),Axle(profile[i+1],a0),Carbon);
                b.Quad(Axle(new Vector2(-.20f,.26f),a0),Axle(new Vector2(.20f,.26f),a0),
                       Axle(new Vector2(.20f,.26f),a1),Axle(new Vector2(-.20f,.26f),a1),Metal);
            }
        }

        static Vector3 Axle(Vector2 xr,float angle)=>new Vector3(xr.x,xr.y*Mathf.Cos(angle),xr.y*Mathf.Sin(angle));

        static void BuildRim(CarMesh b,float s,bool pcDetail=false)
        {
            // Recessed dark wheel faces, five raised ivory spokes on the outer side and a metal nut.
            float x0=s*.17f;const int n=18;
            var outer=new Vector3[n];var inner=new Vector3[n];
            for(int k=0;k<n;k++)
            {
                float a=k*Mathf.PI*2/n;
                outer[k]=new Vector3(x0,.26f*Mathf.Cos(a),.26f*Mathf.Sin(a));
                inner[k]=new Vector3(-x0,outer[k].y,outer[k].z);
            }
            b.Poly(outer,Carbon);b.Poly(inner,Carbon);
            if(pcDetail)
            {
                // Inboard brake ring on the spinning wheel, clear of the dark rim face.
                float disc=-s*.176f;
                for(int k=0;k<n;k++)
                {
                    float a=k*Mathf.PI*2/n, c=(k+1)*Mathf.PI*2/n;
                    Vector3 V(float r,float t)=>new Vector3(disc,r*Mathf.Cos(t),r*Mathf.Sin(t));
                    b.Quad(V(.12f,a),V(.235f,a),V(.235f,c),V(.12f,c),Metal);
                }
            }
            for(int k=0;k<5;k++)
            {
                float a=k*Mathf.PI*2/5;
                var d=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a));var t=new Vector3(0,-Mathf.Sin(a),Mathf.Cos(a));
                var q=new[]{d*.05f-t*.025f,d*.25f-t*.02f,d*.25f+t*.02f,d*.05f+t*.025f};
                b.Prism(q,x0,x0+s*.02f,Ivory);
            }
            var hex=new Vector3[6];
            for(int k=0;k<6;k++)hex[k]=new Vector3(0,.05f*Mathf.Cos(k*Mathf.PI/3),.05f*Mathf.Sin(k*Mathf.PI/3));
            b.Prism(hex,x0,x0+s*.04f,Metal);
        }

        static List<(float,Vector2[])> Sections(float[,] rows)
        {
            var list=new List<(float,Vector2[])>();
            for(int i=0;i<rows.GetLength(0);i++)list.Add((rows[i,0],NoseRing(rows[i,1],rows[i,2],rows[i,3],rows[i,4])));
            return list;
        }

        // Pairs of (z, y) values -> profile points.
        static Vector2[] ZY(params float[] zy)
        {
            var points=new Vector2[zy.Length/2];
            for(int i=0;i<points.Length;i++)points[i]=new Vector2(zy[i*2],zy[i*2+1]);
            return points;
        }

        static void EnsureFolder()
        {
            if(AssetDatabase.IsValidFolder(CarPath))return;
            if(!AssetDatabase.IsValidFolder("Assets/PolygonRacing/Generated"))AssetDatabase.CreateFolder("Assets/PolygonRacing","Generated");
            AssetDatabase.CreateFolder("Assets/PolygonRacing/Generated","Car");
        }

        // Small multi-material mesh builder. Materials are double-sided and the flat shader
        // derives facet normals, so triangle winding does not matter.
        sealed class CarMesh
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int>[] indices;
            public CarMesh(bool pcDetail=false){indices=new List<int>[pcDetail?SlotCount:Weave];for(int i=0;i<indices.Length;i++)indices[i]=new List<int>();}

            public void Tri(Vector3 a,Vector3 b,Vector3 c,int m)
            {
                int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                // Longitudinal and sloped cross-section distances keep the rail weave square.
                Vector2 UV(Vector3 p)=>m==Weave?new Vector2(p.z/ .02f,(Mathf.Abs(p.x)+p.y)*.70710678f/ .02f):Vector2.zero;
                uv.Add(UV(a));uv.Add(UV(b));uv.Add(UV(c));
                indices[m].Add(n);indices[m].Add(n+1);indices[m].Add(n+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int m){Tri(a,b,c,m);Tri(a,c,d,m);}
            public void Poly(IList<Vector3> points,int m){for(int i=1;i<points.Count-1;i++)Tri(points[0],points[i],points[i+1],m);}

            // Rings of (x, y) points at increasing or decreasing z; mats[i] colours segment i -> i+1.
            public void LoftZ(List<(float z,Vector2[] ring)> sections,int[] mats,int capFirst,int capLast,bool closed)
            {
                int count=sections[0].ring.Length,segments=closed?count:count-1;
                for(int r=0;r<sections.Count-1;r++)
                {
                    var (za,ra)=sections[r];var (zb,rb)=sections[r+1];
                    for(int i=0;i<segments;i++)
                    {
                        int j=(i+1)%count;
                        Quad(P(ra[i],za),P(ra[j],za),P(rb[j],zb),P(rb[i],zb),mats[i]);
                    }
                }
                if(capFirst>=0)Cap(sections[0],capFirst);
                if(capLast>=0)Cap(sections[sections.Count-1],capLast);
            }
            void Cap((float z,Vector2[] ring) section,int m)
            {
                var points=new Vector3[section.ring.Length];
                for(int i=0;i<points.Length;i++)points[i]=P(section.ring[i],section.z);
                Poly(points,m);
            }
            static Vector3 P(Vector2 xy,float z)=>new Vector3(xy.x,xy.y,z);

            // 2D profile in (z, y) extruded across x0..x1, with end caps.
            public void ExtrudeX(Vector2[] zy,float x0,float x1,int m)
            {
                var a=new Vector3[zy.Length];var b=new Vector3[zy.Length];
                for(int i=0;i<zy.Length;i++){a[i]=new Vector3(x0,zy[i].y,zy[i].x);b[i]=new Vector3(x1,zy[i].y,zy[i].x);}
                for(int i=0;i<zy.Length;i++){int j=(i+1)%zy.Length;Quad(a[i],a[j],b[j],b[i],m);}
                Poly(a,m);Poly(b,m);
            }

            // Polygon given in the YZ plane (x ignored), extruded along x from x0 to x1.
            public void Prism(Vector3[] yz,float x0,float x1,int m)
            {
                var a=new Vector3[yz.Length];var b=new Vector3[yz.Length];
                for(int i=0;i<yz.Length;i++){a[i]=new Vector3(x0,yz[i].y,yz[i].z);b[i]=new Vector3(x1,yz[i].y,yz[i].z);}
                for(int i=0;i<yz.Length;i++){int j=(i+1)%yz.Length;Quad(a[i],a[j],b[j],b[i],m);}
                Poly(b,m);
            }

            // Axis-aligned box; the face toward -z (the driver) can use its own material.
            public void Box(Vector3 c,Vector3 size,int m,int back=-1)
            {
                var h=size*.5f;
                Vector3 V(float x,float y,float z)=>new Vector3(c.x+x*h.x,c.y+y*h.y,c.z+z*h.z);
                Quad(V(-1,-1,-1),V(1,-1,-1),V(1,1,-1),V(-1,1,-1),back>=0?back:m);
                Quad(V(-1,-1,1),V(-1,1,1),V(1,1,1),V(1,-1,1),m);
                Quad(V(-1,1,-1),V(1,1,-1),V(1,1,1),V(-1,1,1),m);
                Quad(V(-1,-1,-1),V(-1,-1,1),V(1,-1,1),V(1,-1,-1),m);
                Quad(V(-1,-1,-1),V(-1,1,-1),V(-1,1,1),V(-1,-1,1),m);
                Quad(V(1,-1,-1),V(1,-1,1),V(1,1,1),V(1,1,-1),m);
            }

            // Flattened tube (aero wishbone): width rw across, thickness rh vertically.
            public void FlatTube(Vector3 from,Vector3 to,float rw,float rh,int m,int sides=6)
            {
                var d=(to-from).normalized;
                var up=Mathf.Abs(d.y)<.9f?Vector3.up:Vector3.right;
                var u=Vector3.Cross(d,up).normalized;var v=Vector3.Cross(u,d);
                var a=new Vector3[sides];var b=new Vector3[sides];
                for(int k=0;k<sides;k++)
                {
                    float angle=k*Mathf.PI*2/sides;
                    var offset=u*(Mathf.Cos(angle)*rw)+v*(Mathf.Sin(angle)*rh);
                    a[k]=from+offset;b[k]=to+offset;
                }
                for(int k=0;k<sides;k++){int j=(k+1)%sides;Quad(a[k],a[j],b[j],b[k],m);}
            }

            // Polygon in the XY plane extruded from z0 to z1 (both faces capped).
            public void PrismXY(Vector2[] xy,float z0,float z1,int m)
            {
                var a=new Vector3[xy.Length];var b=new Vector3[xy.Length];
                for(int i=0;i<xy.Length;i++){a[i]=new Vector3(xy[i].x,xy[i].y,z0);b[i]=new Vector3(xy[i].x,xy[i].y,z1);}
                for(int i=0;i<xy.Length;i++){int j=(i+1)%xy.Length;Quad(a[i],a[j],b[j],b[i],m);}
                Poly(a,m);Poly(b,m);
            }

            // Thin square tube between two points.
            public void Tube(Vector3 from,Vector3 to,float radius,int m,int sides=4)
            {
                var d=(to-from).normalized;
                var up=Mathf.Abs(d.y)<.9f?Vector3.up:Vector3.right;
                var u=Vector3.Cross(d,up).normalized;var v=Vector3.Cross(u,d);
                var a=new Vector3[sides];var b=new Vector3[sides];
                for(int k=0;k<sides;k++)
                {
                    float angle=k*Mathf.PI*2/sides+Mathf.PI/sides;
                    var offset=(u*Mathf.Cos(angle)+v*Mathf.Sin(angle))*radius;
                    a[k]=from+offset;b[k]=to+offset;
                }
                for(int k=0;k<sides;k++){int j=(k+1)%sides;Quad(a[k],a[j],b[j],b[k],m);}
            }

            public void Fill(Mesh mesh)
            {
                mesh.Clear();
                if(vertices.Count>65000)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetUVs(0,uv);
                mesh.subMeshCount=indices.Length;
                for(int i=0;i<indices.Length;i++)mesh.SetTriangles(indices[i],i);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
            }
        }
    }
}
#endif
