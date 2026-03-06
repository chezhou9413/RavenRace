using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Hypnosis.Commands
{
    /// <summary>
    /// 催眠指令定义。
    /// 定义UI上的按钮、执行的Job、以及带来的Hediff和Thought。
    /// </summary>
    public class HypnosisCommandDef : Def
    {
        public string iconPath;
        public JobDef jobDef;

        // 执行指令期间赋予的 Hediff (如“催眠恍惚”)
        public HediffDef activeHediffDef;

        // 指令结束后赋予的 Thought (如“羞耻的余韵”)
        public ThoughtDef outcomeThoughtDef;

        // UI 排序
        public float order = 0;

        // 缓存图标
        private Texture2D icon;
        public Texture2D Icon
        {
            get
            {
                if (icon == null && !iconPath.NullOrEmpty())
                {
                    icon = ContentFinder<Texture2D>.Get(iconPath, true);
                }
                return icon ?? BaseContent.BadTex;
            }
        }
    }
}