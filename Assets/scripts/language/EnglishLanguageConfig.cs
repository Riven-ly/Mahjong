using System.Collections;
using System.Collections.Generic;

public static class EnglishLanguageConfig
{
    public static Dictionary<string, string> currentTexts = new Dictionary<string, string>()
    {
        {"Loading", "Loading"},
        {"Level", "Level"},
        {"LEVEL", "LEVEL"},
        {"NoThanks", "No,Thanks"},
        {"CLAIMX10", "CLAIMX10"},
        {"CLAIMX2", "CLAIMX2"},
        {"CLAIM", "CLAIM"},
        {"Claim", "Claim"},
        {"ONLY", "ONLY"},
        {"Go", "Go"},
        {"CONTINUE", "CONTINUE"},
        {"QUIT", "QUIT"},
        {"BUY", "BUY"},
        {"FREE", "FREE"},
        {"OK", "OK"},
        {"RESET", "REPLAY"},
        {"PrivacyPolicy", "Privacy Policy"},
        {"TermsofService", "Terms of Service"},
        //网络
        {"RETRY", "RETRY"},
        {"NetworkStr", "Network connection lost. Please check your internet and try again."},       
        //Lobby
        {"HOME", "HOME"},
        {"PLAY", "PLAY"},
        {"DAILYCHALLENGE", "DAILY CHALLENGE"},
        //Task
        {"TaskDailyLogin", "Daily Login"},
        {"TaskCompleteLevel", "Complete {0} Levels"},
        {"TaskPlayAds", "Watch {0} Ads"},
        {"TaskReachLevel", "Reach Level {0}"},
        {"DailyTask", "Daily Task"},
        {"LevelTask", "Level Task"},
         //评分
        {"EvaluationGamePanel_title1", "Are you enjoying the game?"},
        {"EvaluationGamePanel_btn1", "Not Really"},
        {"EvaluationGamePanel_btn2", "Love it!"},
        {"EvaluationGamePanel_btn3", "LATER"},
        {"EvaluationGamePanel_btn4", "5 STARS"},
        {"EvaluationGamePanel_title2", "Your 5 stars are very important to us.please give us 5 stars if you like it."},
        //tipsPanel
        {"NoItemHintTips", "No movable cards available!"},
        {"InsufficientDiamond", "Insufficient diamond!"},
        {"AdsNotReady", "The video is not ready,please try again later."},
        {"CANCLE", "CANCLE"},
        {"SUBMIT", "SUBMIT"},
        {"Special_Diamond__unit", "JA=="},//特殊钻石符号$
        {"wd", "d2l0aGRyYXc="},//withdraw
        {"ch", "Q2FzaCBPdXQ="},//Cash Out
        //AddSceneItemPanel
        {"AddSceneItemDesc", "Watch an ad to get 3 tools. Tap a tool when you want to use it."},
        {"WatchAdX3", "Watch Ad X3"},
        //首关小关卡引导
        {"GuideMatchSymbols", "Match symbols to clear tile!"},
        {"GuideUncoverHidden", "Match to uncover hidden tile!"},
        {"GuideUnlockSide", "Match the side tiles to unlock other tiles!"},
    };
}
