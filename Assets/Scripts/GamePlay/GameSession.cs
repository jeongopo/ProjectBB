using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GamePlay
{
    [DefaultExecutionOrder(100)]
    public sealed class GameSession : MonoBehaviour
    {
        private const string GameplayPath = "Assets/Maps/TestScene.unity";
        private const string LobbyPath = "Assets/Scenes/MainLobby.unity";
        private static bool _enteringFromLobby;
        private static SaveData _pendingSave;
        private GameClock _clock;
        private InventoryManager _inventory;
        private CurrencyWallet _wallet;
        private bool _nextDaySaved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHandler()
        {
            _enteringFromLobby = false;
            _pendingSave = null;
            UnitySceneManager.sceneLoaded -= HandleSceneLoaded;
            UnitySceneManager.sceneLoaded += HandleSceneLoaded;
        }

        internal static bool OpenGameplay(SaveData save)
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(GameplayPath);
            if (_enteringFromLobby || buildIndex < 0)
            {
                return false;
            }

            _enteringFromLobby = true;
            _pendingSave = save;
            UnitySceneManager.LoadScene(buildIndex);
            return true;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == GameplayPath && _enteringFromLobby)
            {
                new GameObject("Game Session").AddComponent<GameSession>();
            }
        }

        private void Awake()
        {
            _clock = FindFirstObjectByType<GameClock>();
            _inventory = FindFirstObjectByType<InventoryManager>();
            _wallet = FindFirstObjectByType<CurrencyWallet>();
            if (_wallet == null)
            {
                _wallet = gameObject.AddComponent<CurrencyWallet>();
            }
        }

        private void Start()
        {
            SaveData save = _pendingSave;
            _pendingSave = null;
            _enteringFromLobby = false;
            if (_clock == null || _inventory == null)
            {
                Debug.LogError("[Save] GameClock 또는 InventoryManager가 없습니다.", this);
                return;
            }

            if (save == null)
            {
                SaveCheckpoint(false);
            }
            else
            {
                _inventory.RestoreStacks(save);
                _wallet.RestoreBalances(save);
                _clock.ReadFrom(save);
            }

            _clock.OnDayEnded += HandleDayEnded;
        }

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.OnDayEnded -= HandleDayEnded;
            }
        }

        private void HandleDayEnded(int day)
        {
            _nextDaySaved = SaveCheckpoint(true);
        }

        internal bool TryReturnToLobby()
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(LobbyPath);
            if (buildIndex < 0 || (!_nextDaySaved && !SaveCheckpoint(true)))
            {
                return false;
            }

            UnitySceneManager.LoadScene(buildIndex);
            return true;
        }

        private bool SaveCheckpoint(bool nextDay)
        {
            SaveData data = new SaveData
            {
                _locationId = "TestScene",
                ItemStacks = _inventory.CaptureStacks(),
                Balances = _wallet.CaptureBalances(),
                coin = _wallet.Get(CurrencyIds.Gold),
            };

            if (nextDay)
            {
                _clock.WriteNextDayTo(data);
            }
            else
            {
                _clock.WriteTo(data);
            }

            bool saved = SaveSystem.Save(data);
            if (!saved)
            {
                SystemMessageUI.Show(GetComponentText("STR_Lobby_SaveFailed"));
            }
            return saved;
        }

        private string GetComponentText(string id)
        {
            DataManager manager = FindFirstObjectByType<DataManager>();
            return manager?.dataStorage.GetBBStringText(id);
        }
    }
}
