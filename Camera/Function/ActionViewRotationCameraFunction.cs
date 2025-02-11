using UnityEngine;
using Cinemachine;
using System;
using Repository.Model;

[Serializable]
public class ActionViewRotationCameraFunction : RotationCameraFunction
{
    [Header("ACTION VIEW")]
    public Vector2 ScreenPosition = new Vector2(0.45f, 0.55f);

    private readonly Vector3 FORWARD_ROTATION_OFFSET = new Vector3(10, -10, 0);
    private readonly Vector3 BATTLE_FORWARD_ROTATION_OFFSET = new Vector3(10, -20, 0);

    public ActionViewRotationCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        // 주석 내용 테이블값 적용 필요
        //ScreenPosition.x = InSetting.X;
        //ScreenPosition.y = InSetting.Y;
        //ScreenPosition.z = InSetting.Z;

        _framingTransposer.m_ScreenX = ScreenPosition.x;
        _framingTransposer.m_ScreenY = ScreenPosition.y;
    }

    protected override void InternalSetup(bool InReset = false)
    {
        base.InternalSetup(InReset);

        ForwardRotationOffset = FORWARD_ROTATION_OFFSET;
        BattleForwardRotationOffset = BATTLE_FORWARD_ROTATION_OFFSET;

        if (InReset == false)
            RotationToFollowTargetForward(MyPlayerEntity);
    }

    public override void ChangeViewMode(bool InChange)
    {
        base.ChangeViewMode(InChange);

        //if (InChange == false && IsActive == false)
        //    RotationToFollowTargetForward(MyPlayerEntity);
    }

    protected override bool DragRotation(float InDeltaTime)
    {
        if (base.DragRotation(InDeltaTime))
        {
            VirtualCamera.LookAt = null;
            return true;
        }

        return false;
    }

    protected override Vector3 GetFollowTargetForwardRotation(CinemachineVirtualCamera InVirtualCamera, MyPlayerEntity InPlayerEntity)
    {
        Vector3 rotation = InVirtualCamera.transform.localEulerAngles;
        Vector3 lookRotation = Quaternion.LookRotation(InPlayerEntity.Forward).eulerAngles;
        rotation.x = lookRotation.x;
        rotation.y = lookRotation.y;
        return rotation;
    }

    protected override void RotationToFollowTargetForwardProcess(float InDeltaTime)
    {
        if (!_isClick)
        {
            MyPlayerEntity playerEntity = MyPlayerEntity;
            if (playerEntity != null)
            {
                if (!playerEntity.OnMove)
                {
                    if (IsEnableRoationSync())
                        RotationToFollowTargetForward(playerEntity, GetRoationSpeed(playerEntity), InDeltaTime);
                }
                else
                    _inputTime = Time.realtimeSinceStartup;
            }
        }
    }

    protected override bool RestoreDefaultSetting(float InDeltaTime)
    {
        return true;
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || _framingTransposer == null)
            return false;

        _framingTransposer.m_ScreenX = ScreenPosition.x;
        _framingTransposer.m_ScreenY = ScreenPosition.y;
        return true;
    }
}
