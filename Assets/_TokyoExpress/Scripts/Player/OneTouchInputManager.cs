using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TokyoExpress.Player
{
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Hyper-casual 1-touch controller:
    /// - Detects swipe in 4 directions
    /// - Detects tap (short touch with minimal drag) for Jump or context action prompt
    /// Supports both the new Unity Input System package and legacy Input!
    /// </summary>
    public class OneTouchInputManager : MonoBehaviour
    {
        public static OneTouchInputManager Instance { get; private set; }

        public event Action<SwipeDirection> OnSwipeDetected;
        public event Action OnTapDetected;

        [Header("Tuning")]
        [SerializeField] private float minSwipeDistancePixels = 40f;
        [SerializeField] private float maxTapDuration = 0.25f;

        private Vector2 touchStartPosition;
        private float touchStartTime;
        private bool isTrackingTouch;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            HandleNewInputSystem();
#else
            HandleLegacyInput();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void HandleNewInputSystem()
        {
            // Touchscreen or Mouse pointer tracking
            bool pressed = false;
            bool released = false;
            Vector2 currentPosition = Vector2.zero;

            // 1. Touchscreen input
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                var touch = Touchscreen.current.touches[0];
                pressed = touch.press.wasPressedThisFrame;
                released = touch.press.wasReleasedThisFrame;
                currentPosition = touch.position.ReadValue();
            }
            // 2. Mouse / Pointer fallback
            else if (Mouse.current != null)
            {
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                currentPosition = Mouse.current.position.ReadValue();
            }

            if (pressed)
            {
                isTrackingTouch = true;
                touchStartPosition = currentPosition;
                touchStartTime = Time.unscaledTime;
            }
            else if (released && isTrackingTouch)
            {
                isTrackingTouch = false;
                Vector2 delta = currentPosition - touchStartPosition;
                float duration = Time.unscaledTime - touchStartTime;

                if (delta.magnitude < minSwipeDistancePixels && duration <= maxTapDuration)
                {
                    OnTapDetected?.Invoke();
                }
                else if (delta.magnitude >= minSwipeDistancePixels)
                {
                    SwipeDirection direction = ResolveSwipeDirection(delta);
                    if (direction != SwipeDirection.None)
                    {
                        OnSwipeDetected?.Invoke(direction);
                    }
                }
            }

            // Keyboard navigation fallback
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    OnTapDetected?.Invoke();
                }
                if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
                {
                    OnSwipeDetected?.Invoke(SwipeDirection.Up);
                }
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                {
                    OnSwipeDetected?.Invoke(SwipeDirection.Down);
                }
                else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                {
                    OnSwipeDetected?.Invoke(SwipeDirection.Left);
                }
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                {
                    OnSwipeDetected?.Invoke(SwipeDirection.Right);
                }
            }
        }
#else
        private void HandleLegacyInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isTrackingTouch = true;
                touchStartPosition = Input.mousePosition;
                touchStartTime = Time.unscaledTime;
            }
            else if (Input.GetMouseButtonUp(0) && isTrackingTouch)
            {
                isTrackingTouch = false;
                Vector2 touchEndPosition = Input.mousePosition;
                Vector2 delta = touchEndPosition - touchStartPosition;
                float duration = Time.unscaledTime - touchStartTime;

                if (delta.magnitude < minSwipeDistancePixels && duration <= maxTapDuration)
                {
                    OnTapDetected?.Invoke();
                }
                else if (delta.magnitude >= minSwipeDistancePixels)
                {
                    SwipeDirection direction = ResolveSwipeDirection(delta);
                    if (direction != SwipeDirection.None)
                    {
                        OnSwipeDetected?.Invoke(direction);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnTapDetected?.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                OnSwipeDetected?.Invoke(SwipeDirection.Up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                OnSwipeDetected?.Invoke(SwipeDirection.Down);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnSwipeDetected?.Invoke(SwipeDirection.Left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnSwipeDetected?.Invoke(SwipeDirection.Right);
            }
        }
#endif

        private SwipeDirection ResolveSwipeDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }
    }
}
