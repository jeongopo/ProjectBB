using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
	Gameplay, //regular state: player moves, attacks, can perform actions
	Pause, //pause menu is opened, the whole game world is frozen
	Dialogue,
}

// <summary>
// 게임 전반적으로 관리해야 하는 클래스
// </summary>
public class GameManager : MonoBehaviour
{
    public GameState CurrentGameState => _currentGameState;

    [Header("Game states")]
	[SerializeField] private GameState _currentGameState = default;
	[SerializeField] private GameState _previousGameState = default;

    [Header("Managers")]
    [SerializeField] private SaveSystem _saveSystem = default;
    [SerializeField] private GameObject _sceneManager = default;
    [SerializeField] private GameObject _inputManager = default;
    [SerializeField] private GameObject _audioManager = default;
    [SerializeField] private GameObject _UIManager = default;
    [SerializeField] private GameObject _dataManager = default;

    [SerializeField] private GameObject _timelineHandler = default;    

    private bool _hasSaveData = false;

    private void Start()
	{
		StartGame();
	}

    void StartGame()
	{
		_hasSaveData = false;
		
		_saveSystem.WriteEmptySaveFile();
		_saveSystem.SetNewGameData();
	}

	private void StartNewGame()
	{
		_hasSaveData = false;
		
		_saveSystem.WriteEmptySaveFile();
		_saveSystem.SetNewGameData();
    }
    private void ContinuePreviousGame()
	{
    }

    public void UpdateGameState(GameState newGameState)
	{
		if (newGameState == CurrentGameState)
			{
                return;
            }

		_previousGameState = _currentGameState;
		_currentGameState = newGameState;
    }
}

