using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TokyoExpress.Data;
using TokyoExpress.Grid;

namespace TokyoExpress.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class TokyoMotoController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseSpeed = 6f;
        [SerializeField] private float currentSpeed = 6f;
        [SerializeField] private float jumpHeight = 2.5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float rotationSmooth = 10f;

        [Header("Ability State")]
        [SerializeField] private BikeItemData equippedBike;
        [SerializeField] private bool isNitroActive;
        [SerializeField] private bool isBulletTimeActive;
        [SerializeField] private bool isInvincible;
        [SerializeField] private float currentAbilityTimer;

        public bool IsNitroActive => isNitroActive;
        public bool IsBulletTimeActive => isBulletTimeActive;
        public bool IsInvincible => isInvincible;
        public float CurrentAbilityTimer => currentAbilityTimer;

        [Header("Ground & Rail Snapping")]
        [SerializeField] private LayerMask roadLayer;
        [SerializeField] private Vector3 moveDirection = Vector3.forward;
        private CharacterController characterController;
        private Vector3 verticalVelocity;
        private bool isGrounded;

        // Current tile tracking
        public Vector2Int CurrentGridCoord { get; private set; }
        private bool isStoppedAtIntersection;
        private IntersectionArrowsIndicator activeArrowsIndicator;
        private Vector2Int lastIntersectionCell = new Vector2Int(-999, -999);
        private List<Vector3> availableForkDirections = new List<Vector3>();

        public event Action OnJump;
        public event Action<Collider> OnCrashWithObstacle;
        public event Action OnAbilityTriggered;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            currentSpeed = baseSpeed;

            if (LevelRuntimeBuilder.Instance != null && LevelRuntimeBuilder.Instance.ActiveLevel != null)
            {
                CurrentGridCoord = LevelRuntimeBuilder.Instance.WorldToGridPosition(transform.position, LevelRuntimeBuilder.Instance.ActiveLevel.cellSize);
                GameObject startTileGO = LevelRuntimeBuilder.Instance.GetTileObjectAt(CurrentGridCoord);
                if (startTileGO != null)
                {
                    currentSplineTrack = startTileGO.GetComponent<RoadSplineTrack>();
                }
            }

            if (OneTouchInputManager.Instance != null)
            {
                OneTouchInputManager.Instance.OnSwipeDetected += HandleSwipe;
                OneTouchInputManager.Instance.OnTapDetected += HandleTap;
            }
        }

        private void OnDestroy()
        {
            if (OneTouchInputManager.Instance != null)
            {
                OneTouchInputManager.Instance.OnSwipeDetected -= HandleSwipe;
                OneTouchInputManager.Instance.OnTapDetected -= HandleTap;
            }
        }

        public void InitializeWithBike(BikeItemData bike, PlayerProfileData profile)
        {
            equippedBike = bike;
            if (bike != null)
            {
                baseSpeed = bike.baseSpeed + (profile != null ? (profile.speedUpgradeLevel - 1) * 1.5f : 0f);
                jumpHeight = bike.jumpForce;
                currentSpeed = baseSpeed;
            }
        }

        private RoadSplineTrack currentSplineTrack;
        private float splineT = 0f;

        private void Update()
        {
            HandleAbilitiesTimers();
            ApplyForwardSplineMovement();
            ApplyVerticalGravity();
            UpdateGridPosition();
        }

        private void ApplyForwardSplineMovement()
        {
            if (isStoppedAtIntersection)
            {
                // Stopped at intersection waiting for player swipe choice!
                return;
            }

            // Ensure we are tracking the spline of the current tile
            if (currentSplineTrack == null && LevelRuntimeBuilder.Instance != null)
            {
                GameObject tileGO = LevelRuntimeBuilder.Instance.GetTileObjectAt(CurrentGridCoord);
                if (tileGO != null)
                {
                    currentSplineTrack = tileGO.GetComponent<RoadSplineTrack>();
                }
            }

            if (currentSplineTrack != null)
            {
                float length = currentSplineTrack.GetLength();
                if (length <= 0.01f) length = 2.0f;

                splineT += (currentSpeed / length) * Time.deltaTime;

                currentSplineTrack.Evaluate(splineT, moveDirection, out Vector3 worldPos, out Vector3 worldTangent);
                
                // Move character controller towards evaluated spline point
                Vector3 targetPos = new Vector3(worldPos.x, transform.position.y, worldPos.z);
                Vector3 delta = targetPos - transform.position;
                characterController.Move(delta);

                if (worldTangent.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(worldTangent.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
                }

                if (splineT >= 1.0f)
                {
                    // Transition to next tile along current direction
                    splineT = 0f;
                    currentSplineTrack = null;
                }
            }
            else
            {
                // Fallback direct movement along moveDirection
                Vector3 forwardMove = moveDirection * (currentSpeed * Time.deltaTime);
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
                }
                characterController.Move(forwardMove);
            }
        }

        private void ApplyVerticalGravity()
        {
            isGrounded = characterController.isGrounded;
            if (isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f;
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            characterController.Move(verticalVelocity * Time.deltaTime);
        }

        private Vector2Int lastTurnedCell = new Vector2Int(-999, -999);

        private void UpdateGridPosition()
        {
            if (LevelRuntimeBuilder.Instance != null && LevelRuntimeBuilder.Instance.ActiveLevel != null)
            {
                var level = LevelRuntimeBuilder.Instance.ActiveLevel;
                float cellSize = level.cellSize;
                CurrentGridCoord = LevelRuntimeBuilder.Instance.WorldToGridPosition(transform.position, cellSize);

                // Update current spline track reference when entering cell
                if (currentSplineTrack == null)
                {
                    GameObject tileGO = LevelRuntimeBuilder.Instance.GetTileObjectAt(CurrentGridCoord);
                    if (tileGO != null)
                    {
                        currentSplineTrack = tileGO.GetComponent<RoadSplineTrack>();
                    }
                }

                // 1. Check for intersection stop & arrows prompt (Crossroad or T-Intersection)
                var entry = level.GetEntryAt(CurrentGridCoord);
                if (entry != null && (entry.tileType == CellTileType.Crossroad || entry.tileType == CellTileType.TIntersection))
                {
                    if (CurrentGridCoord != lastIntersectionCell && !isStoppedAtIntersection)
                    {
                        Vector3 worldCellCenter = LevelRuntimeBuilder.Instance.GridToWorldPosition(CurrentGridCoord, cellSize);
                        float distToCenter = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(worldCellCenter.x, worldCellCenter.z));

                        if (distToCenter < cellSize * 0.45f)
                        {
                            StopAtIntersection(CurrentGridCoord, entry.tileType, worldCellCenter);
                            return;
                        }
                    }
                }

                // 2. Auto-turn on rounded corner tiles (TurnNE, TurnNW, TurnSE, TurnSW)
                if (CurrentGridCoord != lastTurnedCell && !isStoppedAtIntersection)
                {
                    if (entry != null)
                    {
                        Vector3 worldCellCenter = LevelRuntimeBuilder.Instance.GridToWorldPosition(CurrentGridCoord, cellSize);
                        float distToCenter = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(worldCellCenter.x, worldCellCenter.z));

                        // Turn when approaching the tile center
                        if (distToCenter < cellSize * 0.45f)
                        {
                            Vector3 newAutoDir = GetAutoTurnDirection(entry.tileType, moveDirection);
                            if (newAutoDir != Vector3.zero && newAutoDir != moveDirection)
                            {
                                moveDirection = newAutoDir;
                                lastTurnedCell = CurrentGridCoord;
                                currentSplineTrack = null;
                                splineT = 0f;
                            }
                        }
                    }
                }
            }
        }

        private void StopAtIntersection(Vector2Int coord, CellTileType tileType, Vector3 worldPos)
        {
            isStoppedAtIntersection = true;
            lastIntersectionCell = coord;
            currentSplineTrack = null;
            splineT = 0.5f;

            // Snap vehicle exactly to intersection center
            transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);

            // Compute available directions (cannot go backwards)
            availableForkDirections.Clear();
            Vector3[] candidates = new Vector3[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

            foreach (var cand in candidates)
            {
                if (cand == -moveDirection) continue; // Exclude U-turn
                availableForkDirections.Add(cand);
            }

            // Find or spawn arrow indicator at this intersection
            if (activeArrowsIndicator == null)
            {
                GameObject indicatorGO = new GameObject("IntersectionIndicator");
                activeArrowsIndicator = indicatorGO.AddComponent<IntersectionArrowsIndicator>();
            }

            activeArrowsIndicator.transform.position = worldPos;
            activeArrowsIndicator.ShowArrows(availableForkDirections, moveDirection);
        }

        private void ResumeFromIntersection(Vector3 chosenDirection)
        {
            isStoppedAtIntersection = false;
            moveDirection = chosenDirection;
            splineT = 0.5f;

            if (activeArrowsIndicator != null)
            {
                activeArrowsIndicator.HideAll();
            }
        }

        private Vector3 GetAutoTurnDirection(CellTileType type, Vector3 currentDir)
        {
            // Calculate natural 90-degree turn following the curved rail / road
            switch (type)
            {
                case CellTileType.TurnNE: // Corner bottom-left (connects South/Down & East/Right)
                    if (currentDir == Vector3.back) return Vector3.right;
                    if (currentDir == Vector3.left) return Vector3.forward;
                    break;
                case CellTileType.TurnNW: // Corner bottom-right (connects South/Down & West/Left)
                    if (currentDir == Vector3.back) return Vector3.left;
                    if (currentDir == Vector3.right) return Vector3.forward;
                    break;
                case CellTileType.TurnSE: // Corner top-left (connects North/Up & East/Right)
                    if (currentDir == Vector3.forward) return Vector3.right;
                    if (currentDir == Vector3.left) return Vector3.back;
                    break;
                case CellTileType.TurnSW: // Corner top-right (connects North/Up & West/Left)
                    if (currentDir == Vector3.forward) return Vector3.left;
                    if (currentDir == Vector3.right) return Vector3.back;
                    break;
            }
            return Vector3.zero;
        }

        #region Input Handlers
        private void HandleSwipe(SwipeDirection dir)
        {
            Vector3 chosenDir = Vector3.zero;
            switch (dir)
            {
                case SwipeDirection.Left:
                    chosenDir = Vector3.left;
                    break;
                case SwipeDirection.Right:
                    chosenDir = Vector3.right;
                    break;
                case SwipeDirection.Up:
                    chosenDir = Vector3.forward;
                    break;
                case SwipeDirection.Down:
                    chosenDir = Vector3.back;
                    break;
            }

            // Only allow turning when stopped at an intersection (cannot turn in a straight line)
            if (isStoppedAtIntersection)
            {
                if (Audio.TokyoAudioManager.Instance != null)
                {
                    Audio.TokyoAudioManager.Instance.PlaySFX(Audio.SoundEffect.Swipe);
                }

                // Resume in chosen direction if not opposite
                if (chosenDir != -moveDirection && chosenDir != Vector3.zero)
                {
                    ResumeFromIntersection(chosenDir);
                }
            }
        }

        private void HandleTap()
        {
            if (Audio.TokyoAudioManager.Instance != null)
            {
                Audio.TokyoAudioManager.Instance.PlaySFX(Audio.SoundEffect.Tap);
            }

            // Tap triggers Jump to leap over other bikes/obstacles, or context action!
            if (isGrounded)
            {
                PerformJump();
            }
            else
            {
                // In-air double tap or secondary ability activation
                TryActivateAbility();
            }
        }
        #endregion

        public void PerformJump()
        {
            float finalJump = jumpHeight;
            if (equippedBike != null && equippedBike.ability == SpecialAbilityType.HighJump)
            {
                finalJump *= 1.45f;
            }

            verticalVelocity.y = Mathf.Sqrt(finalJump * -2f * gravity);
            if (Audio.TokyoAudioManager.Instance != null)
            {
                Audio.TokyoAudioManager.Instance.PlaySFX(Audio.SoundEffect.Jump);
            }
            OnJump?.Invoke();
        }

        public void ShiftDirectionOrLane(Vector3 newDir)
        {
            // Turn road routing (90 degree turn or lane shift)
            if (newDir != -moveDirection)
            {
                moveDirection = newDir;
            }
        }

        public void TryActivateAbility()
        {
            if (equippedBike == null || equippedBike.ability == SpecialAbilityType.None) return;

            switch (equippedBike.ability)
            {
                case SpecialAbilityType.NitroBoost:
                    StartCoroutine(NitroRoutine(equippedBike.abilityDuration));
                    break;
                case SpecialAbilityType.BulletTime:
                    StartCoroutine(BulletTimeRoutine(equippedBike.abilityDuration));
                    break;
                case SpecialAbilityType.ShieldArmor:
                    isInvincible = true;
                    break;
            }
            OnAbilityTriggered?.Invoke();
        }

        private IEnumerator NitroRoutine(float duration)
        {
            isNitroActive = true;
            isInvincible = true;
            float originalSpeed = baseSpeed;
            currentSpeed = baseSpeed * 1.8f;

            yield return new WaitForSeconds(duration);

            currentSpeed = originalSpeed;
            isNitroActive = false;
            isInvincible = false;
        }

        private IEnumerator BulletTimeRoutine(float duration)
        {
            isBulletTimeActive = true;
            Time.timeScale = 0.35f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            isBulletTimeActive = false;
        }

        private void HandleAbilitiesTimers()
        {
            // Cooldown / status update if needed
        }

        private void OnTriggerEnter(Collider other)
        {
            bool isTraffic = other.GetComponentInParent<TrafficMotoObstacle>() != null;
            bool isObstacle = other.CompareTag("Obstacle");

            if (isTraffic || isObstacle)
            {
                if (isInvincible)
                {
                    // Blast through obstacle!
                    if (isTraffic)
                    {
                        var traffic = other.GetComponentInParent<TrafficMotoObstacle>();
                        if (traffic != null) Destroy(traffic.gameObject);
                        else Destroy(other.gameObject);
                    }
                    else
                    {
                        Destroy(other.gameObject);
                    }
                }
                else
                {
                    if (Audio.TokyoAudioManager.Instance != null)
                    {
                        Audio.TokyoAudioManager.Instance.PlaySFX(Audio.SoundEffect.Crash);
                    }
                    OnCrashWithObstacle?.Invoke(other);
                }
            }
        }
    }
}
