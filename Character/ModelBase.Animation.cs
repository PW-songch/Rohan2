// + 전투(공격, 사망)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Time;
using Cysharp.Threading.Tasks;
using UnityEngine;

using ANIMATOR_STATE_TYPE = AnimatorState.ANIMATOR_STATE_TYPE;

public partial class ModelBase
{
    protected HashSet<int> _animatorParameterNameHashSet;
    private AutoDictionary<ANIMATOR_STATE_TYPE, bool> _dicHasAnimationClips;
    private AutoDictionary<int, AnimationClip> _templateNameHashAnimationClips = null;
    private ANIMATOR_STATE_TYPE _animatorStateType = ANIMATOR_STATE_TYPE.NONE;
    private float _moveBlendParameter;
    private int _attackParameter;
    private int _skillParameter;
    private float _animatorSpeed;
    private bool _isBattleMode;
    private bool _isPlayBlinkAnimation;

    public ANIMATOR_STATE_TYPE AnimatorStateType => _animatorStateType;
    public float MoveWeight => _moveBlendParameter;
    public int AttackParameter => _attackParameter;
    public int SkillParameter => _skillParameter;

    public int IdleSubAnimationCount => _ownerEntity != null ? LogicContext.ANIMATION.GetIdleSubAnimationCount((_ownerEntity.TribeType, _ownerEntity.CharacterGender)) : 0;
    public bool HasIdleSubAnimation => IdleSubAnimationCount > 0;

    public bool IsStop => _moveBlendParameter == 0f;
    public bool UseDeathDelay { get; private set; } = true;

    public bool IsBattleMode => _isBattleMode;

    private const float BLEND_CUT_MIN_VALUE = 0.001f;
    private const float BLEND_CHANGE_RATE = 10.0f;
    public const float BATTLE_END_ANI_TIME = 1f;

    public void SetAnimator()
    {
        if (ModelAnimator == null)
            ModelAnimator = GetComponent<Animator>();

        if (ModelAnimator != null && ModelAnimator.runtimeAnimatorController != null)
        {
            SetAnimatorCullingMode(AnimatorCullingMode.CullUpdateTransforms);
            SetAnimatorDatas();
        }

        ResetAnimatorInfo();
    }

    public void SetAnimatorCullingMode(in AnimatorCullingMode InCullingMode)
    {
        ModelAnimator.cullingMode = InCullingMode;
    }

    public void ChangeAnimatorController(in RuntimeAnimatorController InController)
    {
        if (InController == null) return;

        if (ModelAnimator == null)
        {
            SetAnimator();
            return;
        }

        ModelAnimator.runtimeAnimatorController = InController;
        if (ModelAnimator.isActiveAndEnabled)
            ModelAnimator.Update(0f);

        ClearAnimatorDatas();
        SetUpAnimation();
    }

    public RuntimeAnimatorController InstantiateAnimatorController()
    {
        return Instantiate(ModelAnimator.runtimeAnimatorController);
    }

    private void ClearAnimatorDatas()
    {
        _templateNameHashAnimationClips?.Clear();
        _animatorParameterNameHashSet?.Clear();
        _dicHasAnimationClips?.Clear();
    }

    protected void SetAnimatorParameterNameList()
    {
        if (!_animatorParameterNameHashSet.IsNullOrEmpty())
            return;

        if (ModelAnimator != null && ModelAnimator.runtimeAnimatorController != null)
        {
            if (_animatorParameterNameHashSet == null)
                _animatorParameterNameHashSet = new HashSet<int>();

            if (ModelAnimator.isActiveAndEnabled)
            {
                for (int i = 0; i < ModelAnimator.parameterCount; ++i)
                {
                    _animatorParameterNameHashSet.Add(ModelAnimator.GetParameter(i).nameHash);
                }
            }
        }
    }

