using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEnumDefines;

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
    [SerializeField] private GameObject _sceneManager = default;
    [SerializeField] private GameObject _inputManager = default;
    [SerializeField] private GameObject _audioManager = default;
    [SerializeField] private GameObject _UIManager = default;
    [SerializeField] private GameObject _dataManager = default;

    [SerializeField] private GameObject _timelineHandler = default;    

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

