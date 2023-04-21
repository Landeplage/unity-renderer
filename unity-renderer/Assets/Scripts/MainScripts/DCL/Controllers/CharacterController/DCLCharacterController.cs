using DCL;
using DCL.Configuration;
using DCL.Helpers;
using UnityEngine;
using Cinemachine;
using System;
using Environment = DCL.Environment;

public class DCLCharacterController : MonoBehaviour
{
    public static DCLCharacterController i { get; private set; }

    private const float CONTROLLER_DRIFT_OFFSET = 0.15f;

    [Header("Movement")]
    public float minimumYPosition = 1f;

    public float groundCheckExtraDistance = 0.1f;
    public float gravity = -55f;
    public float jumpForce = 8f;
    public float movementSpeed = 10f;
    public float runningSpeedMultiplier = 1.4f;

    public DCLCharacterPosition characterPosition;

    [Header("Collisions")]
    public LayerMask groundLayers;

    [Header("Additional Camera Layers")]
    public LayerMask cameraLayers;

    [System.NonSerialized]
    public bool initialPositionAlreadySet = false;

    [System.NonSerialized]
    public bool characterAlwaysEnabled = true;

    [System.NonSerialized]
    public CharacterController characterController;

    FreeMovementController freeMovementController;

    new Collider collider;

    float lastUngroundedTime = 0f;
    float lastJumpButtonPressedTime = 0f;
    float lastMovementReportTime;
    float originalGravity;
    Vector3 lastLocalGroundPosition;

    Vector3 lastCharacterRotation;
    Vector3 lastGlobalCharacterRotation;

    Vector3 velocity = Vector3.zero;

    public bool isRunning { get; private set; } = false;
    public bool isMovingByUserInput { get; private set; } = false;
    public bool isJumping { get; private set; } = false;
    public bool isGrounded { get; private set; }
    public bool isOnMovingPlatform { get; private set; }

    internal Transform groundTransform;

    Vector3 lastPosition;
    Vector3 groundLastPosition;
    Quaternion groundLastRotation;
    bool jumpButtonPressed = false;

    [Header("InputActions")]
    public InputAction_Hold jumpAction;

    public InputAction_Hold sprintAction;

    public Vector3 moveVelocity;

    private InputAction_Hold.Started jumpStartedDelegate;
    private InputAction_Hold.Finished jumpFinishedDelegate;
    private InputAction_Hold.Started sprintStartedDelegate;
    private InputAction_Hold.Finished sprintFinishedDelegate;

    private Vector3NullableVariable characterForward => CommonScriptableObjects.characterForward;

    public static System.Action<DCLCharacterPosition> OnCharacterMoved;
    public static System.Action<DCLCharacterPosition> OnPositionSet;
    public event System.Action<float> OnUpdateFinish;

    public GameObject avatarGameObject;
    public GameObject firstPersonCameraGameObject;

    [SerializeField]
    private InputAction_Measurable characterYAxis;

    [SerializeField]
    private InputAction_Measurable characterXAxis;

    private Vector3Variable cameraForward => CommonScriptableObjects.cameraForward;
    private Vector3Variable cameraRight => CommonScriptableObjects.cameraRight;

    private readonly DataStore_Player dataStorePlayer = DataStore.i.player;

    [System.NonSerialized]
    public float movingPlatformSpeed;
    private CollisionFlags lastCharacterControllerCollision;

    public event System.Action OnJump;
    public event System.Action OnHitGround;
    public event System.Action<float> OnMoved;

