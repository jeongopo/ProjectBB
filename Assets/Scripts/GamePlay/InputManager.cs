using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay
{
    /// <summary>
    /// 2D 캐릭터의 입력을 관리하는 InputManager 클래스
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Action Asset")]
        [SerializeField] private InputActionAsset inputActions;
        
        // Input Actions
        private InputAction moveAction;

        private InputAction interactAction;

        // 입력 값
        private Vector2 moveInput;

        private bool isInteracting;
        
        // 프로퍼티
        public Vector2 MoveInput => moveInput;

        public bool IsInteracting => isInteracting;
        
        // 이벤트
        public event System.Action OnInteractPressed;
        public event System.Action OnInteractReleased;

        
        private void Awake()
        {
            InitializeInputActions();
        }
        
        private void OnEnable()
        {
            EnableInputActions();
        }
        
        private void OnDisable()
        {
            DisableInputActions();
        }
        
        /// <summary>
        /// Input Actions 초기화
        /// </summary>
        private void InitializeInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset이 할당되지 않았습니다!");
                return;
            }
            
            var playerActionMap = inputActions.FindActionMap("Default");
            
            if (playerActionMap == null)
            {
                Debug.LogError("Player ActionMap을 찾을 수 없습니다!");
                return;
            }
            
            // 각 액션 찾기
            moveAction = playerActionMap.FindAction("Move");
            interactAction = playerActionMap.FindAction("Interact");
            
            if (interactAction != null)
            {
                interactAction.performed += OnInteract;
                interactAction.canceled += OnInteractCanceled;
            }
        }
        
        /// <summary>
        /// Input Actions 활성화
        /// </summary>
        private void EnableInputActions()
        {
            moveAction?.Enable();
            interactAction?.Enable();
        }
        
        /// <summary>
        /// Input Actions 비활성화
        /// </summary>
        private void DisableInputActions()
        {
            moveAction?.Disable();
            interactAction?.Disable();
        }
        
        private void Update()
        {
            UpdateInputValues();
        }
        
        private void UpdateInputValues()
        {
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }
        
        }
        
        
        // Interact 콜백
        private void OnInteract(InputAction.CallbackContext context)
        {
            isInteracting = true;
            OnInteractPressed?.Invoke();
        }
        
        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            isInteracting = false;
            OnInteractReleased?.Invoke();
        }
        
        public void EnableInput()
        {
            EnableInputActions();
        }

        public void DisableInput()
        {
            DisableInputActions();
            ResetInputValues();
        }

         private void ResetInputValues()
        {
            moveInput = Vector2.zero;
            isInteracting = false;
        }
        
        private void OnDestroy()
        {

            if (interactAction != null)
            {
                interactAction.performed -= OnInteract;
                interactAction.canceled -= OnInteractCanceled;
            }
        }
    }
}
