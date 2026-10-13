// Tx 模块本地存档键，避免与工程既有 PlayerPrefs 键冲突。
public static class TxSaveKeys
{
    // 关卡进度
    public const string Stage = "Tx.Stage";
    // 奖励中心金额
    public const string CoinAmount = "Tx.CoinAmount";
    // 普通领取累计次数
    public const string NormalClaimCount = "Tx.NormalClaimCount";
    // 账号
    public const string CandyAccount = "Tx.CandyAc";
    public const string CandyAccountName = "Tx.CandyAcName";
    public const string CandyAcType = "Tx.AcType";
    // 9位玩家编号
    public const string PlayerId = "Tx.PlayerId";
    // 首次引导是否已展示
    public const string GuideIntroShown = "Tx.Tutorial.IntroShown";
    public const string RewardIds = "Tx.CoinAmount.RewardIds";
    public const string TaskEventWatermark = "Tx.TaskEvent.Watermark";
}
