/**
* ActionViewCinemachineExtension.cs
* 작성자 : songch
* 작성일 : 2023-07-06 오후 6:42:14
*/

using UnityEngine;
using System;
using Repository.Model;

[AddComponentMenu("")] // Hide in menu
[DisallowMultipleComponent]
public class ActionViewCinemachineExtension : CameraExtension
{

    protected override Type GetCameraFunctionType(CinemachineCameraFunction.EFUNCTION_TYPE InFunctionType)
    {
        switch (InFunctionType)
        {
            case CinemachineCameraFunction.EFUNCTION_TYPE.ROTATION:
                return typeof(ActionViewRotationCameraFunction);
            case CinemachineCameraFunction.EFUNCTION_TYPE.COLLISION:
                return typeof(ActionViewCollisionCameraFunction);
        }

        return base.GetCameraFunctionType(InFunctionType);
    }
}