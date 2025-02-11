using UnityEngine;
using Cinemachine;
using Repository.Model;
using System;

/// <summary>
/// 카메라 기능 기본 클래스
/// </summary>
[Serializable]
public abstract class CinemachineCameraFunction
{
    public enum EFUNCTION_TYPE
    {
        ROTATION,
        ZOOM,
        COLLISION,
        FOV,
        SHAKE,
        LOOKAT,
        OBSERVER,
        DAMPING,
        FOCUS,
        RIDE,
        DOF,
    }

    protected WeakReference<CameraExtension> _cameraExtension;
    protected WeakReference<CinemachineVirtualCamera> _virtualCamera;
    protected CinemachineFramingTransposer _framingTransposer = null;

    protected ECAMERA_TYPE _cameraType;
    protected bool _isEnable = true;
    protected bool _isRunning = true;
    protected bool _isClick = false;
    protected Vector2 _mouseDelta = Vector2.zero;

    protected float _restoreDefaultSettingDelay;
    protected float _inputTime;
    protected bool _isInput;
    protected bool _isChangeViewMode;

    private Vector3 _followTargetPosition = Vector3.zero;
    private Quaternion _cameraRotation = Quaternion.identity;

    protected CameraExtension CamExtension => _cameraExtension != null && _cameraExtension.TryGetTarget(out var extension) && extension != null ? extension : null;
    protected CinemachineVirtualCamera VirtualCamera => _virtualCamera != null && _virtualCamera.TryGetTarget(out var camera) && camera != null ? camera : null;
    protected bool IsDragging => _isClick && _mouseDelta != Vector2.zero;
    protected bool IsMovingFollowTarget => _followTargetPosition != _framingTransposer?.FollowTargetPosition;
    protected bool IsRotationCamera => _cameraRotation != _framingTransposer?.VcamState.FinalOrientation;
    protected bool IsWorldCamera => _cameraType == ECAMERA_TYPE.FREE_VIEW || _cameraType == ECAMERA_TYPE.QUARTER_VIEW || _cameraType == ECAMERA_TYPE.ACTION_VIEW;
    
    public bool IsActive => _isEnable && _isRunning;

    protected ECAMERA_TYPE CameraType
    {
        get
        {
            switch (CamExtension)
            {
                case CharacterSelectCinemachineExtension:
                    return ECAMERA_TYPE.LOBBY;
                case FreeViewCinemachineExtension:
                    return ECAMERA_TYPE.FREE_VIEW;
                case QuarterViewCinemachineExtension:
                    return ECAMERA_TYPE.QUARTER_VIEW;
                case ActionViewCinemachineExtension:
                    return ECAMERA_TYPE.ACTION_VIEW;
                case CameraExtension:
                    return ECAMERA_TYPE.SKILL_PREVIEW;
            }

            return ECAMERA_TYPE.NONE;
        }
    }

    public delegate void UpdateCameraFunctionCallback(CinemachineCameraFunction InCameraFunction);
    public UpdateCameraFunctionCallback UpdateCallback { get; set; } = null;

    protected readonly float EPSILON;

    public CinemachineCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
    {
        _cameraExtension = new WeakReference<CameraExtension>(InCameraExtension);
        _virtualCamera = new WeakReference<CinemachineVirtualCamera>(InVirtualCamera);
        EPSILON = InEpsilon;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (virtualCamera != null)
        {
            _framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (_framingTransposer != null)
            {
                _followTargetPosition = _framingTransposer.FollowTargetPosition;
                _cameraRotation = _framingTransposer.VcamState.FinalOrientation;

            }
        }

        _cameraType = GetCameraType();
    }

    public virtual void OnEnable()
    {
        RemoveEvent();
        AddEvent();
    }

    public virtual void OnDisable()
    {
        RemoveEvent();
    }

    public virtual void OnDestroy()
    {
        OnDisable();
        RemoveEvent();
        UpdateCallback = null;
    }

    public virtual void SetFollowTransform(in Transform InFollowTransform) { }

