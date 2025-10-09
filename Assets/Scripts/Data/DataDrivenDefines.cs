// This file is auto-generated from XML files.
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;
using DataEnumDefines;
public class DataStorage
{
	public Dictionary<string,TestCustomer> TestCustomerData;
	public Dictionary<string,TestIngredients> TestIngredientsData;
	public Dictionary<string,TestMinigame_Boiling> TestMinigame_BoilingData;
	public void LoadData()
	{
		TestCustomerData = DataManager.LoadDefineData<TestCustomer>("TestCustomer");
		TestIngredientsData = DataManager.LoadDefineData<TestIngredients>("TestIngredients");
		TestMinigame_BoilingData = DataManager.LoadDefineData<TestMinigame_Boiling>("TestMinigame_Boiling");
	}
	// classDefine
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
			public string NAME;
			public string INGREDIENTS;
			public int COOKINGSTEP;
			public bool MAIN;
			public ENUMGRADE GRADE;
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
