/**
* StatusEffect_Movement.cs
* 작성자 : songch
* 작성일 : 2024-03-14 오후 5:03:00
*/

using UnityEngine;

public class StatusEffect_Movement : StatusEffect
{
    public Vector3 CurrentPosition { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public Vector3 CurrentDirection { get; private set; }
    public Vector3 TargetDirection { get; private set; }

    protected virtual float MovementProcessRatio => ProcessRatio;
    protected bool IsFinishMovenment => MovementProcessRatio == 1f;

    public AnimationCurve AnimationCurve { get; private set; }

    public StatusEffect_Movement(in BaseEntity InOwner, in BuffClient InBuff) : base(InOwner, InBuff)
    {
    }

    public override void SetBuff(BuffClient InBuff)
    {
        base.SetBuff(InBuff);

        AnimationCurve = Defines.Animation.GetAnimationCurve(E_ANIMATION_INFO.SKILL_EFFECT_POSITION, SkillEffectData?.CURVE_TYPE ?? SKILL_CURVE_TYPE.NULL);
    }

    public void SetMovementData(Vector3 InCurrentPosition, Vector3 InTargetPosition, Vector3 InCurrentDirection, Vector3 InTargetDirection)
    {
        CurrentPosition = InCurrentPosition;
        TargetPosition = InTargetPosition;
        CurrentDirection = InCurrentDirection;
        TargetDirection = InTargetDirection;
    }

    public override void Apply()
    {
        var entity = Owner;
        if (entity == null)
            return;

        base.Apply();

        MoveToPosition(entity);
    }

    public override void Finish()
    {
        if( Owner != null && IsProcessEnsuredType(EffectType) )
        {
            MoveToPosition(Owner, IsCompleted==false);
        }

        base.Finish();

    }

    private void MoveToPosition(in BaseEntity InEntity, bool InForceFinish=false)
    {
        var movement = InEntity.MoveComponent;
        if (movement == null)
        {
            return;
        }

        bool setPosToGoal = false;
        var progress = MovementProcessRatio;
        if (InForceFinish)
            progress = 1f;


        if ( 1f <= progress && movement.m_UseFrameMove)
        {
            var distance = CurrentPosition.ToDistanceYZero(TargetPosition);
            setPosToGoal = 0.1 < distance;
            if (setPosToGoal == false)
            {
                return;
            }
        }

        movement.m_UseFrameMove = false;

        var targetPosition = Vector3.Lerp(CurrentPosition, TargetPosition,
            AnimationCurve != null ? AnimationCurve.Evaluate(progress) : progress);
        movement.MoveDelta = targetPosition - InEntity.Position;
        movement.SetPosition(targetPosition, TargetDirection, false);
        movement.UpdateRotation();

        if (progress == 1f)
        {
            movement.m_UseFrameMove = true;
            IsCompleted = true;
        }
    }
}