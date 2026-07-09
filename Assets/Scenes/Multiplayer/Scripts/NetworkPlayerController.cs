using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Spawn")]
    [SerializeField] Vector3 spawnBasePosition = new Vector3(2f, 5.3f, -131.17877f);
    [SerializeField] Vector3 spawnLaneOffset = new Vector3(4f, 0f, 0f);
    [SerializeField] Vector3 spawnEulerAngles = new Vector3(0f, 180f, 0f);

    [Header("Camera")]
    [SerializeField] Vector3 cameraOffset = new Vector3(0f, 10.4f, -20.5f);
    [SerializeField] Vector3 cameraEulerAngles = new Vector3(39.2f, 0f, 0f);
    [SerializeField] float cameraFollowSharpness = 10f;

    static readonly Color[] PlayerColors =
    {
        new Color(0.15f, 0.95f, 1f),
        new Color(1f, 0.28f, 0.78f),
        new Color(1f, 0.82f, 0.18f),
        new Color(0.35f, 1f, 0.45f)
    };

    Mov mov;
    Turbo turbo;
    PlayerAnimations playerAnimations;
    ControlDeVida controlDeVida;
    Rigidbody rb;
    AudioSource[] audioSources;
    Renderer[] renderers;
    Camera localCamera;
    bool cameraSnapped;

    void Awake()
    {
        mov = GetComponent<Mov>();
        turbo = GetComponent<Turbo>();
        playerAnimations = GetComponent<PlayerAnimations>();
        controlDeVida = GetComponent<ControlDeVida>();
        rb = GetComponent<Rigidbody>();
        audioSources = GetComponentsInChildren<AudioSource>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ApplySpawnTransform();
        }

        DisablePrefabCameras();
        ApplyPlayerColor();
        ApplyOwnershipState();
    }

    public override void OnGainedOwnership()
    {
        ApplyOwnershipState();
    }

    public override void OnLostOwnership()
    {
        ApplyOwnershipState();
    }

    void LateUpdate()
    {
        if (!IsOwner || localCamera == null)
        {
            return;
        }

        Vector3 targetPosition = transform.position + transform.rotation * cameraOffset;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(cameraEulerAngles);

        if (!cameraSnapped)
        {
            localCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            cameraSnapped = true;
            return;
        }

        float lerp = Mathf.Clamp01(cameraFollowSharpness * Time.deltaTime);
        localCamera.transform.position = Vector3.Lerp(localCamera.transform.position, targetPosition, lerp);
        localCamera.transform.rotation = Quaternion.Slerp(localCamera.transform.rotation, targetRotation, lerp);
    }

    void ApplySpawnTransform()
    {
        int laneIndex = (int)(OwnerClientId % (ulong)PlayerColors.Length);
        Vector3 spawnPosition = spawnBasePosition + spawnLaneOffset * laneIndex;
        transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(spawnEulerAngles));
    }

    void ApplyOwnershipState()
    {
        bool owner = IsOwner;

        SetBehaviourEnabled(mov, owner);
        SetBehaviourEnabled(turbo, owner);
        SetBehaviourEnabled(playerAnimations, owner);
        SetBehaviourEnabled(controlDeVida, owner);

        if (rb != null)
        {
            rb.isKinematic = !owner;
            rb.detectCollisions = true;
        }

        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.enabled = owner;
            }
        }

        if (owner)
        {
            SetupLocalCamera();
        }
    }

    static void SetBehaviourEnabled(Behaviour behaviour, bool enabled)
    {
        if (behaviour != null)
        {
            behaviour.enabled = enabled;
        }
    }

    void SetupLocalCamera()
    {
        localCamera = Camera.main;

        if (localCamera == null)
        {
            localCamera = FindFirstObjectByType<Camera>();
        }

        if (localCamera == null)
        {
            Debug.LogWarning("[NetworkPlayerController] No se encontro una camara local para seguir al jugador.");
            return;
        }

        localCamera.enabled = true;
        DisableCinemachineBrain(localCamera);
        cameraSnapped = false;
    }

    static void DisableCinemachineBrain(Camera camera)
    {
        foreach (Behaviour behaviour in camera.GetComponents<Behaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == "CinemachineBrain")
            {
                behaviour.enabled = false;
            }
        }
    }

    void DisablePrefabCameras()
    {
        Camera[] childCameras = GetComponentsInChildren<Camera>(true);

        foreach (Camera childCamera in childCameras)
        {
            if (childCamera == null)
            {
                continue;
            }

            childCamera.enabled = false;

            if (childCamera.CompareTag("MainCamera"))
            {
                childCamera.tag = "Untagged";
            }
        }
    }

    void ApplyPlayerColor()
    {
        int colorIndex = (int)(OwnerClientId % (ulong)PlayerColors.Length);
        Color color = PlayerColors[colorIndex];
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }
}
