/**
* ActionViewCollisionCameraFunction.cs
* 작성자 : songch
* 작성일 : 2023-09-08 오후 6:38:37
*/

using Cinemachine;
using Repository.Model;
using Unity.VisualScripting;
using UnityEngine;

public class ActionViewCollisionCameraFunction : CollisionCameraFunction
{

    public ActionViewCollisionCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    protected override bool GetCollisionDistanceFromMinHit(CameraState InState, LayerMask InLayer, out float InDistance)
    {
        float distance = CameraStateData.CurrentZoomDistance;
        bool isMinHit = false;
        if (MinHit.collider != null && InLayer.Contains(MinHit.collider.gameObject.layer))
        {
            isMinHit = true;
            _minHitPoint = MinHit.point;
            distance = Mathf.Max(MinHit.distance - _cameraRadius, _minDistance);
        }
        else if (_isCollision && MinHit.collider == null &&
            Physics.SphereCast(InState.CorrectedPosition, _cameraRadius, (_minHitPoint - InState.CorrectedPosition).normalized, out RaycastHit hit, _cameraRadius, InLayer))
        {
            isMinHit = true;
        }

        InDistance = distance;

        return isMinHit;
    }

    protected override void CollisionObjectsOnLayer(ref CameraState InState, LayerMask InLayer, float InDeltaTime)
    {
        float currentDistance = CameraStateData.CurrentZoomDistance;
        base.CollisionObjectsOnLayer(ref InState, InLayer, InDeltaTime);

        if (CameraStateData.CurrentZoomDistance > _framingTransposer.m_CameraDistance)
        {
            Vector3 displacement = RespectCameraDistance(ref InState, CameraStateData.CurrentZoomDistance, InDeltaTime, InLayer);
            if (displacement != Vector3.zero)
                CameraStateData.CurrentZoomDistance = currentDistance;
        }
    }
}