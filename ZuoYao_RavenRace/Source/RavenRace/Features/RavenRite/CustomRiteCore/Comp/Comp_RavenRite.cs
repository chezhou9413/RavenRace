using RavenRace.Features.RavenRite.CustomRiteCore.Pojo;
using RavenRace.Features.RavenRite.CustomRiteCore.RiteWoker;
using RavenRace.Features.RavenRite.CustomRiteCore.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RavenRace.Features.RavenRite.CustomRiteCore.Comp
{
    public class CompProperties_RavenRite : CompProperties
    {
        //仪式名称，显示在Gizmo标签和UI标题栏
        public string ritualLabel = "渡鸦仪式";
        //Gizmo悬停描述
        public string gizmoDesc = "";
        //冷却天数
        public float cooldownDays = 3f;
        //仪式效果执行类，必须继承 RavenRiteWorker。
        //XML中填写完整类型名
        public Type ritualWorkerClass;
        //角色配置列表，每个对应一个特殊角色槽
        public List<RitualRoleDefinition> roles = new List<RitualRoleDefinition>();
        //冷却ticks数
        public int CooldownTicks => Mathf.RoundToInt(cooldownDays * 60000f);

        public CompProperties_RavenRite()
        {
            compClass = typeof(Comp_RavenRite);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var e in base.ConfigErrors(parentDef)) yield return e;
            if (ritualWorkerClass != null && !typeof(RavenRiteWorker).IsAssignableFrom(ritualWorkerClass))
                yield return parentDef.defName + ": ritualWorkerClass 必须继承 RavenRiteWorker，当前类型: " + ritualWorkerClass.FullName;
            if (roles == null || roles.Count == 0)
                yield return parentDef.defName + ": RavenRite 组件未配置任何 roles，至少需要一个角色。";
            foreach (var role in roles ?? Enumerable.Empty<RitualRoleDefinition>())
            {
                if (role.RoleId.NullOrEmpty())
                    yield return parentDef.defName + ": 某个 role 缺少 roleId。";
            }
        }
    }
    public class Comp_RavenRite : ThingComp
    {
        //剩余冷却ticks，0 表示可用
        private int cooldownTicksRemaining;

        //Worker实例
        private RavenRiteWorker workerCached;

        //属性
        public CompProperties_RavenRite Props => (CompProperties_RavenRite)props;
        public bool OnCooldown => cooldownTicksRemaining > 0;
        public RavenRiteWorker Worker
        {
            get
            {
                if (workerCached == null && Props.ritualWorkerClass != null)
                    workerCached = (RavenRiteWorker)Activator.CreateInstance(Props.ritualWorkerClass);
                return workerCached;
            }
        }
        //生命周期
        public override void CompTick()
        {
            if (cooldownTicksRemaining > 0)
                cooldownTicksRemaining--;
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref cooldownTicksRemaining, "ravenRite_cooldownTicksRemaining", 0);
        }
        //Gizmo
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var cmd = new Command_Action
            {
                defaultLabel = Props.ritualLabel,
                defaultDesc = BuildGizmoDesc(),
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/Ritual", false)
                            ?? ContentFinder<Texture2D>.Get("UI/Commands/Attack", false)
                            ?? BaseContent.WhiteTex,
                action = OpenRitualUI
            };
            //冷却禁用
            if (OnCooldown)
            {
                cmd.Disable("冷却中：" + cooldownTicksRemaining.ToStringTicksToPeriod());
            }
            //Worker自定义禁用原因
            else if (Worker != null)
            {
                string workerReason = Worker.DisabledReason(parent);
                if (!workerReason.NullOrEmpty())
                    cmd.Disable(workerReason);
            }
            //冷却进度条覆盖
            if (OnCooldown && Props.CooldownTicks > 0)
            {
                cmd.shrinkable = false;
                float pct = 1f - (float)cooldownTicksRemaining / Props.CooldownTicks;
                //进度条
                cmd.groupKey = 9832741 + parent.thingIDNumber;
            }

            yield return cmd;

            // 开发模式：重置冷却
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "[Dev] 重置仪式冷却",
                    defaultDesc = "立即清除冷却计时器",
                    icon = ContentFinder<Texture2D>.Get("UI/Widgets/RotLeft", false) ?? BaseContent.WhiteTex,
                    action = () => cooldownTicksRemaining = 0
                };
            }
        }

        //UI触发

        private void OpenRitualUI()
        {
            var candidates = parent.Map?.mapPawns?.FreeColonists
                .Where(p => !p.Dead && !p.Downed && !p.IsQuestLodger())
                .ToList() ?? new List<Pawn>();
            var dialog = new Dialog_PromotionRitual(
                windowTitle: Props.ritualLabel,
                roleDefs: Props.roles,
                candidatePool: candidates,
                onConfirm: OnRitualConfirmed
            );
            Find.WindowStack.Add(dialog);
        }

        private void OnRitualConfirmed(PromotionRitualSelection selection)
        {
            //执行Worker效果
            Worker?.Execute(selection, parent);
            //启动冷却
            cooldownTicksRemaining = Props.CooldownTicks;
        }

        //检视面板

        public override string CompInspectStringExtra()
        {
            if (!OnCooldown) return null;
            return Props.ritualLabel + " 冷却：" + cooldownTicksRemaining.ToStringTicksToPeriod();
        }

        //辅助函数

        private string BuildGizmoDesc()
        {
            if (!Props.gizmoDesc.NullOrEmpty())
                return Props.gizmoDesc;

            if (!OnCooldown)
                return "举行" + Props.ritualLabel + "，指定仪式角色与参与者。";
            return "冷却中，还需 " + cooldownTicksRemaining.ToStringTicksToPeriod() + " 方可再次举行。";
        }
    }
}
