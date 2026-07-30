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
        {"CONTINUE", "CONTINUE"},
        {"QUIT", "QUIT"},
        {"BUY", "BUY"},
        {"FREE", "FREE"},
        {"OK", "OK"},
        {"RESET", "RESET"},
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
        {"TaskDailyLogin", "每日登录"},
        {"TaskCompleteLevel", "完成{0}次关卡"},
        {"TaskPlayAds", "观看{0}次广告"},
        {"TaskReachLevel", "达到第{0}关"},

        //tipsPanel
        {"NoItemHintTips", "No movable cards available!"},
        {"InsufficientDiamond", "Insufficient diamond!"},
        {"AdsNotReady", "The video is not ready,please try again later."},
    
        {"Special_Diamond__unit", "JA=="},//特殊钻石符号$
        {"CHT", "Y2FzaCBvdXQ="},//Cash out
        {"CH", "Q2FzaA=="},//Cash 
        {"WD", "V0lUSERSQVc="},//Withdraw?
        {"wd", "d2l0aGRyYXc="},
        {"Wh", "V2l0aGRyYXdhbA=="},//Withdrawal 
        {"wh", "d2l0aGRyYXdhbA=="},
        {"pp", "cGF5cGFs"},//paypal
        
    };
}
