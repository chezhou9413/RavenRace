using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace RavenRace.Items.Comps
{
    // ==========================================
    // 1. CompProperties
    // ==========================================
    public class CompProperties_UseEffectRavenCoat : CompProperties_UseEffect
    {
        public CompProperties_UseEffectRavenCoat() => this.compClass = typeof(CompTargetEffect_RavenCoat);
    }

    // ==========================================
    // 2. CompUseEffect (空壳，用于兼容 XML 结构，如果不需要可以移除，但建议保留以防万一)
    // ==========================================
    // 注意：如果我们直接用 CompTargetEffect，其实不需要这个 UseEffect 类。
    // 但为了防止之前的引用报错，或者如果你在 XML 里用了这个类名，保留它作为一个空的 UseEffect。
    // 如果 XML 里写的是 <compClass>RavenRace.Items.Comps.CompTargetEffect_RavenCoat</compClass>，那这个类就不需要了。
    // 为了稳妥，我不定义这个类名以免冲突，直接用下面的 TargetEffect。

    // ==========================================
    // 3. CompTargetEffect (核心逻辑)
    // ==========================================
    public class CompTargetEffect_RavenCoat : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            // 解析目标：如果是人，取武器
            Thing weapon = target;
            if (target is Pawn p) weapon = p.equipment?.Primary;

            // 基础检查
            if (weapon == null || !weapon.def.IsMeleeWeapon)
            {
                Messages.Message("目标无效：必须是近战武器或持有近战武器的人。", user, MessageTypeDefOf.RejectInput);
                return;
            }

            // [核心修复] 尝试获取或添加组件
            var comp = weapon.TryGetComp<CompRavenInfusion>();

            // 兜底逻辑：如果真的没有组件，动态添加一个
            if (comp == null)
            {
                // [修复] 只有 ThingWithComps 才能加组件
                if (weapon is ThingWithComps twc)
                {
                    comp = new CompRavenInfusion();
                    comp.parent = twc;
                    // [修复] 访问 AllComps 需要是 ThingWithComps
                    twc.AllComps.Add(comp);
                    comp.Initialize(new CompProperties_RavenInfusion()); // 初始化一下 Props 比较好
                }
                else
                {
                    // 如果这把武器甚至不是 ThingWithComps (极少见，除非是原版极简物品)，那真的没法加
                    Messages.Message("错误：该武器不支持高级属性 (非复杂物品)", user, MessageTypeDefOf.RejectInput);
                    return;
                }
            }

            // 执行淬毒
            comp.AddCharges(20);

            // 特效与反馈
            FleckMaker.ThrowMetaIcon(weapon.PositionHeld, weapon.MapHeld, FleckDefOf.PsycastAreaEffect);
            SoundDefOf.MechSerumUsed.PlayOneShot(new TargetInfo(weapon.PositionHeld, weapon.MapHeld));

            Messages.Message("Raven_Message_WeaponCoated".Translate(parent.Label, weapon.LabelShort, 20), user, MessageTypeDefOf.PositiveEvent);
        }
    }
}