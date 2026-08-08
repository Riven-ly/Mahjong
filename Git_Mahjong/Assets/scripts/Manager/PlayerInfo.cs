using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家存档数据。
/// </summary>
[Serializable]
public class PlayerInfo
{
    public const int CurrencyUnitScale = 100;
    public float Gold
    {
        get => MathF.Round(gold / (float)CurrencyUnitScale, 2);
    }
    public float Diamond
    {
        get => MathF.Round(diamond / (float)CurrencyUnitScale, 2);
    }

    [SerializeField] private int gold = 0;
    private int diamond = 0;
    public int level = 1;
    public int goldLevel; // 金币等级
    public int goldExperience; // 当前金币等级经验，单位为0.1

    public int gameSceneItem_Hint = 50;
    public int gameSceneItem_Extract = 50;
    public int gameSceneItem_Exchange = 50;
    public int gameSceneItem_Return = 50;

    /// <summary>
    /// 增加金币等级经验并处理升级。经验单位为0.1。
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
    /// 获取升至指定金币等级所需经验。返回值单位为0.1。
    /// </summary>
    public int GetGoldLevelExperienceRequired(int targetLevel)
    {
        float exponent = 0.5f + 0.03f * Mathf.Max(targetLevel - 10, 0);
        return Mathf.FloorToInt(10f * Mathf.Pow(targetLevel, exponent)) * 10;
    }

    //========================= 金币 =========================
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

    //========================= 钻石 =========================
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

    //========================= Hint 道具 =========================
    public void Add_item_hint(int _cnt)
    {
        gameSceneItem_Hint += _cnt;
    }
    public void Minus_item_hint(int _cnt)
    {
        gameSceneItem_Hint -= _cnt;
        gameSceneItem_Hint = Mathf.Max(gameSceneItem_Hint, 0);
    }
    //========================= Extract 魔法棒道具 =========================
    public void Add_item_extract(int _cnt)
    {
        gameSceneItem_Extract += _cnt;
    }
    public void Minus_item_extract(int _cnt)
    {
        gameSceneItem_Extract -= _cnt;
        gameSceneItem_Extract = Mathf.Max(gameSceneItem_Extract, 0);
    }

    //========================= Exchange 洗牌道具 =========================
    public void Add_item_exchange(int _cnt)
    {
        gameSceneItem_Exchange += _cnt;
    }
    public void Minus_item_exchange(int _cnt)
    {
        gameSceneItem_Exchange -= _cnt;
        gameSceneItem_Exchange = Mathf.Max(gameSceneItem_Exchange, 0);
    }

    //========================= Return 撤回道具 =========================
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
