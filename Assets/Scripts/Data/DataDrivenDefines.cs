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
	public void LoadData()
	{
		TestCustomerData = DataManager.LoadDefineData<TestCustomer>("TestCustomer");
		TestIngredientsData = DataManager.LoadDefineData<TestIngredients>("TestIngredients");
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
			public EnumGrade Grade;
	}
}
