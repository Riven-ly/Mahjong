using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ABbuildManager
{
    const string ABName = "abres.dat";
    private const string AES_KEY = "1234567890abcdef";
    private const string AES_IV = "abcdef1234567890";


    private static AssetBundle ab = null;


    /// <summary>
    /// 从加密AB包加载AssetBundle
    /// </summary>
    public static IEnumerator LoadAB(Action<AssetBundle> action)
    {
        if (ab != null)
        {
            action?.Invoke(ab);
            yield break;
        }
        string abPath = Path.Combine(Application.streamingAssetsPath, ABName);
        abPath = abPath.Replace('\\', '/');
        Debug.Log("abPath ：" + abPath);
        byte[] encryptedBytes = null;
        using (UnityWebRequest uwr = UnityWebRequest.Get(abPath))
        {
            Debug.Log("AB包加载中");
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"读取失败：{uwr.error}");
                yield break;
            }
            encryptedBytes = uwr.downloadHandler.data;
        }

        // AES解密
        byte[] decryptedBytes = DecryptBytes(encryptedBytes, AES_KEY, AES_IV);

        // 从内存流加载AB包
        using (MemoryStream ms = new MemoryStream(decryptedBytes))
        {
            ab = AssetBundle.LoadFromStream(ms);
            if (ab == null)
            {
                yield break;
            }
            Debug.Log($"✅ AB包加载成功：{ABName}");
            action?.Invoke(ab);
            yield return null;
        }
    }

    public static async Task<AssetBundle> LoadABAsync()
    {
        if (ab != null)
        {
            return ab;
        }

        string abPath = Path.Combine(Application.streamingAssetsPath, ABName);
        abPath = abPath.Replace('\\', '/');
        Debug.Log("abPath：" + abPath);

        try
        {
            byte[] encryptedBytes = null;

            // ==============================
            // 1. 异步读取文件（主线程，带进度 0%~50%）
            // ==============================
            var request = UnityWebRequest.Get(abPath);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                Debug.Log($"读取文件进度：{(int)(operation.progress * 100)}%");
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"读取失败：{request.error}");
                return null;
            }

            encryptedBytes = request.downloadHandler.data;
            Debug.Log("✅ 文件读取完成，开始后台解密");

            // ==============================
            // 2. 解密
            // ==============================
            byte[] decryptedBytes = null;
            await Task.Run(() =>
            {
                decryptedBytes = DecryptBytes(encryptedBytes, AES_KEY, AES_IV);
            });
            Debug.Log($"✅ 解密完成");

            // ==============================
            // 3. 加载AB包
            // ==============================

            using (MemoryStream ms = new MemoryStream(decryptedBytes))
            {
                var abRequest = AssetBundle.LoadFromStreamAsync(ms);
                // 用循环等待，而不是直接 await
                while (!abRequest.isDone)
                {
                    Debug.Log($"加载AB包进度：{(int)(abRequest.progress * 100)}%");
                    await Task.Yield(); // 每帧让出主线程，不卡顿
                }

                ab = abRequest.assetBundle;
                if (ab == null)
                {
                    Debug.LogError($"AB包加载失败：{ABName}");
                    return null;
                }

                Debug.Log($"✅ AB包加载成功：{ABName}");
                return ab;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 加载失败：{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// AES解密字节数组（保持不变，增加异常捕获）
    /// </summary>
    static byte[] DecryptBytes(byte[] inputBytes, string key, string iv)
    {
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = System.Text.Encoding.UTF8.GetBytes(key);
                aes.IV = System.Text.Encoding.UTF8.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(inputBytes, 0, inputBytes.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解密异常：{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 卸载AB包（避免内存泄漏）
    /// </summary>
    public static void UnloadAB(bool unloadAllLoadedObjects = false)
    {
        if (ab != null)
        {
            ab.Unload(unloadAllLoadedObjects);
            ab = null;
            Debug.Log("AB包已卸载");
        }
    }
}
public static class UnityWebRequestExtensions
{
    public static TaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();

        asyncOp.completed += op =>
        {
            tcs.SetResult(((UnityWebRequestAsyncOperation)op).webRequest);
        };

        return tcs.Task.GetAwaiter();
    }
}

