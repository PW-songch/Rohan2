using UnityEngine;
using Cinemachine;
using Repository.Model;
using System;

[Serializable]
public class LookAtCameraFunction : CinemachineCameraFunction
{
    private float _limitAngle;
    private float _stareDistance;
    private bool _isUse = true;
    private ECAMERA_INUSE _useType = ECAMERA_INUSE.LOBBY_SELECT;

    private Transform _followTransform;

    public LookAtComponent LookAtComponent;

    public LookAtCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void OnEnable()
    {
        LookAtComponent?.UpdateData(_limitAngle, _stareDistance);
    }

    public override void OnDisable() { }


    public void Setup(TdCharacterCameraLookAt InData)
    {
        _limitAngle = InData.ROTATION_YAW;
        _stareDistance = InData.CAMERADISTANCE_LOOKAT;

        if(InData.TRIBE_CODE == CHAR_TRIBE.CT_HALFELF)
            _isUse = false;
    }

    public override void Setup(TdOutgameCharacterCamera InData)
    {
        _useType = (ECAMERA_INUSE)InData.CAMERA_USE;


        _limitAngle = InData.ROTATION_YAW;
        _stareDistance = InData.CAMERADISTANCE_LOOKAT;

        _isUse = InData.CHAR_TRIBE_CODE == CHAR_TRIBE.CT_HALFELF ? false : true;
    }

    public override void SetFollowTransform(in Transform InFollowTransform)
    {
        if(_isUse == false)
            return;

        _followTransform = InFollowTransform;
        LookAtComponent = _followTransform.gameObject.GetOrAddComponent<LookAtComponent>();

        LookAtComponent.Setup(_followTransform, LogicContext.CAMERA.CinemachineBrain.transform, _limitAngle, _stareDistance);
    }

    public override void Pause()
    {
        base.Pause();
        LookAtComponent?.SetEnable(false);
    }

    public override void Resume()
    {
        base.Resume();
        LookAtComponent?.SetEnable(true);
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        bool result = base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime);
        PostPipelineStageProcess();
        return result;
    }
}