    void Awake()
    {
        if (i != null)
        {
            Destroy(gameObject);
            return;
        }

        i = this;
        originalGravity = gravity;

        SubscribeToInput();
        CommonScriptableObjects.playerUnityPosition.Set(Vector3.zero);
        dataStorePlayer.playerWorldPosition.Set(Vector3.zero);
        CommonScriptableObjects.playerCoords.Set(Vector2Int.zero);
        dataStorePlayer.playerGridPosition.Set(Vector2Int.zero);
        CommonScriptableObjects.playerUnityEulerAngles.Set(Vector3.zero);

        characterPosition = new DCLCharacterPosition();
        characterController = GetComponent<CharacterController>();
        freeMovementController = GetComponent<FreeMovementController>();
        collider = GetComponent<Collider>();

        CommonScriptableObjects.worldOffset.OnChange += OnWorldReposition;

        lastPosition = transform.position;
        transform.parent = null;

        CommonScriptableObjects.rendererState.OnChange += OnRenderingStateChanged;
        OnRenderingStateChanged(CommonScriptableObjects.rendererState.Get(), false);

        if (avatarGameObject == null || firstPersonCameraGameObject == null)
        {
            throw new System.Exception("Both the avatar and first person camera game objects must be set.");
        }

        var worldData = DataStore.i.Get<DataStore_World>();
        worldData.avatarTransform.Set(avatarGameObject.transform);
        worldData.fpsTransform.Set(firstPersonCameraGameObject.transform);

        dataStorePlayer.lastTeleportPosition.OnChange += Teleport;
    }

    private float jumpHold;

    private void Update()
    {
    }

    private void SubscribeToInput()
    {
        jumpStartedDelegate = (action) =>
        {
            jumpHold = 0;
            lastJumpButtonPressedTime = Time.time;
            jumpButtonPressed = true;
        };
        jumpFinishedDelegate = (action) =>
        {
            jumpHold = Time.time - lastJumpButtonPressedTime;
            jumpButtonPressed = false;
        };
        jumpAction.OnStarted += jumpStartedDelegate;
        jumpAction.OnFinished += jumpFinishedDelegate;

        sprintStartedDelegate = (action) => isRunning = true;
        sprintFinishedDelegate = (action) => isRunning = false;
        sprintAction.OnStarted += sprintStartedDelegate;
        sprintAction.OnFinished += sprintFinishedDelegate;
    }

    void OnDestroy()
    {
        CommonScriptableObjects.worldOffset.OnChange -= OnWorldReposition;
        jumpAction.OnStarted -= jumpStartedDelegate;
        jumpAction.OnFinished -= jumpFinishedDelegate;
        sprintAction.OnStarted -= sprintStartedDelegate;
        sprintAction.OnFinished -= sprintFinishedDelegate;
        CommonScriptableObjects.rendererState.OnChange -= OnRenderingStateChanged;
        dataStorePlayer.lastTeleportPosition.OnChange -= Teleport;
        i = null;
    }

    void OnWorldReposition(Vector3 current, Vector3 previous)
    {
        Vector3 oldPos = this.transform.position;
        this.transform.position = characterPosition.unityPosition; //CommonScriptableObjects.playerUnityPosition;

        if (CinemachineCore.Instance.BrainCount > 0)
        {
            CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera?.OnTargetObjectWarped(transform, transform.position - oldPos);
        }
    }

    public void SetPosition(Vector3 newPosition)
    {
        // failsafe in case something teleports the player below ground collisions
        if (newPosition.y < minimumYPosition)
        {
            newPosition.y = minimumYPosition + 2f;
        }

        lastPosition = characterPosition.worldPosition;
        characterPosition.worldPosition = newPosition;
        transform.position = characterPosition.unityPosition;
        Environment.i.platform.physicsSyncController?.MarkDirty();

        CommonScriptableObjects.playerUnityPosition.Set(characterPosition.unityPosition);
        dataStorePlayer.playerWorldPosition.Set(characterPosition.worldPosition);
        Vector2Int playerPosition = Utils.WorldToGridPosition(characterPosition.worldPosition);
        CommonScriptableObjects.playerCoords.Set(playerPosition);
        dataStorePlayer.playerGridPosition.Set(playerPosition);
        dataStorePlayer.playerUnityPosition.Set(characterPosition.unityPosition);

        if (Moved(lastPosition))
        {
            if (Moved(lastPosition, useThreshold: true))
                ReportMovement();

            OnCharacterMoved?.Invoke(characterPosition);

            float distance = Vector3.Distance(characterPosition.worldPosition, lastPosition) - movingPlatformSpeed;

            if (distance > 0f && isGrounded)
                OnMoved?.Invoke(distance);
        }

        lastPosition = transform.position;
    }

