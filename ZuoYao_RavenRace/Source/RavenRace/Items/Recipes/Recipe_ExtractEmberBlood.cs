using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace
{
    /// <summary>
    /// 提取余烬之血手术
    /// </summary>
    public class Recipe_ExtractEmberBlood : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (pawn.Dead) return;

            // 1. 生成余烬之血
            Thing blood = ThingMaker.MakeThing(ThingDef.Named("Raven_EmberBlood"));
            blood.stackCount = 1;
            GenPlace.TryPlaceThing(blood, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);

            // 2. 发送信件 (使用 Death 信件类型，会有红色提示)
            Find.LetterStack.ReceiveLetter(
                "RavenRace_LetterLabel_EmberExtraction".Translate(),
                "RavenRace_LetterText_EmberExtraction".Translate(billDoer.LabelShort, pawn.LabelShort),
                LetterDefOf.Death,
                new LookTargets(billDoer, blood)
            );

            // 3. 处死受害者
            // 使用 ExecutionCut (处决) 伤害类型，99999伤害确保必死
            DamageInfo dinfo = new DamageInfo(DamageDefOf.ExecutionCut, 99999f, 999f, -1f, billDoer, pawn.RaceProps.body.corePart);
            pawn.TakeDamage(dinfo);

            if (!pawn.Dead) pawn.Kill(dinfo);
        }
    }
}