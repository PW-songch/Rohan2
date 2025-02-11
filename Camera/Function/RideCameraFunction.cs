using Cinemachine;
using UnityEngine;
using System;
using static GameOptionsManager;

[Serializable]
public class RideCameraFunction : CinemachineCameraFunction
{
    private bool _isOnRide = false;
    private float _baseFov = 0f;
    private float _acceleration = 0f;
    private float _lastUpdateFOV = 0f;
    private float _updateAddFOV = 0f;
    private float _blurIntensityMax = 0.7f;
    private Vector3 _lastPosition;

    public float AddFovMin = 6f;
    public float AddFovMax = 20f;

    public float FovIncreaseSpeed = 3f;
    public float FovIncreaseTime = 1f;

    public float FovDecreaseSpeed = 30f;
    public float FovDecreaseTime = 0.5f;

    public float AngleThreadhold = 0.7f;

    public float LastUpdateFOV
    {
        get => _lastUpdateFOV;
    }

    private float GetTargetAddFOV(float InMaxSpeed)
    {
        var targetAddFov = AddFovMin;
        return Mathf.Min(AddFovMax, targetAddFov);
    }

    private enum Phase
    {
        Idle,
        Acceleration,
        Deceleration,
    }

    private Phase _phase = Phase.Idle;
    private bool _isMotionBlurAvailable = false;

    public RideCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera,
        float InEpsilon) : base(InCameraExtension, in InVirtualCamera, InEpsilon)
    {
        _isMotionBlurAvailable = LogicContext.GAME_OPTIONS.GraphicQualityType() ==
                                 GameOptionsManager.EGRAPHICS_GRAPHIC_QUALITY.ULTRA;

        LogicContext.GAME_OPTIONS.AddEventPostApply(EGAME_OPTIONS_KEY.GRAPHICS_GRAPHIC_QUALITY, OnChangeGraphicQuality);
    }

    private void OnChangeGraphicQuality(string InGraphicQuality)
    {
        var mode = (EGRAPHICS_GRAPHIC_QUALITY)LogicContext.GAME_OPTIONS.GetGraphicQualityType(InGraphicQuality);
        var motionBlurAvailable = mode == EGRAPHICS_GRAPHIC_QUALITY.ULTRA;
        if (false == motionBlurAvailable)
            UpdateMotionBlurEffect(0f);

        _isMotionBlurAvailable = motionBlurAvailable;
    }

    private void SetPhase(Phase InPhase)
    {
        if (_phase == InPhase)
            return;

        _phase = InPhase;
        _acceleration = 0f;
    }

    private float _velocity = 0.0f;
    private bool UpdateRideFOV(BaseEntity InPlayerEntity, CinemachineVirtualCameraBase InVcam, CameraState InState)
    {
        if (InPlayerEntity == null)
            return false;

        if (InVcam == null)
            return false;

        if (!CheckRideStatus(InPlayerEntity))
            return false;

        var currentPosition = InPlayerEntity.Position;
        var speed = (currentPosition - _lastPosition).sqrMagnitude;
        var moveDirection = InPlayerEntity.MoveComponent.Direction;
        var cameraAngle = Vector3.Dot(InVcam.transform.forward, moveDirection);
        _lastPosition = currentPosition;

        if (_isOnRide && _phase == Phase.Idle)
        {
            if (cameraAngle > AngleThreadhold && speed > 0f)
                OnMoveStart(InPlayerEntity);
        }
        else if (_isOnRide && _phase == Phase.Deceleration)
        {
            if (cameraAngle > AngleThreadhold && speed > 0f)
            {
                OnMoveStart(InPlayerEntity);
            }
        }
        else if (_phase == Phase.Acceleration)
        {
            if (cameraAngle <= AngleThreadhold || speed <= 0f)
            {
                OnMoveStop();
            }
        }

        if(_phase == Phase.Idle)
            return false;

        if (_phase == Phase.Acceleration)
        {
            _acceleration += FovIncreaseSpeed * Time.deltaTime * cameraAngle;
            _updateAddFOV = Mathf.SmoothDamp(_updateAddFOV, AddFovMin, ref _velocity, FovIncreaseTime);

            if(_updateAddFOV < AddFovMin)
                _velocity += _acceleration;

            _lastUpdateFOV = _baseFov + _updateAddFOV;
            VirtualCamera.m_Lens.FieldOfView = _lastUpdateFOV;
            UpdateMotionBlurEffect(_blurIntensityMax * cameraAngle);
        }
        else if (_phase == Phase.Deceleration)
        {
            _updateAddFOV = Mathf.SmoothDamp(_updateAddFOV, 0f, ref _velocity, FovDecreaseTime, FovDecreaseSpeed);

            _lastUpdateFOV = _baseFov + _updateAddFOV;
            VirtualCamera.m_Lens.FieldOfView = _lastUpdateFOV;
            UpdateMotionBlurEffect(_blurIntensityMax * cameraAngle);
            if (_updateAddFOV <= 0.05f)
            {
                VirtualCamera.m_Lens.FieldOfView = _baseFov;
                UpdateMotionBlurEffect(0f);
                SetPhase(Phase.Idle);
            }
        }
        return true;
    }

    private void UpdateMotionBlurEffect(float InIntensity)
    {
        if(_isMotionBlurAvailable)
            LogicContext.CAMERA.UpdateMotionBlurEffect(InIntensity);
    }

    private bool CheckRideStatus(BaseEntity playerEntity)
    {
        if (!_isOnRide)
        {
            if (LogicContext.RIDE.IsCanRideOff(playerEntity))
                OnRideOn(playerEntity);
            else if(_phase != Phase.Deceleration)
                return false;
        }
        else
        {
            if (!LogicContext.RIDE.IsCanRideOff(playerEntity))
                OnRideOff();
        }

        return true;
    }

    private void OnRideOn(BaseEntity InPlayerEntity)
    {
        _isOnRide = true;
        _lastUpdateFOV = _baseFov = VirtualCamera.m_Lens.FieldOfView;
    }

    private void OnRideOff()
    {
        _updateAddFOV = _lastUpdateFOV - _baseFov;
        _acceleration = 0f;
        _isOnRide = false;

        if(_phase == Phase.Acceleration)
            SetPhase(Phase.Deceleration);
    }

    private void OnMoveStart(BaseEntity InPlayerEntity)
    {
        var characterInfo = LogicContext.STORAGE.CharacterInfo;
        if (null == characterInfo)
            return;

        var idx = characterInfo.RideInfo.TdIndex;
        if (!LogicContext.RIDE.RideDatas.ContainsKey(idx))
            return;

        var rideData = LogicContext.RIDE.RideDatas[idx];
        if (null == rideData)
            return;

        var playerPos = InPlayerEntity.Position;
        if(Vector3.positiveInfinity == playerPos)
            return;

        _lastPosition = playerPos;
        //_rideSpeed = rideData.TdRideInfo.MOVE_SPEED;
        _acceleration = 0f;
        SetPhase(Phase.Acceleration);
    }

    private void OnMoveStop()
    {
        _lastUpdateFOV = _baseFov;
        _acceleration = 0f;
        SetPhase(Phase.Deceleration);
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage,
        ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage != CinemachineCore.Stage.Noise)
            return false;

        var playerEntity = EntityController.MyCharacter;
        if (null == playerEntity)
            return false;

        if (!UpdateRideFOV(playerEntity, InVcam, InState))
            return false;

        PostPipelineStageProcess();

        return true;
    }
}