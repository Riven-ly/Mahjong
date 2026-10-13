using UnityEngine;

public sealed class PhaseTaskRuntime : MonoBehaviour
{
    static PhaseTaskRuntime instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null)
            return;

        GameObject runtime = new GameObject("Tx Phase Task Runtime");
        DontDestroyOnLoad(runtime);
        instance = runtime.AddComponent<PhaseTaskRuntime>();
        runtime.AddComponent<PhaseFlowCoordinator>();
    }

    void Update() => PhaseTaskService.Tick(Time.deltaTime);

    void OnApplicationQuit() => PhaseTaskService.Flush();

    void OnDestroy()
    {
        if (instance != this)
            return;

        PhaseTaskService.Flush();
        instance = null;
    }
}
