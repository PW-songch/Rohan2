/**
* ObserverViewRotationCameraFunction.cs
* 작성자 : songch
* 작성일 : 2024-04-12 오후 12:03:49
*/

using UnityEngine;
using System;
using Cinemachine;
using Repository.Model;

[Serializable]
public class ObserverViewRotationCameraFunction : RotationCameraFunction
{
    public float RotateSpeed = 5f;

    public ObserverViewRotationCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        MinPitch = -MaxPitch;
    }

    protected override void AddEvent()
    {
        base.AddEvent();
        LogicContext.INPUT.OnCameraRotation_Event += OnRotation_Event;
    }

    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        LogicContext.INPUT.OnCameraRotation_Event -= OnRotation_Event;
    }

    private void OnRotation_Event(Vector2 InDelta)
    {
        _isClick = InDelta != Vector2.zero;
        OnPointDelta_Event(InDelta * RotateSpeed);
    }

    protected override void OnPitchRotate(float InValue, float InDeltaTime)
    {
        if (UsePitchRotation == false)
            return;

        if (InValue == 0)
            return;

        InValue *= PitchSpeed * InDeltaTime * -1f;

        var updateX = VirtualCamera.transform.localEulerAngles.x + (InValue * InDeltaTime);

        if (updateX >= 180f)
            updateX -= 360f;

        if (updateX > MaxPitch)
            _rotation.x = MaxPitch;
        else if (updateX < MinPitch)
            _rotation.x = MinPitch + 360f;
        else
            _rotation.x += InValue;
    }
}