using UnityEngine;
using Cinemachine;
using Repository.Model;
using System;

[Serializable]
public class ZoomCameraFunction : CinemachineCameraFunction
{
    [Header("DISTANCE")]
    public float MinDistance;
    public float MaxDistance;
    public float DefaultDistance;

    [Header("TRACKED OFFSET")]
    public Vector3 WaitMinTrackedOffset;
    public Vector3 WaitMaxTrackedOffset;

    public Vector3 BattleMinTrackedOffset;
    public Vector3 BattleMaxTrackedOffset;

    [Space(10)]
    [Header("SPEED")]
    public float ZoomSpeed;

    public float SmoothTime = 0.3f;

    private WeakReference<MyPlayerEntity> _myPlayerEntityRef = null;

    private float _currentZoomDistance;
    private float _moveVelocity;
    private float _rideLimitMinDistance;
    private bool _useZoom;
    private bool _useCollision;

    public float CalculatedMinDistance => _rideLimitMinDistance > 0f ? MinDistance + _rideLimitMinDistance : MinDistance;
    public float DistanceRate => GetDistanceRate(_framingTransposer != null ? _framingTransposer.m_CameraDistance : CameraStateData.CurrentZoomDistance);
    public bool SetUseCollision { set { _useCollision = value; } }

    public ZoomCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        _useZoom = InSetting.USE_ZOOM;

        MinDistance = InSetting.CAMERADISTANCE_MIN;
        MaxDistance = InSetting.CAMERADITANCE_MAX;
        DefaultDistance = InSetting.CAMERADISTANCE_DEFAULT;

        WaitMinTrackedOffset.x = InSetting.TRAKEDPOSITION_BASE_X_WAIT;
        WaitMinTrackedOffset.y = InSetting.TRAKEDPOSITION_BASE_Y_WAIT;
        WaitMinTrackedOffset.z = InSetting.TRAKEDPOSITION_BASE_Z_WAIT;

        WaitMaxTrackedOffset.x = InSetting.TRACKEDPOSITION_OFFSET_X_WAIT;
        WaitMaxTrackedOffset.y = InSetting.TRACKEDPOSITION_OFFSET_Y_WAIT;
        WaitMaxTrackedOffset.z = InSetting.TRACKEDPOSITION_OFFSET_Z_WAIT;

        BattleMinTrackedOffset.x = InSetting.TRAKEDPOSITION_BASE_X_BATTLE;
        BattleMinTrackedOffset.y = InSetting.TRAKEDPOSITION_BASE_Y_BATTLE;
        BattleMinTrackedOffset.z = InSetting.TRAKEDPOSITION_BASE_Z_BATTLE;

        BattleMaxTrackedOffset.x = InSetting.TRACKEDPOSITION_OFFSET_X_BATTLE;
        BattleMaxTrackedOffset.y = InSetting.TRACKEDPOSITION_OFFSET_Y_BATTLE;
        BattleMaxTrackedOffset.z = InSetting.TRACKEDPOSITION_OFFSET_Z_BATTLE;

        ZoomSpeed = InSetting.CAMERADISTANCE_SPEED;

        _useCollision = InSetting.USE_COLLISION;

