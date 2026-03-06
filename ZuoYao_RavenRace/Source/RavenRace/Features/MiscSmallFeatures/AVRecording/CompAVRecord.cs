using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.AVRecording
{
    public class CompProperties_AVRecord : CompProperties
    {
        public CompProperties_AVRecord()
        {
            this.compClass = typeof(CompAVRecord);
        }
    }

    /// <summary>
    /// 自定义记录组件：存储主角名字，并利用随机生成池组合出极度色情露骨的标题和描述。
    /// 伪装成原版的艺术品，提升沉浸感。
    /// </summary>
    public class CompAVRecord : ThingComp
    {
        public string actorName;
        public string partnerName;
        public string customTitle;
        public string customDesc;

        /// <summary>
        /// 核心：初始化并生成极度露骨的文案
        /// </summary>
        public void InitializeRecord(Pawn actor, Pawn partner, bool isPremium)
        {
            this.actorName = actor != null ? actor.LabelShort : "未知实体";
            this.partnerName = partner != null ? partner.LabelShort : "神秘肉棒";

            // 1. 露骨词条前缀池 (参考日本AV高搜索关键词)
            string[] prefixes = new string[]
            {
                "【超绝顶】", "【强制受精】", "【密室监禁】", "【雌堕调教】",
                "【深层开发】", "【极乐潮吹】", "【无底肉壶】", "【痉挛高潮】",
                "【无码流出】", "【彻底沦陷】", "【极致榨汁】", "【肉体便器】"
            };

            // 2. 标题池
            string[] titles = new string[]
            {
                "毫无保留的肉体纠缠与中出",
                "沉沦于快感地狱的肉体",
                "彻底失去理智的疯狂内射",
                "淫靡粘稠的深夜性爱狂欢",
                "被性欲完全支配的绝顶记录",
                "沦为毫无尊严的排卵机器",
                "子宫被温暖浓液填满的实录",
                "在泣音与娇喘中迎来毁灭",
                "被粗暴贯穿至深处的调教",
                "理智溶解后的无尽求欢"
            };

            // 3. 描述动作池
            string[] descActions = new string[]
            {
                "被彻底剥夺了理智，宛如一台专为承受公狗侵犯而生的肉壶机器",
                "在极致的快感逼迫下放弃了所有抵抗，毫无廉耻地迎合着每一次深入",
                "被弄得泥泞不堪，连骨髓都要融化在无尽的阴道内射海啸中",
                "在连续不断的潮吹中翻着白眼，像发情的母畜一样哀求着『再给我更多精液』",
                "敏感的内壁被粗暴地反复刮擦，淫靡的爱液随着抽插飞溅得满屏幕都是"
            };

            // 4. 描述结尾池
            string[] descEndings = new string[]
            {
                "影片的最后，痉挛抽搐的肉体和失神的绝颜无一不在刺激着观看者的最深层兽欲。",
                "最终在极度高潮的余韵中双腿大张，任由浓浊的白浊从深处缓缓溢出。",
                "这份录像完美捕捉了理智溶解的那一瞬间，绝对能让任何买家瞬间下体充血胀痛。",
                "连绵不绝的肉体拍打声和甜腻嘶哑的绝顶娇喘，构成了这部完美的成人色情艺术品。"
            };

            // 组合标题
            string pfx = prefixes.RandomElement();
            string ttl = titles.RandomElement();

            // 如果是典藏版，加一个专属前缀
            string edition = isPremium ? "《影棚奢华精调版》" : "《暗网流出无码原片》";
            this.customTitle = $"{pfx} {ttl} - {this.actorName} 与 {this.partnerName}";

            // 组合描述
            string action = descActions.RandomElement();
            string ending = descEndings.RandomElement();

            this.customDesc = $"这是一部极其珍贵的{edition}成人影片。\n\n画面中，{this.actorName} 在 {this.partnerName} 狂暴且毫不留情的攻势下，{action}。\n\n{ending}";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref actorName, "actorName", "未知");
            Scribe_Values.Look(ref partnerName, "partnerName", "未知");
            Scribe_Values.Look(ref customTitle, "customTitle", "损坏的AV影像");
            Scribe_Values.Look(ref customDesc, "customDesc", "无法读取内容。");
        }

        /// <summary>
        /// 改变物品的显示名称（像艺术品一样展示标题）
        /// </summary>
        public override string TransformLabel(string label)
        {
            if (!string.IsNullOrEmpty(customTitle))
            {
                return $"{label} ({customTitle})";
            }
            return label;
        }

        /// <summary>
        /// 在左下角信息面板显示的额外信息
        /// </summary>
        public override string CompInspectStringExtra()
        {
            return $"主演: {actorName} & {partnerName}\n类别: 限制级成人影片";
        }

        /// <summary>
        /// 在物品详细信息(i图标)中显示的描述文本
        /// </summary>
        public override string GetDescriptionPart()
        {
            return $"{customTitle}\n\n{customDesc}\n\n导演: 扶桑全自动AV监视系统";
        }
    }
}