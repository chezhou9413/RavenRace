using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Operator.Gifting;

namespace RavenRace.Features.Operator.UI
{
    public class Dialog_GiftGiving : Window
    {
        private List<TransferableOneWay> transferables;
        private readonly TransferableOneWayWidget transferableWidget;
        // 核心修复：CS0169 警告。移除未使用的 scrollPosition 字段。
        // private Vector2 scrollPosition; 
        private const float BottomAreaHeight = 80f;
        private const float HeaderHeight = 38f;

        public override Vector2 InitialSize => new Vector2(650, 800);

        public Dialog_GiftGiving()
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;

            PrepareTransferables();
            transferableWidget = new TransferableOneWayWidget(transferables, "殖民地库存", null, "可用数量", true, IgnorePawnsInventoryMode.DontIgnore, true, null, 0f, false, null, true, false, false, false, false, false, false, false, false);
        }

        private void PrepareTransferables()
        {
            transferables = new List<TransferableOneWay>();
            var dummyTrader = new DummyTrader();
            var groupedThings = new Dictionary<string, List<Thing>>();

            foreach (Thing t in dummyTrader.ColonyThingsWillingToBuy(null))
            {
                string groupKey = GetThingGroupKey(t);
                if (!groupedThings.ContainsKey(groupKey))
                {
                    groupedThings[groupKey] = new List<Thing>();
                }
                groupedThings[groupKey].Add(t);
            }

            foreach (var group in groupedThings.Values)
            {
                var transferable = new TransferableOneWay();
                transferable.things.AddRange(group);
                transferables.Add(transferable);
            }
        }

        private string GetThingGroupKey(Thing t)
        {
            QualityCategory qc;
            t.TryGetQuality(out qc); // 使用 out 变量，避免再次调用
            return $"{t.def.defName}_{t.Stuff?.defName ?? "NoStuff"}_{qc}";
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect mainRect = inRect;
            mainRect.yMin += HeaderHeight;
            mainRect.height -= BottomAreaHeight;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, HeaderHeight), "赠送礼物给左爻");
            Text.Font = GameFont.Small;

            transferableWidget.OnGUI(mainRect, out _);

            Rect bottomRect = new Rect(inRect.x, mainRect.yMax, inRect.width, BottomAreaHeight);

            int estimatedFavor = CalculateEstimatedFavor();
            string favorStr = estimatedFavor.ToStringWithSign(); // 使用更标准的方法
            Color favorColor = estimatedFavor > 0 ? Color.green : (estimatedFavor < 0 ? Color.red : Color.gray);

            Text.Font = GameFont.Medium;
            Rect labelRect = new Rect(bottomRect.x + 10, bottomRect.y + 10, bottomRect.width - 200f, 30f);
            GUI.color = Color.white;
            Widgets.Label(labelRect, "预计好感度变化: ");

            Rect valRect = new Rect(labelRect.x + 160f, labelRect.y, 100f, 30f);
            GUI.color = favorColor;
            Widgets.Label(valRect, favorStr);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(bottomRect.xMax - 180f, bottomRect.y + 20f, 160f, 36f), "确认赠送"))
            {
                DoGift();
            }
        }

        private int CalculateEstimatedFavor()
        {
            int total = 0;
            var defaultReaction = DefDatabase<GiftReactionDef>.GetNamedSilentFail("Raven_GiftReaction_Default");
            float multiplier = defaultReaction?.favorChangePerValue ?? 0.05f;
            int maxCap = defaultReaction?.maxFavorChange ?? 10;

            foreach (var t in transferables)
            {
                if (t.CountToTransfer > 0)
                {
                    float value = t.AnyThing.MarketValue * t.CountToTransfer;
                    int change = (int)(value * multiplier);

                    if (multiplier > 0) change = Mathf.Min(change, maxCap);
                    else change = Mathf.Max(change, maxCap);

                    total += change;
                }
            }
            return total;
        }

        private void DoGift()
        {
            int totalFavorChange = 0;
            bool gifted = false;

            foreach (var transferable in transferables)
            {
                if (transferable.CountToTransfer > 0)
                {
                    GiftingManager.HandleGift(transferable.AnyThing, transferable.CountToTransfer, out int favorChange);
                    totalFavorChange += favorChange;

                    transferable.things[0].SplitOff(transferable.CountToTransfer).Destroy();
                    gifted = true;
                }
            }

            if (gifted)
            {
                WorldComponent_OperatorManager.PostGiftMessage += $"\n\n（好感度变化: {totalFavorChange.ToStringWithSign()}）";
            }

            Close();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (WorldComponent_OperatorManager.PostGiftMessage != null)
            {
                var radio = Find.CurrentMap?.listerBuildings.allBuildingsColonist.FirstOrDefault(b => b.def == FusangDefOf.Raven_FusangRadio);
                if (radio != null)
                {
                    Find.WindowStack.Add(new Dialog_FusangComm(radio));
                }
            }
        }

        private class DummyTrader : ITrader
        {
            public TraderKindDef TraderKind => DefDatabase<TraderKindDef>.GetNamed("Base_Player");
            public IEnumerable<Thing> Goods => Enumerable.Empty<Thing>();
            public int RandomPriceFactorSeed => 0;
            public string TraderName => "ZuoYao";
            public bool CanTradeNow => true;
            public float TradePriceImprovementOffsetForPlayer => 0f;
            public Faction Faction => Faction.OfPlayer;
            public TradeCurrency TradeCurrency => TradeCurrency.Silver;
            public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator) { }
            public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator) { }

            public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
            {
                foreach (Thing item in TradeUtility.AllLaunchableThingsForTrade(Find.CurrentMap, this))
                {
                    yield return item;
                }
                foreach (Pawn pawn in TradeUtility.AllSellableColonyPawns(Find.CurrentMap))
                {
                    yield return pawn;
                }
            }
        }
    }
}