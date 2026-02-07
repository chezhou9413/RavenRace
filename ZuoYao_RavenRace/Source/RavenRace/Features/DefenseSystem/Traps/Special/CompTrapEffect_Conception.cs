using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace
{
    public class CompTrapEffect_Conception : CompTrapEffect
    {
        public override void OnTriggered(Pawn triggerer)
        {
            if (triggerer == null || triggerer.Dead) return;

            // 添加过程 Hediff，把逻辑移交给 Hediff 处理
            Hediff_ConceptionProcess process = (Hediff_ConceptionProcess)HediffMaker.MakeHediff(DefenseDefOf.RavenHediff_ConceptionProcess, triggerer);
            process.Initialize(parent);
            triggerer.health.AddHediff(process);

            SoundDefOf.Designate_PlanAdd.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

            // 销毁陷阱
            parent.Destroy(DestroyMode.KillFinalize);
        }
    }
}