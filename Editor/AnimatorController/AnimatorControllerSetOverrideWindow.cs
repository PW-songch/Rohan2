using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityEditor
{
    public partial class AnimatorControllerSetOverrideWindow : EditorWindow
    {
        public enum eAnimatorType
        {
            NONE,
            CHARACTER,
            NPC,
            RIDE
        }

        private readonly string CHARACTER_ANIMATOR_PATH = "Assets/Graphics/02. Character".ToLower();
        public const string CHARACTER_ANIMATOR_CONTROLLER_PATH = "Assets/Graphics/02. Character/Template/Template_Character_Controller.controller";
        public const string CHARACTER_COMMON_FOLDER_PATH = "Assets/Graphics/02. Character/Common";

        private AnimatorController m_SourceAnimatorController;
        private AnimatorOverrideController m_TargetAnimatorOverrideController;
        
        private DefaultAsset m_CommonTargetFolder = null;
        private DefaultAsset m_CharacterCommonTargetFolder = null;
        private DefaultAsset m_FirstClassTargetFolder = null;
        private DefaultAsset m_SecondClassTargetFolder = null;

        private AnimationClip[] m_FolderAnimationClips = null;
        private List<KeyValuePair<AnimationClip, AnimationClip>> m_Clips;

        private eAnimatorType m_AnimatorType;
        private string m_WeaponName = string.Empty;
        private string m_Gender = string.Empty;
        private Vector2 m_scrollPosition = Vector2.zero;
        
        [MenuItem ("PlayWith/Animation/AnimatorOverrideControllerSetWindow")]
        static void Init () 
        {
            var window = (AnimatorControllerSetOverrideWindow)GetWindow (typeof (AnimatorControllerSetOverrideWindow));
            window.Show();
        }

        private void OnFocus()
        {
            m_FolderAnimationClips = FindAnimationClips(m_AnimatorType, m_TargetAnimatorOverrideController, ref m_CommonTargetFolder, 
                new DefaultAsset[] { m_CharacterCommonTargetFolder, m_FirstClassTargetFolder, m_SecondClassTargetFolder }, 
                new string[] { m_WeaponName });
        }

        private void OnGUI()
        {
            GUI.changed = false;

            EditorGUILayout.BeginHorizontal();
            {
                m_SourceAnimatorController = EditorGUILayout.ObjectField("Source Controller", m_SourceAnimatorController, typeof(AnimatorController), false) as AnimatorController;
                if (GUILayout.Button("Character", GUILayout.Width(200)))
                {
                    m_SourceAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(CHARACTER_ANIMATOR_CONTROLLER_PATH);
                    m_CommonTargetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(CHARACTER_COMMON_FOLDER_PATH);
                    if (m_AnimatorType != eAnimatorType.CHARACTER)
                        m_TargetAnimatorOverrideController = null;
                }
                if (GUILayout.Button("NPC", GUILayout.Width(200)))
                {
                    m_SourceAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(NPC_ANIMATOR_CONTROLLER_PATH);
                    if (m_AnimatorType != eAnimatorType.NPC)
                    {
                        m_TargetAnimatorOverrideController = null;
                        m_CommonTargetFolder = null;
                    }
                }
                if (GUILayout.Button("RIDE", GUILayout.Width(200)))
                {
                    m_SourceAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(RIDE_ANIMATOR_CONTROLLER_PATH);
                    if (m_AnimatorType != eAnimatorType.RIDE)
                    {
                        m_TargetAnimatorOverrideController = null;
                        m_CommonTargetFolder = null;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            var controller = m_TargetAnimatorOverrideController;
            m_TargetAnimatorOverrideController = EditorGUILayout.ObjectField("Target Override Controller", 
                m_TargetAnimatorOverrideController, typeof(AnimatorOverrideController), false) as AnimatorOverrideController;
            if (m_SourceAnimatorController == null || controller != m_TargetAnimatorOverrideController)
            {
                string path = AssetDatabase.GetAssetPath(m_TargetAnimatorOverrideController).ToLower();
                if (path.Contains(CHARACTER_ANIMATOR_PATH))
                    m_SourceAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(CHARACTER_ANIMATOR_CONTROLLER_PATH);
                else if (path.Contains(RIDE_ANIMATOR_PATH))
                    m_SourceAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(RIDE_ANIMATOR_CONTROLLER_PATH);
                else
                {
                    for (int i = 0; i < NPC_ANIMATOR_PATH.Length; ++i)
                    {
                        if (path.Contains(NPC_ANIMATOR_PATH[i]))
                        {
                            m_SourceAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(NPC_ANIMATOR_CONTROLLER_PATH);
                            break;
                        }
                    }
                }
            }

            if (m_SourceAnimatorController == null || m_TargetAnimatorOverrideController == null)
                return;

            if (GUI.changed)
            {
                string controllerName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m_SourceAnimatorController)).ToLower();
                foreach (var type in Enum.GetValues(typeof(eAnimatorType)))
                {
                    if (controllerName.Contains(type.ToString().ToLower()))
                    {
                        m_AnimatorType = (eAnimatorType)type;
                        break;
                    }
                }

                switch (m_AnimatorType)
                {
                    case eAnimatorType.CHARACTER:
                        {
                            if (!AssetDatabase.GetAssetPath(m_TargetAnimatorOverrideController).ToLower().Contains(CHARACTER_ANIMATOR_PATH))
                            {
                                m_TargetAnimatorOverrideController = controller;
                                if (m_TargetAnimatorOverrideController == null)
                                    return;
                            }

                            if (m_CommonTargetFolder == null && controller != m_TargetAnimatorOverrideController)
                                m_CommonTargetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(CHARACTER_COMMON_FOLDER_PATH);
                        }
                        break;

                    case eAnimatorType.NPC:
                        {
                            string path = AssetDatabase.GetAssetPath(m_TargetAnimatorOverrideController).ToLower();
                            for (int i = 0; i < NPC_ANIMATOR_PATH.Length; ++i)
                            {
                                if (path.Contains(NPC_ANIMATOR_PATH[i]))
                                    break;

                                if (i == NPC_ANIMATOR_PATH.Length - 1)
                                {
                                    m_TargetAnimatorOverrideController = controller;
                                    if (m_TargetAnimatorOverrideController == null)
                                        return;
                                }
                            }
                        }
                        break;

                    case eAnimatorType.RIDE:
                        {
                            if (!AssetDatabase.GetAssetPath(m_TargetAnimatorOverrideController).ToLower().Contains(RIDE_ANIMATOR_PATH))
                            {
                                m_TargetAnimatorOverrideController = controller;
                                if (m_TargetAnimatorOverrideController == null)
                                    return;
                            }
                        }
                        break;
                }
            }

            m_CommonTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Common Folder", m_CommonTargetFolder, typeof(DefaultAsset), false);

            switch (m_AnimatorType)
            {
                case eAnimatorType.CHARACTER:
                    m_CharacterCommonTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Character Common Folder", m_CharacterCommonTargetFolder, typeof(DefaultAsset), false);
                    m_FirstClassTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("First Class Folder", m_FirstClassTargetFolder, typeof(DefaultAsset), false);
                    m_SecondClassTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Second Class Folder", m_SecondClassTargetFolder, typeof(DefaultAsset), false);

                    if (GUI.changed)
                    {
                        var split = m_TargetAnimatorOverrideController.name.Split("_");
                        if (split.Length > 0)
                        {
                            m_WeaponName = split.Last();
                            if (split.Length > 1)
                                m_Gender = "_" + split[1];
                        }
                    }
                    break;

                case eAnimatorType.NPC:
                    if (GUI.changed)
                        m_MonsterAnimatorControllerName = m_TargetAnimatorOverrideController.name;
                    break;

                case eAnimatorType.RIDE:
                    if (GUI.changed)
                        m_RideAnimatorControllerName = m_TargetAnimatorOverrideController.name;
                    break;
            }

            if (GUI.changed)
            {
                m_FolderAnimationClips = FindAnimationClips(m_AnimatorType, m_TargetAnimatorOverrideController, ref m_CommonTargetFolder, 
                    new DefaultAsset[] { m_CharacterCommonTargetFolder, m_FirstClassTargetFolder, m_SecondClassTargetFolder }, 
                    new string[] { m_WeaponName }, controller != m_TargetAnimatorOverrideController);
            }
            
            GUILayout.Space(10);
            DrawPerformButtons();
            GUILayout.Space(10);
            DrawClipHeader();
            DrawAnimClip();
        }

        private void DrawPerformButtons()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button ("CHANGE"))
                OnProcess();
            
            if (GUILayout.Button ("CLEAR"))
                OnClear();

            GUILayout.EndHorizontal();
        }
        
        private void DrawClipHeader()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("NAME", GUILayout.Width(200));
            GUILayout.Label("FOLDER ASSETS", GUILayout.Width(300));
            GUILayout.Label("OVERRIDE", GUILayout.Width(300));
            GUILayout.Label("CHANGE", GUILayout.Width(100));
            
            GUILayout.EndHorizontal();
        }

        private void DrawAnimClip()
        {
            if (m_SourceAnimatorController == null || m_TargetAnimatorOverrideController == null)
                return;

            var sourcesAnimationClips = m_SourceAnimatorController.animationClips;

            m_Clips ??= new List<KeyValuePair<AnimationClip, AnimationClip>>();
            
            m_TargetAnimatorOverrideController.GetOverrides(m_Clips);

            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);

            string[] detailAniNames = GetDetailAnimationNames();

            foreach (var t in sourcesAnimationClips)
            {
                GUILayout.BeginHorizontal();

                var overrideAnimClip = m_Clips.FirstOrDefault(x => x.Key.name.Equals(t.name));
                
                EditorGUILayout.LabelField(t.name, GUILayout.Width(200));

                var tempAnimationClip = FindAnimationClipByName(m_AnimatorType, m_FolderAnimationClips, t.name, detailAniNames);
                GUI.color = tempAnimationClip != null ? Color.green : Color.red;
                EditorGUILayout.ObjectField(string.Empty, tempAnimationClip, typeof(AnimationClip), true, GUILayout.Width(300));
                
                GUI.color = Color.white;
                
                EditorGUILayout.ObjectField(string.Empty, overrideAnimClip.Value, typeof(AnimationClip), true, GUILayout.Width(300));

                if (GUILayout.Button("RUN", GUILayout.Width(100)))
                {
                    OnChangeClip(t, tempAnimationClip);
                }
                
                if (GUILayout.Button("Remove", GUILayout.Width(100)))
                {
                    OnRemove(t);
                }
                
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        public static AnimationClip[] FindAnimationClips(eAnimatorType InAniType, in AnimatorOverrideController InOverrideController, 
            ref DefaultAsset InTargetFolder, in DefaultAsset[] InFolders, in string[] InNames, bool InChangeController = false)
        {
            AnimationClip[] animationClips = null;
            switch (InAniType)
            {
                case eAnimatorType.CHARACTER:
                    {
                        int totalLength = 0;
                        AnimationClip[] commonAnimationClips = null, characterCommonAnimationClips = null, firstClassAnimationClips = null, secondClassAnimationClips = null;
                        if (InTargetFolder != null)
                        {
                            commonAnimationClips = GatherAnimationClips(InTargetFolder);
                            totalLength += commonAnimationClips.Length;
                        }

                        if (InFolders != null)
                        {
                            if (InFolders.Length > 0 && InFolders[0] != null)
                            {
                                characterCommonAnimationClips = GatherAnimationClips(InFolders[0]);
                                totalLength += characterCommonAnimationClips?.Length ?? 0;
                            }
                            if (InFolders.Length > 1 && InFolders[1] != null)
                            {
                                firstClassAnimationClips = GatherWeaponAnimationClips(InFolders[1], InNames[0]);
                                totalLength += firstClassAnimationClips?.Length ?? 0;
                            }
                            if (InFolders.Length > 2 && InFolders[2] != null)
                            {
                                secondClassAnimationClips = GatherAnimationClips(InFolders[2]);
                                totalLength += secondClassAnimationClips?.Length ?? 0;
                            }
                        }

                        if (InTargetFolder != null && firstClassAnimationClips == null)
                        {
                            firstClassAnimationClips = GatherWeaponAnimationClips(InTargetFolder, InNames[0]);
                            totalLength += firstClassAnimationClips?.Length ?? 0;
                        }

                        var commonAnimationClipsLength = commonAnimationClips != null ? commonAnimationClips.Length : 0;
                        var characterCommonAnimationClipsLength = characterCommonAnimationClips != null ? characterCommonAnimationClips.Length : 0;
                        var firstClassAnimationClipsLength = firstClassAnimationClips != null ? firstClassAnimationClips.Length : 0;
                        var secondClassAnimationClipsLength = secondClassAnimationClips != null ? secondClassAnimationClips.Length : 0;

                        animationClips = new AnimationClip[totalLength];
                        if (commonAnimationClips != null)
                            Array.Copy(commonAnimationClips, 0, animationClips, 0, commonAnimationClipsLength);
                        if (characterCommonAnimationClips != null)
                            Array.Copy(characterCommonAnimationClips, 0, animationClips, commonAnimationClipsLength, characterCommonAnimationClipsLength);
                        if (firstClassAnimationClips != null)
                            Array.Copy(firstClassAnimationClips, 0, animationClips, commonAnimationClipsLength + characterCommonAnimationClipsLength, firstClassAnimationClipsLength);
                        if (secondClassAnimationClips != null)
                            Array.Copy(secondClassAnimationClips, 0, animationClips, commonAnimationClipsLength + characterCommonAnimationClipsLength + firstClassAnimationClipsLength, secondClassAnimationClipsLength);
                    }
                    break;

                case eAnimatorType.NPC:
                    animationClips = UpdateMonsterAnimationClips(InOverrideController, ref InTargetFolder, InChangeController);
                    break;

                case eAnimatorType.RIDE:
                    animationClips = UpdateRideAnimationClips(InOverrideController, ref InTargetFolder, InChangeController);
                    break;
            }

            return animationClips;
        }

        private string[] GetDetailAnimationNames()
        {
            switch(m_AnimatorType)
            {
                case eAnimatorType.CHARACTER:
                    return new string[] { m_WeaponName, m_Gender };
                case eAnimatorType.NPC:
                    return new string[] { m_MonsterAnimatorControllerName };
                case eAnimatorType.RIDE:
                    return new string[] { m_RideAnimatorControllerName };
            }

            return null;
        }

        public static AnimationClip FindAnimationClipByName(eAnimatorType InAniType, in AnimationClip[] InAniClips, in string InAnimName, string[] InDetailNames)
        {
            switch (InAniType)
            {
                case eAnimatorType.CHARACTER:
                    {
                        if (InAniClips == null)
                            return null;

                        string aniName = InAnimName.ToLower();
                        string weaponName = InDetailNames[0].ToLower();
                        string gender = InDetailNames[1].ToLower();

                        foreach (var element in InAniClips)
                        {
                            var split = element.name.ToLower().Split("@");

                            if (InDetailNames[1].IsNullOrEmpty() == false && split.First().Contains(gender) == false)
                                continue;

                            var tempAnimName = split.Last().Replace($"{weaponName}_", string.Empty);
                            if (tempAnimName.Length > aniName.Length)
                                tempAnimName = tempAnimName.Substring(0, aniName.Length);

                            if (tempAnimName.Equals(aniName))
                                return element;
                        }
                    }
                    break;

                case eAnimatorType.NPC:
                    return FindMonsterAnimationClipByName(InAniClips, InAnimName, InDetailNames);

                case eAnimatorType.RIDE:
                    return FindRideAnimationClipByName(InAniClips, InAnimName, InDetailNames);
            }

            return null;
        }
    }
}