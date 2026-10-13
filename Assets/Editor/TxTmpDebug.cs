using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TxTmpDebug
{
    [MenuItem("Tools/调试/打印 TMP 材质 Shader")]
    public static void PrintShaders()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== TMP MATERIAL SHADER DUMP ===");
        var texts = Object.FindObjectsOfType<TMP_Text>(true);
        sb.AppendLine("TMP_Text count: " + texts.Length);
        int i = 0;
        foreach (var t in texts)
        {
            if (t == null) continue;
            var shared = t.fontSharedMaterial;
            var inst = t.fontMaterial;
            sb.AppendLine($"--- [{i++}] {GetPath(t.transform)}");
            sb.AppendLine($"    text = {t.text}");
            sb.AppendLine($"    font = {(t.font != null ? t.font.name : "NULL")}");
            if (shared != null)
                sb.AppendLine($"    sharedMat = {shared.name} | shader = {(shared.shader != null ? shared.shader.name + " | isSupported=" + shared.shader.isSupported : "NULL")}");
            else
                sb.AppendLine("    sharedMat = NULL");
            if (inst != null)
                sb.AppendLine($"    instMat   = {inst.name} | shader = {(inst.shader != null ? inst.shader.name + " | isSupported=" + inst.shader.isSupported : "NULL")}");
            else
                sb.AppendLine("    instMat   = NULL");
            var mfr = t.materialForRendering;
            if (mfr != null)
                sb.AppendLine($"    matForRendering = {mfr.name} | shader = {(mfr.shader != null ? mfr.shader.name + " | isSupported=" + mfr.shader.isSupported : "NULL")}");
            if (inst != null)
            {
                sb.AppendLine("    keywords: " + string.Join(",", inst.shaderKeywords));
                sb.AppendLine($"    _MainTex = {(inst.mainTexture != null ? inst.mainTexture.name : "NULL")}");
                sb.AppendLine($"    _TextureWidth = {inst.GetFloat("_TextureWidth")}  _TextureHeight = {inst.GetFloat("_TextureHeight")}");
                sb.AppendLine($"    _GradientScale = {inst.GetFloat("_GradientScale")}  _ScaleRatioA = {inst.GetFloat("_ScaleRatioA")}");
            }
        }
        sb.AppendLine("=== END ===");
        Debug.Log(sb.ToString());
    }

    static string GetPath(Transform tr)
    {
        string p = tr.name;
        while (tr.parent != null) { tr = tr.parent; p = tr.name + "/" + p; }
        return p;
    }
}
