using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityEditor
{
    public partial class AnimatorControllerSetOverrideWindow : EditorWindow
    {
        private void OnProcess()
        {
            string[] detailAniNames = GetDetailAnimationNames();

            m_TargetAnimatorOverrideController.runtimeAnimatorController = m_SourceAnimatorController;
            foreach (var animationClip in m_SourceAnimatorController.animationClips)
            {
                var findAnimationClip = FindAnimationClipByName(m_AnimatorType, m_FolderAnimationClips, animationClip.name, detailAniNames);
                if (findAnimationClip == null)
                    continue;

                m_TargetAnimatorOverrideController[animationClip.name] = findAnimationClip;
            }
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private void OnClear()
        {
            foreach (var a in m_SourceAnimatorController.animationClips)
            {
                m_TargetAnimatorOverrideController[a.name] = null;
            }
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private void OnChangeClip(in AnimationClip InSourceAnimationClip, in AnimationClip InDestAnimationClip)
        {
            if (InDestAnimationClip == null || InSourceAnimationClip == null)
            {
                EditorUtility.DisplayDialog("Warning", "AnimationClip is null", "Ok");
                return;
            }

            m_TargetAnimatorOverrideController[InSourceAnimationClip.name] = InDestAnimationClip;
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
        
        private void OnRemove(in AnimationClip InAnimationClip)
        {
            if (InAnimationClip == null)
            {
                EditorUtility.DisplayDialog("Warning", "AnimationClip is null", "Ok");
                return;
            }

            m_TargetAnimatorOverrideController[InAnimationClip.name] = null;
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
}