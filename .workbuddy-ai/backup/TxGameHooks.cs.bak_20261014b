using UnityEngine;

public sealed class TxGameHooks : MonoBehaviour, IEventListener
{
    static TxGameHooks instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null)
            return;

        GameObject runtime = new GameObject("Tx Game Hooks");
        DontDestroyOnLoad(runtime);
        instance = runtime.AddComponent<TxGameHooks>();
    }

    void Start()
    {
        Register();
        PhaseTaskService.Initialize();
        NotifyGold();
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        Unregister();
        instance = null;
    }

    void Register()
    {
        EventManager manager = EventManager.Instance;
        if (manager == null)
            return;

        manager.RegisterListener(GameEvent.MahjongGameWon, this);
        manager.RegisterListener(GameEvent.MahjongGameLost, this);
        manager.RegisterListener(GameEvent.GetGold, this);
    }

    void Unregister()
    {
        EventManager manager = EventManager.Instance;
        if (manager == null)
            return;

        manager.UnregisterListener(GameEvent.MahjongGameWon, this);
        manager.UnregisterListener(GameEvent.MahjongGameLost, this);
        manager.UnregisterListener(GameEvent.GetGold, this);
    }

    public void OnEventTriggered(GameEvent eventType, object data = null)
    {
        switch (eventType)
        {
            case GameEvent.MahjongGameWon:
                OnGameWon();
                break;
            case GameEvent.MahjongGameLost:
                NotifyGold();
                break;
            case GameEvent.GetGold:
                NotifyGold();
                break;
        }
    }

    void OnGameWon()
    {
        int completedLevel = TxGameBridge.CurrentLevel;
        PhaseCompleteResult result = PhaseTaskService.NotifyLevelCompleted(completedLevel);
        if (PhaseFlowCoordinator.Instance != null)
            PhaseFlowCoordinator.Instance.StartCoroutine(PhaseFlowCoordinator.Instance.RunLevelFlow(result));
    }

    void NotifyGold()
    {
        PhaseTaskService.NotifyAmountChanged(TxGameBridge.GoldAmount);
    }
}
