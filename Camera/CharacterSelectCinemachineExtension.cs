/**
* CharacterSelectCinemachineExtension.cs
* 작성자 : songch
* 작성일 : 2023-06-28 오후 1:56:31
*/

using Cinemachine;
using Repository.Model;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

using ECAMERA_FUNCTION_TYPE = CinemachineCameraFunction.EFUNCTION_TYPE;

[AddComponentMenu("")] // Hide in menu
[DisallowMultipleComponent]
public class CharacterSelectCinemachineExtension : CameraExtension
{
    [Flags]
    private enum EMOVEMENT_TYPE
    {
        NONE = 0,
        MOVE = 1 << 0,
        ROTATION = 1 << 1,
        ZOOM = 1 << 2,
    }

    [Header("ZOOM")]
    [Header("DISTANCE")]
    public float MinDistance;
    public float MaxDistance;
    public float DefaultDistance;
    public float ZoomFaceDistance;

    [Header("TRACKED OFFSET")]
    public Vector3 MinTrackedOffset;
    public Vector3 MaxTrackedOffset;
    public Vector2 MinMoveTrackedOffset;

    [Header("SPEED")]
    public float ZoomSpeed;
    public float DragMoveSpeed;
    public float SmoothTime = 0.3f;

    [Space(10)]
    [Header("ROTATION")]
    [Header("RANGE")]
    public float DefaultPitch;
    public float DefaultYaw;

    [Header("SPEED")]
    public float YawSpeed;

    private CinemachineFramingTransposer _framingTransposer = null;

    private bool _isClick = false;
    private float _moveVelocity;
    private Vector2 _mouseDelta = Vector2.zero;
    public Vector3 TempMinTrackedOffset = Vector3.zero;

    private float _customizationStartDistance;

    private EMOVEMENT_TYPE _movementType;

    private Coroutine _faceZoomInOutCoroutine;
    private Coroutine _distanceAnimationCoroutine;

    private bool IsPlayAnimation => _faceZoomInOutCoroutine != null || _distanceAnimationCoroutine != null;

    protected override void OnEnable()
    {
        base.OnEnable();

        LogicContext.INPUT.OnChangeScrollValue_Event += OnChangeScrollValue_Event;
        LogicContext.INPUT.OnPointDelta_Event += OnPointDelta_Event;
        LogicContext.INPUT.OnRightClick_Event += OnRightClick_Event;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        LogicContext.INPUT.OnChangeScrollValue_Event -= OnChangeScrollValue_Event;
        LogicContext.INPUT.OnPointDelta_Event -= OnPointDelta_Event;
        LogicContext.INPUT.OnRightClick_Event -= OnRightClick_Event;
    }

