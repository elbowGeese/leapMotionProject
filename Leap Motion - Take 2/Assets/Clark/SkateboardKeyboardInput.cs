using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fingerboard
{
    [RequireComponent(typeof(SkateboardController))]
    public class SkateboardInput : MonoBehaviour
    {
        private SkateboardController board;

        private void Awake()
        {
            board = GetComponent<SkateboardController>();
        }

        private void Update()
        {
            bool push;
            bool left;
            bool right;
            bool manual;
            bool ollie;
            bool kickflip;
            bool reset;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ReleaseHeldInput();
                return;
            }

            push = keyboard.wKey.isPressed;
            left = keyboard.aKey.isPressed;
            right = keyboard.dKey.isPressed;
            manual = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            ollie = keyboard.spaceKey.wasPressedThisFrame;
            kickflip = keyboard.fKey.wasPressedThisFrame;
            reset = keyboard.rKey.wasPressedThisFrame;
#else
    push = Input.GetKey(KeyCode.W);
    left = Input.GetKey(KeyCode.A);
    right = Input.GetKey(KeyCode.D);
    manual = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    ollie = Input.GetKeyDown(KeyCode.Space);
    kickflip = Input.GetKeyDown(KeyCode.F);
    reset = Input.GetKeyDown(KeyCode.R);
#endif

            float sideInput = 0f;
            if (left) sideInput -= 1f;
            if (right) sideInput += 1f;

            board.SetPushInput(push ? 1f : 0f);
            board.SetSideInput(sideInput);
            board.SetManual(manual);

            if (reset)
                board.RequestReset();
            else if (kickflip)
                board.RequestKickflip();
            else if (ollie)
                board.RequestOllie();
        }

        private void OnDisable() => ReleaseHeldInput();

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseHeldInput();
        }

        private void ReleaseHeldInput()
        {
            if (board == null) return;

            board.SetPushInput(0f);

            board.SetSideInput(0f);
            board.SetManual(false);
        }

        private void OnGUI()
        {
            if (board == null) return;

            GUI.Box(new Rect(12, 12, 345, 62),
                "W Push   Shift Manual   Space Ollie   F Kickflip   R Reset\n" + board.Status);
        }
    }
}