    public virtual void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        _restoreDefaultSettingDelay = InSetting.DEFAULT_ROLLBACK_TIME;
    }

    public virtual void Setup(TdOutgameCharacterCamera InSetting) { }

    public virtual void SetToCurrent() { }

    public virtual void Pause() => _isRunning = false;

    public virtual void Resume() => _isRunning = true;

    public virtual void ChangeViewMode(bool InChange) => _isChangeViewMode = InChange;

    public virtual bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (IsActive)
        {
            if (InStage == CinemachineCore.Stage.Finalize)
            {
                if (_framingTransposer != null)
                {
                    _followTargetPosition = _framingTransposer.FollowTargetPosition;
                    _cameraRotation = _framingTransposer.VcamState.FinalOrientation;
                }

                RestoreDefaultSetting(InDeltaTime);
            }

            return true;
        }

        return false;
    }

    protected virtual void PostPipelineStageProcess()
    {
        UpdateCallback?.Invoke(this);
    }

    protected virtual void InternalSetup(bool InReset = false) { }

    protected virtual void OnRightClick_Event(bool InValue)
    {
        if (_isChangeViewMode || LogicContext.ASSIST.IsAssistObserverMode)
            return;

        _isClick = InValue;
        SetInputState();
        if (!InValue)
            _isInput = false;
    }

    protected virtual void OnPointDelta_Event(Vector2 InDelta)
    {
        if (_isChangeViewMode || LogicContext.ASSIST.IsAssistObserverMode)
            return;

        _mouseDelta = InDelta;
        if (IsDragging)
            SetInputState();
    }

    protected virtual void OnChangeScrollValue_Event(float InValue)
    {
        if (_isChangeViewMode || LogicContext.ASSIST.IsAssistObserverMode)
            return;

        if (InValue != 0f)
            SetInputState();
    }

    protected virtual void OnActiveAuto(bool InAuto)
    {
        if (InAuto)
            SetInputState();
    }

    protected virtual void AddEvent()
    {
        LogicContext.INPUT.OnPointDelta_Event += OnPointDelta_Event;
        LogicContext.INPUT.OnRightClick_Event += OnRightClick_Event;
        LogicContext.INPUT.OnChangeScrollValue_Event += OnChangeScrollValue_Event;
        if (EntityController.MyCharacter != null)
            EntityController.MyCharacter.OnEvent_ActiveAuto += OnActiveAuto;
    }

    protected virtual void RemoveEvent()
    {
        LogicContext.INPUT.OnPointDelta_Event -= OnPointDelta_Event;
        LogicContext.INPUT.OnRightClick_Event -= OnRightClick_Event;
        LogicContext.INPUT.OnChangeScrollValue_Event -= OnChangeScrollValue_Event;
        if (EntityController.MyCharacter != null)
            EntityController.MyCharacter.OnEvent_ActiveAuto -= OnActiveAuto;
    }

    protected virtual bool RestoreDefaultSetting(float InDeltaTime)
    {
        bool isEnable = IsEnableRestoreDefaultSetting();
        _isInput = _isClick;
        return isEnable;
    }

    protected bool HasRestoreDefaultSettingData()
    {
        return _restoreDefaultSettingDelay >= 0f;
    }

    protected virtual bool IsEnableRestoreDefaultSetting()
    {
        return _isInput || !HasRestoreDefaultSettingData() ? false : Time.realtimeSinceStartup - _inputTime >= _restoreDefaultSettingDelay;
    }

    protected void SetInputState()
    {
        _isInput = true;
        _inputTime = Time.realtimeSinceStartup;
    }

    public void SetEnable(bool InEnable)
    {
        _isEnable = InEnable;
    }

    protected ECAMERA_TYPE GetCameraType()
    {
        switch (CamExtension)
        {
            case CharacterSelectCinemachineExtension:
                return ECAMERA_TYPE.LOBBY;
            case FreeViewCinemachineExtension:
                return ECAMERA_TYPE.FREE_VIEW;
            case QuarterViewCinemachineExtension:
                return ECAMERA_TYPE.QUARTER_VIEW;
            case ActionViewCinemachineExtension:
                return ECAMERA_TYPE.ACTION_VIEW;
            case CameraExtension:
                return ECAMERA_TYPE.SKILL_PREVIEW;
        }

        return ECAMERA_TYPE.NONE;
    }

#if UNITY_EDITOR
    public virtual void OnDrawGizmosSelected() { }
#endif
}