    private (AnimationClip[] clips, int[] clipNameHashs, ANIMATOR_STATE_TYPE[] animatorStateTypes) GenerateAnimationClipInfos()
    {
        if (_ownerEntity != null && ModelAnimator != null && ModelAnimator.runtimeAnimatorController != null &&
            !ModelAnimator.runtimeAnimatorController.animationClips.IsNullOrEmpty())
        {
            if (_templateNameHashAnimationClips == null)
                _templateNameHashAnimationClips = new();
            else
                _templateNameHashAnimationClips.Clear();

            ANIMATOR_STATE_TYPE[] arrAnimatorStateType = null;
            AnimationClip[] arrClips = null;
            string[] arrClipNames = null;
            int[] arrClipNameHashs = null;

            if (ModelAnimator.runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                List<KeyValuePair<AnimationClip, AnimationClip>> clips = new ();
                overrideController.GetOverrides(clips);

                // 원본 클립 이름 해시 설정
                var clipNameHashs = LogicContext.ANIMATION.GetAnimatorOriginalClipNameHashs(_ownerEntity.EntityType);
                if (clipNameHashs.IsNullOrEmpty())
                {
                    LogicContext.ANIMATION.SetAnimatorOriginalClipNameHashs(_ownerEntity.EntityType, clips);
                    clipNameHashs = LogicContext.ANIMATION.GetAnimatorOriginalClipNameHashs(_ownerEntity.EntityType);

                    if (!clipNameHashs.IsNullOrEmpty())
                    {
                        for (int i = 0; i < clips.Count; ++i)
                        {
                            if (clips[i].Value != null)
                                _templateNameHashAnimationClips[clipNameHashs[i]] = clips[i].Value;
                        }
                    }
                }

                var originalClipDatas = LogicContext.ANIMATION.GetIngameAnimatorOriginalClipDatas(_ownerEntity.EntityType);
                if (originalClipDatas != default)
                {
                    // 인게임용 원본 클립 데이터로 적용
                    arrAnimatorStateType = originalClipDatas.Item1;
                    arrClipNameHashs = originalClipDatas.Item2;

                    // 인게임용 클립 정의 데이터와 애니메이터에 가지고 있는 클립과 비교하여 인게임용 클립 데이터로 설정
                    for (int i = 0; i < arrClipNameHashs.Length; ++i)
                    {
                        if (i >= clips.Count)
                        {
                            clips.Add(default);
                            arrClipNameHashs[i] = 0;
                            continue;
                        }

                        bool find = false;
                        int nameHash = arrClipNameHashs[i];
                        for (int j = 0; j < clipNameHashs.Length; ++j)
                        {
                            if (nameHash == clipNameHashs[j] && clips[j].Value != null)
                            {
                                int nh = clipNameHashs[i];
                                clipNameHashs[i] = clipNameHashs[j];
                                clipNameHashs[j] = nh;

                                var clip = clips[i];
                                clips[i] = clips[j];
                                clips[j] = clip;
                                find = true;
                                break;
                            }
                        }

                        if (find == false)
                        {
                            clips[i] = default;
                            arrClipNameHashs[i] = 0;
                        }
                    }

                    // 초과 클립에 대해 제거
                    if (arrClipNameHashs.Length < clips.Count)
                        clips.RemoveRange(arrClipNameHashs.Length, clips.Count - arrClipNameHashs.Length);
                }
                else
                {
                    // 인게임용 클립 데이터 없는 경우 세팅
                    for (int i = 0; i < clips.Count;)
                    {
                        // 오버라이드 클립 없는 경우 제거
                        if (clips[i].Value == null)
                            clips.RemoveAt(i);
                        else
                            i++;
                    }

                    // 애니메이터에 가지고 있는 클립으로 데이터 설정
                    arrClipNames = new string[clips.Count];
                    arrClipNameHashs = new int[clips.Count];
                    for (int i = 0; i < clips.Count; ++i)
                    {
                        arrClipNames[i] = clips[i].Key?.name.ToLower();
                        arrClipNameHashs[i] = Animator.StringToHash(arrClipNames[i]);
                    }
                }

                arrClips = new AnimationClip[clips.Count];
                for (int i = 0; i < clips.Count; ++i)
                    arrClips[i] = clips[i].Value;
            }
            else
            {
                arrClips = ModelAnimator.runtimeAnimatorController.animationClips;
                if (!arrClips.IsNullOrEmpty())
                {
                    arrClipNames = new string[arrClips.Length];
                    arrClipNameHashs = new int[arrClips.Length];
                    for (int i = 0; i < arrClips.Length; ++i)
                    {
                        arrClipNames[i] = arrClips[i]?.name.ToLower();
                        arrClipNameHashs[i] = Animator.StringToHash(arrClipNames[i]);
                    }
                }
            }

            if (arrAnimatorStateType == null && !arrClipNames.IsNullOrEmpty())
            {
                // 클립 타입 세팅 안된 경우 클립의 이름과 타입 비교하여 타입 적용
                arrAnimatorStateType = new ANIMATOR_STATE_TYPE[arrClips.Length];

                var aniStateTypeNames = AnimatorState.AnimatorStateTypeNames;
                var animatorStateTypes = AnimatorState.AnimatorStateTypes;
                for (int i = 0; i < arrClipNames.Length; ++i)
                {
                    arrAnimatorStateType[i] = ANIMATOR_STATE_TYPE.NONE;

                    for (int j = 1, count = aniStateTypeNames.Length - 1; j < count; ++j)
                    {
                        string typeName = aniStateTypeNames[j];
                        if (arrClipNames[i].Contains(typeName))
                        {
                            arrAnimatorStateType[i] = animatorStateTypes[j];
                            break;
                        }
                    }
                }
            }

            if (_templateNameHashAnimationClips.Count == 0)
            {
                for (int i = 0; i < arrClips.Length; ++i)
                    _templateNameHashAnimationClips[arrClipNameHashs[i]] = arrClips[i];
            }

            return (arrClips, arrClipNameHashs, arrAnimatorStateType);
        }

        return default;
    }

    private void SetHasAnimationClips(in AnimationClip[] InClips, in ANIMATOR_STATE_TYPE[] InAnimatorStateTypes)
    {
        if (InClips.IsNullOrEmpty() || InAnimatorStateTypes.IsNullOrEmpty() || InClips.Length != InAnimatorStateTypes.Length)
            return;

        if (_dicHasAnimationClips == null)
            _dicHasAnimationClips = new AutoDictionary<ANIMATOR_STATE_TYPE, bool>();
        else
            _dicHasAnimationClips.Clear();

        var animatorStateTypes = AnimatorState.AnimatorStateTypes;
        for (int i = 1, count = animatorStateTypes.Length - 1; i < count; ++i)
        {
            for (int j = 0; j < InAnimatorStateTypes.Length; ++j)
            {
                var animatorState = InAnimatorStateTypes[j];
                if (animatorState is ANIMATOR_STATE_TYPE.NONE or ANIMATOR_STATE_TYPE.END)
                    continue;

                if (animatorStateTypes[i] == animatorState)
                {
                    _dicHasAnimationClips[animatorState] = InClips[j] != null;
                    break;
                }
            }
        }
    }

