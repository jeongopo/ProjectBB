
namespace GameEnumDefines
{
    /// <summary>
    /// 게임의 전반적인 상태를 나타내는 열거형
    /// </summary>
public enum GameState
{
	Gameplay, //regular state: player moves, attacks, can perform actions
	Pause, //pause menu is opened, the whole game world is frozen
	Dialogue,
}

public enum InputState
{
    Default, //default input state for player movement and interactions
    Minigame, //input state for minigames, disables player movement and enables minigame controls
    UI, //input state for UI navigation, disables player movement and enables UI controls
}
}