using Leap;
using LeapInternal;
using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class HandInput : MonoBehaviour
{
    public enum HandState { NONE, CRUISING, PUSHING, POP, JUMP, SPIN }
    public HandState _state; //{ get; private set; }

    public enum LeanState { CENTERED, LEFT, RIGHT }
    public LeanState _leanState; //{ get; private set; }

    private LeapProvider leapProvider;
    [Range(0f, 1f)] public float downThreshold = 0.7f;
    [Range(0f, 1f)] public float leanThreshhold = 0.1f;
    public float leanOffset = 0.1f;

    void Start()
    {
        leapProvider = FindAnyObjectByType<LeapProvider>();
    }

    void Update()
    {
        Frame frame = leapProvider.CurrentFrame;

        if (frame.Hands.Count > 0)
        {
            _state = CheckHandState(frame.Hands[0]);
            _leanState = CheckHandLeanState(frame.Hands[0]);
        }
    }

    private LeanState CheckHandLeanState(Hand hand)
    {
        // get angle of palm
        float palmX = hand.PalmNormal.x;

        if (palmX < -leanThreshhold + leanOffset)
        {
            // right
            return LeanState.RIGHT;
        }
        else if(palmX > leanThreshhold + leanOffset)
        {
            // left
            return LeanState.LEFT;
        }
        else
        {
            // centered
            return LeanState.CENTERED;
        }
    }

    private HandState CheckHandState(Hand hand)
    {
        if (IsCruising(hand)) { return HandState.CRUISING; }
        else if(IsPushing(hand)) { return HandState.PUSHING; }
        else if (IsSpinning(hand)) { return HandState.SPIN; }
        else if (IsJumping(hand)) { return HandState.JUMP; }
        else if (IsPopping(hand)) { return HandState.POP; }
        else {  return HandState.NONE; }
    }

    #region Functions That Check Hand State

    private bool IsCruising(Hand hand)
    {
        bool indexDown = false;
        bool middleDown = false;

        // check index finger = fingers[1]
        Vector3 indexFingerDir = hand.fingers[1].Direction;
        float indexDot = Vector3.Dot(indexFingerDir, Vector3.down);
        if (hand.fingers[1].IsExtended && indexDot >= downThreshold)
        {
            indexDown = true;
        }

        // check middle finger = fingers[2]
        Vector3 middleFingerDir = hand.fingers[2].Direction;
        float middleDot = Vector3.Dot(middleFingerDir, Vector3.down);
        if (hand.fingers[2].IsExtended && middleDot >= downThreshold)
        {
            middleDown = true;
        }

        return indexDown && middleDown;
    }

    private bool IsPushing(Hand hand)
    {
        bool indexDown = false;
        bool middleDown = false;

        bool indexPush = false;
        bool middlePush = false;

        // check index finger = fingers[1]
        Vector3 indexFingerDir = hand.fingers[1].Direction;
        float indexDot = Vector3.Dot(indexFingerDir, Vector3.down);
        if (hand.fingers[1].IsExtended && indexDot >= downThreshold)
        {
            indexDown = true;
        }
        else if(indexDot <= downThreshold)
        {
            indexPush = true;
        }

        // check middle finger = fingers[2]
        Vector3 middleFingerDir = hand.fingers[2].Direction;
        float middleDot = Vector3.Dot(middleFingerDir, Vector3.down);
        if (hand.fingers[2].IsExtended && middleDot >= downThreshold)
        {
            middleDown = true;
        }
        else if (middleDot <= downThreshold)
        {
            middlePush = true;
        }

        return (indexDown && middlePush) || (middleDown && indexPush);
    }

    private bool IsPopping(Hand hand)
    {
        bool indexUp = !hand.fingers[1].IsExtended;
        bool middleUp = !hand.fingers[2].IsExtended;

        return (indexUp && !middleUp) || (middleUp && !indexUp);
    }

    private bool IsJumping(Hand hand)
    {
        bool indexUp = !hand.fingers[1].IsExtended;
        bool middleUp = !hand.fingers[2].IsExtended;

        return indexUp && middleUp;
    }

    private bool IsSpinning(Hand hand)
    {
        bool indexUp = !hand.fingers[1].IsExtended;
        bool middleUp = !hand.fingers[2].IsExtended;

        bool indexOut = false;
        bool middleOut = false;

        // check index finger = fingers[1]
        Vector3 indexFingerDir = hand.fingers[1].Direction;
        float indexDot = Vector3.Dot(indexFingerDir, Vector3.down);
        if (hand.fingers[1].IsExtended && indexDot <= downThreshold)
        {
            indexOut = true;
        }

        // check middle finger = fingers[2]
        Vector3 middleFingerDir = hand.fingers[2].Direction;
        float middleDot = Vector3.Dot(middleFingerDir, Vector3.down);
        if (hand.fingers[2].IsExtended && middleDot <= downThreshold)
        {
            middleOut = true;
        }

        return (indexUp && middleOut) || (middleUp && indexOut);
    }

    #endregion

}
