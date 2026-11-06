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
        private bool _temporaryLookLimit;
        private float _temporaryYawCenter;
        private float _temporaryYawOffset;
        private float _temporaryYawRange;
        private float _temporaryPitchCenter;
        private float _temporaryPitchRange;

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
            if (_temporaryLookLimit)
            {
                _temporaryYawOffset = Mathf.Clamp(_temporaryYawOffset + look.x, -_temporaryYawRange, _temporaryYawRange);
                var yaw = transform.eulerAngles;
                yaw.y = _temporaryYawCenter + _temporaryYawOffset;
                transform.eulerAngles = yaw;
                _pitch = Mathf.Clamp(_pitch - look.y, _temporaryPitchCenter - _temporaryPitchRange,
                    _temporaryPitchCenter + _temporaryPitchRange);
            }
            else
            {
                transform.Rotate(Vector3.up, look.x, Space.Self);
                _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
            }
            viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public float StandingViewHeight => _standingViewHeight;

        public void BeginTemporaryLookLimit(float degrees)
        {
            _temporaryLookLimit = true;
            _temporaryYawCenter = transform.eulerAngles.y;
            _temporaryYawOffset = 0f;
            _temporaryYawRange = Mathf.Abs(degrees);
            _temporaryPitchCenter = _pitch;
            _temporaryPitchRange = Mathf.Abs(degrees);
        }

        public void EndTemporaryLookLimit()
        {
            if (_temporaryLookLimit)
            {
                _pitch = _temporaryPitchCenter;
                if (viewPivot != null) viewPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
            _temporaryLookLimit = false;
        }
    }
}
