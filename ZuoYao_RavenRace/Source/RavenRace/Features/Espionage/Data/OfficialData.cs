using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.Espionage
{
    /// <summary>
    /// 代表目标派系权力结构中的一名官员。
    /// 存储必要信息，并负责头像的缓存与渲染管理。
    /// </summary>
    public class OfficialData : IExposable, ILoadReferenceable
    {
        public int uniqueID;

        // --- 基础信息 ---
        public string name;
        public NameTriple nameTriple;
        public Gender gender;
        public int age;
        public OfficialRank rank;

        // --- 实体引用 (可选) ---
        public Pawn pawnReference;
        public Faction factionRef;

        // --- 属性 ---
        public float loyalty = 100f;
        public float corruption = 0f;
        public float competence = 50f;
        public float relationToPlayer = 0f;

        // --- 状态 ---
        public bool isKnown = false;
        public bool isTurncoat = false;
        public bool isDead = false;

        // --- 虚拟形象数据 ---
        public PawnKindDef pawnKind;
        public BodyTypeDef bodyType;
        public HeadTypeDef headType;
        public HairDef hairDef;
        public Color hairColor;
        public Color skinColor;
        public PawnBio bio;
        public List<Trait> traits = new List<Trait>();
        public Dictionary<string, int> skillLevels = new Dictionary<string, int>();

        // --- 结构关系 ---
        public List<OfficialData> subordinates = new List<OfficialData>();

        // --- 运行时缓存 (不保存) ---
        private RenderTexture cachedPortrait;
        private Pawn cachedGhostPawn; // 仅用于渲染头像的临时 Pawn

        public OfficialData() { }
        public OfficialData(int id) { this.uniqueID = id; }

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_Values.Look(ref name, "name");
            Scribe_Deep.Look(ref nameTriple, "nameTriple");
            Scribe_Values.Look(ref gender, "gender");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref rank, "rank");
            Scribe_References.Look(ref pawnReference, "pawnReference");
            Scribe_References.Look(ref factionRef, "factionRef");
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
            Scribe_Values.Look(ref loyalty, "loyalty", 100f);
            Scribe_Values.Look(ref corruption, "corruption", 0f);
            Scribe_Values.Look(ref competence, "competence", 50f);
            Scribe_Values.Look(ref relationToPlayer, "relationToPlayer", 0f);
            Scribe_Values.Look(ref isKnown, "isKnown", false);
            Scribe_Values.Look(ref isTurncoat, "isTurncoat", false);
            Scribe_Values.Look(ref isDead, "isDead", false);
            Scribe_Defs.Look(ref bodyType, "bodyType");
            Scribe_Defs.Look(ref headType, "headType");
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Values.Look(ref hairColor, "hairColor");
            Scribe_Values.Look(ref skinColor, "skinColor");
            Scribe_Deep.Look(ref bio, "bio");
            Scribe_Collections.Look(ref traits, "traits", LookMode.Deep);
            Scribe_Collections.Look(ref skillLevels, "skillLevels", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref subordinates, "subordinates", LookMode.Deep);
        }

        public string GetUniqueLoadID() => "Raven_Official_" + uniqueID;

        public string Label
        {
            get
            {
                if (pawnReference != null) return pawnReference.LabelShort;
                if (nameTriple != null) return nameTriple.Nick ?? nameTriple.First;
                return name ?? "Unknown";
            }
        }

        /// <summary>
        /// 获取用于 UI 显示的头像。
        /// 确保每次渲染前标记 Pawn 为脏，强制系统刷新渲染缓存，防止头像错乱。
        /// </summary>
        public Texture GetPortrait()
        {
            // 1. 如果有真实实体，使用真实实体的头像
            if (pawnReference != null && !pawnReference.Destroyed)
            {
                // [重要] 标记为脏！确保获取的是该 Pawn 当前正确的渲染状态
                PortraitsCache.SetDirty(pawnReference);
                return PortraitsCache.Get(pawnReference, new Vector2(128f, 128f), Rot4.South);
            }

            // 2. 如果已有缓存的 RenderTexture，直接返回
            if (cachedPortrait != null) return cachedPortrait;

            // 3. 生成幽灵 Pawn (如果尚未生成)
            if (cachedGhostPawn == null)
            {
                // 使用工具类生成一个临时的、不加入世界的 Pawn，并应用保存的外观数据
                cachedGhostPawn = Utilities.OfficialPawnUtility.GenerateGhostPawn(this);
            }

            if (cachedGhostPawn != null)
            {
                // [重要] 标记幽灵 Pawn 为脏！
                // 这解决了快速切换派系时，如果 ID 复用或对象池复用导致的头像不更新问题
                PortraitsCache.SetDirty(cachedGhostPawn);

                // 获取并缓存渲染图
                cachedPortrait = PortraitsCache.Get(cachedGhostPawn, new Vector2(128f, 128f), Rot4.South);
            }

            return cachedPortrait ?? (Texture)BaseContent.BadTex;
        }

        /// <summary>
        /// 清理缓存。
        /// </summary>
        public void ClearCache()
        {
            cachedPortrait = null;
            if (cachedGhostPawn != null)
            {
                // 销毁前也标记一下脏，虽然可能多余，但保险起见
                PortraitsCache.SetDirty(cachedGhostPawn);
                cachedGhostPawn.Destroy();
                cachedGhostPawn = null;
            }
        }
    }
}