        InternalSetup(InReset);
    }

    public override void Setup(TdOutgameCharacterCamera InSetting)
    {
        base.Setup(InSetting);

        MinDistance = InSetting.CAMERADISTANCE_MIN;
        MaxDistance = InSetting.CAMERADITANCE_MAX;
        DefaultDistance = InSetting.CAMERADISTANCE_DEFAULT;

        WaitMinTrackedOffset.x = InSetting.TRAKEDPOSITION_BASE_X;
        WaitMinTrackedOffset.y = InSetting.TRAKEDPOSITION_BASE_Y;
        WaitMinTrackedOffset.z = InSetting.TRAKEDPOSITION_BASE_Z;

        WaitMaxTrackedOffset.x = InSetting.TRACKEDPOSITION_OFFSET_X;
        WaitMaxTrackedOffset.y = InSetting.TRACKEDPOSITION_OFFSET_Y;
        WaitMaxTrackedOffset.z = InSetting.TRACKEDPOSITION_OFFSET_Z;

        ZoomSpeed = InSetting.CAMERADISTANCE_SPEED;

        _useCollision = false;

        InternalSetup();
    }

    protected override void InternalSetup(bool InReset = false)
    {
        base.InternalSetup(InReset);

        if (_framingTransposer != null)
        {
            if (InReset == false)
                _framingTransposer.m_CameraDistance = DefaultDistance;
            else
                CameraStateData.OriginZoomDistance = DefaultDistance;

            _currentZoomDistance = DefaultDistance;
            _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(DefaultDistance);
        }
    }

    public override void SetToCurrent()
    {
        base.SetToCurrent();

        if (_framingTransposer != null)
        {
            _currentZoomDistance = CameraStateData.CurrentZoomDistance;
            _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(CameraStateData.CurrentZoomDistance);
        }
    }

    public override void SetFollowTransform(in Transform InFollowTransform)
    {
        if (InFollowTransform == null)
            return;

        if (LogicContext.STAGE.IsLobbyStage == true)
            return;

        var myPlayerEntity = EntityController.MyCharacter;
        if (myPlayerEntity == null)
            return;

        _myPlayerEntityRef = new WeakReference<MyPlayerEntity>(myPlayerEntity);
    }

    protected override bool RestoreDefaultSetting(float InDeltaTime)
    {
        if (!LogicContext.GAME_OPTIONS.CameraAutoZoom)
            return false;

        bool isCollisioning = CamExtension.IsCollisioning;

        if (base.RestoreDefaultSetting(InDeltaTime))
        {
            CameraStateData.OriginZoomDistance = DefaultDistance;
            if (!isCollisioning)
                _currentZoomDistance = CameraStateData.CurrentZoomDistance;
            return true;
        }
        else if (_isInput && HasRestoreDefaultSettingData())
            CameraStateData.OriginZoomDistance = _currentZoomDistance;

        if (!isCollisioning)
            _currentZoomDistance = CameraStateData.OriginZoomDistance;

        return false;
    }

    protected override void OnChangeScrollValue_Event(float InValue)
    {
        if (!_useZoom || _isChangeViewMode || InValue == 0)
            return;

        if (_framingTransposer == null)
            return;

        base.OnChangeScrollValue_Event(InValue);

        InValue *= ZoomSpeed;

#if !UNITY_EDITOR
        InValue *= RepositoryContext.CONST.ZOOM_SENSIBILITY;
#endif

        InValue *= -1;

        CameraStateData.OriginZoomDistance = Mathf.Clamp(CameraStateData.OriginZoomDistance + InValue, CalculatedMinDistance, MaxDistance);
        _currentZoomDistance = CameraStateData.OriginZoomDistance;
    }

    protected Vector3 GetTrackedOffset(in float InDistance)
    {
        bool isBattle = false;
        MyPlayerEntity myEntity = null;

        _rideLimitMinDistance = 0f;

        if (_myPlayerEntityRef != null)
        {
            if (_myPlayerEntityRef.TryGetTarget(out myEntity))
            {
                isBattle = myEntity.IsBattleMode;
                if (myEntity.OnCommand_RideOn)
                {
                    if (IsWorldCamera && _cameraType != ECAMERA_TYPE.ACTION_VIEW)
                        _rideLimitMinDistance = myEntity.CameraLimitMinDistanceOnRide;
                }
            }
        }

        var trackedOffset = Vector3.Lerp(isBattle == true ? BattleMinTrackedOffset : WaitMinTrackedOffset,
            isBattle == true ? BattleMaxTrackedOffset : WaitMaxTrackedOffset, MaxDistance != CalculatedMinDistance ? GetDistanceRate(InDistance) : 1f);
        if (myEntity?.IsStartedRide == true)
            trackedOffset.y = 0f;

        return trackedOffset;
    }

    private float GetDistanceRate(float InDistance)
    {
        float minDistance = CalculatedMinDistance;
        float rate =  (Math.Max(0f, InDistance - minDistance)) / (MaxDistance - minDistance);
        return float.IsNaN(rate) ? 0.5f : rate;
    }

    protected virtual void SetTrackedObjectOffset(float InDeltaTime)
    {
        _framingTransposer.m_TrackedObjectOffset = Vector3.Lerp(_framingTransposer.m_TrackedObjectOffset, GetTrackedOffset(_framingTransposer.m_CameraDistance), InDeltaTime);
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime))
            return false;

        if (_framingTransposer == null)
            return false;

        if (VirtualCamera.Follow == null)
            return false;

        if (_useCollision == false)
        {
            _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance = 
                Mathf.SmoothDamp(_framingTransposer.m_CameraDistance, Mathf.Clamp(CameraStateData.OriginZoomDistance, CalculatedMinDistance, MaxDistance), ref _moveVelocity, SmoothTime);
        }

        SetTrackedObjectOffset(InDeltaTime);

        PostPipelineStageProcess();

        return true;
    }
}
