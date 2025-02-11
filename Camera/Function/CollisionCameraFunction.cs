using Cinemachine;
using Repository.Model;
using UnityEngine;
using Defines;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class CollisionCameraFunction : CinemachineCameraFunction
{
    public LayerMask CollisionLayerMask = 0;
    public LayerMask TransparentLayerMask = 0;
    public float CameraRadius = 0.3f;
    public float MinDistanceCollision = 0.8f;

    protected float _minDistance;
    protected float _maxDistance;
    protected float _defaultDistance;
    protected float _moveVelocity;

    private Vector3 _minTrackedOffset = Vector3.zero;
    private Vector3 _cameraOffset = Vector3.zero;
    private Vector3 _lookAtPosition = Vector3.zero;
    private Vector3 _endPosition = Vector3.zero;

    public RaycastHit[] Hits;
    public RaycastHit MinHit;
    private Vector3[] _raycastPositions;
    protected Vector3 _minHitPoint;

    private CameraCollider _cameraCollider = null;
    private SphereCollider _sphereCollider;
    private GameObject _sphereColliderGameObject;
    private Collider[] _colliderBuffer = new Collider[5];

    protected bool _isCollision = false;
    protected float _collisionDelay = 0.1f;

    protected float _cameraRadius => CameraRadius + (VirtualCamera?.m_Lens.NearClipPlane).Value;

    public bool IsCollisioning => _isCollision || MinHit.collider != null;

    //raycast count
    private const int COLLISION_RAY_COUNT = 3;
    //camera collision size
    public readonly float COLLISION_PROBE_SIZE = 0.5f;
    //collision detect lerp speed
    public readonly float COLLISION_SMOOTH_TIME = 0.2f;
    private const float PRECISION_SLUSH = 0.001f;


    public CollisionCameraFunction(CameraExtension InCameraExtension, in CinemachineVirtualCamera InVirtualCamera, float InEpsilon)
        : base(InCameraExtension, InVirtualCamera, InEpsilon)
    {
    }

    protected override void AddEvent()
    {
        base.AddEvent();
        LogicContext.INPUT.OnChangeScrollValue_Event += OnChangeScrollValue_Event;
    }

    protected override void RemoveEvent()
    {
        base.RemoveEvent();
        LogicContext.INPUT.OnChangeScrollValue_Event -= OnChangeScrollValue_Event;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        DestroySphereCollider();
    }

    public override void Setup(TdCharacterCameraModeSetting InSetting, bool InReset = false)
    {
        base.Setup(InSetting, InReset);

        _minDistance = InSetting.CAMERADISTANCE_MIN;
        _maxDistance = InSetting.CAMERADITANCE_MAX;
        _defaultDistance = InSetting.CAMERADISTANCE_DEFAULT;
        _minTrackedOffset.y = InSetting.TRACKEDPOSITION_OFFSET_Y_WAIT;
        InternalSetup(InReset);
    }

    public override void Setup(TdOutgameCharacterCamera InSetting)
    {
        base.Setup(InSetting);

        _minDistance = InSetting.CAMERADISTANCE_MIN;
        _maxDistance = InSetting.CAMERADITANCE_MAX;
        _defaultDistance = InSetting.CAMERADISTANCE_DEFAULT;
        _minTrackedOffset.y = InSetting.TRACKEDPOSITION_OFFSET_Y;
        InternalSetup();
    }

    protected override void InternalSetup(bool InReset = false)
    {
        base.InternalSetup(InReset);

        _isCollision = false;

        CollisionLayerMask = CollisionLayerMask.AddToMask(Layer.LAYER_TERRAIN, Layer.LAYER_PROP_STATIC);
        TransparentLayerMask = TransparentLayerMask.AddToMask(Layer.LAYER_TERRAIN_ENVIRONMENT);

        if (_raycastPositions == null)
            _raycastPositions = new Vector3[COLLISION_RAY_COUNT];
        if (Hits == null)
            Hits = new RaycastHit[COLLISION_RAY_COUNT];

        _minDistance = Mathf.Min(_minDistance, MinDistanceCollision);

        if (InReset == false)
        {
            _framingTransposer.m_CameraDistance = _defaultDistance;

            CinemachineVirtualCamera virtualCamera = VirtualCamera;
            if (virtualCamera != null)
            {
                _cameraCollider = virtualCamera.GetComponent<CameraCollider>();
                if (_cameraCollider == null)
                    _cameraCollider = virtualCamera.AddComponent<CameraCollider>();

                _cameraCollider?.Setup(virtualCamera, _maxDistance, COLLISION_PROBE_SIZE,
                    EntityController.MyCharacter != null && EntityController.MyCharacter.OnCommand_RideOn ?
                        0f : _framingTransposer.m_TrackedObjectOffset.y - _minTrackedOffset.y, CollisionLayerMask, TransparentLayerMask);
            }
        }
    }

    public override void SetToCurrent()
    {
        base.SetToCurrent();

        _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (virtualCamera != null)
        {
            _cameraCollider = virtualCamera.GetComponent<CameraCollider>();
            if (_cameraCollider == null)
                _cameraCollider = virtualCamera.AddComponent<CameraCollider>();
            _cameraCollider?.Setup(virtualCamera, _maxDistance, COLLISION_PROBE_SIZE,
                EntityController.MyCharacter != null && EntityController.MyCharacter.OnCommand_RideOn ?
                    0f : _framingTransposer.m_TrackedObjectOffset.y - _minTrackedOffset.y, CollisionLayerMask, TransparentLayerMask);
        }
    }

    protected override void OnChangeScrollValue_Event(float InValue)
    {
        base.OnChangeScrollValue_Event(InValue);

        if (_isCollision)
        {
            if (_framingTransposer != null)
            {
                if (_framingTransposer.m_CameraDistance > CameraStateData.OriginZoomDistance)
                {
                    _framingTransposer.m_CameraDistance = CameraStateData.OriginZoomDistance;
                    _isCollision = false;
                }
            }
        }
    }

    private void UpdateEndPosition(in CinemachineVirtualCamera InVirtualCamera)
    {
        _cameraOffset.z = CameraStateData.OriginZoomDistance;
        _endPosition = _lookAtPosition - (InVirtualCamera.transform.rotation * _cameraOffset);
    }

    private void CheckCollisions(in CinemachineVirtualCamera InVirtualCamera)
    {
        for (int i = 0, angle = 0; i < COLLISION_RAY_COUNT; i++, angle += 360 / COLLISION_RAY_COUNT)
        {
            Vector3 raycastLocalEndPoint = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * COLLISION_PROBE_SIZE;
            _raycastPositions[i] = _endPosition + (InVirtualCamera.transform.rotation * raycastLocalEndPoint);
            Physics.Linecast(_lookAtPosition, _raycastPositions[i], out Hits[i], CollisionLayerMask);
        }
    }

    private void UpdateMinHit()
    {
        MinHit = default;

        if (Hits.IsNullOrEmpty() == true)
            return;

        float distance = float.MaxValue;
        int index = 0;
        for ( int i = 0, length = Hits.Length; i<length; ++i)
        {
            if (Hits[i].collider == null || Hits[i].distance > distance)
                continue;

            distance = Hits[i].distance;
            index = i;
        }

        MinHit = Hits[index];
    }

    private void OnDetectCollision(in CinemachineVirtualCamera InVirtualCamera, float InDeltaTime)
    {
        UpdateEndPosition(InVirtualCamera);
        CheckCollisions(InVirtualCamera);
        UpdateMinHit();

        float distance = CameraStateData.CurrentZoomDistance;
        if (_isCollision)
        {
            if (MinHit.collider != null && MinHit.distance < _minDistance)
                distance = MinHit.distance;
        }
        else
            distance = (MinHit.collider == null) ? CameraStateData.OriginZoomDistance : (MinHit.distance <= _minDistance ? _minDistance : MinHit.distance);

        if (CameraStateData.CurrentZoomDistance != distance)
        {
            if (MinHit.collider != null || !IsEnableRestoreDefaultSetting())
                CameraStateData.CurrentZoomDistance = Mathf.Max(Mathf.SmoothDamp(_framingTransposer.m_CameraDistance, distance, ref _moveVelocity, COLLISION_SMOOTH_TIME), _minDistance);
            else
                CameraStateData.CurrentZoomDistance = Mathf.Lerp(_framingTransposer.m_CameraDistance, distance, InDeltaTime);
        }
    }

    private void OnDetectCollisionCameraRadius(ref CameraState InState, float InDeltaTime)
    {
        var layerMask = LayerMaskExtensionMethods.Create(Layer.LAYER_TERRAIN, Layer.LAYER_PROP_STATIC);
        CollisionObjectsOnLayer(ref InState, layerMask, InDeltaTime);
    }

    protected virtual bool GetCollisionDistanceFromMinHit(CameraState InState, LayerMask InLayer, out float InDistance)
    {
        float distance = CameraStateData.CurrentZoomDistance;
        if (MinHit.collider != null && InLayer.Contains(MinHit.collider.gameObject.layer))
        {
            distance = Mathf.Max(MinHit.distance - _cameraRadius, _minDistance);
            _minHitPoint = MinHit.point;
        }

        InDistance = distance;

        return distance != CameraStateData.CurrentZoomDistance;
    }

    protected virtual void CollisionObjectsOnLayer(ref CameraState InState, LayerMask InLayer, float InDeltaTime)
    {
        bool isMinHit = GetCollisionDistanceFromMinHit(InState, InLayer, out float distance);
        if (isMinHit)
            _isCollision = true;

        Vector3 displacement = RespectCameraDistance(ref InState, distance, InDeltaTime, InLayer);
        if (displacement != Vector3.zero)
        {
            _isCollision = true;

            if (CameraStateData.CurrentZoomDistance > MinDistanceCollision)
                distance = Mathf.Max(distance - displacement.magnitude, _minDistance);
        }
        else if (!isMinHit && _isCollision && (IsRotationCamera || IsMovingFollowTarget))
        {
            _isCollision = false;
        }

        if (distance != CameraStateData.CurrentZoomDistance)
        {
            if (distance > _framingTransposer.m_CameraDistance)
            {
                CameraStateData.CurrentZoomDistance = Mathf.Max(Mathf.SmoothDamp(
                    _framingTransposer.m_CameraDistance, distance, ref _moveVelocity, COLLISION_SMOOTH_TIME), _framingTransposer.m_CameraDistance);
            }
            else
                CameraStateData.CurrentZoomDistance = distance;
        }
    }

    public override bool PostPipelineStageCallback(CinemachineVirtualCameraBase InVcam, CinemachineCore.Stage InStage, ref CameraState InState, float InDeltaTime)
    {
        if (!base.PostPipelineStageCallback(InVcam, InStage, ref InState, InDeltaTime) || InStage != CinemachineCore.Stage.Body)
            return false;

        CinemachineVirtualCamera virtualCamera = VirtualCamera;
        if (_framingTransposer == null || virtualCamera.Follow == null || _cameraCollider == null)
            return false;

        _lookAtPosition = InState.CorrectedPosition + InState.CorrectedOrientation * new Vector3(0f, 0f, CameraStateData.CurrentZoomDistance);
        OnDetectCollision(virtualCamera, InDeltaTime);
        OnDetectCollisionCameraRadius(ref InState, InDeltaTime);

        _cameraCollider?.SetColliderLength(
            EntityController.MyCharacter != null && EntityController.MyCharacter.OnCommand_RideOn ?
            0f : _framingTransposer.m_TrackedObjectOffset.y - _minTrackedOffset.y);

        _framingTransposer.m_CameraDistance = CameraStateData.CurrentZoomDistance;

        PostPipelineStageProcess();

        return true;
    }

    private Vector3 PreserveLineOfSight(CameraState state, LayerMask InLayer)
    {
        Vector3 displacement = Vector3.zero;
        if (state.HasLookAt && InLayer != 0 && TransparentLayerMask.Contains(InLayer) == false)
        {
            RaycastHit hitInfo = new RaycastHit();
            displacement = PullCameraInFrontOfNearestObstacle(
                state.CorrectedPosition, state.ReferenceLookAt, InLayer & ~TransparentLayerMask, ref hitInfo);
        }

        return displacement;
    }

    private Vector3 PullCameraInFrontOfNearestObstacle(Vector3 cameraPos, Vector3 lookAtPos, int layerMask, ref RaycastHit hitInfo)
    {
        Vector3 displacement = Vector3.zero;
        Vector3 dir = cameraPos - lookAtPos;
        float targetDistance = dir.magnitude.FloorTo(4);
        if (targetDistance > EPSILON)
        {
            dir /= targetDistance;
            float minDistanceFromTarget = Mathf.Max(_minDistance, EPSILON);
            float rayLength = targetDistance - minDistanceFromTarget;
            if (_maxDistance > EPSILON)
                rayLength = Mathf.Min(_maxDistance, rayLength);

            Ray ray = new Ray(cameraPos - rayLength * dir, dir);
            rayLength += PRECISION_SLUSH;
            if (rayLength > EPSILON)
            {
                if (RuntimeUtility.RaycastIgnoreTag(ray, out hitInfo, rayLength, layerMask, string.Empty))
                {
                    float adjustment = Mathf.Max(0, hitInfo.distance - PRECISION_SLUSH);
                    displacement = ray.GetPoint(adjustment) - cameraPos;
                }
            }
        }

        return displacement;
    }

    private Vector3 RespectCameraRadius(Vector3 cameraPos, Vector3 lookAtPos, LayerMask InLayer)
    {
        Vector3 result = Vector3.zero;
        float radius = _cameraRadius;
        if (radius < EPSILON || InLayer == 0)
            return result;

        Vector3 dir = cameraPos - lookAtPos;
        float distance = dir.magnitude.FloorTo(4);
        if (distance > EPSILON)
            dir /= distance;

        RaycastHit hitInfo;
        int numObstacles = Physics.OverlapSphereNonAlloc(
            cameraPos, radius, _colliderBuffer, InLayer, QueryTriggerInteraction.Ignore);
        if (numObstacles == 0 && TransparentLayerMask != 0 && distance > _minDistance - EPSILON)
        {
            float d = distance - _minDistance;
            Vector3 targetPos = lookAtPos + dir * _minDistance;
            if (RuntimeUtility.RaycastIgnoreTag(new Ray(targetPos, dir), out hitInfo, d, InLayer, string.Empty))
            {
                Collider c = hitInfo.collider;
                if (!c.Raycast(new Ray(cameraPos, -dir), out hitInfo, d))
                    _colliderBuffer[numObstacles++] = c;
            }
        }

        if (numObstacles > 0 /*&& distance == 0 || distance > _minDistance*/)
        {
            var scratchCollider = GetSphereCollider();
            scratchCollider.radius = radius;

            Vector3 newCamPos = cameraPos;
            for (int i = 0; i < numObstacles; ++i)
            {
                Collider c = _colliderBuffer[i];

                if (distance > _minDistance)
                {
                    dir = newCamPos - lookAtPos;
                    float d = dir.magnitude.FloorTo(3);
                    if (d > EPSILON)
                    {
                        dir /= d;
                        var ray = new Ray(lookAtPos, dir);
                        if (c.Raycast(ray, out hitInfo, d + radius))
                            newCamPos = ray.GetPoint(hitInfo.distance) - (dir * PRECISION_SLUSH);
                    }
                }
                if (Physics.ComputePenetration(scratchCollider, newCamPos, Quaternion.identity,
                    c, c.transform.position, c.transform.rotation, out var offsetDir, out var offsetDistance))
                {
                    newCamPos += offsetDir * offsetDistance;
                }
            }
            result = newCamPos - cameraPos;
        }

        return result;
    }

    protected Vector3 RespectCameraDistance(ref CameraState InState, float InDistance, float InDeltaTime, LayerMask InLayer)
    {
        float curDistance = _framingTransposer.m_CameraDistance;
        Vector3 displacement = Vector3.zero;

        if (InDistance != _framingTransposer.m_CameraDistance)
        {
            _framingTransposer.m_CameraDistance = InDistance;
            _framingTransposer.MutateCameraState(ref InState, InDeltaTime);
        }

        displacement = PreserveLineOfSight(InState, InLayer);
        Vector3 cameraPos = InState.CorrectedPosition + displacement;
        displacement += RespectCameraRadius(cameraPos, InState.HasLookAt ? InState.ReferenceLookAt : cameraPos, InLayer);

        if (InDistance != curDistance)
        {
            _framingTransposer.m_CameraDistance = curDistance;
            _framingTransposer.MutateCameraState(ref InState, InDeltaTime);
        }

        return displacement;
    }

    private SphereCollider GetSphereCollider()
    {
        if (_sphereColliderGameObject == null)
        {
            _sphereColliderGameObject = new GameObject("Cinemachine Scratch Collider");
            _sphereColliderGameObject.hideFlags = HideFlags.HideAndDontSave;
            _sphereColliderGameObject.transform.position = Vector3.zero;
            _sphereColliderGameObject.SetActive(true);
            _sphereCollider = _sphereColliderGameObject.AddComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            var rb = _sphereColliderGameObject.AddComponent<Rigidbody>();
            rb.detectCollisions = false;
            rb.isKinematic = true;
        }

        return _sphereCollider;
    }

    private void DestroySphereCollider()
    {
        if (_sphereColliderGameObject != null)
        {
            _sphereColliderGameObject.SetActive(false);
            RuntimeUtility.DestroyObject(_sphereColliderGameObject.GetComponent<Rigidbody>());
        }

        RuntimeUtility.DestroyObject(_sphereCollider);
        RuntimeUtility.DestroyObject(_sphereColliderGameObject);
        _sphereColliderGameObject = null;
        _sphereCollider = null;
    }

    public void OnSetCollisionState(bool InCollision)
    {
        if(_isCollision)
            _isCollision = InCollision;
    }
}
