using System.Collections.Generic;
using Verse;

namespace RavenRace.Features.Operator.Dialogue
{
    /// <summary>
    /// 对话系统的一个节点，包含左爻的台词和玩家的选项。
    /// </summary>
    public class DialogueNode
    {
        public int id; // 节点唯一ID
        public string text; // 左爻的台词
        public string expressionKey; // 表情Key，例如 "smile", "angry"
        public List<DialogueChoice> choices; // 玩家的选项列表
    }

    /// <summary>
    /// 玩家的一个对话选项。
    /// </summary>
    public class DialogueChoice
    {
        public string text; // 选项按钮上显示的文本
        public int gotoNodeId; // 点击后跳转到的下一个对话节点ID
        public float favorabilityChange = 0f; // 好感度变化值
        public string tooltip; // 鼠标悬停提示
    }
}