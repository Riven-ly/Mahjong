using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;


public class ABbuildEditor
{
    const string AssetPath = "Assets/ABRes";
    const string ABName = "abres.dat";

    private const string AES_KEY = "1234567890abcdef";
    private const string AES_IV = "abcdef1234567890";


    [MenuItem("Tools/AB打包/打包AB包")]
    public static void BuildABAndEncrypt()
    {
        try
        {
            //SetABLabelForFolder(AssetPath, ABName);
            // 1. 定义输出路径并强制创建目录
            string abOutputPath = Application.streamingAssetsPath;
            abOutputPath = abOutputPath.Replace('\\', '/');
            Directory.CreateDirectory(abOutputPath); // 强制创建，避免目录不存在

            // 2. 清理旧AB包（避免缓存干扰）
            string oldABPath = Path.Combine(abOutputPath, ABName);
            if (File.Exists(oldABPath))
            {
                File.Delete(oldABPath);
                Debug.Log("清理旧AB包：" + oldABPath);
            }

            // 3. 收集需要打包的资源（增加数量校验）
            string[] assetGuids = AssetDatabase.FindAssets("t:Object", new[] { AssetPath })
                                  .Where(guid => AssetDatabase.GUIDToAssetPath(guid).StartsWith(AssetPath))
                                  .ToArray();
            string[] assetPaths = assetGuids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

            // 新增：打印所有收集到的资源路径（含子文件夹）
            if (assetPaths.Length > 0)
            {
                Debug.Log($"收集到 {assetPaths.Length} 个资源，开始打包");
            }
            else
            {
                Debug.Log($"【警告】{AssetPath} 目录（含子文件夹）下无任何资源");
            }

            // 4. 设置AB包构建参数
            AssetBundleBuild[] builds = new AssetBundleBuild[1];
            builds[0].assetBundleName = ABName;
            builds[0].assetNames = assetPaths;

            // 替换原打包代码，增加返回值校验
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                abOutputPath,
                builds,
                BuildAssetBundleOptions.None,
                EditorUserBuildSettings.activeBuildTarget
            );
            Debug.Log(abOutputPath);
            if (manifest == null)
            {
                Debug.LogError("BuildAssetBundles 执行失败：返回manifest为空");
                return;
            }
 
            Debug.Log("打包执行成功，Manifest包含的AB包列表：" + string.Join(",", manifest.GetAllAssetBundles()));


            // 6. 校验AB包是否生成（核心修复：避免加密不存在的文件）
            string newABPath = Path.Combine(abOutputPath, ABName);
            if (!File.Exists(newABPath))
            {
                Debug.Log($"打包失败：AB包文件未生成 → {newABPath}");
                return;
            }

            // 7. 加密AB包
            EncryptFile(newABPath, newABPath, AES_KEY, AES_IV);

            AssetDatabase.Refresh();
            Debug.Log($" AB包打包并加密完成：{newABPath}");
        }
        catch (Exception e)
        {
            Debug.Log($"打包加密异常：{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 为指定文件夹+所有子文件夹的资源设置AB标签（排除.meta）
    /// </summary>
    private static void SetABLabelForFolder(string folderPath, string abName)
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(abName))
        {
            Debug.Log("AB标签设置失败：路径或AB名不能为空");
            return;
        }

        // 验证文件夹存在性
        string fullFolderPath = Path.Combine(Application.dataPath, "", folderPath).Replace("\\", "/");
        if (!Directory.Exists(fullFolderPath))
        {
            Debug.Log($"AB标签设置失败：文件夹不存在 → {fullFolderPath}");
            return;
        }
        Debug.Log($"AB标签设置 → {fullFolderPath}");
        // 递归获取所有有效资源（排除.meta、文件夹）
        string[] allAssetPaths = Directory.GetFiles(fullFolderPath, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".meta") && !Directory.Exists(path))
            .Select(path => "Assets" + path.Substring(Application.dataPath.Length).Replace("\\", "/"))
            .ToArray();

        int setCount = 0;
        foreach (var assetPath in allAssetPaths)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                Debug.LogWarning($"跳过无效资源：{assetPath}");
                continue;
            }

            // 仅当标签不一致时更新（避免重复操作）
            if (importer.assetBundleName != abName)
            {
                importer.assetBundleName = abName;
                importer.SaveAndReimport();
                setCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ AB标签设置完成：共处理 {allAssetPaths.Length} 个资源，成功设置 {setCount} 个 → AB名：{abName}");
    }

    private static void EncryptFile(string inputFile, string outputFile, string key, string iv)
    {
        if (!File.Exists(inputFile))
        {
            Debug.LogError($"加密失败：输入文件不存在 → {inputFile}");
            return;
        }

        // 步骤1：定义临时文件路径（避免覆盖原文件时的占用冲突）
        string tempOutputFile = outputFile + ".tmp";
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = System.Text.Encoding.UTF8.GetBytes(key);
                aes.IV = System.Text.Encoding.UTF8.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 步骤2：读取原文件，加密到临时文件
                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read)) // 增加FileShare.Read允许只读共享
                using (FileStream fsOutput = new FileStream(tempOutputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (CryptoStream cs = new CryptoStream(fsOutput, encryptor, CryptoStreamMode.Write))
                {
                    fsInput.CopyTo(cs);
                }
            }

            // 步骤3：替换原文件（先删原文件，再重命名临时文件）
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile); // 删除被锁定的原文件
            }
            File.Move(tempOutputFile, outputFile); // 临时文件重命名为目标文件
            Debug.Log($"加密完成：{outputFile}");
        }
        catch (Exception e)
        {
            Debug.LogError($"加密异常：{e.Message}");
        }
        finally
        {
            // 步骤4：清理临时文件（若加密失败）
            if (File.Exists(tempOutputFile))
            {
                File.Delete(tempOutputFile);
            }
        }
    }
}