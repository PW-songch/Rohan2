/**
* QuarterViewCinemachineExtension.cs
* 작성자 : songch
* 작성일 : 2023-07-06 오후 6:41:13
*/

using UnityEngine;
using System;

[AddComponentMenu("")] // Hide in menu
[DisallowMultipleComponent]
public class QuarterViewCinemachineExtension : CameraExtension
{

    protected override Type GetCameraFunctionType(CinemachineCameraFunction.EFUNCTION_TYPE InFunctionType)
    {
        switch (InFunctionType)
        {
            case CinemachineCameraFunction.EFUNCTION_TYPE.ROTATION:
                return typeof(QuarterViewRotationCameraFunction);
        }

        return base.GetCameraFunctionType(InFunctionType);
    }
}