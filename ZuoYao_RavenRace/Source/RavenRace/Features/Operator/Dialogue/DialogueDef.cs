using System.Collections.Generic;
using Verse;

namespace RavenRace.Features.Operator.Dialogue
{
    // 对话定义，对应XML文件
    public class DialogueDef : Def
    {
        public List<DialogueCondition> triggerConditions;
        public int initialNodeID = 0;
        public List<DialogueNodeDef> nodes;
    }

    // 对话节点定义
    public class DialogueNodeDef
    {
        public int id;
        public string text;
        public string expressionKey;
        public string portraitPath; // 特殊CG路径
        public bool closeDialogue = false; // 是否直接结束对话
        public List<DialogueChoiceDef> choices;
    }

    // 对话选项定义
    public class DialogueChoiceDef
    {
        public string text;
        public int gotoNodeID = -1;
        public float favorabilityChange = 0f;
        public string tooltip;
        public bool closeDialogue = false;
    }

    // 对话触发条件基类
    public abstract class DialogueCondition
    {
        public abstract bool IsMet(WorldComponent_Fusang fusangComp, WorldComponent_OperatorManager opManager);
    }

    // 条件：第一次接触
    public class DialogueCondition_FirstContact : DialogueCondition
    {
        public override bool IsMet(WorldComponent_Fusang fusangComp, WorldComponent_OperatorManager opManager)
        {
            return !fusangComp.completedFirstContact;
        }
    }

    // 条件：好感度范围
    public class DialogueCondition_FavorabilityRange : DialogueCondition
    {
        public IntRange range;
        public override bool IsMet(WorldComponent_Fusang fusangComp, WorldComponent_OperatorManager opManager)
        {
            // 只有在完成初见后才触发
            if (!fusangComp.completedFirstContact) return false;

            // [修复] IntRange.Includes 方法不存在，使用标准比较
            return opManager.zuoYaoFavorability >= range.min && opManager.zuoYaoFavorability <= range.max;
        }
    }
}