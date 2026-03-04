using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RavenRace.Features.CustomPawn.Binah
{
    /// <summary>
    /// 单次斩击特效的数据载体。
    /// 记录特效的位置、旋转角度，以及当前的淡入淡出进度。
    /// </summary>
    public class BinahSlashEffect
    {
        /// <summary>特效在世界空间中的绘制位置（含Y轴高度）</summary>
        public readonly Vector3 Position;

        /// <summary>特效的旋转角度（度数），绕Y轴旋转，0°=正东方向</summary>
        public readonly float Angle;

        /// <summary>特效已存在的实时时间（秒），用于计算淡入淡出透明度</summary>
        public float AgeSecs;

        public BinahSlashEffect(Vector3 position, float angle)
        {
            Position = position;
            Angle = angle;
            AgeSecs = 0f;
        }
    }

    /// <summary>
    /// Binah 近战斩击特效的地图级管理器。
    /// 作为 MapComponent 附加到地图上，负责：
    /// 1. 管理所有活跃斩击特效的生命周期（淡入、显示、淡出）
    /// 2. 在每个渲染帧（MapComponentUpdate）用 Graphics.DrawMesh 以精确角度绘制
    ///
    /// 使用 Graphics.DrawMesh 而非 Mote 系统的原因：
    /// Mote 的旋转走 Rot4（仅4方向）→ AngleFromRot → QuatFromRot 路径，
    /// exactRotation 字段只被 MoteThrown 的飞行逻辑消费，静态 Mote 完全忽略它。
    /// Graphics.DrawMesh 可以直接传入任意 Quaternion，实现360°自由旋转。
    /// </summary>
    public class BinahSlashEffectManager : MapComponent
    {
        // =====================================================================
        // 【可调参数区】—— 所有视觉效果的调节旋钮集中在此处，方便后续微调
        // =====================================================================

        /// <summary>
        /// 【贴图方向修正角】（度数）
        /// 补偿贴图本身朝向与"正东方向(右)"之间的偏差。
        ///   贴图朝左(西) → 180f
        ///   贴图朝右(东) → 0f
        ///   贴图朝上(北) → -90f（或 270f）
        ///   贴图朝下(南) → 90f
        /// 方向对了但镜像反了：在当前值基础上 ±180f。
        /// </summary>
        public const float TextureDirectionCorrection = 180f;

        /// <summary>
        /// 【特效生成位置偏移比例】
        /// 斩击特效出现在攻击者→目标连线上的位置：
        ///   0.0f = 攻击者自身位置
        ///   0.5f = 两者正中间
        ///   1.0f = 目标位置
        /// 推荐 0.5f ~ 0.8f，让斩击视觉上贴近命中点。
        /// </summary>
        public const float SlashPositionLerp = 0.65f;

        /// <summary>
        /// 【特效网格大小】（游戏格）
        /// 直接决定绘制网格的物理尺寸，1格=1个标准地砖大小。
        /// 调大更宏大，调小更精致。
        /// </summary>
        public const float SlashMeshSize = 2.5f;

        /// <summary>
        /// 【淡入时长】（秒）
        /// 特效从完全透明过渡到完全不透明所需的时间。
        /// </summary>
        public const float FadeInTime = 0.05f;

        /// <summary>
        /// 【完全显示时长】（秒）
        /// 特效保持完全不透明的持续时间。
        /// </summary>
        public const float SolidTime = 0.10f;

        /// <summary>
        /// 【淡出时长】（秒）
        /// 特效从完全不透明过渡到消失所需的时间。
        /// </summary>
        public const float FadeOutTime = 0.20f;

        /// <summary>
        /// 特效总生命周期（秒），由三段时间相加得出。
        /// </summary>
        private const float TotalLifespan = FadeInTime + SolidTime + FadeOutTime;

        /// <summary>
        /// 【特效颜色】RGBA，范围 0f~1f。
        /// Alpha 此处设为1f，实际透明度由淡入淡出逻辑动态控制。
        ///   金白色：R=1, G=0.95, B=0.75
        ///   纯白：  R=1, G=1,    B=1
        ///   更金：  降低 B 值（蓝色分量）
        /// </summary>
        private static readonly Color SlashColorBase = new Color(1f, 0.95f, 0.75f, 1f);

        // =====================================================================

        /// <summary>斩击特效使用的 shader，MoteGlow 支持透明底和发光效果</summary>
        private static readonly Shader SlashShader = ShaderDatabase.MoteGlow;

        /// <summary>斩击特效的贴图路径（不含扩展名），与 XML 中 iconPath/texPath 格式一致</summary>
        private const string SlashTexPath = "Races/Raven/Special/Binah/Abilities/SlashEffect";

        /// <summary>缓存的绘制 Material，懒加载，首次注册时初始化</summary>
        private Material slashMaterial;

        /// <summary>缓存的网格，与 SlashMeshSize 对应</summary>
        private Mesh slashMesh;

        /// <summary>当前活跃的所有斩击特效列表</summary>
        private readonly List<BinahSlashEffect> activeEffects = new List<BinahSlashEffect>();

        public BinahSlashEffectManager(Map map) : base(map) { }

        /// <summary>
        /// 注册一个新的斩击特效。由 Harmony Patch 在命中时调用。
        /// </summary>
        public void Register(BinahSlashEffect effect)
        {
            // 懒加载 Material 和 Mesh，确保在游戏主线程且资源已就绪时才初始化
            if (slashMaterial == null)
            {
                slashMaterial = MaterialPool.MatFrom(SlashTexPath, SlashShader);
            }
            if (slashMesh == null)
            {
                slashMesh = MeshPool.GridPlane(new Vector2(SlashMeshSize, SlashMeshSize));
            }
            activeEffects.Add(effect);
        }

        /// <summary>
        /// 每帧（渲染帧）调用，负责推进特效时间并绘制所有活跃特效。
        /// MapComponentUpdate 在渲染循环中被调用，适合 Graphics.DrawMesh。
        /// </summary>
        public override void MapComponentUpdate()
        {
            if (activeEffects.Count == 0) return;
            if (slashMaterial == null || slashMesh == null) return;

            float deltaTime = Time.deltaTime;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                BinahSlashEffect effect = activeEffects[i];

                // 推进特效年龄
                effect.AgeSecs += deltaTime;

                // 超出生命周期则移除
                if (effect.AgeSecs >= TotalLifespan)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                // 计算当前帧的透明度（淡入 → 完全显示 → 淡出）
                float alpha = CalculateAlpha(effect.AgeSecs);

                // 构造带透明度的颜色
                Color drawColor = new Color(
                    SlashColorBase.r,
                    SlashColorBase.g,
                    SlashColorBase.b,
                    alpha
                );

                // 将颜色写入 Material 的 _Color 属性
                // 注意：MaterialPool 缓存的 Material 是共享的，
                // 直接修改 color 会影响所有使用该 Material 的地方。
                // MoteGlow shader 支持逐顶点颜色，但 Graphics.DrawMesh 
                // 的简单重载不支持顶点色，因此使用 MaterialPropertyBlock 隔离。
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_Color", drawColor);

                // 构造旋转四元数：绕Y轴旋转，实现地图平面内的任意角度
                Quaternion rotation = Quaternion.Euler(0f, effect.Angle, 0f);

                // 直接绘制网格，绕开 Mote/Thing 的旋转限制
                Graphics.DrawMesh(
                    slashMesh,
                    effect.Position,
                    rotation,
                    slashMaterial,
                    0,              // layer（始终用0）
                    null,           // camera（null=所有相机）
                    0,              // submeshIndex
                    block           // MaterialPropertyBlock，隔离每个特效的颜色
                );
            }
        }

        /// <summary>
        /// 根据特效当前年龄计算透明度。
        /// 淡入段：线性从0到1；完全显示段：保持1；淡出段：线性从1到0。
        /// </summary>
        private static float CalculateAlpha(float ageSecs)
        {
            if (ageSecs <= FadeInTime)
            {
                // 淡入段
                return (FadeInTime > 0f) ? (ageSecs / FadeInTime) : 1f;
            }
            else if (ageSecs <= FadeInTime + SolidTime)
            {
                // 完全显示段
                return 1f;
            }
            else
            {
                // 淡出段
                float fadeOutProgress = ageSecs - FadeInTime - SolidTime;
                return (FadeOutTime > 0f) ? (1f - fadeOutProgress / FadeOutTime) : 0f;
            }
        }
    }
}