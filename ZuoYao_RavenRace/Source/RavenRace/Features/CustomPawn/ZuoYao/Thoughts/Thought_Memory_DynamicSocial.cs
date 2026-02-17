using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.CustomPawn.ZuoYao
{
    /// <summary>
    /// 动态社交记忆：强制锁定OpinionOffset，并从WorldComponent读取自定义标签。
    /// </summary>
    public class Thought_Memory_DynamicSocial : Thought_MemorySocial
    {
        // 使用我们自己的字段存储数值，防止被原版逻辑重置
        private float customOpinionOffset = -9999f;

        public void SetOpinion(float val)
        {
            this.customOpinionOffset = val;
            // 同步基类字段，确保某些未Patch的UI能读取到
            this.opinionOffset = val;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref customOpinionOffset, "customOpinionOffset", -9999f);
        }

        public override float OpinionOffset()
        {
            if (customOpinionOffset != -9999f) return customOpinionOffset;
            return base.OpinionOffset();
        }

        public override void Init()
        {
            base.Init();
            if (customOpinionOffset != -9999f)
            {
                this.opinionOffset = customOpinionOffset;
            }
        }

        public override bool TryMergeWithExistingMemory(out bool showBubble)
        {
            // 禁止合并，确保新施加的别天神效果覆盖旧的
            showBubble = true;
            return false;
        }

        public override string LabelCap
        {
            get
            {
                var comp = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
                if (comp != null)
                {
                    // 获取 "我看对方" 的称呼
                    string label = comp.GetCustomLabel(this.pawn, this.otherPawn);
                    if (!string.IsNullOrEmpty(label)) return label;
                }
                return base.LabelCap;
            }
        }
    }
}