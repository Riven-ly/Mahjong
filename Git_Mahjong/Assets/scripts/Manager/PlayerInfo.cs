using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerInfo
{
    public float gold = 0;
    public float diamond = 0;

    public int level = 1;

    public int gameSceneItem_Hint = 5;
    public int gameSceneItem_Extract = 5;
    public int gameSceneItem_Exchange = 5;
    public int gameSceneItem_Return = 5;

    //========================= 金币 =========================
    public void Add_gold(float _cnt)
    {
        gold += _cnt;
        gold = Mathf.Floor(gold * 100) / 100f;
    }
    public void Minus_gold(float _cnt)
    {
        gold -= _cnt;
        gold = Mathf.Floor(gold * 100) / 100f;
        gold = Mathf.Max(gold, 0);
    }

    //========================= 钻石 =========================
    public void Add_diamond(float _cnt)
    {
        diamond += _cnt;
        diamond = Mathf.Floor(diamond * 100) / 100f;
    }
    public void Minus_diamond(float _cnt)
    {
        diamond -= _cnt;
        diamond = Mathf.Floor(diamond * 100) / 100f;
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