    public void Teleport(string teleportPayload)
    {
        var payload = Utils.FromJsonWithNulls<Vector3>(teleportPayload);
        dataStorePlayer.lastTeleportPosition.Set(payload, notifyEvent: true);
    }

    private void Teleport(Vector3 newPosition, Vector3 prevPosition)
    {
        ResetGround();

        SetPosition(newPosition);

        if (OnPositionSet != null)
        {
            OnPositionSet.Invoke(characterPosition);
        }

        if (!initialPositionAlreadySet)
        {
            initialPositionAlreadySet = true;
        }
    }

    [System.Obsolete("SetPosition is deprecated, please use Teleport instead.", true)]
    public void SetPosition(string positionVector) { Teleport(positionVector); }

    public void SetEnabled(bool enabled) { this.enabled = enabled; }

    bool Moved(Vector3 previousPosition, bool useThreshold = false)
    {
        if (useThreshold)
            return Vector3.Distance(characterPosition.worldPosition, previousPosition) > 0.001f;
        else
            return characterPosition.worldPosition != previousPosition;
    }

    internal void LateUpdate()
    {
        if(!dataStorePlayer.canPlayerMove.Get())
            return;

        if (transform.position.y < minimumYPosition)
        {
            SetPosition(characterPosition.worldPosition);
            return;
        }

        if (freeMovementController.IsActive())
        {
            velocity = freeMovementController.CalculateMovement();
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
            velocity.y += gravity * Time.deltaTime;

            bool previouslyGrounded = isGrounded;

            if (!isJumping || velocity.y <= 0f)
                CheckGround();

            if (isGrounded)
            {
                isJumping = false;
                velocity.y = gravity * Time.deltaTime; // to avoid accumulating gravity in velocity.y while grounded
            }
            else if (previouslyGrounded && !isJumping)
            {
                lastUngroundedTime = Time.time;
            }

            if (characterForward.HasValue())
            {
                // Horizontal movement
                var speed = movementSpeed * (isRunning ? 1f : runningSpeedMultiplier);

                transform.forward = characterForward.Get().Value;

                var xzPlaneForward = Vector3.Scale(cameraForward.Get(), new Vector3(1, 0, 1));
                var xzPlaneRight = Vector3.Scale(cameraRight.Get(), new Vector3(1, 0, 1));

                Vector3 forwardTarget = Vector3.zero;

                if (characterYAxis.GetValue() > CONTROLLER_DRIFT_OFFSET)
                    forwardTarget += xzPlaneForward;
                if (characterYAxis.GetValue() < -CONTROLLER_DRIFT_OFFSET)
                    forwardTarget -= xzPlaneForward;

                if (characterXAxis.GetValue() > CONTROLLER_DRIFT_OFFSET)
                    forwardTarget += xzPlaneRight;
                if (characterXAxis.GetValue() < -CONTROLLER_DRIFT_OFFSET)
                    forwardTarget -= xzPlaneRight;

                if (forwardTarget.Equals(Vector3.zero))
                    isMovingByUserInput = false;
                else
                    isMovingByUserInput = true;


                forwardTarget.Normalize();
                velocity += forwardTarget * speed;
                CommonScriptableObjects.playerUnityEulerAngles.Set(transform.eulerAngles);
            }

            bool jumpButtonPressedWithGraceTime = jumpButtonPressed && (Time.time - lastJumpButtonPressedTime < 0.15f);

            if (jumpButtonPressedWithGraceTime) // almost-grounded jump button press allowed time
            {
                bool justLeftGround = (Time.time - lastUngroundedTime) < 0.1f;

                if (isGrounded || justLeftGround) // just-left-ground jump allowed time
                {
                    Jump();
                }
            }

            if (isJumping && jumpButtonPressed)
            {
                velocity.y += 0.2f;
            }


            //NOTE(Mordi): Detecting when the character hits the ground (for landing-SFX)
            if (isGrounded && !previouslyGrounded && (Time.time - lastUngroundedTime) > 0.4f)
            {
                OnHitGround?.Invoke();
            }
        }

        if (characterController.enabled)
        {
            //NOTE(Brian): Transform has to be in sync before the Move call, otherwise this call
            //             will reset the character controller to its previous position.
            Environment.i.platform.physicsSyncController?.Sync();
            lastCharacterControllerCollision = characterController.Move(velocity * Time.deltaTime);
        }

        SetPosition(PositionUtils.UnityToWorldPosition(transform.position));

        if ((DCLTime.realtimeSinceStartup - lastMovementReportTime) > PlayerSettings.POSITION_REPORTING_DELAY)
        {
            ReportMovement();
        }

        if (isOnMovingPlatform)
        {
            SaveLateUpdateGroundTransforms();
        }

        OnUpdateFinish?.Invoke(Time.deltaTime);
    }

