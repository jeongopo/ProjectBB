using UnityEngine;
using UnityEngine.Tilemaps;

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
        [SerializeField] private Tilemap groundTilemap; // 이동 가능한 타일맵
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("Sprite Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D rb;
        private Animator animator;
        private float lastMoveX = 0f;
        private float lastMoveY = 0f;
        
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
        }
        
        private void Start()
        {
            if (inputManager == null)
            {
                inputManager = FindAnyObjectByType<InputManager>();
                if (inputManager == null)
                {
                    Debug.LogError("InputManager를 찾을 수 없습니다!");
                }
            }

            if (groundTilemap != null)
            {
                transform.position = groundTilemap.GetCellCenterWorld(groundTilemap.WorldToCell(transform.position));
            }
        }
        
        private void Update()
        {
            if (inputManager == null) return;
            
            HandleContinuousMovement();
        }
        
        private void HandleContinuousMovement()
        {
            Vector2 input = inputManager.MoveInput;

            Vector2 movement = input.normalized * moveSpeed;
            rb.linearVelocity = movement;

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

            Debug.Log("Character State changed " + CurrentPlayerState + " -> " + newState);
            CurrentPlayerState = newState;
            animator.SetInteger("State", (int) CurrentPlayerState);
        }
    }
}