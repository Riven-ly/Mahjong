using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
    public const int CurrencyUnitScale = 1000;
    public float Gold
    {
        get => gold / (float)CurrencyUnitScale;
    }
    public float Diamond
    {
        get => diamond / (float)CurrencyUnitScale;
    }

    private int gold = 0;
    private int diamond = 0;
    public int level = 1;

    public int gameSceneItem_Hint = 50;
    public int gameSceneItem_Extract = 50;
    public int gameSceneItem_Exchange = 50;
    public int gameSceneItem_Return = 50;

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
