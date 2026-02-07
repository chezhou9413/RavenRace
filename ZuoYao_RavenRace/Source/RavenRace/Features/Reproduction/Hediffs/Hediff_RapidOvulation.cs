using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.Reproduction
{
    /// <summary>
    /// 强制排卵状态。
    /// 随着严重度增加，最终导致强制产下一枚未受精卵。
    /// </summary>
    public class HediffRapidOvulation : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            // 当严重度达到 1.0 时触发产卵
            if (this.Severity >= 1.0f)
            {
                DoBirth();
                pawn.health.RemoveHediff(this);
            }
        }

        private void DoBirth()
        {
            if (!pawn.Spawned) return;

            // 生成未受精卵
            Thing egg = ThingMaker.MakeThing(RavenDefOf.Raven_SpiritEgg_Unfertilized);
            GenSpawn.Spawn(egg, pawn.Position, pawn.Map);

            // 添加产后虚弱
            pawn.health.AddHediff(HediffDefOf.PostpartumExhaustion);

            Messages.Message("RavenRace_Msg_RapidOvulationBirth".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent);

            // 生成羊水污渍
            FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ThingDefOf.Filth_AmnioticFluid, 2);
        }
    }
}