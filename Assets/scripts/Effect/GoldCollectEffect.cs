using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoldCollectEffect : MonoBehaviour
{
    private const int MahjongMatchSortingOrder = 2450;

    public static GoldCollectEffect Instance;
    public GameObject glodPrefab;
    public GameObject diamondPrefab;
    public int num;

    private List<GoldFlyControl> golds = new List<GoldFlyControl>();
    private List<GoldFlyControl> diamonds = new List<GoldFlyControl>();
    private Canvas sceneEffectCanvas;
    private int defaultSortingOrder;
    private int activeMatchFlights;
    private Transform rewardFlightRoot;

    private void Awake()
    {
        Instance = this;
        sceneEffectCanvas = GetComponentInParent<Canvas>();
        rewardFlightRoot = transform;
        if (sceneEffectCanvas != null)
        {
            defaultSortingOrder = sceneEffectCanvas.sortingOrder;
            var rewardCanvas = new GameObject("RewardFlyLayer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster))
                .GetComponent<Canvas>();
            rewardCanvas.gameObject.layer = gameObject.layer;
            rewardCanvas.transform.SetParent(transform, false);
            rewardCanvas.overrideSorting = true;
            rewardCanvas.sortingLayerID = sceneEffectCanvas.sortingLayerID;
            rewardCanvas.sortingOrder = defaultSortingOrder;
            rewardFlightRoot = rewardCanvas.transform;
        }
    }

    private void OnEnable()
    {
        UpdateSceneEffectSorting();
    }

    private void OnDisable()
    {
        if (sceneEffectCanvas != null)
        {
            sceneEffectCanvas.sortingOrder = defaultSortingOrder;
        }
    }

    private void UpdateSceneEffectSorting()
    {
        if (sceneEffectCanvas != null)
        {
            sceneEffectCanvas.sortingOrder = isActiveAndEnabled && activeMatchFlights > 0
                ? MahjongMatchSortingOrder
                : defaultSortingOrder;
        }
    }

    private void FinishMatchFlight()
    {
        if (--activeMatchFlights == 0)
        {
            UpdateSceneEffectSorting();
        }
    }
    private void Start()
    {
        for (int i = 0; i < num * 3; i++)
        {
            GameObject go = Instantiate(glodPrefab, transform);
            go.transform.position = gameObject.transform.position;
            GoldFlyControl cc = go.GetComponent<GoldFlyControl>();
            if (cc != null)
            {
                cc.gameObject.SetActive(false);
                golds.Add(cc);
            }
        }

        for (int i = 0; i < num; i++)
        {
            GameObject go = Instantiate(diamondPrefab, transform);
            go.transform.position = gameObject.transform.position;
            GoldFlyControl cc = go.GetComponent<GoldFlyControl>();
            if (cc != null)
            {
                cc.gameObject.SetActive(false);
                diamonds.Add(cc);
            }
        }
    }

    public void StartEffect(ItemType itemType,Vector3 start, Vector3 target)
    {
        StartEffect(itemType, start, target, 10);
    }

    public void StartEffect(ItemType itemType, Vector3 start, Vector3 target, int maxCount, bool fromMahjongMatch = false)
    {
        List<GoldFlyControl> temList = golds;
        switch (itemType)
        {
            case ItemType.Gold:
                temList = golds;
                break;
            case ItemType.Diamond:
                temList = diamonds;
                break;

        }

        Transform flightRoot = fromMahjongMatch ? transform : rewardFlightRoot;
        System.Action onArrival = fromMahjongMatch ? FinishMatchFlight : null;
        int cntIndex = 0;
        for (int i = 0; i < temList.Count; i++)
        {
            if(!temList[i].gameObject.activeSelf)
            {
                GoldFlyControl flight = temList[i];
                if (flight.transform.parent != flightRoot)
                {
                    flight.transform.SetParent(flightRoot, false);
                }
                if (fromMahjongMatch && activeMatchFlights++ == 0)
                {
                    UpdateSceneEffectSorting();
                }
                flight.FlyGold(start, target, (cntIndex + 1) * 0.1f, onArrival);
                cntIndex++;
                if (cntIndex == maxCount)
                {
                    break;
                }
            }
        }

        int index = 0;
        foreach (var item in golds)
        {
            if (item.gameObject.activeSelf)
            {
                index++;
            }
        }
        foreach (var item in diamonds)
        {
            if (item.gameObject.activeSelf)
            {
                index++;
            }
        }

        float vS = index <= 10 ? 1f : 0.5f;
        foreach (var item in golds)
        {
            item.source.volume = vS;
        }
        foreach (var item in diamonds)
        {
            item.source.volume = vS;
        }
    }

}
