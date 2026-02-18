using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace RavenRace.Features.StoryEngine
{
    /// <summary>
    /// 负责存储剧情相关的通用状态（Flags）。
    /// </summary>
    public class StoryWorldComponent : WorldComponent
    {
        private Dictionary<string, bool> storyFlags = new Dictionary<string, bool>();

        // 静态访问器
        public static StoryWorldComponent Instance => Find.World.GetComponent<StoryWorldComponent>();

        public StoryWorldComponent(World world) : base(world) { }

        public static bool HasFlag(string key)
        {
            if (Instance == null || Instance.storyFlags == null) return false;
            return Instance.storyFlags.TryGetValue(key, out bool val) && val;
        }

        public static void SetFlag(string key, bool value)
        {
            if (Instance == null) return;
            if (Instance.storyFlags == null) Instance.storyFlags = new Dictionary<string, bool>();

            Instance.storyFlags[key] = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 使用 RimWorld 1.6 的 Scribe_Collections 保存字典
            Scribe_Collections.Look(ref storyFlags, "storyFlags", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && storyFlags == null)
            {
                storyFlags = new Dictionary<string, bool>();
            }
        }
    }
}