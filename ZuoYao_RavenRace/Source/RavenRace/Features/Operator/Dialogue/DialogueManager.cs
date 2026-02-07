using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.Operator.Dialogue
{
    [StaticConstructorOnStartup]
    public static class DialogueManager
    {
        private static List<DialogueDef> allDialogues;
        private static DialogueDef commonResponses;

        static DialogueManager()
        {
            allDialogues = DefDatabase<DialogueDef>.AllDefsListForReading;
            commonResponses = DefDatabase<DialogueDef>.GetNamedSilentFail("ZuoYao_CommonResponses");
        }

        public static DialogueDef GetCurrentDialogueDef()
        {
            var fusangComp = Find.World.GetComponent<WorldComponent_Fusang>();
            var opManager = Find.World.GetComponent<WorldComponent_OperatorManager>();
            if (fusangComp == null || opManager == null) return null;

            foreach (var def in allDialogues)
            {
                if (def.triggerConditions.NullOrEmpty() || def == commonResponses) continue; // 跳过通用回应
                if (def.triggerConditions.All(c => c.IsMet(fusangComp, opManager)))
                {
                    return def;
                }
            }

            return null; // 如果找不到，让调用者处理null，而不是返回默认值
        }

        // [重构] 随机节点选择逻辑移到外部调用处，这里只负责查找特定节点
        public static DialogueNodeDef GetNode(DialogueDef currentDef, int nodeID)
        {
            if (currentDef == null) return null;
            var node = currentDef.nodes.FirstOrDefault(n => n.id == nodeID);
            if (node != null) return node;

            if (commonResponses != null)
            {
                return commonResponses.nodes.FirstOrDefault(n => n.id == nodeID);
            }
            return null;
        }
    }
}