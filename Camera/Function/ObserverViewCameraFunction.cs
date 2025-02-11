/**
* ObserverViewMoveCameraFunction.cs
* 작성자 : songch
* 작성일 : 2024-04-12 오전 10:53:51
*/

using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using System;

[Serializable]
public class ObserverViewCameraFunction : CinemachineCameraFunction
{
    public float MoveSpeed = 0.1f;

    private Vector2 _moveDelta;
    private Vector2 _moveUpDownDelta;

    public ObserverViewCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    protected override void AddEvent()
    {
        LogicContext.INPUT.OnCameraMove_Event += OnMove_Event;
        LogicContext.INPUT.OnCameraMoveUpDown_Event += OnMoveUpDown_Event;
    }

    protected override void RemoveEvent()
    {
        LogicContext.INPUT.OnCameraMove_Event -= OnMove_Event;
        LogicContext.INPUT.OnCameraMoveUpDown_Event -= OnMoveUpDown_Event;
    }

    private void OnMove_Event(Vector2 InDelta)
    {
        _moveDelta = InDelta;
    }

    private void OnMoveUpDown_Event(Vector2 InDelta)
    {
        _moveUpDownDelta = InDelta;
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage != CinemachineCore.Stage.Finalize)
            return false;

        Vector3 position = Vector3.zero;
        if (_moveDelta != Vector2.zero)
        {
            position += VirtualCamera.transform.forward * _moveDelta.y;
            position += VirtualCamera.transform.right * _moveDelta.x;
        }
        if (_moveUpDownDelta != Vector2.zero)
            position += Vector3.up * _moveUpDownDelta.y;
        if (position != Vector3.zero)
            VirtualCamera.transform.position += position * MoveSpeed;

        return true;
    }
}