    public override void Setup(TdOutgameCharacterCamera InSetting)
    {
        _movementType = EMOVEMENT_TYPE.NONE;
        _movementType = _movementType.AddFlag(EMOVEMENT_TYPE.MOVE);

        if (InSetting.USE_ROTATION == true)
            _movementType = _movementType.AddFlag(EMOVEMENT_TYPE.ROTATION);
        if (InSetting.USE_ZOOM == true)
            _movementType = _movementType.AddFlag(EMOVEMENT_TYPE.ZOOM);
        if (InSetting.USE_LOOKAT == true)
        {
            _lookAtCameraFunction = Activator.CreateInstance(GetCameraFunctionType(ECAMERA_FUNCTION_TYPE.LOOKAT),
                this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as LookAtCameraFunction;
            _lookAtCameraFunction.Setup(InSetting);
            AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.LOOKAT, _lookAtCameraFunction);
        }

        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.FOV);

        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.Setup(InSetting);
        }

        MinDistance = InSetting.CAMERADISTANCE_MIN;
        MaxDistance = InSetting.CAMERADITANCE_MAX;
        CameraStateData.OriginZoomDistance = DefaultDistance = InSetting.CAMERADISTANCE_DEFAULT;
        _customizationStartDistance = 0f;

        MinTrackedOffset.x = InSetting.TRAKEDPOSITION_BASE_X;
        MinTrackedOffset.y = InSetting.TRAKEDPOSITION_BASE_Y;
        MinTrackedOffset.z = InSetting.TRAKEDPOSITION_BASE_Z;
        TempMinTrackedOffset = MinTrackedOffset;

        MaxTrackedOffset.x = InSetting.TRACKEDPOSITION_OFFSET_X;
        MaxTrackedOffset.y = InSetting.TRACKEDPOSITION_OFFSET_Y;
        MaxTrackedOffset.z = InSetting.TRACKEDPOSITION_OFFSET_Z;

        ZoomSpeed = InSetting.CAMERADISTANCE_SPEED;

        YawSpeed = InSetting.ROTATION_SPEED_YAW;

        DefaultPitch = InSetting.ROTATION_PITCH_DEFAULT;
        DefaultYaw = InSetting.ROTATION_YAW_DEFAULT;

        DragMoveSpeed = InSetting.Y_DRAG_SPEED;
        MinMoveTrackedOffset.y = InSetting.CENTER_MIN_Y;

        ZoomFaceDistance = InSetting.FACE_DISTANCE;

        InternalSetup();

        // 2024-01-24 송창호 : 케릭터 얼굴 줌인아웃 연출 제거
        //if (_faceZoomInOutCoroutine != null)
        //    StopCoroutine(_faceZoomInOutCoroutine);
        //_faceZoomInOutCoroutine = StartCoroutine(CoroutineZoomInOutFace());
    }

    /// <summary>
    /// 케릭터 얼굴 줌인아웃 연출
    /// </summary>
    private IEnumerator CoroutineZoomInOutFace()
    {
        if (_framingTransposer == null)
        {
            _faceZoomInOutCoroutine = null;
            yield break;
        }

        yield return null;

        // 클래스 선택시에만 실행
        var classSelectUI = LogicContext.UI.GetUI<UILobbyClassSelectWindow>("UI_LobbyClassSelect_Window");
        if (classSelectUI == null || !classSelectUI.isActiveAndEnabled)
        {
            _faceZoomInOutCoroutine = null;
            yield break;
        }

        while (Mathf.Abs(_framingTransposer.m_CameraDistance - ZoomFaceDistance) > Epsilon)
        {
            _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance =
                    Mathf.SmoothDamp(_framingTransposer.m_CameraDistance, ZoomFaceDistance, ref _moveVelocity, SmoothTime);
            _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(_framingTransposer.m_CameraDistance, Time.deltaTime);
            yield return null;
        }

        while (Mathf.Abs(_framingTransposer.m_CameraDistance - DefaultDistance) > Epsilon)
        {
            _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance =
                    Mathf.SmoothDamp(_framingTransposer.m_CameraDistance, DefaultDistance, ref _moveVelocity, SmoothTime);
            _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(_framingTransposer.m_CameraDistance, Time.deltaTime);
            yield return null;
        }

        _faceZoomInOutCoroutine = null;
    }

    public void PlayDistanceAnimation(float InDuration, in AnimationCurve InCurve)
    {
        StopDistanceAnimation(false);
        _distanceAnimationCoroutine = StartCoroutine(CorDistanceAnimation(InDuration, InCurve));
    }

    public void StopDistanceAnimation(bool InSetDefaultDistance = true)
    {
        if (_distanceAnimationCoroutine != null)
        {
            StopCoroutine(_distanceAnimationCoroutine);
            _distanceAnimationCoroutine = null;
        }

        if (InSetDefaultDistance)
        {
            _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance = DefaultDistance;
            _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(_framingTransposer.m_CameraDistance);
        }
    }

    private IEnumerator CorDistanceAnimation(float InDuration, AnimationCurve InCurve)
    {
        if (_framingTransposer == null)
        {
            _distanceAnimationCoroutine = null;
            yield break;
        }

        float time = 0;
        while (time < InDuration)
        {
            time += Time.deltaTime;

            float value = InCurve != null ? InCurve.Evaluate(time / InDuration) : 1f;
            _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance = Mathf.LerpUnclamped(MinDistance, MaxDistance, value);
            _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(_framingTransposer.m_CameraDistance, Time.deltaTime);

            yield return null;
        }

        _distanceAnimationCoroutine = null;
    }

    private void InternalSetup()
    {
        var virtualCamera = VirtualCamera as CinemachineVirtualCamera;
        if (virtualCamera == null)
            return;

        _framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (_framingTransposer == null)
            return;

        _framingTransposer.m_CameraDistance = DefaultDistance;
        _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(CameraStateData.OriginZoomDistance);

        transform.localEulerAngles = CameraStateData.Rotation = new Vector3(DefaultPitch, DefaultYaw);
    }

    private void OnChangeScrollValue_Event(float InValue)
    {
        if (_isClick || InValue == 0 || _framingTransposer == null || IsPlayAnimation || !_movementType.HasFlagUnsafe(EMOVEMENT_TYPE.ZOOM))
            return;

        InValue *= ZoomSpeed;

#if !UNITY_EDITOR
        InValue *= RepositoryContext.CONST.ZOOM_SENSIBILITY;
#endif

        InValue *= -1;

        CameraStateData.OriginZoomDistance = Mathf.Clamp(CameraStateData.OriginZoomDistance + InValue, MinDistance, MaxDistance);
    }

    private void OnPointDelta_Event(Vector2 InDelta)
    {
        if (IsPlayAnimation)
            return;

        _mouseDelta = InDelta;
    }

    public void SetCustomizationMode(float InDistance)
    {
        _customizationStartDistance = CameraStateData.OriginZoomDistance;
        CameraStateData.OriginZoomDistance = InDistance;
        _mouseDelta = Vector3.zero;
        _movementType = _movementType.RemoveFlag(EMOVEMENT_TYPE.MOVE | EMOVEMENT_TYPE.ZOOM);
    }

    private void OnRightClick_Event(bool InValue)
    {
        if (IsPlayAnimation)
            return;

        _isClick = InValue;

        if (_movementType.HasFlagUnsafe(EMOVEMENT_TYPE.ZOOM))
            CameraStateData.OriginZoomDistance = _framingTransposer.m_CameraDistance;
    }

    public Vector3 GetTrackedOffset(float InDistance, float InDeltaTime = 0f)
    {
        float rate = (InDistance - MinDistance) / (MaxDistance - MinDistance);
        if (_movementType.HasFlagUnsafe(EMOVEMENT_TYPE.MOVE))
        {
            if (_isClick && InDistance < MaxDistance)
            {
                // dragging
                TempMinTrackedOffset.y = Mathf.Clamp(TempMinTrackedOffset.y - _mouseDelta.y * DragMoveSpeed * InDeltaTime, MinMoveTrackedOffset.y, MinTrackedOffset.y);
            }
        }
        else if (_customizationStartDistance != 0f)
        {
            TempMinTrackedOffset = Vector3.Lerp(TempMinTrackedOffset, MinTrackedOffset, InDeltaTime * 10f);
        }

        return Vector3.Lerp(TempMinTrackedOffset, MaxTrackedOffset, rate);
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

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (InStage != CinemachineCore.Stage.Body || !_isActive || _framingTransposer == null || VirtualCamera.Follow == null)
            return;

        float deltaTime = Time.fixedDeltaTime;
        base.PostPipelineStageCallback(InVcam, InStage, ref InState, deltaTime);

        if (IsPlayAnimation)
            return;

        // zoom
        _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance =
            Mathf.Lerp(_framingTransposer.m_CameraDistance, CameraStateData.OriginZoomDistance, SmoothTime * deltaTime * 10f);

        // move
        _framingTransposer.m_TrackedObjectOffset = GetTrackedOffset(_framingTransposer.m_CameraDistance, deltaTime);

        // rotation
        if (_isClick && _movementType.HasFlagUnsafe(EMOVEMENT_TYPE.ROTATION))
        {
            OnCharacterRotate(_mouseDelta.x, deltaTime);
        }
    }
}