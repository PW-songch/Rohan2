/**
* ObserverViewCinemachineExtension.cs
* 작성자 : songch
* 작성일 : 2024-04-12 오전 10:48:41
*/

using UnityEngine;
using System;
using Cinemachine;

[AddComponentMenu("")] // Hide in menu
[DisallowMultipleComponent]
public class ObserverViewCinemachineExtension : CameraExtension
{
    [SerializeReference] protected ObserverViewCameraFunction _observerCameraFunction;

    public ObserverViewCameraFunction ObserverCameraFunction => _observerCameraFunction;
    public ObserverViewRotationCameraFunction RotationCameraFunction => _rotationCameraFunction as ObserverViewRotationCameraFunction;

    public const CAMERA_TYPE OBSERVER_CAMERA_TYPE = CAMERA_TYPE.CT_FREE_VIEW;

    protected override void Awake()
    {
        base.Awake();

        AddCinemachineCameraFunction(CinemachineCameraFunction.EFUNCTION_TYPE.ROTATION, OBSERVER_CAMERA_TYPE);
        AddCinemachineCameraFunction(CinemachineCameraFunction.EFUNCTION_TYPE.ZOOM, OBSERVER_CAMERA_TYPE);
        AddCinemachineCameraFunction(CinemachineCameraFunction.EFUNCTION_TYPE.FOV, OBSERVER_CAMERA_TYPE);
        _observerCameraFunction = Activator.CreateInstance(GetCameraFunctionType(CinemachineCameraFunction.EFUNCTION_TYPE.OBSERVER),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as ObserverViewCameraFunction;
        AddCinemachineCameraFunction(CinemachineCameraFunction.EFUNCTION_TYPE.OBSERVER, _observerCameraFunction);

        ResetExtension(RepositoryContext.CHARACTER_CAMERA_MODE_SETTING.Get(EntityController.MyCharacter.TribeType, EntityController.MyCharacter.CharacterGender, OBSERVER_CAMERA_TYPE));

        var zoom = GetCameraFunction(CinemachineCameraFunction.EFUNCTION_TYPE.ZOOM);
        (zoom as ZoomCameraFunction).SetUseCollision = false;
    }

    protected override Type GetCameraFunctionType(CinemachineCameraFunction.EFUNCTION_TYPE InFunctionType)
    {
        switch (InFunctionType)
        {
            case CinemachineCameraFunction.EFUNCTION_TYPE.ROTATION:
                return typeof(ObserverViewRotationCameraFunction);
            case CinemachineCameraFunction.EFUNCTION_TYPE.OBSERVER:
                return typeof(ObserverViewCameraFunction);
        }

        return base.GetCameraFunctionType(InFunctionType);
    }
}