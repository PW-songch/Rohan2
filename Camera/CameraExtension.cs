/**
* CameraExtension.cs
* 작성자 : jeongmin-kim
* 작성일 : 2023-03-16 오후 6:55:28
*/

using UnityEngine;
using Cinemachine;
using Repository.Model;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using ECAMERA_FUNCTION_TYPE = CinemachineCameraFunction.EFUNCTION_TYPE;


[AddComponentMenu("")] // Hide in menu
[DisallowMultipleComponent]
public class CameraExtension : CinemachineExtension
{
    [SerializeReference] protected RotationCameraFunction _rotationCameraFunction;
    [SerializeReference] protected ZoomCameraFunction _zoomCameraFunction;
    [SerializeReference] protected CollisionCameraFunction _collisionCameraFunction;
    [SerializeReference] protected FOVCameraFunction _fovCameraFunction;
    [SerializeReference] protected ShakeCameraFunction _shakeCameraFunction;
    [SerializeReference] protected LookAtCameraFunction _lookAtCameraFunction;
    [SerializeReference] protected DampingCameraFunction _dampingCameraFunction;
    [SerializeReference] protected FocusCameraFunction focusCameraFunction;
    [SerializeReference] protected RideCameraFunction _rideCameraFunction;
    [SerializeReference] protected DOFCameraFunction _dofCameraFunction;

    protected Dictionary<ECAMERA_FUNCTION_TYPE, CinemachineCameraFunction> _dicCinemachineCameraFunction = new();
    protected bool _isActive = true;

    public ZoomCameraFunction ZoomFunction => _zoomCameraFunction;
    public bool IsCollisioning => (_collisionCameraFunction != null) ? _collisionCameraFunction.IsCollisioning : false;

    public virtual void Pause(in IList<ECAMERA_FUNCTION_TYPE> IgnoreTypeList = default)
    {
        _isActive = false;
        if (_dicCinemachineCameraFunction != null)
        {
            foreach (KeyValuePair<ECAMERA_FUNCTION_TYPE, CinemachineCameraFunction> pair in _dicCinemachineCameraFunction)
            {
                if (!(IgnoreTypeList?.Contains(pair.Key) == true))
                    pair.Value.Pause();
            }
        }
    }

    public virtual void Resume(in IList<ECAMERA_FUNCTION_TYPE> IgnoreTypeList = default)
    {
        _isActive = true;
        if (_dicCinemachineCameraFunction != null)
        {
            foreach (KeyValuePair<ECAMERA_FUNCTION_TYPE, CinemachineCameraFunction> pair in _dicCinemachineCameraFunction)
            {
                if (!(IgnoreTypeList?.Contains(pair.Key) == true))
                    pair.Value.Resume();
            }
        }
    }