    private void SaveLateUpdateGroundTransforms()
    {
        lastLocalGroundPosition = groundTransform.InverseTransformPoint(transform.position);

        if (CommonScriptableObjects.characterForward.HasValue())
        {
            lastCharacterRotation = groundTransform.InverseTransformDirection(CommonScriptableObjects.characterForward.Get().Value);
            lastGlobalCharacterRotation = CommonScriptableObjects.characterForward.Get().Value;
        }
    }

    void Jump()
    {
        if (isJumping)
            return;

        isJumping = true;
        isGrounded = false;

        ResetGround();

        velocity.y = jumpForce;
        //cameraTargetProbe.damping.y = dampingOnAir;

        OnJump?.Invoke();
    }

    public void ResetGround()
    {
        if (isOnMovingPlatform)
            CommonScriptableObjects.playerIsOnMovingPlatform.Set(false);

        isOnMovingPlatform = false;
        groundTransform = null;
        movingPlatformSpeed = 0;
    }

    void CheckGround()
    {
        if (groundTransform == null)
            ResetGround();

        if (isOnMovingPlatform)
        {
            Physics.SyncTransforms();
            //NOTE(Brian): This should move the character with the moving platform
            Vector3 newGroundWorldPos = groundTransform.TransformPoint(lastLocalGroundPosition);
            movingPlatformSpeed = Vector3.Distance(newGroundWorldPos, transform.position);
            transform.position = newGroundWorldPos;

            Vector3 newCharacterForward = groundTransform.TransformDirection(lastCharacterRotation);
            Vector3 lastFrameDifference = Vector3.zero;
            if (CommonScriptableObjects.characterForward.HasValue())
            {
                lastFrameDifference = CommonScriptableObjects.characterForward.Get().Value - lastGlobalCharacterRotation;
            }

            //NOTE(Kinerius) CameraStateTPS rotates the character between frames so we add the difference.
            //               if we dont do this, the character wont rotate when moving, only when the platform rotates
            var newForward = newCharacterForward + lastFrameDifference;

            if (newForward is { x: 0, y: 0, z: 0 })
                newForward = Vector3.forward;

            CommonScriptableObjects.characterForward.Set(newForward);
        }

        Transform transformHit = CastGroundCheckingRays();

        if (transformHit != null)
        {
            if (groundTransform == transformHit)
            {
                bool groundHasMoved = (transformHit.position != groundLastPosition || transformHit.rotation != groundLastRotation);

                if (!characterPosition.RepositionedWorldLastFrame()
                    && groundHasMoved)
                {
                    isOnMovingPlatform = true;
                    CommonScriptableObjects.playerIsOnMovingPlatform.Set(true);
                    Physics.SyncTransforms();
                    SaveLateUpdateGroundTransforms();

                    Quaternion deltaRotation = groundTransform.rotation * Quaternion.Inverse(groundLastRotation);
                    CommonScriptableObjects.movingPlatformRotationDelta.Set(deltaRotation);
                }
            }
            else
            {
                groundTransform = transformHit;
                CommonScriptableObjects.movingPlatformRotationDelta.Set(Quaternion.identity);
            }
        }
        else
        {
            ResetGround();
        }

        if (groundTransform != null)
        {
            groundLastPosition = groundTransform.position;
            groundLastRotation = groundTransform.rotation;
        }

        isGrounded = IsLastCollisionGround() || groundTransform != null && groundTransform.gameObject.activeInHierarchy;
    }

