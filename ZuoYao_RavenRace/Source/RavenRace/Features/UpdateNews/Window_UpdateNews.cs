using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.UpdateNews
{
    /// <summary>
    /// 高级动态更新日志面板。
    /// 结合了扶桑UI风格，包含入场动画、余烬粒子、双向平滑渐变和终端打字机效果。
    /// </summary>
    public class Window_UpdateNews : Window
    {
        // ================= 数据与状态 =================
        private List<RavenUpdateNewsDef> allNews;       // 存储所有更新日志的列表
        private RavenUpdateNewsDef selectedNews;        // 当前选中的更新日志

        // 动画与打字机控制
        private float openTime;                         // 窗口打开的物理时间
        private const float AnimDuration = 0.5f;        // 窗口入场动画时长(秒)
        private float textRevealProgress = 0f;          // 当前文字显现的进度
        private const float TypewriterSpeed = 800f;     // 每秒显现的字符数(打字机速度)

        // 滚动状态
        private Vector2 listScrollPos = Vector2.zero;   // 左侧版本列表的滚动位置
        private Vector2 contentScrollPos = Vector2.zero;// 右侧正文内容的滚动位置

        // 资源缓存
        private Texture2D bannerTex;                    // 当前显示的顶部横幅插画

        // 余烬粒子系统定义
        private class Ember
        {
            public Vector2 pos;      // 粒子当前位置
            public float speedY;     // 垂直上升速度
            public float speedX;     // 水平漂移速度
            public float life;       // 当前剩余寿命
            public float maxLife;    // 最大寿命(用于计算渐变)
            public float size;       // 粒子尺寸
            public float maxAlpha;   // 最大透明度
        }
        private List<Ember> embers = new List<Ember>();

        // ================= 窗口基础设置 =================
        public override Vector2 InitialSize => new Vector2(1100f, 750f);
        protected override float Margin => 0f; // 移除原版边距，实现全屏贴边自定义布局

        public Window_UpdateNews()
        {
            this.doCloseButton = false;          // 禁用原版底部的关闭按钮
            this.doCloseX = false;               // 禁用原版右上角的X按钮
            this.forcePause = true;              // 打开时强制暂停游戏，聚焦注意力
            this.absorbInputAroundWindow = true; // 拦截窗口外部的鼠标点击，作为模态窗口
            this.doWindowBackground = false;     // 禁用原版的半透明黑底，使用我们自定义的绘制
            this.drawShadow = false;             // 禁用原版阴影

            // 加载所有通过 XML 定义的更新日志，并确保按版本号从新到旧排序
            allNews = DefDatabase<RavenUpdateNewsDef>.AllDefs.ToList();
            allNews.Sort();

            // 默认选中最新的一条更新日志
            if (allNews.Count > 0)
            {
                SelectNews(allNews[0]);
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            // 记录打开时间，用于计算矩阵缓动动画的进度
            openTime = Time.realtimeSinceStartup;
            InitEmbers();
        }

        /// <summary>
        /// 切换当前显示的更新版本，重置相关状态
        /// </summary>
        private void SelectNews(RavenUpdateNewsDef news)
        {
            if (selectedNews == news) return;
            selectedNews = news;

            // 重置滚动条和打字机进度
            contentScrollPos = Vector2.zero;
            textRevealProgress = 0f;

            // 【核心修改】：强制图片显示逻辑。
            // 如果 XML 中没有填图片路径，或者为空，强制使用默认的色图插画。
            // 这确保了无论是 1.6, 0.8 还是 0.7，UI 结构都保持绝对一致。
            string targetPath = !news.bannerPath.NullOrEmpty() ? news.bannerPath : "UI/UpdateNews/Banner_Default";

            // 安全加载横幅贴图
            bannerTex = ContentFinder<Texture2D>.Get(targetPath, false);
        }

        // ================= 核心绘制循环 =================
        public override void DoWindowContents(Rect inRect)
        {
            float timeSinceOpen = Time.realtimeSinceStartup - openTime;
            float animT = Mathf.Clamp01(timeSinceOpen / AnimDuration);

            // 1. 呼吸边框颜色：利用物理时间正弦波在面板暗灰与纯金之间游走
            float pulse = (Mathf.Sin(Time.realtimeSinceStartup * 3f) + 1f) * 0.5f;
            Color currentBorderColor = Color.Lerp(FusangUIStyle.BorderColor, FusangUIStyle.MainColor_Gold, pulse * 0.5f);

            // 2. GUI 矩阵动画：Cubic Out 缓出缩放
            // 算法：使窗口从 80% 大小快速弹至 100%，并伴随整体透明度渐变，赋予高级弹出感
            float easeOut = 1f - Mathf.Pow(1f - animT, 3f);
            float scale = Mathf.Lerp(0.8f, 1.0f, easeOut);

            GUI.color = new Color(1f, 1f, 1f, easeOut);

            // 保存旧的 GUI 矩阵，应用缩放矩阵进行物理形变
            Matrix4x4 oldMatrix = GUI.matrix;
            Vector2 center = new Vector2(UI.screenWidth / 2f, UI.screenHeight / 2f);
            GUI.matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(scale, scale, 1f)) *
                         Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one) *
                         oldMatrix;

            try
            {
                // ========== 绘制底层背景 ==========
                Widgets.DrawBoxSolid(inRect, FusangUIStyle.MainColor_Black);
                FusangUIStyle.DrawBorder(inRect, currentBorderColor, 2);

                if (selectedNews == null) return;

                // 划分 UI 区域
                Rect bottomRect = new Rect(inRect.x, inRect.yMax - 50f, inRect.width, 50f);
                Rect leftRect = new Rect(inRect.x, inRect.y, 260f, inRect.height - bottomRect.height);
                Rect rightRect = new Rect(leftRect.xMax, inRect.y, inRect.width - leftRect.width, inRect.height - bottomRect.height);

                // 绘制位于文字底层的动态余烬粒子
                UpdateAndDrawEmbers(rightRect);

                // ========== 1. 绘制左侧边栏 (版本列表) ==========
                Widgets.DrawBoxSolid(leftRect, FusangUIStyle.PanelColor);
                FusangUIStyle.DrawBorder(leftRect, FusangUIStyle.BorderColor, 1);

                Rect listOutRect = leftRect.ContractedBy(8f);
                Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, allNews.Count * 45f);

                Widgets.BeginScrollView(listOutRect, ref listScrollPos, listViewRect);
                float curY = 0f;
                foreach (var news in allNews)
                {
                    Rect rowRect = new Rect(0, curY, listViewRect.width, 40f);
                    bool isSelected = (news == selectedNews);

                    // 鼠标悬停或选中时的视觉反馈高亮
                    if (Mouse.IsOver(rowRect) || isSelected)
                    {
                        Widgets.DrawBoxSolid(rowRect, new Color(1f, 0.8f, 0.3f, isSelected ? 0.2f : 0.05f));
                    }

                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Small;
                    GUI.color = isSelected ? FusangUIStyle.MainColor_Gold : FusangUIStyle.TextColor;

                    // 绘制版本号
                    Rect textRect = rowRect;
                    textRect.xMin += 10f;
                    Widgets.Label(textRect, $"v{news.version}");

                    // 绘制发布日期
                    Text.Font = GameFont.Tiny;
                    GUI.color = FusangUIStyle.TerminalGray;
                    Text.Anchor = TextAnchor.MiddleRight;
                    Rect dateRect = rowRect;
                    dateRect.xMax -= 5f;
                    Widgets.Label(dateRect, news.publishDate);

                    // 点击行区域切换版本
                    if (Widgets.ButtonInvisible(rowRect))
                    {
                        SelectNews(news);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                    curY += 45f;
                }
                Widgets.EndScrollView();
                GUI.color = Color.white;

                // ========== 2. 绘制右侧主内容区 ==========
                float contentY = rightRect.y;
                float rightContentWidth = rightRect.width;

                // 绘制顶部横幅插画 (包含上下双向平滑渐变)
                if (bannerTex != null)
                {
                    float bannerHeight = 300f;
                    Rect bannerRect = new Rect(rightRect.x, contentY, rightContentWidth, bannerHeight);

                    // ScaleToFit 保证原图不被拉伸变形，完美居中显示全身
                    GUI.DrawTexture(bannerRect, bannerTex, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 0f);

                    // 顶部黑色平滑过渡层：消除插画直接撞到 UI 顶部的切割感
                    Rect topGradientRect = new Rect(bannerRect.x, bannerRect.y, bannerRect.width, 40f);
                    for (int i = 0; i < 20; i++)
                    {
                        float alpha = 1f - (i * 0.05f); // 从黑 (1.0) 渐变到透明 (0.0)
                        Widgets.DrawBoxSolid(new Rect(topGradientRect.x, topGradientRect.y + i * 2f, topGradientRect.width, 2f), new Color(0.05f, 0.05f, 0.05f, alpha));
                    }

                    // 底部黑色平滑过渡层：让插画完美融入下方的纯黑文字区
                    Rect botGradientRect = new Rect(bannerRect.x, bannerRect.yMax - 40f, bannerRect.width, 40f);
                    for (int i = 0; i < 20; i++)
                    {
                        float alpha = i * 0.05f; // 从透明 (0.0) 渐变到黑 (1.0)
                        Widgets.DrawBoxSolid(new Rect(botGradientRect.x, botGradientRect.y + i * 2f, botGradientRect.width, 2f), new Color(0.05f, 0.05f, 0.05f, alpha));
                    }

                    contentY += bannerHeight;
                }

                // 绘制大标题
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Medium;
                GUI.color = FusangUIStyle.MainColor_Gold;
                Rect titleRect = new Rect(rightRect.x + 20f, contentY + 10f, rightContentWidth - 40f, 40f);
                Widgets.Label(titleRect, selectedNews.title);
                contentY += 50f;

                // 绘制装饰性分隔线
                Widgets.DrawLineHorizontal(rightRect.x + 20f, contentY, rightContentWidth - 40f);
                contentY += 15f;

                // 终端打字机正文区
                Rect textOutRect = new Rect(rightRect.x + 20f, contentY, rightContentWidth - 40f, rightRect.yMax - contentY - 10f);

                // 推进打字机进度：仅当窗口动画完全展开后，才开始“打印”文字
                if (animT >= 1f)
                {
                    textRevealProgress += Time.deltaTime * TypewriterSpeed;
                }

                string fullText = selectedNews.content;
                int revealCount = Mathf.FloorToInt(textRevealProgress);
                bool typingFinished = revealCount >= fullText.Length;

                if (revealCount > fullText.Length) revealCount = fullText.Length;
                string visibleText = fullText.Substring(0, revealCount);

                // 模拟终端系统：在末尾追加闪烁的光标
                if (!typingFinished && Mathf.FloorToInt(Time.realtimeSinceStartup * 4f) % 2 == 0)
                {
                    visibleText += "<color=#ffcc00>_</color>";
                }

                // 严格限定文本为左上角对齐，修复文字排版混乱
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = FusangUIStyle.TextColor;

                // 计算完整文字所需高度，以防打字过程中滚动条剧烈抖动
                float textHeight = Text.CalcHeight(fullText, textOutRect.width - 16f) + 20f;
                Rect textViewRect = new Rect(0, 0, textOutRect.width - 16f, textHeight);

                Widgets.BeginScrollView(textOutRect, ref contentScrollPos, textViewRect);
                Widgets.Label(textViewRect, visibleText);
                Widgets.EndScrollView();

                // ========== 3. 绘制底部操作栏 ==========
                Widgets.DrawBoxSolid(bottomRect, FusangUIStyle.PanelColor);
                FusangUIStyle.DrawBorder(bottomRect, FusangUIStyle.BorderColor, 1);

                // 左下角复选框：控制下次进档是否继续弹出
                Rect checkRect = new Rect(bottomRect.x + 20f, bottomRect.y + 12f, 150f, 26f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = FusangUIStyle.TextColor;

                bool doNotShow = !RavenRaceMod.Settings.enableUpdateNews;
                Widgets.CheckboxLabeled(checkRect, "不再自动弹出", ref doNotShow);
                // 实时监听勾选状态的改变并立即保存到 Mod Settings
                if (doNotShow != !RavenRaceMod.Settings.enableUpdateNews)
                {
                    RavenRaceMod.Settings.enableUpdateNews = !doNotShow;
                    RavenRaceMod.Settings.Write();
                }

                // 在复选框右侧紧跟渡鸦交流群的提示信息
                Rect groupRect = new Rect(checkRect.xMax + 10f, bottomRect.y + 14f, 200f, 26f);
                Text.Font = GameFont.Tiny;
                GUI.color = FusangUIStyle.MainColor_Gold;
                Widgets.Label(groupRect, "渡鸦交流群: 518495086");

                // 右下角：关闭窗口的专属设计按钮
                Rect closeBtnRect = new Rect(bottomRect.xMax - 140f, bottomRect.y + 7f, 120f, 36f);
                if (FusangUIStyle.DrawButton(closeBtnRect, "阅毕闭入暗影"))
                {
                    Close();
                }

                // 底部正中间：表达对玩家的感谢语
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                GUI.color = FusangUIStyle.TerminalGray;
                Widgets.Label(bottomRect, "感谢您对渡鸦族：暗影中的余烬的支持！");
            }
            finally
            {
                // 【核心防护】确保在当前窗口渲染结束后，恢复原版的 GUI 矩阵、颜色和文本对齐。
                // 避免污染外部游戏画面或其他 UI 元素的渲染。
                GUI.matrix = oldMatrix;
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
        }

        // ================= 余烬粒子特效逻辑 =================

        /// <summary>
        /// 初始化环境中的余烬火星
        /// </summary>
        private void InitEmbers()
        {
            embers.Clear();
            for (int i = 0; i < 40; i++) // 维持适中的粒子数量，兼顾表现与性能
            {
                SpawnEmber(true);
            }
        }

        /// <summary>
        /// 生成单颗余烬粒子
        /// </summary>
        /// <param name="randomY">是否在全屏高度随机。若为 false，则统一从最底部生成</param>
        private void SpawnEmber(bool randomY = false)
        {
            Ember e = new Ember();
            // X轴限制在右侧阅读区随机生成
            e.pos.x = Rand.Range(260f, 1100f);
            e.pos.y = randomY ? Rand.Range(0f, 750f) : 770f;

            // 设定物理运动速度：向上漂移与横向抖动
            e.speedY = Rand.Range(-15f, -50f);
            e.speedX = Rand.Range(-15f, 15f);

            // 设定生命周期与显示属性
            e.maxLife = Rand.Range(3f, 8f);
            e.life = e.maxLife;
            e.size = Rand.Range(2f, 6f);
            e.maxAlpha = Rand.Range(0.2f, 0.6f);

            embers.Add(e);
        }

        /// <summary>
        /// 每帧更新并绘制所有余烬粒子
        /// </summary>
        private void UpdateAndDrawEmbers(Rect bounds)
        {
            float dt = Time.deltaTime;
            Color emberColor = new Color(1.0f, 0.4f, 0.1f); // 渡鸦风格的偏红色余烬

            for (int i = embers.Count - 1; i >= 0; i--)
            {
                Ember e = embers[i];
                e.life -= dt;

                // 应用物理位移：基础向上移动 + 正弦波控制的横向风力摇摆
                e.pos.y += e.speedY * dt;
                e.pos.x += Mathf.Sin(Time.realtimeSinceStartup * 2f + e.maxLife) * e.speedX * dt;

                // 如果粒子寿命耗尽，或飞出了窗口上边缘，则销毁并在底部重新生成
                if (e.life <= 0 || e.pos.y < bounds.y - 10f)
                {
                    embers.RemoveAt(i);
                    SpawnEmber(false);
                    continue;
                }

                // 根据生命周期计算抛物线透明度：生成和消失时暗淡，存活中期最亮
                float lifeRatio = e.life / e.maxLife;
                float currentAlpha = e.maxAlpha * (1f - Mathf.Pow(2f * lifeRatio - 1f, 2f));

                // 绘制像素块模拟粒子
                Rect emberRect = new Rect(bounds.x + e.pos.x - 260f, bounds.y + e.pos.y, e.size, e.size);

                // 为了性能优化，仅当粒子身处边界内时才进行渲染调用
                if (bounds.Contains(emberRect.position))
                {
                    GUI.color = new Color(emberColor.r, emberColor.g, emberColor.b, currentAlpha);
                    GUI.DrawTexture(emberRect, BaseContent.WhiteTex);
                }
            }
            GUI.color = Color.white; // 恢复全局颜色
        }
    }
}