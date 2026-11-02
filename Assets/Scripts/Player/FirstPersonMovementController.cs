using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.Player
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class FirstPersonMovementController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Transform viewPivot;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private StaminaResource stamina;
        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _targetHeight;
        private float _standingViewHeight;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _targetHeight = _characterController.height;
            if (viewPivot != null) _standingViewHeight = viewPivot.localPosition.y;
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (stamina == null) stamina = GetComponent<StaminaResource>();
        }

        private void Update()
        {
            if (gameConfig == null || inputReader == null || _characterController == null)
            {
                return;
            }

            UpdateCrouch(Time.deltaTime);
            var input = Vector2.ClampMagnitude(inputReader.Move, 1f);
            var direction = transform.forward * input.y + transform.right * input.x;
            var moving = input.sqrMagnitude > 0.01f;
            var sprinting = moving && !inputReader.CrouchHeld && inputReader.SprintHeld &&
                stamina != null && stamina.TryDrain(Time.deltaTime);
            if (!sprinting && stamina != null)
            {
                stamina.Recover(Time.deltaTime);
            }

            var speed = inputReader.CrouchHeld ? gameConfig.CrouchSpeed :
                sprinting ? gameConfig.SprintSpeed : gameConfig.WalkSpeed;
            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = 0f;
            }

            _verticalVelocity += gameConfig.PlayerGravity * Time.deltaTime;
            var velocity = direction * speed;
            velocity.y = _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);
        }

        private void UpdateCrouch(float deltaTime)
        {
            _targetHeight = inputReader.CrouchHeld ? gameConfig.CrouchedHeight : gameConfig.StandingHeight;
            _characterController.height = Mathf.MoveTowards(
                _characterController.height,
                _targetHeight,
                gameConfig.CrouchTransitionSpeed * deltaTime);
            _characterController.center = new Vector3(0f, _characterController.height * 0.5f, 0f);
            if (viewPivot != null)
            {
                var desiredCameraHeight = gameConfig.StandingHeight > 0f
                    ? _standingViewHeight * _characterController.height / gameConfig.StandingHeight
                    : 0f;
                var position = viewPivot.localPosition;
                position.y = desiredCameraHeight;
                viewPivot.localPosition = position;
            }
        }
    }
}


