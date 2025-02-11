using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    public partial class AnimatorControllerSetOverrideWindow : EditorWindow
    {
        private readonly string[] NPC_ANIMATOR_PATH = { "Assets/Graphics/04. Npc".ToLower(), "Assets/Graphics/03. Monster".ToLower() };
        public const string NPC_ANIMATOR_CONTROLLER_PATH = "Assets/Graphics/04. Npc/Template/Template_NPC_Controller.controller";

        private string m_MonsterAnimatorControllerName = string.Empty;

        public static AnimationClip[] UpdateMonsterAnimationClips(in AnimatorOverrideController InOverrideController, ref DefaultAsset InTargetFolder, bool InChangeController = false)
        {
            if (InOverrideController == null)
                return null;

            if (InTargetFolder == null || InChangeController)
            {
                string path = AssetDatabase.GetAssetPath(InOverrideController);
                InTargetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path.Replace("/" + Path.GetFileName(path), ""));
            }

            if (InTargetFolder != null)
                return GatherAnimationClips(InTargetFolder);
            
            return null;
        }

        public static AnimationClip FindMonsterAnimationClipByName(in AnimationClip[] InAniClips, in string InAnimName, params string[] InDetailNames)
        {
            if (InAniClips == null)
                return null;

            string aniName = InAnimName.ToLower();
            string monsterName = InDetailNames[0].ToLower();

            foreach (var element in InAniClips)
            {
                var split = element.name.ToLower().Split("@");
                if (monsterName.Equals(split.FirstOrDefault()))
                {
                    var animName = split.Last();
                    if (animName.Equals(aniName))
                        return element;
                }
            }

            return null;
        }
    }
}