    private bool HasAnimationClip(ANIMATOR_STATE_TYPE InStateType)
    {
        return _dicHasAnimationClips?[InStateType] == true;
    }

    private void SetIdleSubAnimationCount(in int[] InClipNameHashs)
    {
        if (_ownerEntity != null && _ownerEntity.IsPlayerEntity)
            LogicContext.ANIMATION.SetIdleSubAnimationCount((_ownerEntity.TribeType, _ownerEntity.CharacterGender), InClipNameHashs);
    }

    public int GetIdleSubAnimationType(int InIndex)
    {
        return LogicContext.ANIMATION.GetIdleSubAnimationType((_ownerEntity.TribeType, _ownerEntity.CharacterGender), InIndex);
    }

    private void ResetAnimatorInfo()
    {
        _animatorStateType = ANIMATOR_STATE_TYPE.NONE;
        _moveBlendParameter = 0f;
        _attackParameter = 0;
        _skillParameter = 0;
        _animatorSpeed = ModelAnimator != null ? ModelAnimator.speed : 1f;
    }

    public bool PlayAnimation(string InAnimation, float InNormalizedTime = 0f, float InCrossFade = 0f)
    {
        var tempAnimationName = InAnimation;

        if (_animationClips.IsNullOrEmpty())
            return false;

        if (_animationClips.ContainsKey(tempAnimationName) == false)
            return false;

        if (ModelAnimator == null)
            return false;

        // temp : add AnimatorOverrideController function : 20231011 - sucheol.park
        if (ModelAnimator.runtimeAnimatorController is AnimatorOverrideController)
        {
            var weaponName = ModelAnimator.runtimeAnimatorController.name.Split("_").Last();

            tempAnimationName = InAnimation.Split("@")
                .Last()
                .Replace($"{weaponName}_", string.Empty);
        }

        PlayAnimation(Animator.StringToHash(tempAnimationName), InNormalizedTime, InCrossFade);
        return true;
    }

    public void PlayAnimation(int InAnimationNameHash, float InNormalizedTime = 0f, float InCrossFade = 0f)
    {
        InNormalizedTime = Mathf.Clamp01(InNormalizedTime);

        if (InCrossFade == 0)
            ModelAnimator.Play(InAnimationNameHash, -1, InNormalizedTime);
        else
            ModelAnimator.CrossFade(InAnimationNameHash, InCrossFade, -1, 0, InNormalizedTime);
    }

    public void SetCurrentAnimationNormalizedTime(float InNormalizedTime, float InCrossFade = 0f)
    {
        AnimatorStateInfo stateInfo = ModelAnimator.GetCurrentAnimatorStateInfo(AnimatorLayerIndex.DEFAULT);
        PlayAnimation(stateInfo.fullPathHash, InNormalizedTime, InCrossFade);
    }

    public void SetNextAnimationNormalizedTime(float InNormalizedTime, float InCrossFade = 0f)
    {
        AnimatorStateInfo stateInfo = ModelAnimator.GetNextAnimatorStateInfo(AnimatorLayerIndex.DEFAULT);
        PlayAnimation(stateInfo.fullPathHash, InNormalizedTime, InCrossFade);
    }

    public void StartAnimation(int InAnimationTypes)
    {
        _animatorStateType = (ANIMATOR_STATE_TYPE)InAnimationTypes;
        _animationEvents?.OnAnimationEvent_Start(InAnimationTypes);
    }

    public void FinishAnimation(int InAnimationTypes = int.MaxValue)
    {
        _animationEvents?.OnAnimationEvent_Finish(InAnimationTypes);
        if (_ownerEntity is MyPlayerEntity myPlayer)
            myPlayer.AnimationFinishTime = UtcTime.NowEpoch;
    }

    public void ResetAnimationEvents(int InAnimationTypes = int.MinValue)
    {
        _animationEvents?.OnAnimationEvent_Reset(InAnimationTypes);
    }

    public void SetMoveBlend(float InMoveBlend)
    {
        _moveBlendParameter = InMoveBlend;
        SetAnimatorFloatParameterValue(AnimatorParameters.MOVE_BLEND, InMoveBlend);
    }

    public void SetMoveSpeed(float InSpeed)
    {
        SetAnimatorFloatParameterValue(AnimatorParameters.MOVE_SPEED, InSpeed);
    }

    public float ConvertToMoveAnimationSpeed(float InMoveSpeed, bool InLimitMax = true)
    {
        return Mathf.Clamp(InMoveSpeed, RepositoryContext.CONST.MOVE_SPEED_BLENDING_MIN * 0.01f,
            InLimitMax ? RepositoryContext.CONST.MOVE_SPEED_BLENDING_MAX * 0.01f : float.MaxValue);
    }

    public void SetAttackSpeed(float InSpeed)
    {
        SetAnimatorFloatParameterValue(AnimatorParameters.ATTACK_SPEED, InSpeed);
    }

