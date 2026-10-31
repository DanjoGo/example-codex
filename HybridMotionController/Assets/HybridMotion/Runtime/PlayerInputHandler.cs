using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridMotion
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset instance;
        private InputAction move, look, sprint, walk, jump, cursor;
        private bool captureRequested;
        private int suppressLookThroughFrame;
        public bool Captured => captureRequested && Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
        public Vector2 Move => Captured && move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
        public Vector2 Look => Captured && look != null && Time.frameCount > suppressLookThroughFrame
            ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool Sprint => Captured && sprint != null && sprint.IsPressed();
        public bool Walk => Captured && walk != null && walk.IsPressed();
        public bool JumpPressed => Captured && jump != null && jump.WasPressedThisFrame();

        public void Configure(InputActionAsset asset) { actions = asset; }

        private void OnEnable()
        {
            if (!actions) { Debug.LogError("Assign Player.inputactions to PlayerInputHandler.", this); enabled = false; return; }
            instance = Instantiate(actions); // Do not change the shared project asset.
            var map = instance.FindActionMap("Gameplay", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            sprint = map.FindAction("Sprint", true);
            walk = map.FindAction("Walk", true);
            jump = map.FindAction("Jump", true);
            cursor = map.FindAction("Cursor", true);
            map.Enable();
            SetCapture(true);
        }

        private void Update()
        {
            // The editor can unlock the OS cursor on Escape before this Update.
            // Toggle our requested state rather than the OS lock state.
            if (cursor != null && cursor.WasPressedThisFrame()) SetCapture(!captureRequested);
        }

        private void OnApplicationFocus(bool focused) { if (!focused) SetCapture(false); }
        private void OnDisable()
        {
            SetCapture(false);
            if (instance) { instance.Disable(); Destroy(instance); }
        }

        private void SetCapture(bool capture)
        {
            captureRequested = capture;
            suppressLookThroughFrame = Time.frameCount + 1;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
    }
}
