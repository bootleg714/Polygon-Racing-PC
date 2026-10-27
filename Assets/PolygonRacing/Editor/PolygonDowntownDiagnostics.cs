#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PolygonRacing.Editor
{
    // Temporary diagnostic (October 27): reports mesh data for one Downtown chunk.
    public static class PolygonDowntownDiagnostics
    {
        [MenuItem("Polygon Racing/Debug - Report Downtown Chunk 0,0")]
        static void Report()
        {
            var block=GameObject.Find("Downtown block 0,0");
            if(!block){Debug.LogWarning("Downtown block 0,0 not found.");return;}
            var sb=new System.Text.StringBuilder();
            foreach(var f in block.GetComponentsInChildren<MeshFilter>(true))
            {
                var m=f.sharedMesh;var r=f.GetComponent<MeshRenderer>();
                if(!m){sb.AppendLine(f.name+": NO MESH");continue;}
                var v=m.vertices;int bad=0;
                foreach(var p in v)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||Mathf.Abs(p.x)>1e5f||Mathf.Abs(p.y)>1e5f||Mathf.Abs(p.z)>1e5f)bad++;
                sb.AppendLine($"{f.name}: verts {m.vertexCount} idx {m.GetIndexCount(0)} fmt {m.indexFormat} bounds {m.bounds.center} {m.bounds.size} bad {bad} asset {AssetDatabase.GetAssetPath(m)} enabled {(r?r.enabled:false)} mat {(r&&r.sharedMaterial?r.sharedMaterial.name+"/"+r.sharedMaterial.shader.name:"none")}");
            }
            Debug.Log(sb.ToString());
            System.IO.File.WriteAllText("Temp/downtown_chunk_report.txt",sb.ToString());
        }
        [MenuItem("Polygon Racing/Debug - Report Large Downtown Meshes")]
        static void Large()
        {
            var root=GameObject.Find("Circuit - editable scenery");
            if(!root){Debug.LogWarning("Scenery not found.");return;}
            var sb=new System.Text.StringBuilder();int total=0,big=0;
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var m=f.sharedMesh;if(!m)continue;total++;
                if(m.vertexCount>65000||m.indexFormat==UnityEngine.Rendering.IndexFormat.UInt32)
                {big++;sb.AppendLine($"{f.name}: verts {m.vertexCount} fmt {m.indexFormat} bounds {m.bounds.size} asset {AssetDatabase.GetAssetPath(m)}");}
            }
            sb.Insert(0,$"{big} of {total} meshes are large\r\n");
            Debug.Log(sb.ToString());
            System.IO.File.WriteAllText("Temp/downtown_large_meshes.txt",sb.ToString());
        }
    }
}
#endif
