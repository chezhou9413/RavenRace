using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace RavenRace.Features.UniqueWeapons.SpiritBeads
{
    public class CompProperties_AbilityGrandClimax : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityGrandClimax()
        {
            this.compClass = typeof(CompAbilityEffect_GrandClimax);
        }
    }

    public class CompAbilityEffect_GrandClimax : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster.Map;

            // 检查设置是否允许
            if (!RavenRaceMod.Settings.enableGrandClimax)
            {
                Messages.Message("RavenRace_Msg_GrandClimaxDisabled".Translate(), caster, MessageTypeDefOf.RejectInput, false);
                return;
            }

            //无论是否有敌人，只要发动了就先弹出灵珠并清除 Hediff
            CompSpiritBeads beads = caster.equipment?.Primary?.GetComp<CompSpiritBeads>();
            if (beads != null)
            {
                beads.SetInserted(caster, false); // 释放后弹出灵珠，内部已包含清除 Hediff 的逻辑
            }

            // 1. 模拟时停 (全图敌人强晕)
            List<Pawn> enemies = map.mapPawns.AllPawnsSpawned
                .Where(p => !p.Dead && !p.Downed && p.HostileTo(caster.Faction))
                .ToList();

            if (enemies.Count == 0)
            {
                Messages.Message("RavenRace_Msg_GrandClimaxUnleashed".Translate(caster.LabelShort), caster, MessageTypeDefOf.PositiveEvent);
                return;
            }

            Messages.Message("RavenRace_Msg_GrandClimaxUnleashed".Translate(caster.LabelShort), caster, MessageTypeDefOf.PositiveEvent);
        }
    }
}