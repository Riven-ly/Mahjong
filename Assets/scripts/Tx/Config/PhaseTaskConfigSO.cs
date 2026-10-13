using UnityEngine;

[CreateAssetMenu(menuName = "Game/Phase Task Config", fileName = "PhaseTaskConfig")]
public sealed class PhaseTaskConfigSO : ScriptableObject
{
    [SerializeField] int schemaVersion = 1;

    [SerializeField] PhaseDefinition[] phases;

    [SerializeField] LevelPhaseConfig levelPhase;

    [SerializeField] ActiveMinutePhaseConfig activeMinutePhase;

    [SerializeField] CoinPhaseConfig coinPhase;

    [SerializeField] PhaseRewardConfig phaseReward;

    [SerializeField] ActiveDayPhaseConfig activeDayPhase;

    [SerializeField] PlayerLevelPhaseConfig playerLevelPhase;

    public int SchemaVersion => schemaVersion;
    public PhaseDefinition[] Phases => phases;
    public LevelPhaseConfig LevelPhase => levelPhase;
    public ActiveMinutePhaseConfig ActiveMinutePhase => activeMinutePhase;
    public CoinPhaseConfig CoinPhase => coinPhase;
    public PhaseRewardConfig PhaseReward => phaseReward;
    public ActiveDayPhaseConfig ActiveDayPhase => activeDayPhase;
    public PlayerLevelPhaseConfig PlayerLevelPhase => playerLevelPhase;

    void OnValidate()
    {
        ValidateMinuteConfig();
        ValidatePlayerLevelConfig();
    }

    void ValidateMinuteConfig()
    {
        const int slotCount = 19;
        int[] targets = { 20, 21, 22, 23, 25, 27, 29, 32, 35, 38, 42, 46, 50, 55, 60, 65, 71, 77, 83 };
        if (activeMinutePhase == null)
        {
            Debug.LogError("101", this);
            return;
        }

        if (activeMinutePhase.SlotDurationSeconds != 60)
            Debug.LogError("101", this);
        if (activeMinutePhase.CarryEscapesToNextSlot)
            Debug.LogError("101", this);
        if (activeMinutePhase.Slots == null || activeMinutePhase.Slots.Length != slotCount)
        {
            Debug.LogError("101", this);
            return;
        }

        for (int index = 0; index < activeMinutePhase.Slots.Length; index++)
        {
            MinuteSlotDefinition slot = activeMinutePhase.Slots[index];
            if (slot == null
                || slot.SlotIndex != index + 1
                || slot.RequiredSuccessfulEscapes != targets[index])
            {
                Debug.LogError("101", this);
            }
        }
    }

    void ValidatePlayerLevelConfig()
    {
        if (playerLevelPhase == null)
        {
            Debug.LogError("101", this);
            return;
        }

        if (playerLevelPhase.TargetPlayerLevel != 30)
            Debug.LogError("101", this);

        PlayerLevelRequirement[] requirements = playerLevelPhase.LevelRequirements;
        if (requirements == null || requirements.Length != 30)
        {
            Debug.LogError("101", this);
            return;
        }

        for (int index = 0; index < requirements.Length; index++)
        {
            PlayerLevelRequirement requirement = requirements[index];
            if (requirement == null)
            {
                Debug.LogError("101", this);
                continue;
            }

            if (requirement.Level != index + 1)
                Debug.LogError("101", this);
            if (index < 29 && requirement.RequiredCompletedLevels <= 0)
                Debug.LogError("101", this);
            if (index == 29 && requirement.RequiredCompletedLevels != 0)
                Debug.LogError("101", this);
            if (requirement.QueueAhead < 0)
                Debug.LogError("101", this);
        }
    }
}
