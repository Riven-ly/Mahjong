#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace CashRewards.ComboEffects.Editor
{
    public static class ComboDependencySetup
    {
        [InitializeOnLoadMethod]
        static void Schedule(){EditorApplication.delayCall+=EnsureSettings;}
        public static void EnsureSettings()
        {
            if(Resources.Load<TMP_Settings>("TMP Settings"))return;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CashRewardsComboEffects/Fonts/ComboLatinSDF.asset");
            if(!font)return;
            Directory.CreateDirectory("Assets/CashRewardsComboEffects/Resources");
            var settings=ScriptableObject.CreateInstance<TMP_Settings>();
            var so=new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue=font;
            so.FindProperty("m_fallbackFontAssets").arraySize=0;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(settings,"Assets/CashRewardsComboEffects/Resources/TMP Settings.asset");
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
