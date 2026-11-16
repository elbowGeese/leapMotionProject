using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fingerboard
{
    [RequireComponent(typeof(SkateboardController))]
    public class SkateboardKeyboardInput : MonoBehaviour
    {
        private SkateboardController board;

        private void Awake() => board = GetComponent<SkateboardController>();

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                board.SetSideInput(0f);
                return;
            }

            bool left = keyboard.aKey.isPressed;
            bool right = keyboard.dKey.isPressed;
            bool ollie = keyboard.spaceKey.wasPressedThisFrame;
            bool grind = keyboard.eKey.wasPressedThisFrame;
            bool kickflip = keyboard.fKey.wasPressedThisFrame;
            bool shoveIt = keyboard.gKey.wasPressedThisFrame;
            bool reset = keyboard.rKey.wasPressedThisFrame;
#else
            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);
            bool ollie = Input.GetKeyDown(KeyCode.Space);
            bool grind = Input.GetKeyDown(KeyCode.E);
            bool kickflip = Input.GetKeyDown(KeyCode.F);
            bool shoveIt = Input.GetKeyDown(KeyCode.G);
            bool reset = Input.GetKeyDown(KeyCode.R);
#endif

            board.SetSideInput((right ? 1f : 0f) - (left ? 1f : 0f));

            if (reset)
            {
                board.RequestReset();
                return;
            }

            if (grind)
            {
                board.RequestGrind();
                return;
            }

            if (ollie || kickflip || shoveIt)
                board.RequestOllie();

            if (kickflip)
                board.RequestKickflip();
            else if (shoveIt)
                board.RequestPopShoveIt();
        }

        private void OnDisable()
        {
            if (board != null) board.SetSideInput(0f);
        }
    }
}