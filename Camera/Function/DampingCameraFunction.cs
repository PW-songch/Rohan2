using UnityEngine;
using Cinemachine;
using System;


[Serializable]
public class DampingCameraFunction : CinemachineCameraFunction
{
    [SerializeField] private bool m_UseDebug = false;

    
    private float _damping = 6;
    private float _duration = 1.2f;
    private float _remained = 0f;
    private float _velocity = 0f;
    private Vector3 _originDamping = Vector3.zero;

    public DampingCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    protected override void AddEvent()
    {
        LogicContext.CAMERA.OnStartDamping_Event += OnStartDamping_Event;
    }

    protected override void RemoveEvent()
    {
        LogicContext.CAMERA.OnStartDamping_Event -= OnStartDamping_Event;
    }

    private void OnStartDamping_Event(float InDamping, float InDuration)
    {
        if (InDuration <= 0)
            return;

        if( _remained <= 0 )
        {
            if (_framingTransposer != null )
            {
                _damping = InDamping;
                _remained = InDuration;
                _duration = InDuration;
                _velocity = 0f;

                //_originDamping.x = _framingTransposer.m_XDamping;
                //_originDamping.y = _framingTransposer.m_YDamping;
                //_originDamping.z = _framingTransposer.m_ZDamping;
                _originDamping.x = 0;
                _originDamping.y = 0;
                _originDamping.z = 0;

                _framingTransposer.m_XDamping = _damping;
                _framingTransposer.m_YDamping = _damping;
                _framingTransposer.m_ZDamping = _damping;
            }
            
        }
    }


    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || _remained <= 0)
            return false;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (virtualCamera == null) 
            return false;

        _remained -= InDeltaTime;
        if (_remained <= 0)
        {
            _remained = 0;
            if (_framingTransposer != null)
            {
                _framingTransposer.m_XDamping = _originDamping.x;
                _framingTransposer.m_YDamping = _originDamping.y;
                _framingTransposer.m_ZDamping = _originDamping.z;
            }
        }
        else if(_framingTransposer != null)
        {
            float curDamping = 0;
            if( 0 < _duration )
                curDamping = Mathf.Lerp(_damping, 0, (_duration - _remained) / _duration);

            _framingTransposer.m_XDamping = _originDamping.x + curDamping;
            _framingTransposer.m_YDamping = _originDamping.y + curDamping;
            _framingTransposer.m_ZDamping = _originDamping.z + curDamping;
        }

        PostPipelineStageProcess();

        return true;
    }
}
