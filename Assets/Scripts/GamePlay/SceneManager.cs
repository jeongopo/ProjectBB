using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 어떤 용도로 쓸지 생각해봐야할듯
public class SceneManager : MonoBehaviour
{
    public GameSceneType sceneType;
	public enum GameSceneType
	{
		//Playable scenes
		MainMap,
		Menu,

		//Special scenes
		Initialisation,
		PersistentManagers,
		Gameplay,

		Art,
	}
}