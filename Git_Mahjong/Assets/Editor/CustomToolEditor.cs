
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
}