    public void SetSkillSpeed(float InSpeed)
    {
        SetAnimatorFloatParameterValue(AnimatorParameters.SKILL_SPEED, InSpeed);
    }

    private float GetMoveSpeed()
    {
        return _ownerEntity != null ? _ownerEntity.AnimationMoveSpeedRate : 1f;
    }

    public float UpdateMoveBlendWithSpeed(float InCurrentValue, float InTargetValue)
    {
        if (_ownerEntity == null)
            return 0;

        float moveSpeed = GetMoveSpeed();
        float blendValue = Mathf.Clamp(Mathf.Lerp(InCurrentValue, InTargetValue, Time.deltaTime * BLEND_CHANGE_RATE), 0f, 1f);

        if (_ownerEntity.MoveComponent.OnMove == false)
        //if (_ownerEntity.MoveComponent.MoveState == MoveState.None)
        {
            if (blendValue <= BLEND_CUT_MIN_VALUE)
                blendValue = 0f;
            moveSpeed = 1f;
        }

        SetMoveBlend(blendValue);
        SetMoveSpeed(ConvertToMoveAnimationSpeed(moveSpeed, !_ownerEntity.OnCommand_RideOn));

        if (_ownerEntity.OnCommand_RideOn)
        {
            if (_ownerEntity is MyPlayerEntity myPlayer)
            {
                (myPlayer.RideModel as RideModel)?.SetMoveBlend(blendValue);
                (myPlayer.RideModel as RideModel)?.SetMoveSpeed(ConvertToMoveAnimationSpeed(moveSpeed, false));
            }
            else if (_ownerEntity is PlayerEntity player)
            {
                (player.RideModel as RideModel)?.SetMoveBlend(blendValue);
                (player.RideModel as RideModel)?.SetMoveSpeed(ConvertToMoveAnimationSpeed(moveSpeed, false));
            }
        }

        return blendValue;
    }

    public async UniTask PauseAnimation(int InTime)
    {
        PauseAnimation(true);

        await UniTask.Delay(InTime);

        PauseAnimation(false);
    }

    public void PauseAnimation(bool InPause)
    {
        if (ModelAnimator == null)
            return;

        if (InPause)
            _animatorSpeed = ModelAnimator.speed;

        ModelAnimator.speed = InPause ? 0f : _animatorSpeed;
    }

    public void SetAnimationSpeed(float InChangeSpeed)
    {
        if (ModelAnimator == null)
            return;

        _animatorSpeed = InChangeSpeed;
        ModelAnimator.speed = InChangeSpeed;
    }

