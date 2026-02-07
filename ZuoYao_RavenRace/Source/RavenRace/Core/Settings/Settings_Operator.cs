using UnityEngine;
using Verse;

namespace RavenRace.Settings
{
    public static class Settings_Operator
    {
        public static void Draw(Listing_Standard listing)
        {
            // 移除了所有UI元素，只保留一个说明
            listing.Label("接线员 · 左爻 好感度与表情设置");
            listing.GapLine();

            GUI.color = Color.gray;
            listing.Label("好感度现在与扶桑组织的派系关系自动挂钩。\n\n你可以在通讯界面中，通过点击肖像旁的设置按钮来切换已解锁的表情套装。");
            GUI.color = Color.white;
        }
    }
}