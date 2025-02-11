using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    public partial class AnimatorControllerSetOverrideWindow : EditorWindow
    {
        private static AnimationClip[] GatherAnimationClips(in DefaultAsset InFolderAsset)
        {
            var folderPath = InFolderAsset.GetAbsolutePath();
            var animationClips = AssetPathExtensions.GetAtDirectoryPath<AnimationClip>(folderPath);
            
            return animationClips;
        }
        
        private static AnimationClip[] GatherWeaponAnimationClips(in DefaultAsset InFolderAsset, string InWeaponName)
        {
            var folderPath = InFolderAsset.GetAbsolutePath();
            var animationClips = AssetPathExtensions.GetAtDirectoryPath<AnimationClip>(folderPath);

            return animationClips.Where(x => x.name.Contains(InWeaponName)).ToArray();
        }
    }
}