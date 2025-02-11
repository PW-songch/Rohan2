using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    public partial class AnimatorControllerSetOverrideWindow : EditorWindow
    {
        private readonly string RIDE_ANIMATOR_PATH = "Assets/Graphics/07. Ride".ToLower();
        public const string RIDE_ANIMATOR_CONTROLLER_PATH = "Assets/Graphics/07. Ride/Template/Template_Ride_Controller.controller";

        private string m_RideAnimatorControllerName = string.Empty;

        public static AnimationClip[] UpdateRideAnimationClips(in AnimatorOverrideController InOverrideController, ref DefaultAsset InTargetFolder, bool InChangeController = false)
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

        public static AnimationClip FindRideAnimationClipByName(in AnimationClip[] InAniClips, in string InAnimName, params string[] InDetailNames)
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
