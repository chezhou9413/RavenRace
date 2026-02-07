using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Text;

namespace RavenRace
{
    public class CompProperties_RavenFeatherDrop : CompProperties
    {
        public CompProperties_RavenFeatherDrop()
        {
            this.compClass = typeof(CompRavenFeatherDrop);
        }
    }

    public class CompRavenFeatherDrop : ThingComp
    {
        // 冷却 Tick 记录 (保存的是 下一次可触发的绝对Tick)
        private int cooldownMood = -99999;
        private int cooldownDowned = -99999;
        private int cooldownBirth = -99999;

        // 使用 Settings 中的动态冷却
        private int CooldownTicks => (int)(RavenRaceMod.Settings.featherCooldownDays * 60000f);

        public Pawn Pawn => (Pawn)parent;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref cooldownMood, "cooldownMood", -99999);
            Scribe_Values.Look(ref cooldownDowned, "cooldownDowned", -99999);
            Scribe_Values.Look(ref cooldownBirth, "cooldownBirth", -99999);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!Pawn.Spawned || Pawn.Dead) return;
            // 只有冷却结束后才检查逻辑
            if (Find.TickManager.TicksGame < cooldownMood) return;
            if (Pawn.needs?.mood == null) return;

            float mood = Pawn.needs.mood.CurLevel;
            float threshold = RavenRaceMod.Settings.featherDropMoodThreshold;
            float chance = RavenRaceMod.Settings.featherDropChance;

            bool trigger = false;
            string reason = "";

            if (mood < threshold && Rand.Chance(chance))
            {
                trigger = true;
                reason = "HugeGrief";
            }
            else if (mood > (1f - threshold) && Rand.Chance(chance))
            {
                trigger = true;
                reason = "HugeJoy";
            }

            if (trigger)
            {
                DoDrop("Mood", reason, ref cooldownMood);
            }
        }

        public void Notify_MentalBreak()
        {
            if (Find.TickManager.TicksGame < cooldownMood) return;
            DoDrop("Mood", "MentalBreak", ref cooldownMood);
        }

        public void HandleDownedEvent()
        {
            if (Find.TickManager.TicksGame < cooldownDowned) return;
            float healthPct = Pawn.health.summaryHealth.SummaryHealthPercent;
            bool extremePain = Pawn.health.hediffSet.PainTotal > 0.8f;

            if (healthPct < 0.2f || extremePain)
            {
                DoDrop("Health", "NearDeath", ref cooldownDowned);
            }
        }

        public void Notify_Birth()
        {
            if (Find.TickManager.TicksGame < cooldownBirth) return;
            DoDrop("Birth", "NewLife", ref cooldownBirth);
        }

        private void DoDrop(string type, string reasonSuffix, ref int cooldownField)
        {
            if (!Pawn.Spawned) return;
            Thing feather = ThingMaker.MakeThing(ThingDef.Named("Raven_GoldenFeather"));
            feather.stackCount = 1;
            GenSpawn.Spawn(feather, Pawn.Position, Pawn.Map);
            FleckMaker.ThrowLightningGlow(Pawn.TrueCenter(), Pawn.Map, 1.5f);

            // 更新冷却时间
            cooldownField = Find.TickManager.TicksGame + CooldownTicks;

            string labelKey = $"RavenRace_LetterLabel_Feather_{type}";
            string textKey = $"RavenRace_LetterText_Feather_{reasonSuffix}";

            Find.LetterStack.ReceiveLetter(
                labelKey.Translate(),
                textKey.Translate(Pawn.LabelShort),
                LetterDefOf.NeutralEvent,
                new LookTargets(feather, Pawn)
            );
        }

        // [修复] 检查面板显示逻辑
        public override string CompInspectStringExtra()
        {
            // [修正] 完全移除 DevMode 判断，只看设置
            // 如果用户关闭了 "显示金羽冷却状态"，则返回 null，界面上不显示。
            if (!RavenRaceMod.Settings.showFeatherCooldown) return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("金羽冷却状态:");
            sb.AppendLine($"- 情绪: {GetCooldownStr(cooldownMood)}");
            sb.AppendLine($"- 濒死: {GetCooldownStr(cooldownDowned)}");
            sb.AppendLine($"- 新生: {GetCooldownStr(cooldownBirth)}");
            return sb.ToString().TrimEnd();
        }

        private string GetCooldownStr(int targetTick)
        {
            int remaining = targetTick - Find.TickManager.TicksGame;
            if (remaining <= 0) return "就绪"; // Ready
            return remaining.ToStringTicksToPeriod();
        }
    }
}