    public void ChangeIdleState(bool InForce = true)
    {
        ResetAnimatorParameters(false);

        if (InForce || _animatorStateType != ANIMATOR_STATE_TYPE.IDLE)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.IDLE, true, true);
        }

        _animatorStateType = ANIMATOR_STATE_TYPE.IDLE;
    }

    public void ChangeIdleSubState(int InSubType, bool InForce = true)
    {
        ResetAnimatorParameters(false);

        if (InForce || _animatorStateType == ANIMATOR_STATE_TYPE.IDLE)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.IDLE_SUB, true);
            SetAnimatorIntParameterValue(AnimatorParameters.IDLE_SUB_TYPE, InSubType);
        }

        _animatorStateType = ANIMATOR_STATE_TYPE.IDLE_SUB;
    }

    public bool ChangeIdleStandState()
    {
        if (!HasAnimationClip(ANIMATOR_STATE_TYPE.IDLE_STAND))
            return false;
        
        ResetAnimatorParameters(false);

        //if (_animatorStateType != ANIMATOR_STATE_TYPE.IDLE_STAND)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.IDLE_STAND, true);
        }

        _animatorStateType = ANIMATOR_STATE_TYPE.IDLE_STAND;
        return true;
    }

    public void ChangeWalkState()
    {
        if (_animatorStateType != ANIMATOR_STATE_TYPE.WALK)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.WALK, true, false);
        }

        SetMoveSpeed(ConvertToMoveAnimationSpeed(GetMoveSpeed()));

        _animatorStateType = ANIMATOR_STATE_TYPE.WALK;
    }

    public void ChangeRunState()
    {
        if (_animatorStateType != ANIMATOR_STATE_TYPE.IDLE && _animatorStateType != ANIMATOR_STATE_TYPE.RUN)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.MOVE, true, false);
        }

        _animatorStateType = ANIMATOR_STATE_TYPE.RUN;
    }

    public void ChangeRideState(bool InValue = true, int InRideType = 1)
    {
        if (InValue)
        {
            ResetAnimatorParameters(false);
            SetAnimatorIntParameterValue(AnimatorParameters.RIDE_TYPE, InRideType);
            SetAnimatorBoolParameterValue(AnimatorParameters.RIDE, true);
            SetAnimatorBoolParameterValue(AnimatorParameters.ON_RIDE, true);
            _animatorStateType = ANIMATOR_STATE_TYPE.RIDE;
        }
        else
        {
            SetAnimatorBoolParameterValue(AnimatorParameters.ON_RIDE, false);
            if (OwnerEntity != null && !OwnerEntity.OnCommand_Interaction)
                ChangeBattleState(OwnerEntity.IsBattleMode);
        }
    }

    public void ChangeStartRideOnState(bool InValue = true)
    {
        if (InValue)
        {
            SetAnimatorTriggerParameterValue(AnimatorParameters.START_RIDE_ON, true);
        }
        else
        {
            SetAnimatorTriggerParameterValue(AnimatorParameters.START_RIDE_ON, false);
        }
    }

    public void ChangeBattleState(bool InValue, bool InSetTrigger = false)
    {
        if (IsUnableBattle())
            return;

        if (InValue)
        {
            ResetStateParameters();
            if (InSetTrigger && (_isBattleMode != InValue || (_animatorStateType != ANIMATOR_STATE_TYPE.IDLE && _animatorStateType != ANIMATOR_STATE_TYPE.RUN)))
                SetAnimatorTriggerParameterValue(AnimatorParameters.IDLE, true, false);
            
            if(_ownerEntity != null)
                _ownerEntity.SetHideWeapon(false);
        }
        else
        {
            if (InSetTrigger)
            {
                if (_isBattleMode != InValue && IsContainsParameter(AnimatorParameters.BATTLE_END))
                {
                    ChangeBattleEndState();

                    if (_ownerEntity != null)
                        _ownerEntity.OnPlayWeaponShowHide(false, BATTLE_END_ANI_TIME);
                }
                else
                    ChangeIdleState(_isBattleMode != InValue);
            }
        }

        SetBattleMode(InValue);
    }

    private void ChangeBattleEndState()
    {
        if (_animatorStateType != ANIMATOR_STATE_TYPE.BATTLE_END)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.BATTLE_END, true);
        }

        _animatorStateType = ANIMATOR_STATE_TYPE.BATTLE_END;
    }

    private void SetBattleMode(bool InBattle)
    {
        _isBattleMode = InBattle;
        if (_isBattleMode)
        {
            SetAnimatorTriggerParameterValue(AnimatorParameters.BATTLE_END, false);
            _animatorStateType = ANIMATOR_STATE_TYPE.BATTLE_IDLE;
        }

        SetAnimatorBoolParameterValue(AnimatorParameters.ON_BATTLE, _isBattleMode);
    }

    private bool IsUnableBattle()
    {
        if (OwnerEntity == null)
            return false;

        return OwnerEntity.OnCommand_RideOn;
    }

    public void ChangeAttackState(int InAttackType)
    {
        if (InAttackType > 0)
        {
            _attackParameter = InAttackType;
            SetAnimatorTriggerParameterValue(AnimatorParameters.ATTACK, true);
            SetAnimatorIntParameterValue(AnimatorParameters.ATTACK_TYPE, InAttackType);
        }
        else
        {
            ReleaseAttackState();
        }
    }

    public void ReleaseAttackState()
    {
        _attackParameter = 0;
        SetAnimatorIntParameterValue(AnimatorParameters.ATTACK_TYPE);
    }

    public void ChangeCastingState(int InCastingType, int InSkillType = 0)
    {
        if (InCastingType > 0)
        {
            ReleaseAttackState();
            SetBattleMode(true);
            SetAnimatorTriggerParameterValue(AnimatorParameters.CASTING, true);
            SetAnimatorIntParameterValue(AnimatorParameters.CASTING_TYPE, InCastingType);
            if (InSkillType > 0)
            {
                SetAnimatorIntParameterValue(AnimatorParameters.SKILL_TYPE, InSkillType);
                _skillParameter = InSkillType;
            }
        }
        else
        {
            ReleaseCastingState();
        }
    }

    public void ReleaseCastingState()
    {
        SetAnimatorIntParameterValue(AnimatorParameters.CASTING_TYPE);
    }

    public void ChangeSkillState(int InSkillType, bool InShortBlend = false)
    {
        if (InSkillType > 0)
        {
            ReleaseAttackState();
            ReleaseCastingState();
            SetBattleMode(true);
            if (_skillParameter != InSkillType)
                SetAnimatorTriggerParameterValue(InShortBlend ? AnimatorParameters.SKILL_0_05 : AnimatorParameters.SKILL, true);
            SetAnimatorIntParameterValue(AnimatorParameters.SKILL_TYPE, InSkillType);
            _skillParameter = InSkillType;
        }
        else
            ReleaseSkillState();
    }

    public void ChangeSkillSubState(int InSubType)
    {
        if (InSubType > 0)
            SetAnimatorIntParameterValue(AnimatorParameters.SKILL_SUB_TYPE, InSubType);
    }

    public void ReleaseSkillState()
    {
        _skillParameter = 0;
        ReleaseCastingState();
        SetAnimatorIntParameterValue(AnimatorParameters.SKILL_TYPE);
        SetAnimatorIntParameterValue(AnimatorParameters.SKILL_SUB_TYPE);
    }

    public void ChangeDeathState(bool InValue = true)
    {
        ResetAnimatorParameters(true);
        if (InValue)
            SetAnimatorTriggerParameterValue(AnimatorParameters.DEATH, true);
        SetAnimatorBoolParameterValue(AnimatorParameters.ON_DEATH, InValue);
        if (InValue)
            _animatorStateType = ANIMATOR_STATE_TYPE.DEATH;
    }

    public void ChangeHittedState(int InHitType)
    {
        if (InHitType > 0)
        {
            AllResetTriggerParametersForMove();
            SetAnimatorTriggerParameterValue(AnimatorParameters.HIT, true);
            SetAnimatorIntParameterValue(AnimatorParameters.HIT_TYPE, InHitType);
        }
        else
        {
            ReleaseHittedState();
        }
    }

    public void ReleaseHittedState()
    {
        SetAnimatorIntParameterValue(AnimatorParameters.HIT_TYPE);
    }

    public void ChangeAbnormalState(int InAbnormalType, bool InShortBlend = false)
    {
        if (InAbnormalType > 0)
        {
            ResetAnimatorParameters(true);
            SetAnimatorTriggerParameterValue(InShortBlend ? AnimatorParameters.ABNORMAL_0_05 : AnimatorParameters.ABNORMAL, true);
            SetAnimatorIntParameterValue(AnimatorParameters.ABNORMAL_STATUS_TYPE, InAbnormalType);
            _animatorStateType = ANIMATOR_STATE_TYPE.ABNORMAL;
        }
        else
        {
            ReleaseAbnormalState();
        }
    }

    public void ChangeAbnormalSubState(int InSubType)
    {
        if (InSubType > 0)
        {
            SetAnimatorIntParameterValue(AnimatorParameters.ABNORMAL_STATUS_SUB_TYPE, InSubType);
            _animatorStateType = ANIMATOR_STATE_TYPE.ABNORMAL;
        }
    }

    public void ReleaseAbnormalState()
    {
        SetAnimatorIntParameterValue(AnimatorParameters.ABNORMAL_STATUS_TYPE);
        SetAnimatorIntParameterValue(AnimatorParameters.ABNORMAL_STATUS_SUB_TYPE);
    }

    public void ChangeInteractionState(int InInteractionType)
    {
        if (InInteractionType > 0)
        {
            ResetAnimatorParameters(true);
            SetAnimatorTriggerParameterValue(AnimatorParameters.INTERACTION, true);
            SetAnimatorIntParameterValue(AnimatorParameters.INTERACTION_TYPE, InInteractionType);
            _animatorStateType = ANIMATOR_STATE_TYPE.INTERACTION;
        }
        else
        {
            ReleaseInteractionState();
        }
    }

    public void ReleaseInteractionState()
    {
        SetAnimatorIntParameterValue(AnimatorParameters.INTERACTION_TYPE);

        if (OwnerEntity.IsBattleMode)
            ChangeBattleState(true, true);
        else
            ChangeIdleState();
    }

    private bool IsContainsParameter(int InParameterNameHash)
    {
        if (_animatorParameterNameHashSet.IsNullOrEmpty())
            return false;

        return _animatorParameterNameHashSet.Contains(InParameterNameHash);
    }

    private void AllResetTriggerParametersForMove()
    {
        SetAnimatorTriggerParameterValue(AnimatorParameters.IDLE, false);
        SetAnimatorTriggerParameterValue(AnimatorParameters.IDLE_STAND, false);
        SetAnimatorTriggerParameterValue(AnimatorParameters.WALK, false);
        SetAnimatorTriggerParameterValue(AnimatorParameters.MOVE, false);
        SetAnimatorTriggerParameterValue(AnimatorParameters.BATTLE_END, false);
    }

    private void ResetAnimatorParameters(bool InApplyTrigger, bool InResetSpeed = false)
    {
        if (ModelAnimator == null)
            return;

        foreach (var animatorControllerParameterType in AnimatorParameters.AnimParameterTypeInfo)
        {
            var e = animatorControllerParameterType;

            var parameterNameHash = e.Key;

            switch (parameterNameHash)
            {
                case var _ when parameterNameHash == AnimatorParameters.ON_DEATH:
                    SetAnimatorBoolParameterValue(parameterNameHash, _ownerEntity?.IsDeath == true);
                    break;
                case var _ when parameterNameHash == AnimatorParameters.ON_BATTLE:
                    SetAnimatorBoolParameterValue(parameterNameHash, _ownerEntity?.IsBattleMode == true);
                    break;
                case var _ when parameterNameHash == AnimatorParameters.ON_RIDE:
                    SetAnimatorBoolParameterValue(parameterNameHash, _ownerEntity?.OnCommand_RideOn == true);
                    break;
                case var _ when parameterNameHash == AnimatorParameters.MOVE_BLEND:
                    break;
                case var _ when parameterNameHash == AnimatorParameters.RIDE_TYPE:
                    break;
                case var _ when parameterNameHash == AnimatorParameters.ATTACK_SPEED:
                case var _ when parameterNameHash == AnimatorParameters.SKILL_SPEED:
                case var _ when parameterNameHash == AnimatorParameters.MOVE_SPEED:
                    if (InResetSpeed)
                        SetAnimatorFloatParameterValue(parameterNameHash, 1f);
                    break;
                default:
                    SetAnimatorParameterDefaultValue(parameterNameHash, InApplyTrigger);
                    break;
            }
        }
    }

    private void ResetTriggerParameters()
    {
        if (ModelAnimator == null)
            return;

        foreach (var animatorControllerParameterType in AnimatorParameters.AnimParameterTypeInfo)
        {
            switch (animatorControllerParameterType.Value)
            {
                case AnimatorControllerParameterType.Trigger:
                    SetAnimatorTriggerParameterValue(animatorControllerParameterType.Key);
                    break;
            }
        }
    }

    private void ResetStateParameters(bool InMaintainBattle = false)
    {
        SetAnimatorBoolParameterValue(AnimatorParameters.ON_RIDE, false);
        if (InMaintainBattle == false)
            SetBattleMode(false);
    }

    private void SetAnimatorParameterDefaultValue(int InParameterNameHash, bool InApplyTrigger = false)
    {
        switch (AnimatorParameters.GetParameterType(InParameterNameHash))
        {
            case AnimatorControllerParameterType.Float:
                SetAnimatorFloatParameterValue(InParameterNameHash);
                break;
            case AnimatorControllerParameterType.Int:
                SetAnimatorIntParameterValue(InParameterNameHash);
                break;
            case AnimatorControllerParameterType.Bool:
                SetAnimatorBoolParameterValue(InParameterNameHash);
                break;
            case AnimatorControllerParameterType.Trigger:
                if (InApplyTrigger)
                    SetAnimatorTriggerParameterValue(InParameterNameHash);
                break;
        }
    }

    public void SetAnimatorFloatParameterValue(int InParameterNameHash, float InValue = 0f, bool InCheckContains = true)
    {
        if (!InCheckContains || IsContainsParameter(InParameterNameHash))
            ModelAnimator?.SetFloat(InParameterNameHash, InValue);
    }

    public void SetAnimatorIntParameterValue(int InParameterNameHash, int InValue = 0, bool InCheckContains = true)
    {
        if (!InCheckContains || IsContainsParameter(InParameterNameHash))
            ModelAnimator?.SetInteger(InParameterNameHash, InValue);
    }

    public void SetAnimatorBoolParameterValue(int InParameterNameHash, bool InValue = false, bool InCheckContains = true)
    {
        if (!InCheckContains || IsContainsParameter(InParameterNameHash))
            ModelAnimator?.SetBool(InParameterNameHash, InValue);
    }

    public void SetAnimatorTriggerParameterValue(int InParameterNameHash, bool InValue = false, bool InCheckContains = true)
    {
        if (!InCheckContains || IsContainsParameter(InParameterNameHash))
        {
            if (InValue)
                ModelAnimator?.SetTrigger(InParameterNameHash);
            else
                ModelAnimator?.ResetTrigger(InParameterNameHash);
        }
    }

    public T GetParameterValue<T>(int InParameterNameHash) where T : struct
    {
        var parameter = ModelAnimator.GetParameterFromHash(InParameterNameHash);
        if (parameter != null)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    return (T)Convert.ChangeType(ModelAnimator.GetFloat(InParameterNameHash), typeof(T));
                case AnimatorControllerParameterType.Int:
                    return (T)Convert.ChangeType(ModelAnimator.GetInteger(InParameterNameHash), typeof(T));
                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    return (T)Convert.ChangeType(ModelAnimator.GetBool(InParameterNameHash), typeof(T));
            }
        }

        return default;
    }

    public bool GetParameterValue<T>(int InParameterNameHash, out T InParameter) where T : struct
    {
        if (ModelAnimator.ContainsParameter(InParameterNameHash))
        {
            InParameter = GetParameterValue<T>(InParameterNameHash);
            return true;
        }

        InParameter = default;
        return false;
    }

    public float GetCurrentAnimationTime()
    {
        return ModelAnimator.runtimeAnimatorController != null ? ModelAnimator.GetCurrentAnimatorStateInfo(AnimatorLayerIndex.DEFAULT).length : 0f;
    }

    public float GetNextAnimationTime()
    {
        return ModelAnimator.runtimeAnimatorController != null ? ModelAnimator.GetNextAnimatorStateInfo(AnimatorLayerIndex.DEFAULT).length : 0f;
    }

    public float GetAnimationTime(string InAniName)
    {
        if (_templateNameHashAnimationClips.IsNullOrEmpty() || string.IsNullOrEmpty(InAniName))
            return 0;

        return _templateNameHashAnimationClips.TryGetValue(Animator.StringToHash(InAniName.ToLower()), out var ani) ? ani.length : 0;
    }

    public void Debug_BlendLog(bool blog)
    {
#if LOG_BLEND
        bool forcelog = Input.GetKey(KeyCode.O);
        if (blog == false && forcelog == false)
            return;

        if (_ownerEntity is MyPlayerEntity player)
        {
            var anistateinfo = ModelAnimator.GetCurrentAnimatorStateInfo(AnimatorLayerIndex.DEFAULT);
            var nextAniStateInfo = ModelAnimator.GetNextAnimatorStateInfo(AnimatorLayerIndex.DEFAULT);

            int stateNameHash = anistateinfo.fullPathHash;
            string layername = ModelAnimator.GetLayerName(AnimatorLayerIndex.DEFAULT);
            

            //AnimatorTransitionInfo transitionInfo = ModelAnimator.GetAnimatorTransitionInfo(AnimatorLayerIndex.DEFAULT);
          
            //RM2Logger.LogColorFormat("[{0}] [ani]: layer : {1} , weight {2} , IsInTranstion : {5} ,  transitionInfo : {3}, duration : {4} ", Color.yellow, Time.time, layername, ModelAnimator.GetLayerWeight(AnimatorLayerIndex.DEFAULT),
            //    transitionInfo.fullPathHash.ToString(), transitionInfo.duration, ModelAnimator.IsInTransition(AnimatorLayerIndex.DEFAULT));

            //int s2h_skill_4 = Animator.StringToHash("Skill_4");
            //int s2h_skill_4_1 = Animator.StringToHash("Skill_4_1");
            //int s2h_skill = Animator.StringToHash("Skill");

            //var curStateInfo = ModelAnimator.GetCurrentAnimatorStateInfo(AnimatorLayerIndex.DEFAULT);
            //var nextStateInfo = ModelAnimator.GetNextAnimatorStateInfo(AnimatorLayerIndex.DEFAULT);
            //RM2Logger.LogColorFormat("[{0}] [ani]: state name : skill : {1} , skill_4 : {2} , skill_4_1 : {3} ", Color.yellow, Time.time, 
            //    s2h_skill, s2h_skill_4, s2h_skill_4_1);

            //RM2Logger.LogColorFormat("[{0}] [ani]: cur state short: {1},  full: {2} ,  nextStae : short:{3} , full: {4} ", Color.yellow, Time.time,
            //    curStateInfo.shortNameHash,  curStateInfo.fullPathHash, nextStateInfo.shortNameHash, nextStateInfo.fullPathHash);



            var anis = ModelAnimator.GetCurrentAnimatorClipInfo(AnimatorLayerIndex.DEFAULT);
            if (anis != null && 0 < anis.Length)
            {
                foreach (var item in anis)
                {
                    RM2Logger.LogColorFormat("[{0}] [ani]: cur ani : {1} , weight : {2}", Color.yellow, Time.time, item.clip.name, item.weight);
                }
            }

            var nextanis = ModelAnimator.GetNextAnimatorClipInfo(AnimatorLayerIndex.DEFAULT);
            if (nextanis != null && 0 < nextanis.Length)
            {
                foreach (var item in nextanis)
                {
                    RM2Logger.LogColorFormat("[{0}] [ani]: next ani : {1} , weight : {2}", Color.yellow, Time.time, item.clip.name, item.weight);
                }
            }
        }
#endif
    }



    public void UpdateMoveBlendValueForStop()
    {
        _moveBlendParameter = UpdateMoveBlendWithSpeed(_moveBlendParameter, 0f);
    }

    public void SetBlinkAnimationParameter(int InValue)
    {
        SetAnimatorIntParameterValue(AnimatorParameters.IDLE_BLINK, InValue);
    }

    #region OnHitted --------------------------------------------------------------------------

    private const float HITTED_ROTATION_SPEED = 10f;
    private int _hittedRotation = 0;
    private Vector3 _hittedAngles;
    private Vector3 _targetForward;
    private bool _isForwardRotate;
    private Vector3 _restoreForward;
    private Coroutine _coroutine_Hitted = null;

    public void OnHitted(in Vector3 InAttackerPos, in int InHittedRotation)
    {
        if (InHittedRotation == 0) return;

        StopHitted();

        Vector3 targetPos = this.transform.position;
        targetPos.y = InAttackerPos.y;
        _restoreForward = (InAttackerPos - targetPos).normalized;
        _hittedRotation = InHittedRotation;
        this.transform.forward = _restoreForward;
        ChangeHittedState(1);
        _coroutine_Hitted = StartCoroutine(onHitted());
    }

    IEnumerator onHitted()
    {
        bool _forward2backward = false;
        float timeMax = Time.time + 3.0f;
        while (!(_forward2backward && ((this.transform.forward - _restoreForward).magnitude < 0.1f)) || Time.time < timeMax)
        {
            if (_ownerEntity == null) break;

            RotateHittedDirection();
            _forward2backward = !_isForwardRotate;
            yield return null;
        }

        ResetHitted();
    }

    public void StopHitted()
    {
        if (_coroutine_Hitted != null)
        {
            StopCoroutine(_coroutine_Hitted);
            ResetHitted();
        }
    }

    private void ResetHitted()
    {
        this.transform.forward = (_restoreForward == Vector3.zero) ? Vector3.forward : _restoreForward;

        _isForwardRotate = false;
        _targetForward = _restoreForward;
        _restoreForward = Vector3.zero;
        _hittedAngles = Vector3.zero;
    }

    private void RotateHittedDirection()
    {
        if (_ownerEntity.HittedAngles != Vector3.zero && _hittedAngles != _ownerEntity.HittedAngles)
        {
            _isForwardRotate = true;
            _hittedAngles = _ownerEntity.HittedAngles;

            float angle = Vector3.zero.CalculateAngle(_hittedAngles) > 0 ? -_hittedRotation : _hittedRotation;
            _targetForward = (Quaternion.AngleAxis(angle, this.transform.up) * this.transform.forward).normalized;
        }

        if (_targetForward != Vector3.zero)
        {
            if (_isForwardRotate)
                this.transform.forward = Vector3.Lerp(this.transform.forward, _targetForward, Time.deltaTime * HITTED_ROTATION_SPEED);
            else
                this.transform.forward = Vector3.Lerp(this.transform.forward, _restoreForward, Time.deltaTime * HITTED_ROTATION_SPEED);

            if (_isForwardRotate && (this.transform.forward - _targetForward).magnitude < 0.1f)
                _isForwardRotate = false;
        }
    }
    #endregion
}

