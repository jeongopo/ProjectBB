using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace GamePlay
{
    enum CharacterState
    {
        Idle,
        Moving,
        Interacting,
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class MainCharacter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager inputManager;
        [SerializeField] private Tilemap groundTilemap;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("Sprite Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color _occlusionColor = new Color(1f, 0.92f, 0.76f, 0.45f);

        private Rigidbody2D rb;
        private Animator animator;
        private InputAction moveAction;
        private PhysicsMaterial2D _movementMaterial;
        private Vector2 _desiredVelocity;
        private float lastMoveX = 0f;
        private float lastMoveY = 0f;

        public Vector2 FacingDirection => (lastMoveX == 0f && lastMoveY == 0f)
            ? Vector2.down
            : new Vector2(lastMoveX, lastMoveY).normalized;
        
        private CharacterState CurrentPlayerState = CharacterState.Idle;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            
            animator = GetComponent<Animator>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (TryGetComponent(out Collider2D bodyCollider))
            {
                // 발 콜라이더가 벽에 비스듬히 닿을 때 접선 방향의 이동을 마찰로 잃지 않는다.
                _movementMaterial = new PhysicsMaterial2D("Player Slide") { friction = 0f, bounciness = 0f };
                bodyCollider.sharedMaterial = _movementMaterial;
            }
        }
        
        private void Start()
        {
            if (spriteRenderer != null && _occlusionColor.a > 0f)
            {
                gameObject.AddComponent<CharacterOcclusionSilhouette>().Initialize(spriteRenderer, _occlusionColor);
            }

            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<InputManager>();
            }

            if (inputManager != null)
            {
                var actionMap = inputManager.GetCurrentActionMap();
                if (actionMap != null)
                {
                    moveAction = actionMap.FindAction("Move");
                }
            }

            if (groundTilemap != null)
            {
                transform.position = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(transform.position));
            }

            if (!TryGetComponent(out PlayerFootsteps footsteps))
            {
                footsteps = gameObject.AddComponent<PlayerFootsteps>();
            }

            footsteps.Initialize(groundTilemap, rb);
            GameAudio.StartMusic();
            gameObject.AddComponent<AudioOptionsUI>().Initialize(inputManager);
        }
        
        private void Update()
        {
            if (inputManager == null) return;

            if(CanMoveCharacter())
            {
                HandleContinuousMovement();
            }
            else
            {
                StopMovement();
            }
        }

        private void StopMovement()
        {
            _desiredVelocity = Vector2.zero;

            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", 0f);
            }

            UpdateCharacterState(CharacterState.Idle);
        }

        bool CanMoveCharacter()
        {
            return inputManager.CanCharacterMove;
        }
        
        private void HandleContinuousMovement()
        {
            if (moveAction == null) return;
            
            Vector2 input = moveAction.ReadValue<Vector2>();

            Vector2 movement = input.normalized * moveSpeed;
            _desiredVelocity = movement;

            // 마지막 방향 유지: 입력이 있을 때만 lastMoveX/Y 갱신
            if(input.x != 0f || input.y != 0f)
            {
                lastMoveX = 0f;
                lastMoveY = 0f;
                
                if (input.x > 0f) lastMoveX = 1f;
                else if (input.x < 0f) lastMoveX = -1f;

                if (input.y > 0f) lastMoveY = 1f;
                else if (input.y < 0f) lastMoveY = -1f;
            }

            // 애니메이터 파라미터 업데이트 (BlendTree용)
            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveX);
                animator.SetFloat("MoveY", lastMoveY);
                animator.SetFloat("MoveSpeed", movement.magnitude);
            }

            if(movement.magnitude > 0)
            {
                UpdateCharacterState(CharacterState.Moving);
                
                // 멈췄을 때도 마지막 X 방향을 유지
                if (input.x != 0)
                {
                    FlipSprite(input.x);
                }
                else
                {
                    FlipSprite(lastMoveX);
                }
            }
            else
            {
                UpdateCharacterState(CharacterState.Idle);
            }
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = inputManager != null && inputManager.CanCharacterMove
                ? _desiredVelocity
                : Vector2.zero;
        }

        private void OnDisable()
        {
            _desiredVelocity = Vector2.zero;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            if (_movementMaterial != null)
            {
                Destroy(_movementMaterial);
            }
        }

        private void FlipSprite(float directionX)
        {
            if (spriteRenderer == null) return;
            
            if (directionX > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (directionX < 0)
            {
                spriteRenderer.flipX = true;
            }
        }

        void UpdateCharacterState(CharacterState newState)
        {
            if (CurrentPlayerState == newState) return;

            CurrentPlayerState = newState;
            if (animator != null)
            {
                animator.SetInteger("State", (int)CurrentPlayerState);
            }
        }
    }
}
