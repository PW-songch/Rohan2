using Cinemachine;
using Repository.Model;
using System;
using UnityEngine;

[Serializable]
public class QuarterViewRotationCameraFunction : RotationCameraFunction
{

    public QuarterViewRotationCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        FollowTargetForwardSyncDelay = InSetting.DEFAULT_ROLLBACK_TIME;
    }

    protected override void InternalSetup(bool InReset = false)
    {
        base.InternalSetup(InReset);

        if (InReset == false)
            RotationToFollowTargetForward(MyPlayerEntity);
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
        if (!LogicContext.GAME_OPTIONS.CameraAutoTrailing)
            return false;

        if (!CamExtension.IsCollisioning && IsEnableRestoreDefaultSetting())
        {
            _isInput = _isClick;

            MyPlayerEntity playerEntity = MyPlayerEntity;
            if (playerEntity != null)
            {
                RotationToFollowTargetForward(playerEntity, InDeltaTime: InDeltaTime);
                return true;
            }

            return base.RestoreDefaultSetting(InDeltaTime);
        }

        _isInput = _isClick;
        return false;
    }
}
