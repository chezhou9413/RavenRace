using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.ZuoYao.UI
{
    public class Dialog_KotoamatsukamiCustomization : Window
    {
        private Pawn caster;
        private Pawn target;
        private string masterLabel;
        private string servantLabel;

        // 默认好感度值
        private float opinionServantToMaster = 100f;
        private float opinionMasterToServant = 50f;

        private CompAbilityEffect_Kotoamatsukami effectComp;

        public override Vector2 InitialSize => new Vector2(550f, 600f);

        public Dialog_KotoamatsukamiCustomization(Pawn caster, Pawn target, CompAbilityEffect_Kotoamatsukami comp)
        {
            this.caster = caster;
            this.target = target;
            this.effectComp = comp;
            this.closeOnClickedOutside = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            // 默认标签
            this.masterLabel = "Raven_Default_Master".Translate();
            this.servantLabel = "Raven_Default_Servant".Translate();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("RavenRace_Kotoamatsukami_Title".Translate());
            listing.Gap();

            Text.Font = GameFont.Small;
            listing.Label("RavenRace_Kotoamatsukami_Desc".Translate(caster.LabelShort, target.LabelShort));
            listing.GapLine();

            // --- 称呼设置 ---
            // 奴仆称呼主人
            listing.Label("RavenRace_Label_Master".Translate(target.LabelShort, caster.LabelShort));
            masterLabel = listing.TextEntry(masterLabel);
            listing.Gap();

            // 主人称呼奴仆
            listing.Label("RavenRace_Label_Servant".Translate(caster.LabelShort, target.LabelShort));
            servantLabel = listing.TextEntry(servantLabel);
            listing.GapLine();

            // --- 好感度设置 ---
            string opValStr1 = opinionServantToMaster.ToString("+0;-0");
            listing.Label("RavenRace_Label_Opinion_S2M".Translate(target.LabelShort, caster.LabelShort, opValStr1));
            opinionServantToMaster = listing.Slider(opinionServantToMaster, -100f, 100f);
            listing.Gap();

            string opValStr2 = opinionMasterToServant.ToString("+0;-0");
            listing.Label("RavenRace_Label_Opinion_M2S".Translate(caster.LabelShort, target.LabelShort, opValStr2));
            opinionMasterToServant = listing.Slider(opinionMasterToServant, -100f, 100f);
            listing.Gap(30f);

            // --- 确认按钮 ---
            if (Widgets.ButtonText(listing.GetRect(40f), "Confirm".Translate()))
            {
                Apply();
                Close();
            }
            listing.End();
        }

        private void Apply()
        {
            // 1. 保存配置到 WorldComponent
            var tracker = Find.World.GetComponent<WorldComponent_RavenRelationTracker>();
            if (tracker != null)
            {
                tracker.SetRelationData(caster, target, masterLabel, servantLabel, (int)opinionServantToMaster, (int)opinionMasterToServant);
            }

            // 2. 执行实际的能力效果
            effectComp.ApplyEffectFinal(caster, target);
        }
    }
}