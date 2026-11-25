// This file is auto-generated from XML files.
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;
using DataEnumDefines;
public class DataStorage
{
	public Dictionary<string,BBString> BBStringData;
	public Dictionary<string,Currency> CurrencyData;
	public Dictionary<string,Customer> CustomerData;
	public Dictionary<string,Drop> DropData;
	public Dictionary<string,Hunting> HuntingData;
	public Dictionary<string,Item> ItemData;
	public Dictionary<string,Minigame_Boiling> Minigame_BoilingData;
	public Dictionary<string,Minigame_Cutting> Minigame_CuttingData;
	public Dictionary<string,Recipe_Combine> Recipe_CombineData;
	public Dictionary<string,Recipe_Interaction> Recipe_InteractionData;
	public Dictionary<string,TestCustomer> TestCustomerData;
	public Dictionary<string,TestIngredients> TestIngredientsData;
	public Dictionary<string,TestMinigame_Boiling> TestMinigame_BoilingData;
	public void LoadData()
	{
		BBStringData = DataManager.LoadDefineData<BBString>("BBString");
		CurrencyData = DataManager.LoadDefineData<Currency>("Currency");
		CustomerData = DataManager.LoadDefineData<Customer>("Customer");
		DropData = DataManager.LoadDefineData<Drop>("Drop");
		HuntingData = DataManager.LoadDefineData<Hunting>("Hunting");
		ItemData = DataManager.LoadDefineData<Item>("Item");
		Minigame_BoilingData = DataManager.LoadDefineData<Minigame_Boiling>("Minigame_Boiling");
		Minigame_CuttingData = DataManager.LoadDefineData<Minigame_Cutting>("Minigame_Cutting");
		Recipe_CombineData = DataManager.LoadDefineData<Recipe_Combine>("Recipe_Combine");
		Recipe_InteractionData = DataManager.LoadDefineData<Recipe_Interaction>("Recipe_Interaction");
		TestCustomerData = DataManager.LoadDefineData<TestCustomer>("TestCustomer");
		TestIngredientsData = DataManager.LoadDefineData<TestIngredients>("TestIngredients");
		TestMinigame_BoilingData = DataManager.LoadDefineData<TestMinigame_Boiling>("TestMinigame_Boiling");
	}
	// classDefine
	public class BBString
	{
			public string ID;
			public string BODY;
	}
	public class Currency
	{
			public string ID;
			public string ICONPATH;
	}
	public class Customer
	{
			public string ID;
			public int PATIENCE_MINUTES;
	}
	public class Drop
	{
			public string ID;
			public int MIN;
			public int MAX;
			public float RATE;
			public string ITEMID;
	}
	public class Hunting
	{
			public string ID;
			public string GROUND_NAME;
			public string NAME_K;
			public int OPEN_CONDITION;
			public int TOTAL_BATTLE_PHASE;
			public string[] DROP_ID;
	}
	public class Item
	{
			public string ID;
			public string NAME;
			public string DESC;
			public string ICONPATH;
			public string DISPLAY_NAME;
	}
	public class Minigame_Boiling
	{
			public string ID;
			public string NAME;
			public string INGREDIENT;
			public int[] SWEET_SPOT;
			public int BOILING_TIME;
			public int BOILING_DIFFICULTY;
	}
	public class Minigame_Cutting
	{
			public string ID;
			public string NAME;
			public string INGREDIENT;
			public int CUTTING_CYCLES;
			public int CUTTING_COUNTS;
			public float CUTTING_INTERVAL;
			public int CUTTING_ACCELERATION;
			public int[] CUTTING_RANGE;
	}
	public class Recipe_Combine
	{
			public string ID;
			public string _DEV_NAME;
			public string NAME;
			public string PRODUCT_DISH_ID;
			public string COMPONENT_1;
			public string COMPONENT_2;
			public string COMPONENT_3;
			public string COMPONENT_4;
			public string COMPONENT_5;
			public string TRASH_DISH_ID;
			public string DESC_BODY;
			public string DISPLAY_NAME;
			public int PRICE;
			public string ICONPATH;
	}
	public class Recipe_Interaction
	{
			public string ID;
			public string INPUT_ITEM_ID;
			public ENUMINTERACTIONTYPE INTERACTION_TYPE;
			public string OUTPUT_ITEM_ID;
			public string OVERCOOKED_PRODUCT_ID;
			public string DESC;
			public string DESC_BODY;
	}
	public class TestCustomer
	{
			public string ID;
			public string NAME;
			public int LEVEL;
			public int COOKINGSTEP;
			public bool SPECIAL;
	}
	public class TestIngredients
	{
			public string ID;
			public string GROUND_NAME;
			public string NAME_K;
			public int OPEN_CONDITION;
			public int TOTAL_BATTLE_PHASE;
			public int[] REWARD_INTERVAL;
	}
	public class TestMinigame_Boiling
	{
			public string ID;
			public string NAME;
			public string INGREDIENT;
			public int[] SWEET_SPOT;
			public int BOILING_TIME;
			public int BOILING_DIFFICULTY;
	}
}
