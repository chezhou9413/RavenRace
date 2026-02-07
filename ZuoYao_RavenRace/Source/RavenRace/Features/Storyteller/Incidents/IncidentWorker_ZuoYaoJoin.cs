using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace
{
    public class IncidentWorker_ZuoYaoJoin : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var colonists = map.mapPawns.FreeColonistsSpawned;
            if (colonists.Count == 0) return false;

            // 只有所有殖民者都倒下时才触发
            foreach (Pawn p in colonists)
            {
                if (!p.Downed) // 只要有一个没倒下就不触发
                {
                    return false;
                }
            }

            if (IsZuoYaoExisting()) return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            IntVec3 loc;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out loc))
            {
                return false;
            }

            // [修改] 使用 RavenDefOf
            PawnKindDef zuoYaoKind = RavenDefOf.Raven_PawnKind_ZuoYao;

            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: zuoYaoKind,
                faction: Faction.OfPlayer,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: true,
                fixedGender: Gender.Female,
                fixedBiologicalAge: 22f,
                forceAddFreeWarmLayerIfNeeded: true
            );

            Pawn zuoYao = PawnGenerator.GeneratePawn(request);
            if (zuoYao == null) return false;

            // ========================================================
            // 手动属性覆写
            // ========================================================

            zuoYao.Name = new NameTriple("爻", "左爻", "左");

            // [修改] 使用 RavenDefOf
            BackstoryDef childDef = RavenDefOf.Raven_Backstory_ZuoYao_Child;
            BackstoryDef adultDef = RavenDefOf.Raven_Backstory_ZuoYao_Adult;
            if (childDef != null) zuoYao.story.Childhood = childDef;
            if (adultDef != null) zuoYao.story.Adulthood = adultDef;

            zuoYao.story.traits.allTraits.Clear();

            // [修改] 使用 RavenDefOf
            TraitDef specialTrait = RavenDefOf.Raven_Trait_ZuoYao;
            if (specialTrait != null) zuoYao.story.traits.GainTrait(new Trait(specialTrait));

            TraitDef memoryTrait = TraitDefOf.GreatMemory;
            if (memoryTrait != null) zuoYao.story.traits.GainTrait(new Trait(memoryTrait));

            // 手动设置技能
            OverrideSkills(zuoYao);

            if (ModsConfig.IdeologyActive)
            {
                Ideo targetIdeo = Find.IdeoManager.IdeosListForReading.FirstOrDefault(i => i.culture.defName == "Raven_Culture") ?? Faction.OfPlayer.ideos.PrimaryIdeo;
                if (targetIdeo != null) zuoYao.ideo.SetIdeo(targetIdeo);
            }

            // [修改] 使用 RavenDefOf
            AbilityDef koto = RavenDefOf.Raven_Ability_Kotoamatsukami;
            if (zuoYao.abilities.GetAbility(koto) == null) zuoYao.abilities.GainAbility(koto);

            GenSpawn.Spawn(zuoYao, loc, map);

            this.def.letterText = "一名叫左爻的渡鸦族接线员加入了你的殖民地，既然她出现在这里说明她做了许多思想斗争并且下了很大的决心，她卸下了所有的虚伪和欺瞒，除了一颗真心和一片真情什么都没有带来，当心！她的性欲是普通渡鸦族女子的十倍！而且可以接受很多玩法！如果你不够变态请不要向她告白！";
            base.SendStandardLetter(parms, new LookTargets(zuoYao));

            return true;
        }

        private void OverrideSkills(Pawn p)
        {
            if (p.skills == null) return;

            foreach (var skill in p.skills.skills)
            {
                skill.Level = 0;
                skill.passion = Passion.None;
            }

            p.skills.GetSkill(SkillDefOf.Melee).Level = 6;
            p.skills.GetSkill(SkillDefOf.Shooting).Level = 8;
            p.skills.GetSkill(SkillDefOf.Cooking).Level = 10;
            p.skills.GetSkill(SkillDefOf.Crafting).Level = 10;
            p.skills.GetSkill(SkillDefOf.Social).Level = 15;
            p.skills.GetSkill(SkillDefOf.Intellectual).Level = 10;
        }

        private bool IsZuoYaoExisting()
        {
            foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_Alive)
            {
                // [修改] 使用 RavenDefOf
                if (p.def == RavenDefOf.Raven_Race)
                {
                    if (p.Name is NameTriple triple && triple.Last == "左" && triple.First == "爻")
                        return true;

                    // [修改] 使用 RavenDefOf
                    if (p.story?.traits?.HasTrait(RavenDefOf.Raven_Trait_ZuoYao) ?? false)
                        return true;
                }
            }
            return false;
        }
    }
}