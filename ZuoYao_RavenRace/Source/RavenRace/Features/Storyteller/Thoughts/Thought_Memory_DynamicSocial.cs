using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace
{
    public class Thought_Memory_DynamicSocial : Thought_MemorySocial
    {
        // 使用我们自己的字段，绝对安全
        private float customOpinionOffset = -9999f;

        public void SetOpinion(float val)
        {
            this.customOpinionOffset = val;
            // [关键] 同步基类字段，防止UI直接读取 base.opinionOffset
            this.opinionOffset = val;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 独立保存
            Scribe_Values.Look(ref customOpinionOffset, "customOpinionOffset", -9999f);
        }

        // 强制返回我们的值
        public override float OpinionOffset()
        {
            if (customOpinionOffset != -9999f) return customOpinionOffset;
            return base.OpinionOffset();
        }

        public override void Init()
        {
            base.Init();
            // Init 会重置 opinionOffset，所以必须再次覆盖
            if (customOpinionOffset != -9999f)
            {
                this.opinionOffset = customOpinionOffset;
            }
        }

        public override bool TryMergeWithExistingMemory(out bool showBubble)
        {
            // 绝对禁止合并，确保新的覆盖旧的
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
                    string masterLabel = comp.GetMasterLabel(this.otherPawn, this.pawn);
                    if (!string.IsNullOrEmpty(masterLabel)) return masterLabel;

                    string servantLabel = comp.GetServantLabel(this.pawn, this.otherPawn);
                    if (!string.IsNullOrEmpty(servantLabel)) return servantLabel;
                }
                return base.LabelCap;
            }
        }
    }
}