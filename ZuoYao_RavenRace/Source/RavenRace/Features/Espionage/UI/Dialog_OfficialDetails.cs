using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.FusangOrganization.UI;

namespace RavenRace.Features.Espionage.UI
{
    public class Dialog_OfficialDetails : FusangWindowBase
    {
        private OfficialData official;
        public override Vector2 InitialSize => new Vector2(600f, 450f); // 稍微加宽

        public Dialog_OfficialDetails(OfficialData official) : base()
        {
            this.official = official;
            this.doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // [新增] 左侧绘制大头像 (仅当已知时)
            float portraitWidth = 150f;
            if (official.isKnown)
            {
                Rect portraitRect = new Rect(20, 50, portraitWidth, 200f);
                Widgets.DrawBoxSolid(portraitRect, new Color(0.1f, 0.1f, 0.1f));
                FusangUIStyle.DrawBorder(portraitRect, FusangUIStyle.BorderColor);

                Texture portrait = official.GetPortrait();
                GUI.DrawTexture(portraitRect.ContractedBy(4), portrait, ScaleMode.ScaleToFit);
            }

            // 右侧信息列表
            float listX = official.isKnown ? (20 + portraitWidth + 20) : 20;
            Rect listRect = new Rect(listX, 20, inRect.width - listX - 20, inRect.height - 80);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(listRect);

            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;

            string titleText;
            if (official.isKnown)
            {
                titleText = official.Label;
                if (official.isTurncoat)
                {
                    titleText += " " + "RavenRace_Espionage_TurncoatTag".Translate();
                    GUI.color = Color.green;
                }
            }
            else
            {
                titleText = "RavenRace_Espionage_UnknownTarget".Translate().ToString();
                GUI.color = Color.gray;
            }

            listing.Label(titleText);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.GapLine();

            if (official.isKnown)
            {
                string rankLabel = $"RavenRace_OfficialRank_{official.rank}".Translate().ToString();

                listing.Label("RavenRace_Official_Rank".Translate(rankLabel));
                listing.Label("RavenRace_Official_Age".Translate(official.age));
                // 修改后 (1.6 标准):
                string text = "Raven_Official_Rank".Translate(official.rank.ToString().Named("RANK"));

                if (official.bio != null && official.bio.adulthood != null)
                {
                    GUI.color = Color.gray;
                    listing.Label($"背景: {official.bio.adulthood.title.CapitalizeFirst()}");
                    GUI.color = Color.white;
                }

                listing.Gap();

                listing.Label("RavenRace_Official_Competence".Translate(official.competence.ToString("F0")));
                listing.Label("RavenRace_Official_Corruption".Translate(official.corruption.ToString("F0")));
                listing.Label("RavenRace_Official_Relation".Translate(official.relationToPlayer.ToString("F0")));
                listing.Label("RavenRace_Official_Loyalty".Translate(official.loyalty.ToString("F0")));
            }
            else
            {
                listing.Label("RavenRace_Official_UnknownDesc".Translate());
                listing.Gap();
                GUI.color = Color.gray;
                // 即使未知，因为他在图表上的位置，我们也能知道他的职级
                string rankLabel = $"RavenRace_OfficialRank_{official.rank}".Translate();
                listing.Label($"推测职级: {rankLabel}");
                GUI.color = Color.white;
            }

            listing.End();

            Rect btnRect = new Rect(inRect.width - 140, inRect.height - 50, 120, 40);
            if (FusangUIStyle.DrawButton(btnRect, "RavenRace_Official_ActionBtn".Translate(), official.factionRef != null))
            {
                Find.WindowStack.Add(new Dialog_MissionSelection(official.factionRef, official));
                Close();
            }
        }
    }
}