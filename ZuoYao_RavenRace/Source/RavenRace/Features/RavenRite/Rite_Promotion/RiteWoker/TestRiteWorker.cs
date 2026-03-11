using RavenRace.Features.RavenRite.CustomRiteCore.Pojo;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps;
using RavenRace.Features.RavenRite.Rite_Promotion.RaveLordJob;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.RavenRite.CustomRiteCore.RiteWoker
{
    public class TestRiteWorker : RavenRiteWorker
    {
        public override void Execute(PromotionRitualSelection selection, Thing building)
        {
            RavenRiteLordFactory.StartRite(
     selection: selection,
     building: building,
     durationTicks: 2500,
     ritualLabel: "渡鸦突破仪式",
     onFinished: wasInterrupted =>
     {
         if (wasInterrupted) return;
         Pawn host = selection.GetFirst("host");
         if (host == null) return;
         var comp = host.TryGetComp<CompPurification>();
         if (comp == null) return;
         float successChance = RavenRaceMod.Settings.purificationSuccessChance;
         if (Rand.Chance(successChance))
         {
             comp.currentPurificationStage++;
             comp.TryAddGoldenCrowConcentration(0.01f, 1.0f);
             FleckMaker.ThrowExplosionCell(host.Position, host.Map, FleckDefOf.ExplosionFlash, Color.yellow);
             SoundDefOf.PsychicPulseGlobal.PlayOneShot(host);
             Find.LetterStack.ReceiveLetter(
                 "RavenRace_LetterLabel_PurificationSuccess".Translate(),
                 "经过艰苦的试炼，" + host.LabelShort + " 体内的金乌血脉打破了桎梏，成功迈入了阶段 "
                     + comp.currentPurificationStage + "！\n\n其浓度上限已解锁，并额外获得了 1% 的纯净浓度奖励。",
                 LetterDefOf.PositiveEvent,
                 host
             );
         }
         else
         {
             FleckMaker.ThrowSmoke(host.DrawPos, host.Map, 2f);
             Find.LetterStack.ReceiveLetter(
                 "RavenRace_LetterLabel_PurificationFailed".Translate(),
                 "仪式结束了。基座的能量耗尽，但 " + host.LabelShort + " 体内的血脉并未产生质变。除了虚脱，什么也没有发生。",
                 LetterDefOf.NegativeEvent,
                 host
             );
         }
     });
        }

        //演示禁用条件：地图上没有任何自由殖民者时禁用 Gizmo。
        public override string DisabledReason(Thing building)
        {
            if (building?.Map == null) return "建筑未在地图上";

            bool hasColonist = building.Map.mapPawns.FreeColonists
                .Any(p => !p.Dead && !p.Downed);

            return hasColonist ? null : "没有可参与仪式的殖民者";
        }
    }
}
