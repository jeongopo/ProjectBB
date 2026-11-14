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

        // 이동이 허용되는 상태: Popup(인벤토리 등)은 열려 있어도 플레이어가 계속 움직일 수 있어야 함
        public bool CanCharacterMove => currentInputState == InputState.Default || currentInputState == InputState.Popup;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("InputActionAsset이 할당되지 않았습니다!");
                return;
            }

            EnableActionMaps(GetActionMapNames(currentInputState));
        }

        public void SwitchInputState(InputState newState)
        {
            if (currentInputState == newState) return;
            if (inputActions == null) return;

            currentInputState = newState;

            // 전체 Disable 후 재Enable 하면, 콜백 안에서 호출될 때 눌린 키가 다시 감지되어 같은 액션이 한 번 더 발동됨
            // → 새 상태에 필요 없는 맵만 끄고, 이미 켜져 있는 맵은 그대로 둔다
            var newMapNames = GetActionMapNames(newState);
            foreach (var map in inputActions.actionMaps)
            {
                if (System.Array.IndexOf(newMapNames, map.name) < 0)
                    map.Disable();
            }
            EnableActionMaps(newMapNames);

            OnInputStateChanged?.Invoke(newState);
        }

        private void EnableActionMaps(string[] mapNames)
        {
            foreach (var mapName in mapNames)
            {
                inputActions.FindActionMap(mapName)?.Enable();
            }
        }

        public InputActionMap GetCurrentActionMap()
            => inputActions?.FindActionMap(GetActionMapName(currentInputState));

        public InputActionMap GetActionMap(InputState state)
            => inputActions?.FindActionMap(GetActionMapName(state));

        private string GetActionMapName(InputState state) => state switch
        {
            InputState.Default => "Default",
            InputState.Minigame => "Minigame",
            InputState.UI => "UI",
            InputState.Popup => "UI",
            _ => "Default"
        };

        // Popup은 이동(Default)과 UI 조작(UI) 액션맵을 동시에 활성화함
        private string[] GetActionMapNames(InputState state) => state switch
        {
            InputState.Popup => new[] { "Default", "UI" },
            _ => new[] { GetActionMapName(state) }
        };
    }
}
