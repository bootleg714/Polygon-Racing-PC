#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PolygonRacing.Editor
{
    public static class PolygonRivalPaceSetup
    {
        const string Path="Assets/Resources/PolygonRivalPace.asset";
        [MenuItem("Polygon Racing/8 - Opponent Pace Settings")]
        static void OpenSettings()
        {
            if(EditorApplication.isPlaying)
            {Debug.LogWarning("Stop Play Mode before creating or editing persistent opponent settings.");return;}
            var settings=AssetDatabase.LoadAssetAtPath<PolygonRivalPaceSettings>(Path);
            if(!settings)
            {
                if(File.Exists(Path))
                {Debug.LogError("A different asset already exists at "+Path+". It was not replaced.");return;}
                Directory.CreateDirectory("Assets/Resources");AssetDatabase.Refresh();
                settings=ScriptableObject.CreateInstance<PolygonRivalPaceSettings>();
                AssetDatabase.CreateAsset(settings,Path);AssetDatabase.SaveAssets();
            }
            Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);
            Debug.Log("Opponent pace settings selected. Adjust them in the Inspector outside Play Mode, then Play or rebuild to test. Untick Rubber Banding to compare baseline pace alone.");
        }
    }
}
#endif
