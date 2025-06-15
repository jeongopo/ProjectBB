// This file is auto-generated from XML files.
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;
public class DataStorage
{
	public Dictionary<string,TestCustomer> TestCustomerData;
	public Dictionary<string,TestIngredients> TestIngredientsData;
	public Dictionary<string,TestItem1> TestItem1Data;
	public void LoadData()
	{
		TestCustomerData = DataManager.LoadDefineData<TestCustomer>("TestCustomer");
		TestIngredientsData = DataManager.LoadDefineData<TestIngredients>("TestIngredients");
		TestItem1Data = DataManager.LoadDefineData<TestItem1>("TestItem1");
	}
	// classDefine
	public class TestCustomer
	{
			public string ID;
			public string Name;
			public int Level;
			public int CookingStep;
			public bool Special;
	}
	public class TestIngredients
	{
			public string ID;
			public string Name;
			public string Ingredients;
			public int CookingStep;
			public bool main;
	}
	public class TestItem1
	{
			public string ID;
			public string Name;
			public int Level;
			public int CookingStep;
			public bool Special;
	}
}
