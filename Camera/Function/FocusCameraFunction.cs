using UnityEngine;
using Cinemachine;
using System;

[Serializable]
public class FocusCameraFunction : CinemachineCameraFunction
{
    [SerializeField] private float m_BaseFov;
    [SerializeField] private float m_BeginFadeTime;
    [SerializeField] private float m_HoldTime;
    [SerializeField] private float m_EndFadeTime;
    [SerializeField] private float m_DiffFov;
    
    private float m_Duration = 0f;
    private float _passedTime = 0f;
    private float _lastUpdateFOV = 0f;
    
    public float LastUpdateFOV
    {
        get => _lastUpdateFOV;  
    }

    public enum Phase
    {
        Idle,
        Begin_Fade,
        Hold,
        End_Fade,
    }

    private Phase _phase;

    private Phase NextPhase()
    {
        var phase = _phase switch
        {
            Phase.Begin_Fade => Phase.Hold,
            Phase.Hold => Phase.End_Fade,
            Phase.End_Fade => Phase.Idle,
            _ => Phase.Begin_Fade
        };

        if (phase != _phase)
        {
            _phase = phase;
        }
        return _phase;
    }
    
    private float UpdateTimeProgress()
    {
        var timeProgress=  _phase switch
        {
            Phase.Begin_Fade => m_BeginFadeTime > 0f ?_passedTime / m_BeginFadeTime : 1f,
            Phase.Hold => m_HoldTime > 0f ? _passedTime / m_HoldTime : 1f,
            Phase.End_Fade => m_EndFadeTime > 0f ? _passedTime / m_EndFadeTime : 1f,
            _ => 0f
        };

        return timeProgress;
    }

    private float TimeProfressToProgress(float timeProgress)
    {
        return _phase switch
        {
            Phase.Begin_Fade => timeProgress,
            Phase.Hold => 1f,
            Phase.End_Fade => 1f - timeProgress,
            _ => 0f
        };
    }
    
    public FocusCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    protected override void AddEvent()
    {
        LogicContext.CAMERA.OnStartFocusCamera_Event += OnStartFocusCamera_Event;
        LogicContext.CAMERA.OnStopSkilHit_Event += OnStopFocusCamera_Event;
    }

    protected override void RemoveEvent()
    {
        LogicContext.CAMERA.OnStartFocusCamera_Event -= OnStartFocusCamera_Event;
        LogicContext.CAMERA.OnStopSkilHit_Event -= OnStopFocusCamera_Event;
    }

    private void OnStartFocusCamera_Event(float InFadeInTime, float InFadeOutTime, float InHoldTime,  float InAddFov)
    {
        m_BaseFov = VirtualCamera.m_Lens.FieldOfView;
        m_BeginFadeTime = InFadeInTime;
        m_HoldTime = InHoldTime;
        m_EndFadeTime = InFadeOutTime;
        
        m_Duration = m_BeginFadeTime + m_HoldTime + m_EndFadeTime;
        m_DiffFov = InAddFov;
        
        _passedTime = 0f;
        _phase = Phase.Begin_Fade;
    }

    private void OnStopFocusCamera_Event()
    {
        m_BeginFadeTime = 0f;
        m_HoldTime = 0f;
        m_EndFadeTime = 0f;
        m_Duration = 0f;
        _passedTime = 0f;

        VirtualCamera.m_Lens.FieldOfView = m_BaseFov;
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage != CinemachineCore.Stage.Noise || _phase == Phase.Idle)
            return false;

        if (_passedTime >= m_Duration)
        {
            _passedTime = 0f;
            _lastUpdateFOV = m_BaseFov;
            InState.Lens.FieldOfView = _lastUpdateFOV;
            PostPipelineStageProcess();
            return true;
        }

        var timeProgress = UpdateTimeProgress();
        if (timeProgress >= 1f)
        {
            timeProgress = timeProgress - 1f; 
            _passedTime = 0f;

            if (Phase.Idle == NextPhase())
            {
                _lastUpdateFOV = m_BaseFov;
                InState.Lens.FieldOfView = _lastUpdateFOV;
                PostPipelineStageProcess();
                return true;
            }
        }

        var progress = TimeProfressToProgress(timeProgress);
        _lastUpdateFOV = m_BaseFov + (m_DiffFov * progress);
        InState.Lens.FieldOfView = _lastUpdateFOV;
        
        _passedTime += InDeltaTime;
        PostPipelineStageProcess();

        return true;
    }
}
