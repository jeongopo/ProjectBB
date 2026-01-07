using UnityEngine;
using UnityEngine.Tilemaps;

namespace GamePlay
{
    /// <summary>
    /// 타일맵에서 상하좌우로 이동하는 메인 캐릭터
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MainCharacter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputManager inputManager;
        [SerializeField] private Tilemap groundTilemap; // 이동 가능한 타일맵
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool useGridMovement = true; // true: 타일 단위 이동, false: 연속 이동
        [SerializeField] private float gridMoveSpeed = 8f; // 그리드 이동 속도
        
        [Header("Sprite Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        // 컴포넌트
        private Rigidbody2D rb;
        
        // 그리드 이동 관련
        private bool isMoving = false;
        private Vector3 targetPosition;
        private Vector2 lastMoveDirection;
        
        // 현재 그리드 위치
        private Vector3Int currentGridPosition;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            // Rigidbody2D 설정
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        private void Start()
        {
            // InputManager 자동 찾기
            if (inputManager == null)
            {
                inputManager = FindObjectOfType<InputManager>();
                if (inputManager == null)
                {
                    Debug.LogError("InputManager를 찾을 수 없습니다!");
                }
            }
            
            // 현재 위치를 그리드 위치로 스냅
            if (groundTilemap != null)
            {
                currentGridPosition = groundTilemap.WorldToCell(transform.position);
                if (useGridMovement)
                {
                    transform.position = groundTilemap.GetCellCenterWorld(currentGridPosition);
                }
            }
            
            targetPosition = transform.position;
        }
        
        private void Update()
        {
            if (inputManager == null) return;
            
            if (useGridMovement)
            {
                HandleGridMovement();
            }
            else
            {
                HandleContinuousMovement();
            }
        }
        
        /// <summary>
        /// 그리드 기반 이동 (타일 단위로 스냅)
        /// </summary>
        private void HandleGridMovement()
        {
            // 이동 중이면 목표 위치로 부드럽게 이동
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, gridMoveSpeed * Time.deltaTime);
                
                // 목표 위치 도달 시
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                }
                return;
            }
            
            // 입력 받기
            Vector2 input = inputManager.MoveInput;
            
            // 입력이 없으면 리턴
            if (input.magnitude < 0.1f) return;
            
            // 상하좌우 방향 결정 (대각선 입력 무시)
            Vector3Int moveDirection = Vector3Int.zero;
            
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                // 좌우 이동
                moveDirection = input.x > 0 ? Vector3Int.right : Vector3Int.left;
                lastMoveDirection = new Vector2(input.x > 0 ? 1 : -1, 0);
            }
            else
            {
                // 상하 이동
                moveDirection = input.y > 0 ? Vector3Int.up : Vector3Int.down;
                lastMoveDirection = new Vector2(0, input.y > 0 ? 1 : -1);
            }
            
            // 다음 그리드 위치 계산
            Vector3Int nextGridPosition = currentGridPosition + moveDirection;
            
            // 타일맵에서 이동 가능한지 확인
            if (CanMoveTo(nextGridPosition))
            {
                currentGridPosition = nextGridPosition;
                targetPosition = groundTilemap.GetCellCenterWorld(currentGridPosition);
                isMoving = true;
                
                // 스프라이트 방향 전환 (좌우만)
                FlipSprite(lastMoveDirection.x);
            }
        }
        
        /// <summary>
        /// 연속적인 이동 (부드러운 이동)
        /// </summary>
        private void HandleContinuousMovement()
        {
            Vector2 input = inputManager.MoveInput;
            
            // 이동
            Vector2 movement = input.normalized * moveSpeed;
            rb.linearVelocity = movement;
            
            // 스프라이트 방향 전환
            if (input.x != 0)
            {
                FlipSprite(input.x);
                lastMoveDirection = input;
            }
        }
        
        /// <summary>
        /// 해당 그리드 위치로 이동 가능한지 확인
        /// </summary>
        private bool CanMoveTo(Vector3Int gridPosition)
        {
            if (groundTilemap == null) return true;
            
            // 타일이 존재하는지 확인
            TileBase tile = groundTilemap.GetTile(gridPosition);
            
            // 타일이 없으면 이동 불가
            if (tile == null) return false;
            
            // 추가 검사: 장애물 등 (필요시 추가)
            // 예: Physics2D.OverlapCircle로 충돌 검사
            
            return true;
        }
        
        /// <summary>
        /// 스프라이트 좌우 반전
        /// </summary>
        private void FlipSprite(float directionX)
        {
            if (spriteRenderer == null) return;
            
            if (directionX > 0)
            {
                spriteRenderer.flipX = false; // 오른쪽
            }
            else if (directionX < 0)
            {
                spriteRenderer.flipX = true; // 왼쪽
            }
        }
        
        /// <summary>
        /// 특정 그리드 위치로 순간이동
        /// </summary>
        public void TeleportTo(Vector3Int gridPosition)
        {
            if (groundTilemap == null) return;
            
            currentGridPosition = gridPosition;
            targetPosition = groundTilemap.GetCellCenterWorld(gridPosition);
            transform.position = targetPosition;
            isMoving = false;
        }
        
        /// <summary>
        /// 특정 월드 위치로 순간이동
        /// </summary>
        public void TeleportTo(Vector3 worldPosition)
        {
            if (groundTilemap == null)
            {
                transform.position = worldPosition;
                return;
            }
            
            Vector3Int gridPos = groundTilemap.WorldToCell(worldPosition);
            TeleportTo(gridPos);
        }
        
        /// <summary>
        /// 이동 방식 변경
        /// </summary>
        public void SetGridMovement(bool enabled)
        {
            useGridMovement = enabled;
            
            if (enabled && groundTilemap != null)
            {
                // 그리드 모드로 전환 시 현재 위치를 그리드에 스냅
                currentGridPosition = groundTilemap.WorldToCell(transform.position);
                targetPosition = groundTilemap.GetCellCenterWorld(currentGridPosition);
                isMoving = false;
            }
            
            rb.linearVelocity = Vector2.zero;
        }
        
        /// <summary>
        /// 현재 그리드 위치 반환
        /// </summary>
        public Vector3Int GetCurrentGridPosition()
        {
            return currentGridPosition;
        }
        
        /// <summary>
        /// 현재 이동 중인지 반환
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (groundTilemap == null) return;
            
            // 현재 그리드 위치 표시
            Gizmos.color = Color.green;
            Vector3 cellCenter = groundTilemap.GetCellCenterWorld(currentGridPosition);
            Gizmos.DrawWireCube(cellCenter, Vector3.one * 0.9f);
            
            // 목표 위치 표시
            if (isMoving)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(targetPosition, Vector3.one * 0.8f);
            }
        }
#endif
    }
}
