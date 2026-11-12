using Fingerboard;
using UnityEngine;

public class HandToSkateboardHookup : MonoBehaviour
{
    private HandInput handInput;
    private SkateboardController board;

    public bool isActive = false;

    private void Awake()
    {
        handInput = FindAnyObjectByType<HandInput>();
        board = GetComponent<SkateboardController>();
    }

    private void OnDisable() => ReleaseHeldInput();

    void Update()
    {
        if (!isActive) { return; }

        bool push = false;
        bool ollie = false;
        bool kickflip = false;
        bool manual = false;
        float sideInput = 0f;
        bool isBraced = false;

        // get input
        switch (handInput._state)
        {
            case HandInput.HandState.CRUISING:
                isBraced = true;
                break;
            case HandInput.HandState.PUSHING:
                push = true;
                break;
            case HandInput.HandState.JUMP:
                ollie = true;
                break;
            case HandInput.HandState.POP:
                manual = true;
                break;
            case HandInput.HandState.SPIN:
                kickflip = true;
                break;
            default:
                break;
        }

        switch (handInput._leanState)
        {
            case HandInput.LeanState.LEFT:
                sideInput -= 1f;
                break;
            case HandInput.LeanState.RIGHT: 
                sideInput += 1f; 
                break;
            default:
                sideInput = 0f;
                break;
        }

        // set output
        board.SetPushInput(push ? 1f : 0f);
        board.SetManual(manual);
        board.SetSideInput(sideInput);
        board.isBraced = isBraced;

        if (kickflip)
            board.RequestKickflip();
        else if (ollie)
            board.RequestOllie();
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
