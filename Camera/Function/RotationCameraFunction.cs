using UnityEngine;
using Cinemachine;
using Repository.Model;
using System.Linq;
using System;

[Serializable]
public class RotationCameraFunction : CinemachineCameraFunction
{
    [Header("USE")]
    public bool UseYawRotation = true;
    public bool UsePitchRotation = true;
    public bool UseCharacterRotation = false;
    public bool UseAutoTargetRotation = true;

    [Header("RANGE")]
    public float MinPitch;
    public float MaxPitch;
    public float DefaultPitch;
    public float DefaultYaw;

    [Header("SPEED")]
    public float PitchSpeed;
    public float YawSpeed;
    public float MoveRotationSpeed = 1f;
    public float AttackRotationSpeed = 5f;

    [Header("TIME")]
    public float FollowTargetForwardSyncDelay;

    [Header("ANGLES")]
    public Vector3 ForwardRotationOffset;
    public Vector3 BattleForwardRotationOffset;

    protected Vector3 _rotation = Vector3.zero;

    private bool _isRotationFollowTargetForward;

    private WeakReference<MyPlayerEntity> _myPlayerEntityRef = null;
    protected MyPlayerEntity MyPlayerEntity => _myPlayerEntityRef != null && _myPlayerEntityRef.TryGetTarget(out var playerEntity) ? playerEntity : null;

    public Vector3 RotationOffset => (MyPlayerEntity?.IsBattleMode == true ? BattleForwardRotationOffset : ForwardRotationOffset);

    public RotationCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        UseYawRotation = InSetting.USEYAWROTATION;
        UsePitchRotation = InSetting.USEPITCHROTATION;
        UseCharacterRotation = false;
        UseAutoTargetRotation = InSetting.CAMERACODE != CAMERA_TYPE.CT_QUARTER_VIEW;

        MinPitch = InSetting.ROTATION_PITCH_MIN;
        MaxPitch = InSetting.ROTATION_PITCH_MAX;

        PitchSpeed = InSetting.ROTATION_SPEED_PITCH;
        YawSpeed = InSetting.ROTATION_SPEED_YAW;

        DefaultPitch = InSetting.ROTATION_PITCH_DEFAULT;
        DefaultYaw = InSetting.ROTATION_YAW_DEFAULT;

        MoveRotationSpeed = InSetting.DIRECTION_TO_MOVING_SPEED;
        AttackRotationSpeed = InSetting.DIRECTION_TO_TARGETING_SPEED;

        //ForwardRotationOffset.x = InSetting.X;
        //ForwardRotationOffset.y = InSetting.Y;
        //ForwardRotationOffset.z = InSetting.Z;

        //BattleForwardRotationOffset.x = InSetting.X;
        //BattleForwardRotationOffset.y = InSetting.Y;
        //BattleForwardRotationOffset.z = InSetting.Z;

