using RimWorld;
using Verse;
using Verse.AI;

namespace RavenRace.Features.MiscSmallFeatures.AVTelevision
{
    /// <summary>
    /// 专属 AV 娱乐供给者：仅在电视 AV 开启时工作。
    /// </summary>
    public class JoyGiver_WatchAV : JoyGiver_WatchBuilding
    {
        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
        {
            if (!base.CanInteractWith(pawn, t, inBed)) return false;

            // 核心检查：只有 AV 开启时才允许此 Job
            var comp = t.TryGetComp<CompTV_AV>();
            return comp != null && comp.avModeActive;
        }

        public override float GetChance(Pawn pawn)
        {
            // 应用设置中的吸引力权重
            return base.GetChance(pawn) * RavenRaceMod.Settings.avJoyWeightMultiplier;
        }
    }
}