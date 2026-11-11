using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimaCinta.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        private InputActionMap _actions;
        private InputAction _move;
        private InputAction _look;
        private InputAction _sprint;
        private InputAction _crouch;
        private InputAction _interact;
        private InputAction _nightVision;
        private InputAction _zoom;
        private InputAction _flashlight;
        private InputAction _slotOne;
        private InputAction _slotTwo;
        private InputAction _slotThree;
        private InputAction _dropItem;
        private InputAction _useItem;

        public Vector2 Move => _move?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 LookDelta => _look?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool SprintHeld => _sprint?.IsPressed() ?? false;
        public bool CrouchHeld => _crouch?.IsPressed() ?? false;
        public bool InteractPressed => _interact?.WasPressedThisFrame() ?? false;
        public bool NightVisionPressed => _nightVision?.WasPressedThisFrame() ?? false;
        public float ZoomDelta => _zoom?.ReadValue<Vector2>().y ?? 0f;
        public bool FlashlightPressed => _flashlight?.WasPressedThisFrame() ?? false;
        public int InventorySlotPressed => _slotThree?.WasPressedThisFrame() == true ? 3 :
            _slotTwo?.WasPressedThisFrame() == true ? 2 :
            _slotOne?.WasPressedThisFrame() == true ? 1 : 0;
        public bool DropItemPressed => _dropItem?.WasPressedThisFrame() ?? false;
        public bool UseItemPressed => _useItem?.WasPressedThisFrame() ?? false;

        private void Awake()
        {
            CreateActions();
        }

        private void OnEnable()
        {
            _actions?.Enable();
        }

        private void OnDisable()
        {
            _actions?.Disable();
        }

        private void OnDestroy()
        {
            _actions?.Dispose();
            _actions = null;
        }

        private void CreateActions()
        {
            if (_actions != null)
            {
                return;
            }

            _actions = new InputActionMap("Player");
            _move = _actions.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            _look = _actions.AddAction("Look", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            _look.AddBinding("<Mouse>/delta");
            _look.AddBinding("<Gamepad>/rightStick");

            _sprint = AddButton("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            _crouch = AddButton("Crouch", "<Keyboard>/c", "<Gamepad>/buttonEast");
            _interact = AddButton("Interact", "<Keyboard>/e", "<Gamepad>/buttonNorth");
            _nightVision = AddButton("NightVision", "<Keyboard>/n");
            _flashlight = AddButton("Flashlight", "<Keyboard>/f");
            _slotOne = AddButton("InventorySlot1", "<Keyboard>/1");
            _slotTwo = AddButton("InventorySlot2", "<Keyboard>/2");
            _slotThree = AddButton("InventorySlot3", "<Keyboard>/3");
            _dropItem = AddButton("DropItem", "<Keyboard>/g");
            _useItem = AddButton("UseItem", "<Keyboard>/q", "<Gamepad>/buttonWest");

            _zoom = _actions.AddAction("Zoom", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            _zoom.AddBinding("<Mouse>/scroll");
        }

        private InputAction AddButton(string actionName, string keyboardPath, string gamepadPath = null)
        {
            var action = _actions.AddAction(actionName, InputActionType.Button);
            action.AddBinding(keyboardPath);
            if (!string.IsNullOrEmpty(gamepadPath))
            {
                action.AddBinding(gamepadPath);
            }

            return action;
        }
    }
}

