public static class PlayerLevelCalculator
{
    public static PlayerLevelCalculation Calculate(int totalCompletedLevels, PlayerLevelRequirement[] requirements)
    {
        int remaining = totalCompletedLevels < 0 ? 0 : totalCompletedLevels;
        if (!HasValidRequirements(requirements))
            return new PlayerLevelCalculation(1, 0, 0, false);

        for (int level = 1; level < 30; level++)
        {
            int required = requirements[level - 1].RequiredCompletedLevels;
            if (remaining < required)
                return new PlayerLevelCalculation(level, remaining, required, true);

            remaining -= required;
        }

        return new PlayerLevelCalculation(30, 0, 0, true);
    }

    public static int CalculateLevel(int totalCompletedLevels, PlayerLevelRequirement[] requirements) =>
        Calculate(totalCompletedLevels, requirements).Level;

    public static int GetCurrentCompletedLevels(int totalCompletedLevels, PlayerLevelRequirement[] requirements) =>
        Calculate(totalCompletedLevels, requirements).CurrentCompletedLevels;

    public static int GetRequiredCompletedLevels(int level, PlayerLevelRequirement[] requirements)
    {
        if (!HasValidRequirements(requirements) || level < 1 || level >= 30)
            return 0;
        return requirements[level - 1].RequiredCompletedLevels;
    }

    static bool HasValidRequirements(PlayerLevelRequirement[] requirements)
    {
        if (requirements == null || requirements.Length != 30)
            return false;

        for (int index = 0; index < 29; index++)
        {
            PlayerLevelRequirement requirement = requirements[index];
            if (requirement == null || requirement.Level != index + 1 || requirement.RequiredCompletedLevels <= 0)
                return false;
        }

        PlayerLevelRequirement maxLevel = requirements[29];
        return maxLevel != null && maxLevel.Level == 30 && maxLevel.RequiredCompletedLevels == 0;
    }
}
