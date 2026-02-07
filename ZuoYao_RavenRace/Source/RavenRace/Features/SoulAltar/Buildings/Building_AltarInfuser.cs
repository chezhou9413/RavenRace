using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace
{
    // 注灵器和注入仪共用此类
    public class Building_AltarInfuser : Building, IThingHolder
    {
        public ThingOwner innerContainer;

        // 玩家指定的目标物品 (null 表示未指定)
        public ThingDef targetDef;

        public Building_AltarInfuser()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Defs.Look(ref targetDef, "targetDef");
        }

        // 设置目标物品 (由 UI 调用)
        public void SetTarget(ThingDef def)
        {
            this.targetDef = def;
        }

        // [核心修复] 尝试接受物品
        public bool TryAcceptItem(Thing item)
        {
            if (innerContainer.Count > 0) return false;

            // 严格检查类型，防止放错
            if (targetDef != null && item.def != targetDef) return false;

            // 如果物品还在别的地方（比如小人手里），必须用 TryAddOrTransfer
            // 我们只需要 1 个
            int countToTake = 1;

            // TryAddOrTransfer 会自动处理 SplitOff 和持有者变更
            int transferred = innerContainer.TryAddOrTransfer(item, countToTake);

            return transferred > 0;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            // 弹出物品
            if (innerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "弹出物品",
                    defaultDesc = "取出当前放入的物品。",
                    icon = innerContainer[0].def.uiIcon,
                    action = () => innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near)
                };
            }

            // 取消指派
            if (targetDef != null && innerContainer.Count == 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "取消指派",
                    defaultDesc = $"取消等待搬运 {targetDef.LabelCap}。",
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                    action = () => targetDef = null
                };
            }
        }

        public SoulAltarUpgradeDef GetCurrentUpgrade()
        {
            if (innerContainer.Count == 0) return null;
            return DefDatabase<SoulAltarUpgradeDef>.AllDefsListForReading.Find(x => x.inputItem == innerContainer[0].def);
        }

        // 辅助方法：获取当前应显示的图标 (已放入的物品 > 指派的物品 > 默认)
        public Texture2D GetUIIcon()
        {
            if (innerContainer.Count > 0) return innerContainer[0].def.uiIcon;
            if (targetDef != null) return targetDef.uiIcon;
            return null; // 让 UI 绘制默认方块
        }
    }
}