    public Transform CastGroundCheckingRays()
    {
        RaycastHit hitInfo;

        var result = CastGroundCheckingRays(transform, collider, groundCheckExtraDistance, 0.9f, groundLayers, out hitInfo);

        if ( result )
        {
            return hitInfo.transform;
        }

        return null;
    }

    public bool CastGroundCheckingRays(float extraDistance, float scale, out RaycastHit hitInfo)
    {
        if (CastGroundCheckingRays(transform, collider, extraDistance, scale, groundLayers | cameraLayers , out hitInfo))
            return true;

        return IsLastCollisionGround();
    }

    public bool CastGroundCheckingRay(float extraDistance, out RaycastHit hitInfo)
    {
        Bounds bounds = collider.bounds;
        float rayMagnitude = (bounds.extents.y + extraDistance);
        bool test = CastGroundCheckingRay(transform.position, out hitInfo, rayMagnitude, groundLayers);
        return IsLastCollisionGround() || test;
    }

    // We secuentially cast rays in 4 directions (only if the previous one didn't hit anything)
    public static bool CastGroundCheckingRays(Transform transform, Collider collider, float extraDistance, float scale, int groundLayers, out RaycastHit hitInfo)
    {
        Bounds bounds = collider.bounds;

        float rayMagnitude = (bounds.extents.y + extraDistance);
        float originScale = scale * bounds.extents.x;

        if (!CastGroundCheckingRay(transform.position, out hitInfo, rayMagnitude, groundLayers) // center
            && !CastGroundCheckingRay( transform.position + transform.forward * originScale, out hitInfo, rayMagnitude, groundLayers) // forward
            && !CastGroundCheckingRay( transform.position + transform.right * originScale, out hitInfo, rayMagnitude, groundLayers) // right
            && !CastGroundCheckingRay( transform.position + -transform.forward * originScale, out hitInfo, rayMagnitude, groundLayers) // back
            && !CastGroundCheckingRay( transform.position + -transform.right * originScale, out hitInfo, rayMagnitude, groundLayers)) // left
        {
            return false;
        }

        // At this point there is a guaranteed hit, so this is not null
        return true;
    }

    public static bool CastGroundCheckingRay(Vector3 origin, out RaycastHit hitInfo, float rayMagnitude, int groundLayers)
    {
        var ray = new Ray();
        ray.origin = origin;
        ray.direction = Vector3.down * rayMagnitude;

        var result = Physics.Raycast(ray, out hitInfo, rayMagnitude, groundLayers);

#if UNITY_EDITOR
        if ( result )
            Debug.DrawLine(ray.origin, hitInfo.point, Color.green);
        else
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
#endif

        return result;
    }

    void ReportMovement()
    {
        var reportPosition = characterPosition.worldPosition;
        var compositeRotation = Quaternion.LookRotation(characterForward.HasValue() ? characterForward.Get().Value : cameraForward.Get());
        var cameraRotation = Quaternion.LookRotation(cameraForward.Get());

        //NOTE(Brian): We have to wait for a Teleport before sending the ReportPosition, because if not ReportPosition events will be sent
        //             When the spawn point is being selected / scenes being prepared to be sent and the Kernel gets crazy.

        //             The race conditions that can arise from not having this flag can result in:
        //                  - Scenes not being sent for loading, making ActivateRenderer never being sent, only in WSS mode.
        //                  - Random teleports to 0,0 or other positions that shouldn't happen.
        //We are sending a fixated camera height of 1.675f
        if (initialPositionAlreadySet)
            DCL.Interface.WebInterface.ReportPosition(reportPosition, compositeRotation, characterController.height, cameraRotation);

        lastMovementReportTime = DCLTime.realtimeSinceStartup;
    }

    public void PauseGravity()
    {
        gravity = 0f;
        velocity.y = 0f;
    }

    public void ResumeGravity() { gravity = originalGravity; }

    void OnRenderingStateChanged(bool isEnable, bool prevState)
    {
        SetEnabled(isEnable && !DataStore.i.common.isSignUpFlow.Get());
    }

    bool IsLastCollisionGround()
    {
        return (lastCharacterControllerCollision & CollisionFlags.Below) != 0;
    }
}
