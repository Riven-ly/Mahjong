using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

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
        //TX
        {"TxBtn_t1", "Only <color=#DE131A>{0}</color> left to <color=#4EA617>{1}</color> {2}!"},
        {"TxBtn_t2", "Only <color=#DE131A>{0}</color> levels left to {1}!"},
        {"TxBtn_t3", "Only <color=#DE131A>{0}</color> more days to{1}!"},
        {"TxBtn_t4", "The <color=#DE131A>{0}</color> task has been completed!"},
        {"Change", "Change"},
        {"ChooseAmount", " Choose Amount"},
        {"TxPanel_BoBao", "Congrats {0} {1} {2} successful!"},
        {"TxPanel_Account", "Account"},
        {"TxPanel_t1", "{0} at {1}"},
        {"TxPanel_t2", "Complete {0} more levels"},
        {"TxPanel_t3", "Sign‑in for {0} days"},
        {"TxPanel_t4", "Task completed"},
        {"TxPanel_cBtn", " {0} requirements not met."},
         

        {"TxElementTypeSelectPanel_title", " Choose Your {0} Method"}, 
        {"TxElementTypeSelectPanel_explain", "Please enter your account"},
        {"TxElementTypeSelectPanel_input1", "Please enter your {0} account"},
        {"TxElementTypeSelectPanel_input2", "Verify your {0} account"},
        {"TxElementTypeSelectPanel_Error", "Accounts are inconsistent!"},
        {"TxElementTypeSelectPanel_Error2", "Incorrect accounts input!"},
        {"CANCLE", "CANCLE"},
        {"SUBMIT", "SUBMIT"},
        //-------
        {"Special_Diamond__unit", "JA=="},//特殊钻石符号$
        {"CHT", "Y2FzaCBvdXQ="},//Cash out
        {"CH", "Q2FzaA=="},//Cash 
        {"WD", "V0lUSERSQVc="},
        {"wd", "d2l0aGRyYXc="},
        {"Wh", "V2l0aGRyYXdhbA=="},//Withdrawal 
        {"wh", "d2l0aGRyYXdhbA=="},
        {"pp", "cGF5cGFs"},//paypal
        {"Bl", "QmFsYW5jZQ=="},//Balance    
    };
}
