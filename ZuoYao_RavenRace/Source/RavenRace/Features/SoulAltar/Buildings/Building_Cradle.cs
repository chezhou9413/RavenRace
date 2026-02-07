using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using RavenRace.Features.Reproduction; // [关键] 添加引用

namespace RavenRace
{
    public class Building_Cradle : Building, IThingHolder
    {
        // ... (保持类成员定义不变) ...
        protected ThingOwner innerContainer;
        public bool allowAutoLoad = true;
        private Graphic graphicFull;
        private bool attemptedLoadGraphic = false;

        public Building_Cradle()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override Graphic Graphic
        {
            get
            {
                if (innerContainer != null && innerContainer.Count > 0)
                {
                    if (!attemptedLoadGraphic && this.def.graphicData != null)
                    {
                        attemptedLoadGraphic = true;
                        string fullPath = this.def.graphicData.texPath + "_Full";
                        var baseData = this.def.graphicData;
                        try
                        {
                            graphicFull = GraphicDatabase.Get(baseData.graphicClass, fullPath, baseData.shaderType.Shader, baseData.drawSize, baseData.color, baseData.colorTwo, baseData, null);
                            if (graphicFull == null || graphicFull.MatSingle == BaseContent.BadMat) graphicFull = null;
                        }
                        catch { graphicFull = null; }
                    }
                    if (graphicFull != null) return graphicFull;
                }
                return base.Graphic;
            }
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
            Scribe_Values.Look(ref allowAutoLoad, "allowAutoLoad", true);
        }

        protected override void Tick()
        {
            base.Tick();
            innerContainer.DoTick();

            if (innerContainer.Count > 0 && this.GetComp<CompSoulAltar>() == null)
            {
                // [关键] 使用新类名 CompSpiritEgg
                CompSpiritEgg eggComp = innerContainer[0].TryGetComp<CompSpiritEgg>();
                if (eggComp != null)
                {
                    if (!eggComp.isIncubating)
                    {
                        eggComp.isIncubating = true;
                        eggComp.storedUpgradeDefNames.Clear();
                    }

                    float warmthBonus = eggComp.warmthProgress * 0.5f;
                    float multiplier = 1.0f + warmthBonus;

                    eggComp.TickIncubation(multiplier);
                }
            }
        }

        public bool TryAcceptEgg(Thing egg)
        {
            if (innerContainer.Count > 0) return false;

            if (innerContainer.TryAddOrTransfer(egg, true))
            {
                Thing heldEgg = innerContainer[0];
                CompSpiritEgg eggComp = heldEgg.TryGetComp<CompSpiritEgg>();

                if (eggComp != null)
                {
                    var altarComp = this.GetComp<CompSoulAltar>();
                    if (altarComp == null)
                    {
                        eggComp.isIncubating = true;
                        eggComp.storedUpgradeDefNames.Clear();
                    }
                    else
                    {
                        eggComp.isIncubating = false;
                    }
                }
                return true;
            }
            return false;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            this.Graphic.Draw(drawLoc, this.Rotation, this, 0f);
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.GetInspectString());

            if (innerContainer != null && innerContainer.Count > 0)
            {
                Thing egg = innerContainer[0];
                CompSpiritEgg comp = egg.TryGetComp<CompSpiritEgg>();
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("----------");
                sb.AppendLine(egg.LabelCap);
                if (comp != null) sb.Append(comp.CompInspectStringExtra());
            }
            else
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("RavenRace_CradleEmpty".Translate());
                if (!allowAutoLoad) sb.Append(" (禁止自动装填)");
            }
            return sb.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            yield return new Command_Toggle
            {
                defaultLabel = "自动装填",
                defaultDesc = "如果开启，殖民者会自动将灵卵搬运至此摇篮。",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Install", true),
                isActive = () => allowAutoLoad,
                toggleAction = () => allowAutoLoad = !allowAutoLoad
            };

