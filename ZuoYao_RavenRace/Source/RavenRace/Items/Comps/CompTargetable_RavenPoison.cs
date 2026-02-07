using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Items.Comps
{
    public class CompProperties_TargetableRavenPoison : CompProperties_Targetable
    {
        public CompProperties_TargetableRavenPoison() => this.compClass = typeof(CompTargetable_RavenPoison);
    }

    public class CompTargetable_RavenPoison : CompTargetable
    {
        protected override bool PlayerChoosesTarget => true;

        protected override TargetingParameters GetTargetingParameters()
        {
            // 允许选择 Pawn (持有人) 或 Thing (地上的武器)
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = (TargetInfo t) =>
                {
                    // 1. 如果是地上的物品
                    if (t.Thing != null && t.Thing.def.IsMeleeWeapon) return true;

                    // 2. 如果是人，检查是否有近战武器
                    if (t.Thing is Pawn p && p.equipment?.Primary != null && p.equipment.Primary.def.IsMeleeWeapon) return true;

                    return false;
                }
            };
        }

        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }

        // 核心：把选中的 Pawn 转换为其武器
        public Thing GetWeaponFromTarget(Thing target)
        {
            if (target is Pawn p) return p.equipment?.Primary;
            return target;
        }
    }
}