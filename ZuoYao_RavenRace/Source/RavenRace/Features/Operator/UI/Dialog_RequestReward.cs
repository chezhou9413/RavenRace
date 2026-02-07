using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RavenRace.Features.Operator.Rewards;

namespace RavenRace.Features.Operator.UI
{
    public class Dialog_RequestReward : Window
    {
        private List<RewardDef> availableRewards;
        public override Vector2 InitialSize => new Vector2(500, 400);

        public Dialog_RequestReward(List<RewardDef> rewards)
        {
            this.availableRewards = rewards;
            forcePause = true;
            doCloseButton = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("选择一份回礼");
            Text.Font = GameFont.Small;
            listing.GapLine();

            if (availableRewards.Count == 0)
            {
                listing.Label("当前没有可领取的回礼。");
            }
            else
            {
                foreach (var reward in availableRewards)
                {
                    if (listing.ButtonTextLabeled(reward.label, "领取"))
                    {
                        ClaimReward(reward);
                        Close();
                        return;
                    }
                    // [修复] 将颜色参数移到 GUI.color 中设置
                    GUI.color = Color.gray;
                    listing.Label(reward.description);
                    GUI.color = Color.white;
                    listing.Gap();
                }
            }

            listing.End();
        }

        private void ClaimReward(RewardDef rewardDef)
        {
            var manager = Find.World.GetComponent<WorldComponent_OperatorManager>();
            manager.unlockedRewardDefs.Remove(rewardDef.defName);
            manager.collectedUnderwearDefs.Add(rewardDef.rewardThing.defName);

            Thing rewardThing = ThingMaker.MakeThing(rewardDef.rewardThing);
            IntVec3 dropPos = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            DropPodUtility.DropThingsNear(dropPos, Find.CurrentMap, new List<Thing> { rewardThing }, 110, false, false, false);

            Find.LetterStack.ReceiveLetter("一份特殊的回礼", $"左爻送来了一份礼物：{rewardDef.label}。\n\n{rewardDef.description}", LetterDefOf.PositiveEvent, new LookTargets(rewardThing));
        }
    }
}