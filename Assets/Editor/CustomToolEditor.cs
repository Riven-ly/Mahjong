
using System.IO;
using UnityEditor;
using UnityEngine;

public class CustomToolEditor
{
    [MenuItem("Tools/所有资源重新序列化/Force Reserialize All Assets")]
    public static void ForceReserializeAssets()
    {
        if (EditorUtility.DisplayDialog("确认操作",
            "此操作会重新序列化所有资源，建议先备份项目。是否继续？",
            "继续", "取消"))
        {
            AssetDatabase.ForceReserializeAssets();
            Debug.Log("✅ 所有资源重新序列化完成");
        }
    }

    [MenuItem("Tools/PlayerPrefs_DeleteAll")]
    public static void PlayerPrefsDeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }

    [MenuItem("Tools/Screenshot/Take Screenshot %#y")] // 快捷键 Ctrl+Shift+Y
    private static void CaptureRuntimeScreenshot()
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string savePath = Path.Combine(folderPath, $"Screenshot_{timestamp}.png");

        ScreenCapture.CaptureScreenshot(savePath); // 截取当前 Game 视图内容
        Debug.Log($"截图已保存至：{savePath}"); // 输出日志
    }
}