        InternalSetup(InReset);
    }

    public override void Setup(TdOutgameCharacterCamera InSetting)
    {
        base.Setup(InSetting);

        UseYawRotation = InSetting.USEYAWROTATION;
        UsePitchRotation = InSetting.USEPITCHROTATION;
        UseCharacterRotation = true;

        MinPitch = InSetting.ROTATION_PITCH_MIN;
        MaxPitch = InSetting.ROTATION_PITCH_MAX;

        PitchSpeed = InSetting.ROTATION_SPEED_PITCH;
        YawSpeed = InSetting.ROTATION_SPEED_YAW;

        DefaultPitch = InSetting.ROTATION_PITCH_DEFAULT;
        DefaultYaw = InSetting.ROTATION_YAW_DEFAULT;

        InternalSetup();
    }

    protected override void InternalSetup(bool InReset = false)
    {
        base.InternalSetup(InReset);

        _rotation.x = DefaultPitch;
        _rotation.y = DefaultYaw;

        if (InReset == false)
            VirtualCamera.transform.localEulerAngles = _rotation;

        var myPlayerEntity = EntityController.MyCharacter;
        if (myPlayerEntity != null)
            _myPlayerEntityRef = new WeakReference<MyPlayerEntity>(myPlayerEntity);
    }

    public override void SetToCurrent()
    {
        base.SetToCurrent();

        _rotation = CameraStateData.Rotation;
        VirtualCamera.transform.localEulerAngles = _rotation;
    }

    public override void ChangeViewMode(bool InChange)
    {
        base.ChangeViewMode(InChange);
        _isRotationFollowTargetForward = false;

        if (InChange == true && IsActive == true)
        {
            RotationToFollowTargetForward(MyPlayerEntity);
            _rotation.x = DefaultPitch;
            //_rotation.y = DefaultYaw;
            VirtualCamera.transform.localEulerAngles = _rotation;
        }
    }

    protected virtual void OnPitchRotate(float InValue, float InDeltaTime)
    {
        if (UsePitchRotation == false)
            return;

        if (InValue == 0)
            return;

        InValue *= PitchSpeed * InDeltaTime * -1f;

// temp : Window Build Sensitivity issues : 20230915 - sucheol.park
// #if !UNITY_EDITOR
//         InValue *= RepositoryContext.CONST.ROTATE_SENSIBILITY;
// #endif

        var updateX = VirtualCamera.transform.localEulerAngles.x + (InValue * InDeltaTime);

        if (updateX >= 180f)
            updateX -= 360f;

        if (updateX > MaxPitch)
            _rotation.x = MaxPitch;
        else
            _rotation.x += InValue;
    }

    private void OnYawRotate(float InValue, float InDeltaTime)
    {
        if (UseYawRotation == false)
            return;

        if (InValue == 0)
            return;

        InValue *= YawSpeed * InDeltaTime;
// temp : Window Build Sensitivity issues : 20230915 - sucheol.park
// #if !UNITY_EDITOR
//         InValue *= RepositoryContext.CONST.ROTATE_SENSIBILITY;
// #endif

        _rotation.y += InValue;
    }

    private void OnCharacterRotate(float InValue, float InDeltaTime)
    {
        if (InValue == 0)
            return;

        if (LogicContext.UI.OpenList.Any(x => x.GetType() == typeof(UIMessageBoxPopup) || x.GetType().IsSubclassOf(typeof(UIMessageBoxPopup))))
            return;

        InValue *= YawSpeed * InDeltaTime;
// temp : Window Build Sensitivity issues : 20230915 - sucheol.park
// #if !UNITY_EDITOR
//         InValue *= RepositoryContext.CONST.ROTATE_SENSIBILITY;
// #endif

        var angle = VirtualCamera.Follow.localEulerAngles;
        angle.y -= InValue;

        VirtualCamera.Follow.localEulerAngles = angle;
    }

    protected virtual bool DragRotation(float InDeltaTime)
    {
        if (!_isClick || _mouseDelta == Vector2.zero)
            return false;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;

        if (UseCharacterRotation == true)
        {
            OnCharacterRotate(_mouseDelta.x, InDeltaTime);
        }
        else
        {
            _rotation = virtualCamera.transform.localEulerAngles;

            OnPitchRotate(_mouseDelta.y, InDeltaTime);
            OnYawRotate(_mouseDelta.x, InDeltaTime);
        }

        virtualCamera.transform.localEulerAngles = CameraStateData.Rotation = Vector3.Lerp(virtualCamera.transform.localEulerAngles, _rotation, InDeltaTime);

        _inputTime = Time.realtimeSinceStartup;

        return true;
    }

    protected virtual Vector3 GetFollowTargetForwardRotation(CinemachineVirtualCamera InVirtualCamera, MyPlayerEntity InPlayerEntity)
    {
        Vector3 rotation = InVirtualCamera.transform.localEulerAngles;
        Vector3 lookRotation = Quaternion.LookRotation(InPlayerEntity.Forward).eulerAngles;
        rotation.y = lookRotation.y;
        return rotation;
    }

    protected bool RotationToFollowTargetForward(MyPlayerEntity InPlayerEntity, float InSpeed = 1, float InDeltaTime = 1)
    {
        if (InPlayerEntity == null)
            return false;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        _rotation = GetFollowTargetForwardRotation(virtualCamera, InPlayerEntity) + RotationOffset;
        virtualCamera.transform.localRotation = Quaternion.Lerp(virtualCamera.transform.localRotation, Quaternion.Euler(_rotation), InDeltaTime * InSpeed);
        CameraStateData.Rotation = virtualCamera.transform.localEulerAngles;
        CameraStateData.Rotation.z = _rotation.z;
        virtualCamera.transform.localEulerAngles = CameraStateData.Rotation;
        return virtualCamera.transform.localEulerAngles != _rotation;
    }

    protected virtual void RotationToFollowTargetForwardProcess(float InDeltaTime)
    {
        if (!_isClick)
        {
            MyPlayerEntity playerEntity = MyPlayerEntity;
            if (playerEntity != null)
            {
                if (!_isRotationFollowTargetForward)
                    _isRotationFollowTargetForward = (playerEntity.OnMove || IsMovingFollowTarget);

                if (_isRotationFollowTargetForward && IsEnableRoationSync())
                    _isRotationFollowTargetForward = RotationToFollowTargetForward(playerEntity, GetRoationSpeed(playerEntity), InDeltaTime);
            }
        }
        else
            _isRotationFollowTargetForward = false;
    }

    protected virtual float GetRoationSpeed(MyPlayerEntity InPlayerEntity)
    {
        return InPlayerEntity.HasTarget && InPlayerEntity.IsBattleMode ? AttackRotationSpeed : MoveRotationSpeed;
    }

    protected bool IsEnableRoationSync()
    {
        return Time.realtimeSinceStartup - _inputTime >= FollowTargetForwardSyncDelay;
    }

    protected override bool RestoreDefaultSetting(float InDeltaTime)
    {
        if (!LogicContext.GAME_OPTIONS.CameraAutoTrailing)
            return false;

        if (!CamExtension.IsCollisioning && base.RestoreDefaultSetting(InDeltaTime))
        {
            _rotation.x = DefaultPitch;
            _rotation.y = DefaultYaw;

            CinemachineVirtualCamera virtualCamera = VirtualCamera;
            VirtualCamera.transform.localRotation = Quaternion.Lerp(virtualCamera.transform.localRotation, Quaternion.Euler(_rotation), InDeltaTime);
            CameraStateData.Rotation = VirtualCamera.transform.localEulerAngles;
            return true;
        }

        return false;
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage != CinemachineCore.Stage.Body)
            return false;

        // 드래그 회전
        DragRotation(Time.fixedDeltaTime);

        // 플레이어 방향으로 회전
        if (LogicContext.GAME_OPTIONS.CameraAutoTrailing)
            RotationToFollowTargetForwardProcess(Time.fixedDeltaTime);

        // pitch min max 적용
        var virtualCam = VirtualCamera;
        var ratation = virtualCam.transform.localEulerAngles;
        if (ratation.x > 180f)
            ratation.x -= 360f;
        ratation.x = Mathf.Clamp(ratation.x, MinPitch, MaxPitch);
        ratation.z = 0f;
        virtualCam.transform.localEulerAngles = CameraStateData.Rotation = ratation;

        PostPipelineStageProcess();

        return true;
    }
}
