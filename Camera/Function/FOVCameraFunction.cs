using UnityEngine;
using Cinemachine;
using System;
using Repository.Model;

[Serializable]
public class FOVCameraFunction : CinemachineCameraFunction
{
    [SerializeField] private float MinFOV;
    [SerializeField] private float MaxFOV;

    private float _minDistance;
    private float _maxDistance;
    private float _defaultDistance;
    
    private float _lastUpdateFOV;
    public float LastUpdateFOV
    {
        get => _lastUpdateFOV;
    }

    public FOVCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        _minDistance = InSetting.CAMERADISTANCE_MIN;
        _maxDistance = InSetting.CAMERADITANCE_MAX;
        _defaultDistance = InSetting.CAMERADISTANCE_DEFAULT;

        MinFOV = InSetting.FOV_MIN;
        MaxFOV = InSetting.FOV_MAX;

        InternalSetup(InReset);
    }

    public override void Setup(TdOutgameCharacterCamera InSetting)
    {
        base.Setup(InSetting);

        _minDistance = InSetting.CAMERADISTANCE_MIN;
        _maxDistance = InSetting.CAMERADITANCE_MAX;
        _defaultDistance = InSetting.CAMERADISTANCE_DEFAULT;

        MinFOV = InSetting.FOV_MIN;
        MaxFOV = InSetting.FOV_MAX;

        InternalSetup();
    }

    protected override void InternalSetup(bool InReset = false)
    {
        base.InternalSetup(InReset);

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (virtualCamera == null)
            return;

        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(MinFOV, MaxFOV,
            _maxDistance != _minDistance ? (_defaultDistance - _minDistance) / (_maxDistance - _minDistance) : 1f);
    }

    public override void SetToCurrent()
    {
        base.SetToCurrent();

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (virtualCamera == null)
            return;

        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(MinFOV, MaxFOV,
            _maxDistance != _minDistance ? (CameraStateData.CurrentZoomDistance - _minDistance) / (_maxDistance - _minDistance) : 1f);
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage == CinemachineCore.Stage.Noise || MaxFOV == 0 || MinFOV > MaxFOV)
            return false;

        var curFov = Mathf.Lerp(MinFOV, MaxFOV, _maxDistance != _minDistance ? (CameraStateData.CurrentZoomDistance - _minDistance) / (_maxDistance - _minDistance) : 1f);
        if (curFov == _lastUpdateFOV)
            return false;
        
        InState.Lens.FieldOfView = curFov;
        _lastUpdateFOV = curFov;

        PostPipelineStageProcess();

        return true;
    }
}
