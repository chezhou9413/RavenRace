using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.AVTelevision
{
    [StaticConstructorOnStartup]
    public static class AVTelevision_Bootloader
    {
        static AVTelevision_Bootloader()
        {
            InjectComps();
        }

        private static void InjectComps()
        {
            JoyKindDef tvKind = DefDatabase<JoyKindDef>.GetNamed("Television", false);
            if (tvKind == null) return;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                // 1.6 电视识别逻辑：只要娱乐类型是 Television 且是建筑
                if (def.building != null && def.building.joyKind == tvKind)
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    if (!def.HasComp(typeof(CompTV_AV)))
                    {
                        def.comps.Add(new CompProperties { compClass = typeof(CompTV_AV) });
                    }
                    // 确保建筑支持实时渲染以绘制 Gizmo
                    def.drawerType = DrawerType.MapMeshAndRealTime;
                }
            }
        }
    }
}