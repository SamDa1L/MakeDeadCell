using Cinemachine;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CinemachineCameraBootstrap : MonoBehaviour
    {
        [Header("Cinemachine References")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Collider2D confinerShape;

        [Header("Follow Settings")]
        [SerializeField] private Vector2 trackedObjectOffset = Vector2.zero;
        [SerializeField, Min(0.01f)] private float followDamping = 0.2f;
        [SerializeField, Min(0.05f)] private float reacquireInterval = 0.5f;

        private float lastAcquireAttempt;

        private void Awake()
        {
            EnsureBrain();
            EnsureVirtualCamera();
            ConfigureVirtualCamera();
        }

        private void Start()
        {
            AcquireTarget(force: true);
        }

        private void Update()
        {
            AcquireTarget(force: false);
        }

        private void EnsureBrain()
        {
            if (!TryGetComponent(out CinemachineBrain brain))
            {
                brain = gameObject.AddComponent<CinemachineBrain>();
            }

            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
        }

        private void EnsureVirtualCamera()
        {
            if (virtualCamera != null)
            {
                return;
            }

            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera != null)
            {
                return;
            }

            GameObject vcamObject = new GameObject("PlayerFollowVcam");
            vcamObject.transform.position = transform.position;
            vcamObject.transform.rotation = Quaternion.identity;
            virtualCamera = vcamObject.AddComponent<CinemachineVirtualCamera>();
            virtualCamera.m_Priority = 10;
        }

        private void ConfigureVirtualCamera()
        {
            if (virtualCamera == null)
            {
                return;
            }

            Camera mainCamera = GetComponent<Camera>();
            if (mainCamera != null)
            {
                virtualCamera.m_Lens.ModeOverride = mainCamera.orthographic
                    ? LensSettings.OverrideModes.Orthographic
                    : LensSettings.OverrideModes.Perspective;

                if (mainCamera.orthographic)
                {
                    virtualCamera.m_Lens.OrthographicSize = mainCamera.orthographicSize;
                }
                else
                {
                    virtualCamera.m_Lens.FieldOfView = mainCamera.fieldOfView;
                }
            }

            CinemachineFramingTransposer framing = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing == null)
            {
                framing = virtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
            }

            framing.m_XDamping = followDamping;
            framing.m_YDamping = followDamping;
            framing.m_ZDamping = 0f;
            framing.m_SoftZoneWidth = 0.8f;
            framing.m_SoftZoneHeight = 0.8f;
            framing.m_DeadZoneWidth = 0.05f;
            framing.m_DeadZoneHeight = 0.05f;
            framing.m_TrackedObjectOffset = new Vector3(trackedObjectOffset.x, trackedObjectOffset.y, 0f);

            if (confinerShape != null)
            {
                CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner == null)
                {
                    confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();
                }

                confiner.m_Damping = followDamping;
                confiner.m_BoundingShape2D = confinerShape;
                confiner.InvalidateCache();
            }
        }

        private void AcquireTarget(bool force)
        {
            if (virtualCamera == null)
            {
                return;
            }

            if (!force && virtualCamera.Follow != null)
            {
                return;
            }

            float time = Time.unscaledTime;
            if (!force && time - lastAcquireAttempt < reacquireInterval)
            {
                return;
            }

            lastAcquireAttempt = time;

            Transform candidate = null;
            if (PlayerSpawnManager.Instance != null && PlayerSpawnManager.Instance.SpawnedPlayer != null)
            {
                candidate = PlayerSpawnManager.Instance.SpawnedPlayer.transform;
            }
            else
            {
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    candidate = player.transform;
                }
            }

            if (candidate != null)
            {
                virtualCamera.Follow = candidate;

                CinemachineFramingTransposer framing = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
                if (framing != null)
                {
                    framing.m_TrackedObjectOffset = new Vector3(trackedObjectOffset.x, trackedObjectOffset.y, 0f);
                }
            }
        }
    }
}
