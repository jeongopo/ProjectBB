using UnityEngine;
using UnityEngine.InputSystem;
using GameEnumDefines;

namespace GamePlay
{
    public class InputManager : MonoBehaviour
    {
        [Header("Input Action Asset")]
        [SerializeField] private InputActionAsset inputActions;
        
        private InputState currentInputState = InputState.Default;
        
        public InputState CurrentInputState => currentInputState;
        public event System.Action<InputState> OnInputStateChanged;

        
        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset이 할당되지 않았습니다!");
                return;
            }
        }

        public void SwitchInputState(InputState newState)
        {
            if (currentInputState == newState) return;
            if (inputActions == null) return;

            inputActions.Disable();
            currentInputState = newState;
            
            var actionMap = inputActions.FindActionMap(GetActionMapName(newState));
            if (actionMap != null) actionMap.Enable();
            
            OnInputStateChanged?.Invoke(newState);
        }

        public InputActionMap GetCurrentActionMap() 
            => inputActions?.FindActionMap(GetActionMapName(currentInputState));

        private string GetActionMapName(InputState state) => state switch
        {
            InputState.Default => "Default",
            InputState.Minigame => "Minigame",
            InputState.UI => "UI",
            _ => "Default"
        };
    }
}
