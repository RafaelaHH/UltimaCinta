using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class FirstPersonLookController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform viewPivot;
        [SerializeField] private PlayerInputReader inputReader;
        private float _pitch;
        private float _standingViewHeight;

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (viewPivot != null)
            {
                _standingViewHeight = viewPivot.localPosition.y;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (gameConfig == null || inputReader == null || viewPivot == null)
            {
                return;
            }

            var look = inputReader.LookDelta * gameConfig.LookSensitivity;
            transform.Rotate(Vector3.up, look.x, Space.Self);
            _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
            viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public float StandingViewHeight => _standingViewHeight;
    }
}
