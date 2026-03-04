using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.ConfessionBooth
{
    public class CompProperties_AssignableToNun : CompProperties_AssignableToPawn
    {
        public CompProperties_AssignableToNun()
        {
            this.compClass = typeof(CompAssignableToPawn_Nun);
        }
    }

    public class CompAssignableToPawn_Nun : CompAssignableToPawn
    {
        /// <summary>
        /// 限制分配界面的候选人：必须是自由殖民者且为女性
        /// 取消了对小孩(Child)的限制，但保留了婴儿(Baby)限制，因为婴儿无法执行前往建筑的 Job
        /// </summary>
        public override IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                if (!parent.Spawned) return Enumerable.Empty<Pawn>();
                return parent.Map.mapPawns.FreeColonists.Where(p =>
                    p.gender == Gender.Female && !p.DevelopmentalStage.Baby());
            }
        }

        public override AcceptanceReport CanAssignTo(Pawn pawn)
        {
            if (pawn.gender != Gender.Female)
            {
                return "必须是女性才能担任修女。";
            }
            if (pawn.DevelopmentalStage.Baby())
            {
                return "婴儿无法担任修女（无法行动）。";
            }
            return AcceptanceReport.WasAccepted;
        }

        protected override string GetAssignmentGizmoLabel() => "指定修女";

        protected override string GetAssignmentGizmoDesc() =>
            "指定一名女性（包括女孩）作为这座忏悔室的修女。她将负责在隔板后接收信徒们的“忏悔”。任何种族的女性皆可胜任此职。";

        // 允许一名修女兼职多个忏悔室
        public override bool AssignedAnything(Pawn pawn) => false;
    }
}