    public virtual void ChangeViewMode(bool InChange)
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.ChangeViewMode(InChange);
        }
    }

    public virtual void SetFollowTransform(in Transform InFollowTransform)
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.SetFollowTransform(InFollowTransform);
        }
    }

    public virtual void Setup(TdCharacterCameraModeSetting InSetting)
    {
        Clear();

        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.FOV, InSetting.CAMERACODE);
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.ZOOM, InSetting.CAMERACODE);
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.SHAKE, InSetting.CAMERACODE);
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.DAMPING, InSetting.CAMERACODE);

        if (InSetting.USE_ROTATION == true)
            AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.ROTATION, InSetting.CAMERACODE);

        if (InSetting.USE_COLLISION == true)
            AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.COLLISION, InSetting.CAMERACODE);

        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.FOCUS, InSetting.CAMERACODE);
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.RIDE, InSetting.CAMERACODE);
        
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.Setup(InSetting);
        }
    }

    public virtual void Setup(TdOutgameCharacterCamera InSetting)
    {
        Clear();

        if (InSetting.USE_ROTATION == true)
            AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.ROTATION);

        if (InSetting.USE_LOOKAT == true)
        {
            _lookAtCameraFunction = Activator.CreateInstance(GetCameraFunctionType(ECAMERA_FUNCTION_TYPE.LOOKAT),
                this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as LookAtCameraFunction;
            _lookAtCameraFunction.Setup(InSetting);
            AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.LOOKAT, _lookAtCameraFunction);
        }

        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.ZOOM);

        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.Setup(InSetting);
        }
    }

    public virtual void SetToCurrent()
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.SetToCurrent();
        }
    }

    public virtual void SetupForSkillPreview()
    {
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.SHAKE, CAMERA_TYPE.CT_NULL);
        AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE.DAMPING, CAMERA_TYPE.CT_NULL);
    }

    public virtual void ResetExtension(TdCharacterCameraModeSetting InSetting)
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.Setup(InSetting, true);
        }
    }

    protected void AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE InFunctionType, CAMERA_TYPE InCameraType = CAMERA_TYPE.CT_NULL)
    {
        switch (InFunctionType)
        {
            case ECAMERA_FUNCTION_TYPE.ROTATION:
                _rotationCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as RotationCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _rotationCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.ZOOM:
                _zoomCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as ZoomCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _zoomCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.COLLISION:
                _collisionCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as CollisionCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _collisionCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.FOV:
                _fovCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as FOVCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _fovCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.SHAKE:
                _shakeCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as ShakeCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _shakeCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.DAMPING:
                {
                    var camFunction = GetCameraFunction(ECAMERA_FUNCTION_TYPE.DAMPING);
                    if (camFunction == null)
                    {
                        _dampingCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                            this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as DampingCameraFunction;
                        AddCinemachineCameraFunction(InFunctionType, _dampingCameraFunction);
                    }
                    else if (_dampingCameraFunction == null)
                    {
                        _dampingCameraFunction = camFunction as DampingCameraFunction;
                    }
                }
                break;
            case ECAMERA_FUNCTION_TYPE.FOCUS:
                focusCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as FocusCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, focusCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.RIDE:
                _rideCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as RideCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _rideCameraFunction);
                break;
            case ECAMERA_FUNCTION_TYPE.DOF:
                _dofCameraFunction = Activator.CreateInstance(GetCameraFunctionType(InFunctionType),
                    this, VirtualCamera as CinemachineVirtualCamera, Epsilon) as DOFCameraFunction;
                AddCinemachineCameraFunction(InFunctionType, _dofCameraFunction);
                break;
        }
    }

    protected virtual Type GetCameraFunctionType(ECAMERA_FUNCTION_TYPE InFunctionType)
    {
        switch (InFunctionType)
        {
            case ECAMERA_FUNCTION_TYPE.ROTATION:
                return typeof(RotationCameraFunction);
            case ECAMERA_FUNCTION_TYPE.ZOOM:
                return typeof(ZoomCameraFunction);
            case ECAMERA_FUNCTION_TYPE.COLLISION:
                return typeof(CollisionCameraFunction);
            case ECAMERA_FUNCTION_TYPE.FOV:
                return typeof(FOVCameraFunction);
            case ECAMERA_FUNCTION_TYPE.SHAKE:
                return typeof(ShakeCameraFunction);
            case ECAMERA_FUNCTION_TYPE.LOOKAT:
                return typeof(LookAtCameraFunction);
            case ECAMERA_FUNCTION_TYPE.DAMPING:
                return typeof(DampingCameraFunction);
            case ECAMERA_FUNCTION_TYPE.FOCUS:
                return typeof(FocusCameraFunction);
            case ECAMERA_FUNCTION_TYPE.RIDE:
                return typeof(RideCameraFunction);
            case ECAMERA_FUNCTION_TYPE.DOF:
                return typeof(DOFCameraFunction);
        }

        return null;
    }

    protected void AddCinemachineCameraFunction(ECAMERA_FUNCTION_TYPE InFunctionType, CinemachineCameraFunction function)
    {
        if (function != null && _dicCinemachineCameraFunction.ContainsKey(InFunctionType) == false)
            _dicCinemachineCameraFunction.Add(InFunctionType, function);
    }

    public CinemachineCameraFunction GetCameraFunction(ECAMERA_FUNCTION_TYPE InType)
    {
        if (_dicCinemachineCameraFunction.TryGetValue(InType, out var cameraFunction))
            return cameraFunction;
        return null;
    }

    public void EnableCameraFunction(ECAMERA_FUNCTION_TYPE InType, bool InEnable)
    {
        GetCameraFunction(InType)?.SetEnable(InEnable);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.OnEnable();
        }
    }

    protected virtual void OnDisable()
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.OnDisable();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Clear();
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime);
        }
    }

    public void Clear()
    {
        if (_dicCinemachineCameraFunction != null)
        {
            var e = _dicCinemachineCameraFunction.Values.GetEnumerator();
            while (e.MoveNext())
                e.Current.OnDestroy();

            _dicCinemachineCameraFunction.Clear();
        }
    }
}