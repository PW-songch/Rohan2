using UnityEngine;
using System;
using Repository.Model;
using Defines;

public class StatusEffect
{
    public BuffClient BuffInfo { get; private set; }
    public SKILL_EFFECT_TYPE EffectType { get; private set; }

    public float Duration => BuffInfo.Duration;

    // TODO : 민규???�인 ?�요

    public bool IsUseAnimation { get; private set; }
    public bool IsPlayAnimation { get; private set; }

    private WeakReference<BaseEntity> _owner;
    protected BaseEntity Owner => _owner != null && _owner.TryGetTarget(out var entity) ? entity : null;

    protected float _duration;
    protected float _elapsedTime;

    public long ID => BuffInfo.BuffInstanceID;
    public float ProcessRatio => IsFinish ? 1f : Mathf.Clamp(_elapsedTime / _duration, 0f, 1f);
    public bool IsFinish => BuffInfo.Duration == 0f;
    public TdSkillEffect SkillEffectData => BuffInfo != null ? RepositoryContext.SKILL_EFFECT.Get(BuffInfo.SkillLinkData.SKILLEFFECT_INDEX) : null;
    public TdSkillEffectResource SkillEffectResourceData => BuffInfo != null ? RepositoryContext.SKILL_EFFECT_RESOURCE.Get(BuffInfo.SkillLinkData.RESOURCE_INDEX) : null;

    public bool IsCompleted { get; protected set; }

    public bool IsDisableCommand
    {
        get
        {
            if( BuffInfo != null && BuffInfo.SkillEffectData != null )
            {
                var effectData = BuffInfo.SkillEffectData;
                if (effectData.CAN_NOT_CONTROL)
                    return true;
            }

            switch (EffectType)
            {
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKBACK_VALUE:
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKDOWN_VALUE:
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_PULL_VALUE:
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_STUN:
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_SLEEP:
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_FREEZE_RATE:
                    return true;
            }

            return false;
        }
    }
    public static bool IsProcessEnsuredType(SKILL_EFFECT_TYPE InEffectType)
    {
        switch( InEffectType)
        {
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKBACK_VALUE:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_PULL_VALUE:
                return true;
        }
        return false;
    }

    public static StatusEffect CreateStatusEffect(in BaseEntity InOwner, in BuffClient InBuff)
    {
        switch (RepositoryContext.SKILL_EFFECT.Get(InBuff.SkillLinkData.SKILLEFFECT_INDEX)?.SKILL_EFFECT_TYPE ?? SKILL_EFFECT_TYPE.SET_NULL)
        {
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKBACK_VALUE:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_PULL_VALUE:
                return new StatusEffect_Movement(InOwner, InBuff);
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKDOWN_VALUE:
                return new StatusEffect_KnockDown(InOwner, InBuff);
            default:
                return new StatusEffect(InOwner, InBuff);
        }
    }

    public StatusEffect(in BaseEntity InOwner, in BuffClient InBuff)
    {
        _owner = new WeakReference<BaseEntity>(InOwner);
        
        SetBuff(InBuff);
    }

    public virtual void SetBuff(BuffClient InBuff)
    {
        if (BuffInfo == null || BuffInfo.ExpireTime != InBuff.ExpireTime)
            _duration = InBuff.Duration;

        BuffInfo = InBuff;
        EffectType = RepositoryContext.SKILL_EFFECT.Get(BuffInfo.SkillLinkData.SKILLEFFECT_INDEX)?.SKILL_EFFECT_TYPE ?? SKILL_EFFECT_TYPE.SET_NULL;
        _elapsedTime = 0;
        IsCompleted = false;
    }

    public void SetUseAnimation(bool InUseAnimation)
    {
        IsUseAnimation = InUseAnimation;
        if (IsUseAnimation == false)
            IsPlayAnimation = false;
    }

    public virtual void Start()
    {
        _elapsedTime = 0;
    }

    public void Update(float InTimeDelta)
    {
        Apply();
        _elapsedTime += InTimeDelta;
    }

    public virtual void Apply()
    {
        var entity = Owner;
        if (entity == null)
            return;

        PlayAnimation(entity.ModelBase);
    }

    protected virtual void PlayAnimation(in ModelBase InModel)
    {
        if (InModel != null && IsUseAnimation && !IsPlayAnimation)
        {
            switch (EffectType)
            {
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_STUN:
                    IsPlayAnimation = true;
                    InModel.ChangeAbnormalState(1);
                    break;
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_SLEEP:
                    IsPlayAnimation = true;
                    InModel.ChangeAbnormalState(2);
                    break;
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_PULL_VALUE:
                    IsPlayAnimation = true;
                    InModel.ChangeAbnormalState(3, true);
                    break;
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKBACK_VALUE:
                    IsPlayAnimation = true;
                    InModel.ChangeAbnormalState(4, true);
                    break;
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKDOWN_VALUE:
                    IsPlayAnimation = true;
                    InModel.ChangeAbnormalState(5, true);
                    break;
                case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_FREEZE_RATE:
                    IsPlayAnimation = true;
                    InModel.PauseAnimation(true);
                    break;
            }
        }
    }

    public virtual void Finish()
    {
        _elapsedTime = _duration;
        IsPlayAnimation = true;

        switch (EffectType)
        {
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_FREEZE_RATE:
                Owner?.ModelBase?.PauseAnimation(false);
                break;
        }
    }

    public static bool IsStatusEffectType(SKILL_EFFECT_TYPE InType)
    {
        switch (InType)
        {
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKBACK_VALUE:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_KNOCKDOWN_VALUE:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_PULL_VALUE:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_STUN:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_SLEEP:
            case SKILL_EFFECT_TYPE.SET_ABNORMAL_DEBUFF_FREEZE_RATE:
                return true;
        }

        return false;
    }
}
