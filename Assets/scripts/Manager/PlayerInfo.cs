using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��Ҵ浵���ݡ�
/// </summary>
[Serializable]
public class PlayerInfo
{
    public const int CurrencyUnitScale = 100;
    [JsonIgnore]
    public float Gold
    {
        get => MathF.Round(gold / (float)CurrencyUnitScale, 2);
    }
    [JsonIgnore]
    public float Diamond
    {
        get => MathF.Round(diamond / (float)CurrencyUnitScale, 2);
    }

    [JsonProperty]
    private int gold = 0;
    [JsonProperty]
    private int diamond = 0;
    public int level = 1;
    public int goldLevel = 0; // ��ҵȼ�
    public int goldExperience; // ��ǰ��ҵȼ����飬��λΪ0.1

    public int gameSceneItem_Hint = 1;
    public int gameSceneItem_Extract = 0;
    public int gameSceneItem_Exchange = 1;
    public int gameSceneItem_Return = 0;

    public int consecutiveNoAdCount = 0;
    [JsonIgnore] public bool adShownThisLevel = false;

    /// <summary>
    /// ���ӽ�ҵȼ����鲢�������������鵥λΪ0.1��
    /// </summary>
    public void AddGoldExperience(int experience)
    {
        if (goldLevel >= 100)
        {
            goldLevel = 100;
            goldExperience = 0;
            return;
        }

        goldExperience += experience;
        while (goldLevel < 100)
        {
            int requiredExperience = GetGoldLevelExperienceRequired(goldLevel + 1);
            if (goldExperience < requiredExperience)
            {
                break;
            }

            goldExperience -= requiredExperience;
            goldLevel++;
        }

        if (goldLevel >= 100)
        {
            goldExperience = 0;
        }
    }

    /// <summary>
    /// ��ȡ����ָ����ҵȼ����辭�顣����ֵ��λΪ0.1��
    /// </summary>
    public int GetGoldLevelExperienceRequired(int targetLevel)
    {
        float exponent = 0.5f + 0.03f * Mathf.Max(targetLevel - 10, 0);
        return Mathf.FloorToInt(10f * Mathf.Pow(targetLevel, exponent)) * 10;
    }

    //========================= ��� =========================
    public void Add_gold(int _cnt)
    {
        gold += _cnt;
        gold = Mathf.Min(gold, 9999999);
    }
    public void Minus_gold(int _cnt)
    {
        gold -= _cnt;
        gold = Mathf.Max(gold, 0);
    }

    //========================= ��ʯ =========================
    public void Add_diamond(int _cnt)
    {
        diamond += _cnt;
        diamond = Mathf.Min(diamond, 9999999);
    }
    public void Minus_diamond(int _cnt)
    {
        diamond -= _cnt;
        diamond = Mathf.Max(diamond, 0);

    }

    //========================= Hint ���� =========================
    public void Add_item_hint(int _cnt)
    {
        gameSceneItem_Hint += _cnt;
    }
    public void Minus_item_hint(int _cnt)
    {
        gameSceneItem_Hint -= _cnt;
        gameSceneItem_Hint = Mathf.Max(gameSceneItem_Hint, 0);
    }
    //========================= Extract ħ������� =========================
    public void Add_item_extract(int _cnt)
    {
        gameSceneItem_Extract += _cnt;
    }
    public void Minus_item_extract(int _cnt)
    {
        gameSceneItem_Extract -= _cnt;
        gameSceneItem_Extract = Mathf.Max(gameSceneItem_Extract, 0);
    }

    //========================= Exchange ϴ�Ƶ��� =========================
    public void Add_item_exchange(int _cnt)
    {
        gameSceneItem_Exchange += _cnt;
    }
    public void Minus_item_exchange(int _cnt)
    {
        gameSceneItem_Exchange -= _cnt;
        gameSceneItem_Exchange = Mathf.Max(gameSceneItem_Exchange, 0);
    }

    //========================= Return ���ص��� =========================
    public void Add_item_return(int _cnt)
    {
        gameSceneItem_Return += _cnt;
    }
    public void Minus_item_return(int _cnt)
    {
        gameSceneItem_Return -= _cnt;
        gameSceneItem_Return = Mathf.Max(gameSceneItem_Return, 0);
    }
}