            // 高级祭坛的蓝图规划功能
            if (this.def.defName == "Raven_SoulAltar_Core")
            {
                bool allInfusersPlaced = CheckAllPlaced(AltarGeometryUtility.InfuserOffsets, "Raven_Altar_Infuser");
                yield return new Command_Action
                {
                    defaultLabel = allInfusersPlaced ? "清除注灵器蓝图" : "规划注灵器",
                    defaultDesc = "点击规划缺失的注灵器蓝图。\n如果所有位置都已有蓝图或建筑，点击则清除蓝图。",
                    icon = ContentFinder<Texture2D>.Get("Buildings/SoulAltar/RavenInfuser", true),
                    action = () => ToggleBlueprints(AltarGeometryUtility.InfuserOffsets, "Raven_Altar_Infuser")
                };

                bool allPylonsPlaced = CheckAllPlaced(AltarGeometryUtility.PylonOffsets, "Raven_Altar_Pylon");
                yield return new Command_Action
                {
                    defaultLabel = allPylonsPlaced ? "清除共鸣桩蓝图" : "规划共鸣桩",
                    defaultDesc = "一键规划/取消共鸣桩蓝图。",
                    icon = ContentFinder<Texture2D>.Get("Buildings/SoulAltar/RavenPylon", true),
                    action = () => ToggleBlueprints(AltarGeometryUtility.PylonOffsets, "Raven_Altar_Pylon")
                };

                bool allInjectorsPlaced = CheckAllPlaced(AltarGeometryUtility.InjectorOffsets, "Raven_Altar_Injector");
                yield return new Command_Action
                {
                    defaultLabel = allInjectorsPlaced ? "清除注入仪蓝图" : "规划注入仪",
                    defaultDesc = "一键规划/取消注入仪蓝图。",
                    icon = ContentFinder<Texture2D>.Get("Buildings/SoulAltar/RavenInjector", true),
                    action = () => ToggleBlueprints(AltarGeometryUtility.InjectorOffsets, "Raven_Altar_Injector")
                };

                if (DebugSettings.godMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: 一键建设",
                        defaultDesc = "瞬间生成所有配套设施 (调试用)",
                        action = DevInstantBuildAll
                    };
                }
            }

            if (innerContainer != null && innerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "弹出灵卵",
                    defaultDesc = "将灵卵从摇篮中取出。",
                    icon = innerContainer[0].def.uiIcon,
                    action = () => innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near)
                };

                if (DebugSettings.godMode)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: 瞬间孵化",
                        defaultDesc = "立即孵化 (调试用)",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                        action = () =>
                        {
                            CompSpiritEgg comp = innerContainer[0].TryGetComp<CompSpiritEgg>();
                            comp?.Hatch();
                        }
                    };
                }
            }
        }

        private bool CheckAllPlaced(List<IntVec3> offsets, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return false;
            foreach (var offset in offsets)
            {
                IntVec3 pos = this.Position + AltarGeometryUtility.GetRotatedOffset(offset, this.Rotation);
                if (!pos.InBounds(Map)) continue;
                var things = pos.GetThingList(Map);
                bool hasIt = things.Any(t => t.def == def || (t.def.IsBlueprint && t.def.entityDefToBuild == def));
                if (!hasIt) return false;
            }
            return true;
        }

        private void ToggleBlueprints(List<IntVec3> offsets, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null) return;

            bool anyMissing = !CheckAllPlaced(offsets, defName);

            if (anyMissing)
            {
                foreach (var offset in offsets)
                {
                    IntVec3 pos = this.Position + AltarGeometryUtility.GetRotatedOffset(offset, this.Rotation);
                    if (!pos.InBounds(Map)) continue;

                    if (GenConstruct.CanPlaceBlueprintAt(def, pos, Rot4.North, Map, false, null, null, null).Accepted)
                    {
                        GenConstruct.PlaceBlueprintForBuild(def, pos, Map, Rot4.North, Faction.OfPlayer, null);
                    }
                }
                Messages.Message("已规划蓝图。", MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                foreach (var offset in offsets)
                {
                    IntVec3 pos = this.Position + AltarGeometryUtility.GetRotatedOffset(offset, this.Rotation);
                    if (!pos.InBounds(Map)) continue;
                    var things = pos.GetThingList(Map).ToList();
                    foreach (var t in things)
                    {
                        if (t.def.IsBlueprint && t.def.entityDefToBuild == def)
                            t.Destroy(DestroyMode.Cancel);
                    }
                }
                Messages.Message("已清除蓝图。", MessageTypeDefOf.TaskCompletion);
            }
        }

        private void DevInstantBuildAll()
        {
            void Build(List<IntVec3> offsets, string defName)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null) return;
                foreach (var offset in offsets)
                {
                    IntVec3 pos = this.Position + AltarGeometryUtility.GetRotatedOffset(offset, this.Rotation);
                    if (pos.InBounds(Map))
                    {
                        var things = pos.GetThingList(Map).ToList();
                        foreach (var existingThing in things)
                        {
                            if (existingThing.def.IsBuildingArtificial || existingThing.def.IsBlueprint || existingThing.def.IsFrame)
                                existingThing.Destroy();
                        }
                        Thing newThing = GenSpawn.Spawn(def, pos, Map);
                        newThing.SetFaction(Faction.OfPlayer);
                        var power = newThing.TryGetComp<CompPowerTrader>();
                        if (power != null) power.PowerOn = true;
                    }
                }
            }
            Build(AltarGeometryUtility.InfuserOffsets, "Raven_Altar_Infuser");
            Build(AltarGeometryUtility.PylonOffsets, "Raven_Altar_Pylon");
            Build(AltarGeometryUtility.InjectorOffsets, "Raven_Altar_Injector");
            Messages.Message("DEV: 祭坛组件已生成。", MessageTypeDefOf.TaskCompletion);
        }
    }
}