using System.Collections.Generic;
using MjolnirMenu.Core;
using UnityEngine;

namespace MjolnirMenu.Features
{
    /// <summary>Direct edits of Skills.Skill.m_level; the game persists them with the character save.</summary>
    public static class SkillCheats
    {
        public const float MaxLevel = 100f;

        public static List<Skills.Skill> GetSkills()
        {
            var p = Player.m_localPlayer;
            if (p == null) return new List<Skills.Skill>();
            var skills = p.GetSkills();
            return skills != null ? skills.GetSkillList() : new List<Skills.Skill>();
        }

        public static string Name(Skills.Skill s)
        {
            // Game keys skill names as $skill_<enum int>; fall back to the enum name if the key is missing.
            string loc = Localization.instance.Localize("$skill_" + (int)s.m_info.m_skill);
            return loc.StartsWith("[") ? s.m_info.m_skill.ToString() : loc;
        }

        public static void SetLevel(Skills.Skill s, float level)
        {
            s.m_level = Mathf.Clamp(level, 0f, MaxLevel);
            s.m_accumulator = 0f;
        }

        public static void SetAll(float level)
        {
            foreach (var s in GetSkills()) SetLevel(s, level);
            Hotkeys.Notify($"All skills set to {Mathf.Clamp(level, 0f, MaxLevel):0}");
        }

        public static void MaxAll() => SetAll(MaxLevel);
        public static void ResetAll() => SetAll(0f);
    }
}
