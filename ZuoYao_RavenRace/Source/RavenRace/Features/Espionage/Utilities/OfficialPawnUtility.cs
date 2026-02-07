using System.Collections.Generic;
using System.Linq;
using AlienRace;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.Utilities
{
    /// <summary>
    /// 负责 OfficialData 与 Pawn 实体之间的转换。
    /// </summary>
    public static class OfficialPawnUtility
    {
        /// <summary>
        /// 从一个实体 Pawn 提取所有数据到 OfficialData。
        /// </summary>
        public static void SnapshotPawnData(OfficialData data, Pawn pawn)
        {
            if (pawn == null || data == null) return;

            data.name = pawn.Name.ToStringFull;
            data.nameTriple = pawn.Name as NameTriple;
            data.gender = pawn.gender;
            data.age = pawn.ageTracker.AgeBiologicalYears;
            data.pawnKind = pawn.kindDef;
            data.factionRef = pawn.Faction;

            // 外貌
            data.bodyType = pawn.story?.bodyType;
            data.headType = pawn.story?.headType;
            data.hairDef = pawn.story?.hairDef;
            data.hairColor = pawn.story?.HairColor ?? Color.white;
            data.skinColor = pawn.story?.SkinColor ?? Color.white;

            // HAR 颜色兼容 (如果存在)
            var alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp != null)
            {
                data.skinColor = alienComp.GetChannel("skin").first;
            }

            // 背景
            if (pawn.story != null)
            {
                data.bio = new PawnBio
                {
                    name = data.nameTriple,
                    gender = (data.gender == Gender.Male) ? GenderPossibility.Male : GenderPossibility.Female,
                    childhood = pawn.story.Childhood,
                    adulthood = pawn.story.Adulthood
                };
            }

            // 特性 (深拷贝)
            data.traits.Clear();
            if (pawn.story?.traits != null)
            {
                foreach (var t in pawn.story.traits.allTraits)
                {
                    data.traits.Add(new Trait(t.def, t.Degree));
                }
            }

            // 技能
            data.skillLevels.Clear();
            if (pawn.skills != null)
            {
                foreach (var s in pawn.skills.skills)
                {
                    data.skillLevels[s.def.defName] = s.Level;
                }
            }
        }

        /// <summary>
        /// 根据 OfficialData 生成一个“幽灵” Pawn。
        /// 仅用于 UI 渲染，绝不应该 Spawn 到地图上。
        /// </summary>
        public static Pawn GenerateGhostPawn(OfficialData data)
        {
            if (data == null || data.pawnKind == null) return null;

            // 1. 创建请求
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: data.pawnKind,
                faction: data.factionRef,
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: data.age,
                fixedChronologicalAge: data.age,
                fixedGender: data.gender,
                fixedBirthName: data.nameTriple?.First, // 尝试锁定名字
                fixedLastName: data.nameTriple?.Last,
                allowDead: false,
                allowDowned: false,
                forceNoGear: false // UI显示需要衣服
            );

            // 2. 生成基础 Pawn
            Pawn ghost = PawnGenerator.GeneratePawn(request);

            // 3. 强制覆盖数据以匹配 OfficialData
            ApplyDataToPawn(ghost, data);

            return ghost;
        }

        /// <summary>
        /// 将 OfficialData 实体化为一个真正的 Pawn (用于绑架、暗杀、政变等任务)。
        /// </summary>
        public static Pawn RealizePawn(OfficialData data, Faction faction)
        {
            if (data.pawnReference != null && !data.pawnReference.Destroyed)
            {
                return data.pawnReference;
            }

            // 生成逻辑同 GhostPawn，但这次我们会将其标记为 Official
            Pawn realPawn = GenerateGhostPawn(data);

            // 确保派系正确
            if (realPawn.Faction != faction)
            {
                realPawn.SetFaction(faction);
            }

            // 加入世界 Pawns 管理，防止被自动清理
            if (!Find.WorldPawns.Contains(realPawn))
            {
                Find.WorldPawns.PassToWorld(realPawn, PawnDiscardDecideMode.KeepForever);
            }

            data.pawnReference = realPawn;
            return realPawn;
        }

        /// <summary>
        /// 将数据强制应用到 Pawn 身上 (覆盖随机生成的内容)
        /// </summary>
        private static void ApplyDataToPawn(Pawn p, OfficialData data)
        {
            if (p == null || data == null) return;

            // 名字
            if (data.nameTriple != null) p.Name = data.nameTriple;

            // 外貌
            if (p.story != null)
            {
                if (data.bodyType != null) p.story.bodyType = data.bodyType;
                if (data.headType != null) p.story.headType = data.headType;
                if (data.hairDef != null) p.story.hairDef = data.hairDef;
                p.story.HairColor = data.hairColor;
                p.story.skinColorOverride = data.skinColor;

                // HAR 颜色
                var alienComp = p.TryGetComp<AlienPartGenerator.AlienComp>();
                if (alienComp != null)
                {
                    alienComp.OverwriteColorChannel("skin", data.skinColor, data.skinColor);
                    alienComp.OverwriteColorChannel("hair", data.hairColor, data.hairColor);
                }
            }

            // 特性
            if (p.story != null && data.traits != null)
            {
                p.story.traits.allTraits.Clear();
                foreach (var t in data.traits)
                {
                    p.story.traits.GainTrait(new Trait(t.def, t.Degree));
                }
            }

            // 技能
            if (p.skills != null && data.skillLevels != null)
            {
                foreach (var kvp in data.skillLevels)
                {
                    SkillDef skill = DefDatabase<SkillDef>.GetNamedSilentFail(kvp.Key);
                    if (skill != null)
                    {
                        var record = p.skills.GetSkill(skill);
                        if (record != null) record.Level = kvp.Value;
                    }
                }
            }

            // 刷新缓存
            p.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}