using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace RavenRace.Features.UniqueWeapons.SpiritBeads
{
    public class JobDriver_InsertBeads : JobDriver
    {
        private const int InsertDuration = 120; // 2秒

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 准备动作
            yield return Toils_General.Wait(10);

            // 2. 塞入动作
            Toil insert = Toils_General.Wait(InsertDuration);
            insert.WithProgressBarToilDelay(TargetIndex.A);
            insert.tickAction = delegate
            {
                // 播放一点点娇喘或摩擦声? 
                if (pawn.IsHashIntervalTick(40))
                {
                    // 只是视觉抖动
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            };

            insert.AddFinishAction(delegate
            {
                // 完成
                Thing weapon = job.targetB.Thing;
                CompSpiritBeads comp = weapon?.TryGetComp<CompSpiritBeads>();
                if (comp != null)
                {
                    comp.SetInserted(pawn, true);

                    // 播放音效
                    SoundDef sound = SoundDefOf.Standard_Pickup; // 或者那种滑入的声音
                    sound.PlayOneShot(pawn);

                    // 文字提示
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "RavenRace_Text_BeadsInserted".Translate(), 2f);
                }
            });

            yield return insert;
        }
    }
}