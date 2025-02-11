using UnityEngine;
using Cinemachine;
using System;

public enum ECAMERA_SHAKE_TYPE : byte
{
    RANDOM,
    HORIZONTAL,
    VERTICAL,
    FORWARD,
    DIAGONAL_POSITIVE,
    DIAGONAL_NEGATIVE,
    SPHERE,
    CUBE
}

[Serializable]
public class ShakeCameraFunction : CinemachineCameraFunction
{
    [SerializeField] private ECAMERA_SHAKE_TYPE m_ShakeType;
    [SerializeField] private float m_Amplitude;
    [SerializeField] private float m_Duration;
    [SerializeField] private float m_Radius;
    [SerializeField] private Vector3 m_Size;

    [SerializeField] private bool m_UseDebug = false;

    private Vector3 _direction = Vector3.zero;
    private bool _isShaking = false;
    private float _duration = 0;

    public ShakeCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    protected override void AddEvent()
    {
        LogicContext.CAMERA.OnStartShake_Event += OnStartShake_Event;
        LogicContext.CAMERA.OnStopShake_Event += OnStopShake_Event;
    }

    protected override void RemoveEvent()
    {
        LogicContext.CAMERA.OnStartShake_Event -= OnStartShake_Event;
        LogicContext.CAMERA.OnStopShake_Event -= OnStopShake_Event;
    }

    private void OnStartShake_Event(ECAMERA_SHAKE_TYPE InType, float InAmplitude, float InDuration, float InRadius, Vector3 InSize)
    {
        if (InDuration < 0)
            return;

        m_ShakeType = InType;
        m_Amplitude = InAmplitude;
        m_Duration = InDuration;
        m_Radius = InRadius;
        m_Size = InSize;

        _duration = m_Duration;
        _isShaking = true;
    }

    private void OnStopShake_Event()
    {
        m_Amplitude = 0;
        m_Duration = 0;
        _isShaking = false;
        _duration = 0;
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || _isShaking == false)
            return false;

        if (_duration <= 0)
        {
            _duration = 0;
            _isShaking = false;
            return false;
        }

        CinemachineVirtualCamera virtualCamera = VirtualCamera;

        switch (m_ShakeType)
        {
            case ECAMERA_SHAKE_TYPE.RANDOM:
                {
                    Vector2 randomVector = UnityEngine.Random.insideUnitCircle;
                    _direction = (virtualCamera.transform.right * randomVector.x) + (virtualCamera.transform.up * randomVector.y);
                    break;
                }
            case ECAMERA_SHAKE_TYPE.HORIZONTAL:
                {
                    _direction = virtualCamera.transform.right;
                    break;
                }
            case ECAMERA_SHAKE_TYPE.VERTICAL:
                {
                    _direction = virtualCamera.transform.up;
                    break;
                }
            case ECAMERA_SHAKE_TYPE.FORWARD:
                {
                    _direction = virtualCamera.transform.forward;
                    break;
                }
            case ECAMERA_SHAKE_TYPE.DIAGONAL_POSITIVE:
                {
                    _direction = virtualCamera.transform.right + virtualCamera.transform.up;
                    _direction.Normalize();
                    break;
                }
            case ECAMERA_SHAKE_TYPE.DIAGONAL_NEGATIVE:
                {
                    _direction = -virtualCamera.transform.right + virtualCamera.transform.up;
                    _direction.Normalize();
                    break;
                }
            case ECAMERA_SHAKE_TYPE.SPHERE:
                {
                    _direction = UnityEngine.Random.insideUnitSphere * m_Radius;
                    break;
                }
            case ECAMERA_SHAKE_TYPE.CUBE:
                {
                    Vector3 size = m_Size * 0.5f;
                    _direction.x = UnityEngine.Random.Range(-size.x, size.x);
                    _direction.y = UnityEngine.Random.Range(-size.y, size.y);
                    _direction.z = UnityEngine.Random.Range(-size.z, size.z);
                    break;
                }
        }

        var randomValue = RandomExtensionMethods.RandomValue(-1, 2);
        _direction *= randomValue;

        if (m_UseDebug == true)
        {
            OnScreenLog.Add(51, $"Direction = {_direction}  Duration = {_duration}");
        }

        InState.PositionCorrection += _direction * ((_duration / m_Duration) * m_Amplitude);
        _duration -= InDeltaTime;

        PostPipelineStageProcess();

        return true;
    }
}
