/**
* FreeViewCinemachineExtension.cs
* 작성자 : songch
* 작성일 : 2023-07-06 오후 6:41:49
*/

using UnityEngine;
using Repository.Model;
using ECAMERA_FUNCTION_TYPE = CinemachineCameraFunction.EFUNCTION_TYPE;

[AddComponentMenu("")] // Hide in menu
[DisallowMultipleComponent]
public class FreeViewCinemachineExtension : CameraExtension
{
    public override void Setup(TdCharacterCameraModeSetting InSetting)
    {
        base.Setup(InSetting);
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.DOF, InSetting.CAMERACODE);
    }
}