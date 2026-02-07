using RavenRace.Features.FusangOrganization.UI;
using RavenRace.Features.Operator;
using RavenRace.Features.Operator.Dialogue;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace
{
    [StaticConstructorOnStartup]
    public class Dialog_FusangComm : FusangWindowBase
    {
        // --- 字段 ---
        private readonly Thing radio;
        private readonly Faction fusangFaction;
        private readonly WorldComponent_OperatorManager operatorManager;
        private readonly WorldComponent_Fusang fusangWorldComp;

        // 当前对话状态
        private DialogueDef currentDialogueDef;
        private DialogueNodeDef currentNode;
        private Texture2D currentPortrait;

        // 打字机效果
        private string fullDialogueText;
        private string displayedText;
        private int typewriterCharIndex;
        private float typewriterCounter;
        private const float CharsPerSecond = 50f;

        // 静态资源
        private static Texture2D iconSupplies, iconMilitary, iconIntel, iconInfluence;

        // --- 窗口属性 ---
        protected override float Margin => 0f;
        public override Vector2 InitialSize => new Vector2(950f, 650f);

        // --- 构造函数 ---
        public Dialog_FusangComm(Thing radioSource) : base()
        {
            this.radio = radioSource;
            this.closeOnClickedOutside = true;

            this.fusangFaction = Find.FactionManager.FirstFactionOfDef(FusangDefOf.Fusang_Hidden);
            this.operatorManager = Find.World.GetComponent<WorldComponent_OperatorManager>();
            this.fusangWorldComp = Find.World.GetComponent<WorldComponent_Fusang>();

            InitializeDialogue();

            // 加载图标
            if (iconSupplies == null) iconSupplies = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Supplies") ?? BaseContent.BadTex;
            if (iconMilitary == null) iconMilitary = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Military") ?? BaseContent.BadTex;
            if (iconIntel == null) iconIntel = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Intel") ?? BaseContent.BadTex;
            if (iconInfluence == null) iconInfluence = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Influence") ?? BaseContent.BadTex;
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            UpdateTypewriter();
        }

        // --- 核心UI绘制 ---
        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 标题
            var titleRect = new Rect(20, 15, inRect.width - 40, 30);
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(titleRect, "RavenRace_FusangRadioTitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            GUI.color = FusangUIStyle.BorderColor;
            Widgets.DrawLineHorizontal(15, 50, inRect.width - 30);
            GUI.color = Color.white;

            // 左侧面板
            float leftWidth = 260f;
            Rect leftRect = new Rect(15, 60, leftWidth, inRect.height - 75);
            FusangComm_UIPanels.DrawLeftPanel(leftRect, radio, currentPortrait, operatorManager);

            // 右侧
            float rightX = leftRect.xMax + 15;
            float rightWidth = inRect.width - rightX - 15;

            // 右侧资源条
            Rect resourceRect = new Rect(rightX, 60, rightWidth, 50f);
            FusangComm_UIPanels.DrawResourceBar(resourceRect, iconSupplies, iconMilitary, iconIntel, iconInfluence);

            // 对话框
            float chatBoxHeight = 234f;
            Rect chatRect = new Rect(rightX, resourceRect.yMax + 10, rightWidth, chatBoxHeight);
            DrawChatBox(chatRect);

            // 关闭按钮
            Rect closeButtonRect = new Rect(inRect.width - 150 - 15, inRect.height - 35 - 15, 150, 35);
            if (FusangUIStyle.DrawButton(closeButtonRect, "关闭"))
            {
                Close();
            }

            // 底部区域
            float panelY = chatRect.yMax + 10;
            float panelHeight = closeButtonRect.y - panelY - 10;
            Rect bottomPanelRect = new Rect(rightX, panelY, rightWidth, panelHeight);

            if (currentNode?.choices.NullOrEmpty() ?? true)
            {
                // 如果没有选项（闲聊或对话结束），显示功能面板
                FusangComm_UIPanels.DrawFunctionPanel(bottomPanelRect, this);
            }
            else
            {
                // 如果有选项，显示选项
                DrawChoices(bottomPanelRect);
            }
        }

        // --- 对话逻辑 ---
        private void InitializeDialogue()
        {
            // 1. 优先处理赠礼后的特殊对话
            if (WorldComponent_OperatorManager.PostGiftMessage != null)
            {
                SetNode(new DialogueNodeDef { text = WorldComponent_OperatorManager.PostGiftMessage, expressionKey = "default" });
                WorldComponent_OperatorManager.PostGiftMessage = null;
                return;
            }

            // 2. 检查并修正状态：如果当前ID已经指向了一个终结节点（例如101,102），强制结束初见
            if (!fusangWorldComp.completedFirstContact && fusangWorldComp.firstContactDialogueStage > 100)
            {
                fusangWorldComp.completedFirstContact = true;
                fusangWorldComp.firstContactDialogueStage = 0;
            }

            // 3. 判断是否进入随机闲聊模式
            bool isRandomChat = fusangWorldComp.completedFirstContact;

            if (isRandomChat)
            {
                // 获取随机闲聊 Def
                currentDialogueDef = DefDatabase<DialogueDef>.GetNamedSilentFail("ZuoYao_RandomChat_Favor0");

                if (currentDialogueDef != null && !currentDialogueDef.nodes.NullOrEmpty())
                {
                    // [关键] 直接随机选取，不依赖ID
                    SetNode(currentDialogueDef.nodes.RandomElement());
                }
                else
                {
                    // 兜底
                    SetNode(new DialogueNodeDef { text = "......（信号似乎有些不稳定）", expressionKey = "default" });
                }
                return;
            }

            // 4. 初见流程 (依赖 ID 跳转)
            currentDialogueDef = DialogueManager.GetCurrentDialogueDef();
            if (currentDialogueDef != null)
            {
                int nodeID = fusangWorldComp.firstContactDialogueStage;
                var nextNode = DialogueManager.GetNode(currentDialogueDef, nodeID);

                if (nextNode != null)
                {
                    SetNode(nextNode);
                }
                else
                {
                    // 找不到节点，强制完成
                    Log.Warning("[RavenRace] Could not find next node for First Contact. Forcing completion.");
                    fusangWorldComp.completedFirstContact = true;
                    InitializeDialogue();
                }
            }
            else
            {
                // 无对话可用，强制完成
                fusangWorldComp.completedFirstContact = true;
                InitializeDialogue();
            }
        }

        private void SetNode(DialogueNodeDef node)
        {
            currentNode = node;
            if (node == null)
            {
                Close();
                return;
            }

            // [核心修复] 在显示节点时检查是否为终结节点
            // 如果节点标记为 closeDialogue=true，或者没有选项且我们还在初见阶段
            // 这意味着这是初见对话的最后一句话（例如ID 101, 102）
            if (!fusangWorldComp.completedFirstContact)
            {
                bool isTerminalNode = node.closeDialogue || (node.choices.NullOrEmpty() && node.id > 0);

                if (isTerminalNode)
                {
                    // 标记初见完成！下次打开就是随机对话了
                    fusangWorldComp.completedFirstContact = true;
                    fusangWorldComp.firstContactDialogueStage = 0;
                    Log.Message($"[RavenRace] First Contact completed at node {node.id}. Next interaction will be random.");
                }
            }

            fullDialogueText = node.text;
            typewriterCharIndex = 0;
            displayedText = "";
            typewriterCounter = 0f;

            // 更新立绘
            if (!node.portraitPath.NullOrEmpty())
            {
                currentPortrait = ContentFinder<Texture2D>.Get(node.portraitPath, true);
            }
            else
            {
                currentPortrait = operatorManager.GetOperatorPortrait();
            }
        }

        private void UpdateTypewriter()
        {
            if (string.Equals(displayedText, fullDialogueText)) return;

            typewriterCounter += Time.deltaTime;
            int targetIndex = (int)(typewriterCounter * CharsPerSecond);

            if (targetIndex > typewriterCharIndex)
            {
                typewriterCharIndex = Mathf.Min(targetIndex, fullDialogueText.Length);
                displayedText = fullDialogueText.Substring(0, typewriterCharIndex);
            }
        }

        private void SkipTypewriter()
        {
            if (typewriterCharIndex < fullDialogueText.Length)
            {
                typewriterCharIndex = fullDialogueText.Length;
                displayedText = fullDialogueText;
                SoundDefOf.DialogBoxAppear.PlayOneShotOnCamera(null);
            }
        }

        private void DrawChatBox(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.MainColor_Black);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            Rect textRect = rect.ContractedBy(15);
            GUI.color = FusangUIStyle.TextColor;
            Widgets.Label(textRect, displayedText);
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(rect)) SkipTypewriter();
        }

        private void DrawChoices(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(10f));
            foreach (var choice in currentNode.choices)
            {
                if (FusangUIStyle.DrawButton(listing.GetRect(32f), choice.text))
                {
                    HandleChoice(choice);
                }
                listing.Gap(8f);
            }
            listing.End();
        }

        private void HandleChoice(DialogueChoiceDef choice)
        {
            SoundDefOf.Click.PlayOneShotOnCamera();

            // 应用好感度
            if (choice.favorabilityChange != 0f)
            {
                WorldComponent_OperatorManager.ChangeFavorability((int)choice.favorabilityChange);
                string changeText = choice.favorabilityChange > 0 ? $"+{choice.favorabilityChange}" : choice.favorabilityChange.ToString();
                MoteMaker.ThrowText(radio.DrawPos, radio.Map, $"左爻好感度 {changeText}", Color.magenta, 2.5f);
            }

            // 结束对话
            if (choice.closeDialogue)
            {
                if (!fusangWorldComp.completedFirstContact)
                {
                    // 如果通过选项直接结束（例如"滚开"），也视为初见完成
                    fusangWorldComp.completedFirstContact = true;
                    fusangWorldComp.firstContactDialogueStage = 0;
                }
                Close();
                return;
            }

            // 跳转节点
            var nextNode = DialogueManager.GetNode(currentDialogueDef, choice.gotoNodeID);
            if (nextNode != null)
            {
                if (!fusangWorldComp.completedFirstContact)
                {
                    fusangWorldComp.firstContactDialogueStage = nextNode.id;
                }
                // 直接刷新当前界面
                SetNode(nextNode);
            }
            else
            {
                if (!fusangWorldComp.completedFirstContact) fusangWorldComp.completedFirstContact = true;
                Close();
            }
        }
    }
}