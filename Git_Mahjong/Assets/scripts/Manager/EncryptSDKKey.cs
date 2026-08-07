using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class EncryptSDKKey : MonoBehaviour
{
    // 32位随机加密密钥（AES-256 标准，必须32位）
    private const string EncryptionKey = "8k9S7b2Q8x4P9m6Z7y3A8s9D5f8G7h2J9";
    // 16位随机初始向量（IV，必须16位）
    private const string IvKey = "7d8s9a6f4g7h2j8k";

    public static EncryptSDKKey Instance = null;
    private void Awake()
    {
        Instance = this;

    }
    public static void TestKey(string originalSdkKey)
    {
        // 你的原始SDK Key
        Debug.Log("----------------------------------------------------------------------------");
        // 加密（每次结果不同）
        string encryptedSdkKey = EncryptWithRandomSalt(originalSdkKey);
        Debug.Log("加密结果（每次不同）：" + encryptedSdkKey);

        // 解密（还原原始值）
        string decryptedSdkKey = DecryptWithRandomSalt(encryptedSdkKey);
        Debug.Log("解密结果（还原原值）：" + decryptedSdkKey);
        Debug.Log("----------------------------------------------------------------------------");

    }

    /// <summary>
    /// 带随机盐值的加密方法（每次结果不同）
    /// </summary>
    /// <param name="plainText">原始字符串（SDK Key）</param>
    /// <returns>Base64编码的加密结果</returns>
    public static string EncryptWithRandomSalt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        try
        {
            // 1. 生成随机盐值（每次加密生成不同的8位随机数）
            byte[] salt = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt); // 安全的随机数生成（避免伪随机）
            }

            // 2. AES加密配置
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32)); // 确保32位
                aes.IV = Encoding.UTF8.GetBytes(IvKey.PadRight(16).Substring(0, 16));         // 确保16位
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 3. 加密原始字符串
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        encryptedBytes = ms.ToArray();
                    }
                }

                // 4. 拼接「盐值 + 加密数据」，再转Base64（解密时需要先提取盐值）
                byte[] resultBytes = new byte[salt.Length + encryptedBytes.Length];
                Buffer.BlockCopy(salt, 0, resultBytes, 0, salt.Length);
                Buffer.BlockCopy(encryptedBytes, 0, resultBytes, salt.Length, encryptedBytes.Length);

                return Convert.ToBase64String(resultBytes);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("加密失败：" + ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// 解密方法（还原带随机盐值的加密字符串）
    /// </summary>
    /// <param name="encryptedText">加密后的Base64字符串</param>
    /// <returns>原始字符串</returns>
    public static string DecryptWithRandomSalt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

        try
        {
            // 1. 解析Base64，提取盐值和加密数据
            byte[] resultBytes = Convert.FromBase64String(encryptedText);
            byte[] salt = new byte[8];
            byte[] encryptedBytes = new byte[resultBytes.Length - 8];

            Buffer.BlockCopy(resultBytes, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(resultBytes, 8, encryptedBytes, 0, encryptedBytes.Length);

            // 2. AES解密配置（和加密一致）
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = Encoding.UTF8.GetBytes(IvKey.PadRight(16).Substring(0, 16));
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 3. 解密数据
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] decryptedBytes;

                using (MemoryStream ms = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream msDecrypt = new MemoryStream())
                        {
                            cs.CopyTo(msDecrypt);
                            decryptedBytes = msDecrypt.ToArray();
                        }
                    }
                }

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("解密失败：" + ex.Message);
            return string.Empty;
        }
    }
}