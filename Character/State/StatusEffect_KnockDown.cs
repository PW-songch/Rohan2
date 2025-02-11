/**
* StatusEffect_KnockDown.cs
* 작성자 : songch
* 작성일 : 2024-03-15 오후 4:46:32
*/

using Repository.Model;
using UnityEngine;

public class StatusEffect_KnockDown : StatusEffect_Movement
{
    private enum EKOCKDOWN_STATE
    {
        KNOCKBACK,
        KNOCKDOWN,
        STAND_UP,
        SHOW_WEAPON
    }

    private EKOCKDOWN_STATE _state;
    private float _knockBackMoveTime;
    private float _knockBackAnimationTime;
    private float _downStateTime;

    protected override float MovementProcessRatio => IsFinish ? 1f : Mathf.Clamp(_elapsedTime / _knockBackMoveTime, 0f, 1f);

    private const float FALL_DOWN_MOVE_TIME = 0.1f;
    private const float WEAPON_SHOW_DURATION = 0.2f;
    private const float WEAPON_SHOW_TIME = 0.5f;

    public StatusEffect_KnockDown(in BaseEntity InOwner, in BuffClient InBuff) : base(InOwner, InBuff)
    {
    }

    public override void Start()
    {
        base.Start();
        Owner?.SetHideWeapon(true);
    }

    public override void SetBuff(BuffClient InBuff)
    {
        base.SetBuff(InBuff);

        _state = EKOCKDOWN_STATE.KNOCKBACK;

        _knockBackAnimationTime = BuffInfo.SkillLinkData.EFFECT_ARGUMENT_2 / 1000f - RepositoryContext.CONST.KNOCKDOWN_LOOP_END;
        _knockBackMoveTime = _knockBackAnimationTime + FALL_DOWN_MOVE_TIME;
        _downStateTime = Mathf.Max(0f, Duration - RepositoryContext.CONST.KNOCKDOWN_END);
    }

    protected override void PlayAnimation(in ModelBase InModel)
    {
        base.PlayAnimation(InModel);

        if (InModel != null && IsUseAnimation)
        {
            switch (_state)
            {
                case EKOCKDOWN_STATE.KNOCKBACK:
                    if (_elapsedTime >= _knockBackAnimationTime)
                    {
                        InModel.ChangeAbnormalSubState(1);
                        _state = EKOCKDOWN_STATE.KNOCKDOWN;
                    }
                    break;
                case EKOCKDOWN_STATE.KNOCKDOWN:
                    if (_elapsedTime >= _downStateTime)
                    {
                        InModel.ChangeAbnormalSubState(2);
                        _state = EKOCKDOWN_STATE.STAND_UP;
                    }
                    break;
                case EKOCKDOWN_STATE.STAND_UP:
                    if (Duration < WEAPON_SHOW_TIME)
                    {
                        Owner?.OnPlayWeaponShowHide(true, WEAPON_SHOW_DURATION);
                        _state = EKOCKDOWN_STATE.SHOW_WEAPON;
                    }
                    break;
                case EKOCKDOWN_STATE.SHOW_WEAPON:
                    break;
            }
        }
    }
}