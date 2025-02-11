using Cinemachine;
using UnityEngine;
using static GameOptionsManager;

public class DOFCameraFunction : CinemachineCameraFunction
{
    public float MinDistance = 5f;
    public float IntensityMax = 1f;
    public float FocalLengthMax = 100f;
    public float DistanceWieght = 0.5f;

    private bool _isDOFAvailable = false;
    private Vector3 _lookOffset = new Vector3(0f, 1.65f, 0f);

    public DOFCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera,
        float InEpsilon)
        : base(InCameraExtension, in InVirtualCamera, InEpsilon)
    {
        _isDOFAvailable = LogicContext.GAME_OPTIONS.GraphicQualityType() ==
                          GameOptionsManager.EGRAPHICS_GRAPHIC_QUALITY.ULTRA;

        LogicContext.GAME_OPTIONS.AddEventPostApply(EGAME_OPTIONS_KEY.GRAPHICS_GRAPHIC_QUALITY, OnChangeGraphicQuality);
    }

    private void OnChangeGraphicQuality(string InGraphicQuality)
    {
        var mode = (EGRAPHICS_GRAPHIC_QUALITY)LogicContext.GAME_OPTIONS.GetGraphicQualityType(InGraphicQuality);
        var dofAvailable = mode == EGRAPHICS_GRAPHIC_QUALITY.ULTRA;
        if (_isDOFAvailable && !dofAvailable)
            LogicContext.CAMERA.StopDOFEffect();
        _isDOFAvailable = dofAvailable;
    }

    private bool GetCameraDistance(out float OutDistance, out float OutIntensity, out float OutFocalLength)
    {
        OutDistance = 0f;
        OutIntensity = 0f;
        OutFocalLength = 0f;

        var playerEntity = EntityController.MyCharacter;
        if (null == playerEntity)
            return false;

        var distance = Vector3.Distance(LogicContext.CAMERA.MainCameraTransform.position, playerEntity.Position + _lookOffset);
        if (distance > MinDistance)
            return false;

        OutDistance = distance;
        OutFocalLength = FocalLengthMax;
        var factor = Mathf.Clamp01((MinDistance - distance) * DistanceWieght);
        OutIntensity = Mathf.Min((factor * IntensityMax), IntensityMax);

        return true;
    }

    public override void ChangeViewMode(bool InChange)
    {
        base.ChangeViewMode(InChange);
        if (InChange)
        {
            LogicContext.CAMERA.FadeOutDOFEffect();
        }
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage,
        ref CameraState InState, float InDeltaTime)
    {
        if (!_isDOFAvailable)
            return false;

        if (LogicContext.CINEMA.IsPlaying == true || LogicContext.GACHA.IsInGacha)
        {
            LogicContext.CAMERA.StopDOFEffect();
            return false;
        }

        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage != CinemachineCore.Stage.Noise)
            return false;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (virtualCamera == null)
            return false;

        if (!GetCameraDistance(out float outDistance, out float outIntensity, out float outFocalLength))
        {
            LogicContext.CAMERA.StopDOFEffect();
            return false;
        }

        LogicContext.CAMERA.PlayDOFEffect(outDistance, outIntensity, outFocalLength);
        PostPipelineStageProcess();

        return true;
    }
}