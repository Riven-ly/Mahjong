#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

namespace CashRewards.ComboEffects.Editor
{
    // The original export referenced DLL MonoScripts. Rebind only those known references
    // when the receiving project instead has the matching Spine 4.2 runtime as source.
    public static class ComboSpineBindings
    {
        const string OriginalDllGuid="ebe353a9e51fc6708625f16edcb81e06";
        [InitializeOnLoadMethod]
        static void Schedule(){EditorApplication.delayCall+=Repair;}
        [MenuItem("Tools/CashRewards Combo Effects/Repair Spine Bindings")]
        public static void Repair()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlaying)return;
            var data=ScriptableObject.CreateInstance<SkeletonDataAsset>();
            var atlas=ScriptableObject.CreateInstance<SpineAtlasAsset>();
            var go=new GameObject("SpineBindingProbe"){hideFlags=HideFlags.HideAndDontSave};
            var graphic=go.AddComponent<SkeletonGraphic>();
            try
            {
                string[] folders={"Assets/CashRewardsComboEffects","Assets/Resources/mahjongcore/art/effects/combofeedback","Assets/Resources/mahjongcore/art/effects/comboemoji"};
                var bindings=new[]{MonoScript.FromScriptableObject(data),MonoScript.FromScriptableObject(atlas),MonoScript.FromMonoBehaviour(graphic)};
                // DLL local IDs obtained from AssetDatabase in the export project.
                var oldIds=new long[]{106799970,1497261852,0};
                // SkeletonGraphic's original DLL local ID is discovered from the shipped prefab.
                string prefab="Assets/CashRewardsComboEffects/Prefabs/ComboEffects.prefab";
                if(File.Exists(prefab))
                {
                    foreach(Match m in Regex.Matches(File.ReadAllText(prefab),@"m_Script: \{fileID: (-?\d+), guid: "+OriginalDllGuid+@", type: 3\}"))
                    {oldIds[2]=long.Parse(m.Groups[1].Value);break;}
                }
                for(int i=0;i<bindings.Length;i++)
                {
                    if(!bindings[i]||oldIds[i]==0)continue;
                    string guid;long id;if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(bindings[i],out guid,out id))continue;
                    if(guid==OriginalDllGuid&&id==oldIds[i])continue;
                    string from="{fileID: "+oldIds[i]+", guid: "+OriginalDllGuid+", type: 3}";
                    string to="{fileID: "+id+", guid: "+guid+", type: 3}";
                    foreach(string folder in folders)
                    {
                        if(!Directory.Exists(folder))continue;
                        foreach(string file in Directory.GetFiles(folder,"*",SearchOption.AllDirectories))
                        {
                            string ext=Path.GetExtension(file);if(ext!=".asset"&&ext!=".prefab"&&ext!=".unity")continue;
                            string text=File.ReadAllText(file);if(!text.Contains(from))continue;
                            File.WriteAllText(file,text.Replace(from,to));AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceUpdate);
                        }
                    }
                }
            }
            finally{Object.DestroyImmediate(data);Object.DestroyImmediate(atlas);Object.DestroyImmediate(go);}
        }
    }
}
#endif
