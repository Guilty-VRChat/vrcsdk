using interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

public class CustomDLLMaker
{
	public const string SOURCE_PATH = "Assets/VRCScripts";

	private static bool DEBUG_SCENE = false;

	public static string SOURCE_FULL_PATH = Application.get_dataPath() + "/VRCScripts";

	public static string SOURCE_TMP_PATH = Application.get_dataPath() + "/../VRCSDK/tmp/scripts";

	public static string EXTERNAL_DLL_OUTPUT_FULL_PATH = Application.get_dataPath() + "/../VRCSDK/tmp/bin";

	public static string INTERNAL_DLL_OUTPUT_FULL_PATH = Application.get_dataPath() + "/VRCSDK/tmp/bin";

	public static string INTERNAL_TMP_PATH = "Assets/VRCSDK/tmp";

	public static string INTERNAL_TMP_FULL_PATH = Application.get_dataPath() + "/VRCSDK/tmp";

	public static void PrepareSceneForExport()
	{
		if (CustomScriptsAvailable())
		{
			CopyAndOpenCurrentScene();
			BuildAndLoadPlugin();
			AssetDatabase.Refresh();
			EditorAssemblies.AddOnAssemblyReloadCallback("CustomDLLMaker", "SwapScripts");
		}
	}

	public static void CopyAvatarSceneAndBuildPlugin()
	{
		if (CustomScriptsAvailable())
		{
			EditorAssemblies.AddOnAssemblyReloadCallback("CustomDLLMaker", "SwapScripts");
			CreateNewSceneWithSelectedGameObject();
			BuildAndLoadPlugin();
			AssetDatabase.Refresh();
		}
	}

	public static void SwapScripts()
	{
		string @string = EditorPrefs.GetString("pluginNamespace");
		MigrateMonobehaviour(@string);
	}

	public static GameObject CreateNewSceneWithSelectedGameObject()
	{
		Object activeObject = Selection.get_activeObject();
		activeObject.set_name("vrc_" + activeObject.get_name());
		EditorApplication.SaveScene();
		EditorPrefs.SetString("currentActiveObjectName", activeObject.get_name());
		GameObject val = PrefabUtility.CreatePrefab("Assets/_CustomAvatarTMP.prefab", activeObject as GameObject);
		string currentScene = EditorApplication.get_currentScene();
		EditorPrefs.SetString("originalScenePath", currentScene);
		EditorApplication.NewEmptyScene();
		GameObject result = Object.Instantiate<GameObject>(val);
		AssetDatabase.DeleteAsset("Assets/_CustomAvatarTMP.prefab");
		AssetDatabase.Refresh();
		return result;
	}

	public static void CopyAndOpenCurrentScene()
	{
		EditorApplication.SaveScene();
		string currentScene = EditorApplication.get_currentScene();
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentScene);
		string text = INTERNAL_TMP_PATH + "/Scenes/" + fileNameWithoutExtension + "_TMP.unity";
		EditorPrefs.SetString("originalScenePath", currentScene);
		EditorApplication.SaveScene(text, true);
		AssetDatabase.Refresh();
		EditorApplication.OpenScene(text);
	}

	public static bool Cleanup()
	{
		if (!DEBUG_SCENE)
		{
			bool flag = false;
			try
			{
				CleanupInternalPlugin();
				CleanupSceneCopy();
				CleanupTmpFiles();
				return true;
			}
			catch (Exception)
			{
				Debug.LogWarning((object)"Could not cleanup tmp files. Attemping a second time.");
				return false;
			}
		}
		return true;
	}

	private static void CleanupInternalPlugin()
	{
		if (!DEBUG_SCENE)
		{
			while (Directory.Exists(INTERNAL_DLL_OUTPUT_FULL_PATH))
			{
				Directory.Delete(INTERNAL_DLL_OUTPUT_FULL_PATH, recursive: true);
			}
			AssetDatabase.Refresh();
		}
	}

	private static void CleanupSceneCopy()
	{
		if (!DEBUG_SCENE && EditorPrefs.HasKey("originalScenePath"))
		{
			string @string = EditorPrefs.GetString("originalScenePath");
			EditorPrefs.DeleteKey("originalScenePath");
			string currentScene = EditorApplication.get_currentScene();
			EditorApplication.OpenScene(@string);
			AssetDatabase.DeleteAsset(currentScene);
		}
	}

	private static void CleanupTmpFiles()
	{
		if (Directory.Exists(INTERNAL_TMP_FULL_PATH))
		{
			Directory.Delete(INTERNAL_TMP_FULL_PATH, recursive: true);
			if (File.Exists(INTERNAL_TMP_FULL_PATH + ".meta"))
			{
				File.Delete(INTERNAL_TMP_FULL_PATH + ".meta");
			}
			AssetDatabase.Refresh();
		}
	}

	public static bool CustomScriptsAvailable()
	{
		return AreCustomScriptsUsedInCurrentScene();
	}

	public static List<string> GetCustomScripts()
	{
		DLLMaker dLLMaker = new DLLMaker();
		return dLLMaker.getAllSourceFromCSFilesInPathRecursive(SOURCE_FULL_PATH);
	}

	public static bool AreCustomScriptsUsedInCurrentScene()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		bool result = false;
		List<MonoBehaviour> list = new List<MonoBehaviour>();
		Scene activeScene = SceneManager.GetActiveScene();
		GameObject[] array = Object.FindObjectsOfType<GameObject>();
		List<GameObject> list2 = new List<GameObject>();
		GameObject[] array2 = array;
		foreach (GameObject val in array2)
		{
			if (val.get_scene() == activeScene)
			{
				list2.Add(val);
			}
		}
		Object[] array3 = EditorUtility.CollectDependencies((Object[])list2.ToArray());
		Object[] array4 = array3;
		foreach (Object val2 in array4)
		{
			if (val2 != null && ((object)val2).GetType() == typeof(GameObject))
			{
				GameObject val3 = val2;
				list.AddRange(val3.GetComponentsInChildren<MonoBehaviour>(true));
			}
		}
		list.RemoveAll((MonoBehaviour c) => c == null);
		List<string> customScripts = GetCustomScripts();
		foreach (MonoBehaviour item in list)
		{
			foreach (string item2 in customScripts)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item2);
				if (((object)item).GetType().Name == fileNameWithoutExtension)
				{
					result = true;
					break;
				}
			}
		}
		return result;
	}

	public static bool IsCustomAssemblyLoaded()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		return (from a in assemblies
		where a.GetName().Name.StartsWith("vrc")
		select a).ToList().Count > 0;
	}

	public static string BuildAndLoadPlugin(bool recompileCurrentPlugin = false)
	{
		if (Directory.Exists(EXTERNAL_DLL_OUTPUT_FULL_PATH))
		{
			Directory.Delete(EXTERNAL_DLL_OUTPUT_FULL_PATH, recursive: true);
		}
		Directory.CreateDirectory(EXTERNAL_DLL_OUTPUT_FULL_PATH);
		string @string = EditorPrefs.GetString("lastExternalPluginPath");
		string text = (!recompileCurrentPlugin) ? ("vrc" + GetRandomString()) : Path.GetFileNameWithoutExtension(@string);
		EditorPrefs.SetString("pluginNamespace", text);
		DLLMaker dLLMaker = new DLLMaker();
		CreateNamespacedSourceFiles(dLLMaker, text);
		dLLMaker.sourcePaths = new List<string>();
		dLLMaker.sourcePaths.Add(SOURCE_TMP_PATH);
		dLLMaker.dllDependencies = new List<string>();
		dLLMaker.dllDependencies.Add(DLLMaker.unityExtensionDLLDirectoryPath + "GUISystem" + Path.DirectorySeparatorChar + "UnityEngine.UI.dll");
		dLLMaker.dllDependencies.Add(Application.get_dataPath() + Path.DirectorySeparatorChar + "VRCSDK" + Path.DirectorySeparatorChar + "Dependencies" + Path.DirectorySeparatorChar + "VRChat" + Path.DirectorySeparatorChar + "VRCSDK2.dll");
		dLLMaker.buildTargetName = EXTERNAL_DLL_OUTPUT_FULL_PATH + "/" + text + ".dll";
		try
		{
			dLLMaker.createDLL();
		}
		catch (Exception ex)
		{
			Debug.LogError((object)(ex.Message + "\n" + ex.StackTrace));
		}
		if (!DEBUG_SCENE)
		{
			CleanupNamespacedSourceFiles();
		}
		EditorPrefs.SetString("externalPluginPath", dLLMaker.buildTargetName);
		EditorPrefs.SetString("lastExternalPluginPath", dLLMaker.buildTargetName);
		if (!recompileCurrentPlugin)
		{
			if (!Directory.Exists(INTERNAL_DLL_OUTPUT_FULL_PATH))
			{
				Directory.CreateDirectory(INTERNAL_DLL_OUTPUT_FULL_PATH);
			}
			File.Copy(dLLMaker.buildTargetName, INTERNAL_DLL_OUTPUT_FULL_PATH + "/" + text + ".dll");
			EditorUtility.DisplayProgressBar("Reloading Assemblies", "Waiting for Unity to finish asset import!", 1f);
		}
		return text;
	}

	private static void MigrateMonobehaviour(string namespaceName)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
		EditorUtility.ClearProgressBar();
		List<Component> list = new List<Component>();
		Dictionary<GameObject, GameObject> dictionary = new Dictionary<GameObject, GameObject>();
		Dictionary<Component, Component> dictionary2 = new Dictionary<Component, Component>();
		List<Component> list2 = new List<Component>();
		Scene activeScene = SceneManager.GetActiveScene();
		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
		GameObject[] array = rootGameObjects;
		foreach (GameObject val in array)
		{
			list.AddRange(val.GetComponentsInChildren<Component>(true));
			list.RemoveAll((Component c) => c == null);
		}
		try
		{
			List<Component> list3 = new List<Component>();
			Object[] array2 = EditorUtility.CollectDependencies((Object[])Enumerable.ToArray(list));
			List<Object> list4 = new List<Object>();
			Object[] array3 = array2;
			foreach (Object val2 in array3)
			{
				if (val2 != null && ((object)val2).GetType() == typeof(GameObject) && AssetDatabase.Contains(val2) && HasCustomScripts(val2))
				{
					list4.Add(val2);
				}
			}
			Dictionary<GameObject, string> dictionary3 = new Dictionary<GameObject, string>();
			foreach (Object item in list4)
			{
				GameObject val3 = item as GameObject;
				string assetPath = AssetDatabase.GetAssetPath(val3);
				if (!Directory.Exists(Application.get_dataPath() + "/VRCSDK/tmp/Assets"))
				{
					Directory.CreateDirectory(Application.get_dataPath() + "/VRCSDK/tmp/Assets");
				}
				string text = "Assets/VRCSDK/tmp/Assets/" + Path.GetFileName(assetPath);
				AssetDatabase.CopyAsset(assetPath, text);
				dictionary3.Add(val3, text);
			}
			AssetDatabase.Refresh();
			foreach (KeyValuePair<GameObject, string> item2 in dictionary3)
			{
				GameObject key = item2.Key;
				string value = item2.Value;
				GameObject val4 = AssetDatabase.LoadAssetAtPath<GameObject>(value);
				list3.AddRange(val4.GetComponentsInChildren<Component>(true));
				dictionary.Add(key, val4);
			}
			foreach (Component item3 in list3)
			{
				if (item3 == null)
				{
					Debug.Log((object)"comp is null");
				}
				else
				{
					GameObject gameObject = item3.get_gameObject();
					Type type = ((object)item3).GetType();
					string typeName = namespaceName + "." + type.Name + ", " + namespaceName;
					Type type2 = Type.GetType(typeName);
					if (type2 != null)
					{
						if (!dictionary2.ContainsKey(item3))
						{
							Component value2 = gameObject.AddComponent(Type.GetType(typeName));
							dictionary2.Add(item3, value2);
						}
						list2.Add(item3);
					}
				}
			}
			foreach (Component item4 in list)
			{
				GameObject gameObject2 = item4.get_gameObject();
				Type type3 = ((object)item4).GetType();
				Component val5 = null;
				if (type3.Namespace == "VRCSDK2")
				{
					val5 = item4;
				}
				else
				{
					string typeName2 = namespaceName + "." + type3.Name + ", " + namespaceName;
					Type type4 = Type.GetType(typeName2);
					if (type4 == null)
					{
						continue;
					}
					val5 = gameObject2.AddComponent(type4);
					list2.Add(item4);
				}
				dictionary2.Add(item4, val5);
			}
			foreach (KeyValuePair<Component, Component> item5 in dictionary2)
			{
				MigrateObjectToNamespacedObject(item5.Key, item5.Value, namespaceName, dictionary, dictionary2);
			}
			for (int k = 0; k < list2.Count; k++)
			{
				Object.DestroyImmediate(list2[k], true);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError((object)("Error Migrating Monobehaviours - " + ex.Message + "\n" + ex.StackTrace));
		}
	}

	private static void MigrateObjectToNamespacedObject(object o, object nO, string namespaceName, Dictionary<GameObject, GameObject> prefabMap, Dictionary<Component, Component> componentMap)
	{
		//IL_0837: Unknown result type (might be due to invalid IL or missing references)
		//IL_08dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_08e6: Expected O, but got Unknown
		//IL_08ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f9: Expected O, but got Unknown
		if (o != null && nO != null)
		{
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			Type type = o.GetType();
			Type type2 = nO.GetType();
			PropertyInfo[] properties = type.GetProperties(bindingAttr);
			PropertyInfo[] properties2 = type2.GetProperties(bindingAttr);
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].GetType() != properties2[i].GetType())
				{
					Debug.LogError((object)"Dll and code property infos are out of order");
				}
				if (properties2[i].CanWrite)
				{
					try
					{
						properties2[i].SetValue(nO, properties[i].GetValue(o, null), null);
					}
					catch (Exception ex)
					{
						Debug.LogError((object)("Exception: " + ex.ToString()));
					}
				}
			}
			FieldInfo[] fields = type.GetFields(bindingAttr);
			FieldInfo[] fields2 = type2.GetFields(bindingAttr);
			for (int j = 0; j < fields.Length; j++)
			{
				if (fields[j].GetType() != fields2[j].GetType())
				{
					Debug.LogError((object)"DLL and code field infos are out of sync.");
				}
				FieldInfo fieldInfo = fields[j];
				object value = fieldInfo.GetValue(o);
				Type fieldType = GetFieldType(o, fieldInfo);
				if (fieldType == null)
				{
					fieldType = fieldInfo.FieldType;
				}
				FieldInfo fieldInfo2 = fields2[j];
				Type fieldType2 = GetFieldType(nO, fieldInfo2);
				if (fieldType2 == null)
				{
					fieldType2 = fieldInfo2.FieldType;
				}
				if (value != null)
				{
					if (fieldType == typeof(GameObject[]))
					{
						GameObject[] array = value as GameObject[];
						GameObject[] array2 = (GameObject[])new GameObject[array.Length];
						for (int k = 0; k < array.Length; k++)
						{
							GameObject val = array[k];
							if (val != null && AssetDatabase.Contains(val.get_transform().get_root().get_gameObject()))
							{
								GameObject val2 = val;
								GameObject val3 = val2;
								if (prefabMap.ContainsKey(val2))
								{
									val3 = prefabMap[val2];
								}
								array2[k] = val3;
							}
							else
							{
								array2[k] = val;
							}
						}
						fields2[j].SetValue(nO, array2);
					}
					else if (fieldType == typeof(List<GameObject>))
					{
						List<GameObject> list = value as List<GameObject>;
						List<GameObject> list2 = new List<GameObject>();
						fieldType2 = fieldInfo2.FieldType;
						for (int l = 0; l < list.Count; l++)
						{
							GameObject val4 = list[l];
							if (val4 != null && AssetDatabase.Contains(val4.get_transform().get_root().get_gameObject()))
							{
								GameObject val5 = val4;
								GameObject item = val5;
								if (prefabMap.ContainsKey(val5))
								{
									item = prefabMap[val5];
								}
								list2.Add(item);
							}
							else
							{
								list2.Add(val4);
							}
						}
						fields2[j].SetValue(nO, list2);
					}
					else if (fieldType != null && typeof(Component[]).IsAssignableFrom(fieldType))
					{
						Component[] array3 = value as Component[];
						fieldType2 = fieldInfo2.FieldType;
						Array array4 = Array.CreateInstance(fieldType2.GetElementType(), array3.Length);
						for (int m = 0; m < array3.Length; m++)
						{
							Component val6 = array3[m];
							if (val6 != null && AssetDatabase.Contains(val6.get_transform().get_root().get_gameObject()))
							{
								GameObject gameObject = val6.get_transform().get_root().get_gameObject();
								GameObject val7 = gameObject;
								if (prefabMap.ContainsKey(gameObject))
								{
									val7 = prefabMap[gameObject];
								}
								array4.SetValue(val7.GetComponent(fieldType2.GetElementType()), m);
							}
							else if (val6 == null)
							{
								array4.SetValue(val6, m);
							}
							else
							{
								array4.SetValue(val6.GetComponent(fieldType2.GetElementType()), m);
							}
						}
						fields2[j].SetValue(nO, array4);
					}
					else if (fieldType != null && fieldType.GetGenericArguments().Length > 0 && fieldType.GetGenericArguments()[0].IsSubclassOf(typeof(Component)))
					{
						IEnumerable source = value as IEnumerable;
						IEnumerable<Component> enumerable = source.OfType<Component>();
						fieldType2 = fieldInfo2.FieldType;
						Type type3 = fieldType2.GetGenericArguments()[0];
						Type type4 = typeof(List<>).MakeGenericType(type3);
						IList list3 = (IList)Activator.CreateInstance(type4);
						foreach (Component item2 in enumerable)
						{
							if (item2 != null && AssetDatabase.Contains(item2.get_gameObject().get_transform().get_root()
								.get_gameObject()))
							{
								GameObject gameObject2 = item2.get_gameObject().get_transform().get_root()
									.get_gameObject();
								GameObject val8 = gameObject2;
								if (prefabMap.ContainsKey(gameObject2))
								{
									val8 = prefabMap[gameObject2];
								}
								Component component = val8.GetComponent(type3);
								list3.Add(component);
							}
							else
							{
								list3.Add(item2.get_gameObject().GetComponent(type3));
							}
						}
						fields2[j].SetValue(nO, list3);
					}
					else if (fieldType != null && fieldType.IsArray)
					{
						Type elementType = fieldInfo2.FieldType.GetElementType();
						if (elementType.Namespace.Contains(namespaceName))
						{
							object[] array5 = value as object[];
							if (array5 != null)
							{
								object obj = Activator.CreateInstance(fieldInfo2.FieldType, array5.Length);
								object[] array6 = obj as object[];
								for (int n = 0; n < array6.Length; n++)
								{
									array6[n] = Activator.CreateInstance(elementType);
								}
								for (int num = 0; num < array5.Length; num++)
								{
									MigrateObjectToNamespacedObject(array5[num], array6[num], namespaceName, prefabMap, componentMap);
								}
								fields2[j].SetValue(nO, array6);
							}
						}
						else
						{
							fields2[j].SetValue(nO, fields[j].GetValue(o));
						}
					}
					else if (fieldType != null && fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
					{
						Type type5 = fieldInfo2.FieldType.GetGenericArguments().Single();
						if (type5.Namespace.Contains(namespaceName))
						{
							object obj2 = Activator.CreateInstance(fieldInfo2.FieldType);
							IList list4 = value as IList;
							IList list5 = obj2 as IList;
							for (int num2 = 0; num2 < list4.Count; num2++)
							{
								list5.Add(Activator.CreateInstance(type5));
							}
							for (int num3 = 0; num3 < list4.Count; num3++)
							{
								MigrateObjectToNamespacedObject(list4[num3], list5[num3], namespaceName, prefabMap, componentMap);
							}
							fields2[j].SetValue(nO, list5);
						}
						else
						{
							fields2[j].SetValue(nO, fields[j].GetValue(o));
						}
					}
					else if (fieldType != null && typeof(Object[]).IsAssignableFrom(fieldType))
					{
						fields2[j].SetValue(nO, fields[j].GetValue(o));
					}
					else if (fieldType != null && fieldType.GetGenericArguments().Length > 0 && fieldType.GetGenericArguments()[0].IsSubclassOf(typeof(Object)))
					{
						fields2[j].SetValue(nO, fields[j].GetValue(o));
					}
					else if (fieldType != null && fieldType.IsGenericType)
					{
						Debug.LogError((object)("Unsupported generic type: " + fieldType + ". Tell graham!"));
					}
					else if (fieldType != null && fieldType.IsEnum)
					{
						fields2[j].SetValue(nO, (int)value);
					}
					else if (IsAssetFieldType(o, fieldInfo))
					{
						GameObject val9 = (GetFieldType(o, fieldInfo) != typeof(GameObject)) ? ((object)value.get_gameObject()) : ((object)(value as GameObject));
						GameObject val10 = val9;
						if (prefabMap.ContainsKey(val9))
						{
							val10 = prefabMap[val9];
						}
						object value2 = (fieldType == typeof(GameObject)) ? val10 : ((fieldType != typeof(Transform)) ? ((object)val10.GetComponent(fieldType2)) : ((object)val10.get_transform()));
						fields2[j].SetValue(nO, value2);
					}
					else if (fieldType != null && fieldType.IsSubclassOf(typeof(Component)) && value != null && componentMap.ContainsKey(value))
					{
						Component value3 = componentMap[value];
						fields2[j].SetValue(nO, value3);
					}
					else if (!fieldInfo2.FieldType.Namespace.Contains(namespaceName))
					{
						fields2[j].SetValue(nO, fields[j].GetValue(o));
					}
					else if (fieldInfo2.FieldType.Namespace.Contains(namespaceName))
					{
						object value4 = Activator.CreateInstance(fieldInfo2.FieldType);
						fieldInfo2.SetValue(nO, value4);
						MigrateObjectToNamespacedObject(value, fieldInfo2.GetValue(nO), namespaceName, prefabMap, componentMap);
					}
				}
			}
		}
	}

	private static bool IsAssetFieldType(object c, FieldInfo fieldInfo)
	{
		bool result = false;
		Type fieldType = GetFieldType(c, fieldInfo);
		if (fieldType == null)
		{
			result = false;
		}
		else if (fieldType == typeof(GameObject))
		{
			GameObject val = fieldInfo.GetValue(c) as GameObject;
			result = (AssetDatabase.Contains(val) && !AssetDatabase.GetAssetPath(val).Contains("tmp/Assets"));
		}
		else if (fieldType.IsSubclassOf(typeof(Component)))
		{
			Component val2 = fieldInfo.GetValue(c) as Component;
			result = (AssetDatabase.Contains(val2) && !AssetDatabase.GetAssetPath(val2).Contains("tmp/Assets"));
		}
		return result;
	}

	private static Object GetAssetField(object c, FieldInfo fieldInfo)
	{
		Object result = null;
		Type fieldType = fieldInfo.FieldType;
		if (fieldType == null)
		{
			result = null;
		}
		if (fieldType == typeof(GameObject))
		{
			result = (fieldInfo.GetValue(c) as GameObject);
		}
		else if (fieldType.IsSubclassOf(typeof(Component)))
		{
			result = (fieldInfo.GetValue(c) as Component);
		}
		return result;
	}

	private static Type GetFieldType(object c, FieldInfo fieldInfo)
	{
		return (fieldInfo.GetValue(c) != null) ? fieldInfo.GetValue(c).GetType() : null;
	}

	private static void CreateNamespacedSourceFiles(DLLMaker maker, string namespaceName)
	{
		if (Directory.Exists(SOURCE_TMP_PATH))
		{
			Directory.Delete(SOURCE_TMP_PATH, recursive: true);
		}
		Directory.CreateDirectory(EXTERNAL_DLL_OUTPUT_FULL_PATH);
		List<string> allSourceFromCSFilesInPathRecursive = maker.getAllSourceFromCSFilesInPathRecursive(SOURCE_FULL_PATH);
		foreach (string item in allSourceFromCSFilesInPathRecursive)
		{
			string text = SOURCE_TMP_PATH + "/" + Path.GetFileName(item);
			if (!Directory.Exists(SOURCE_TMP_PATH))
			{
				Directory.CreateDirectory(SOURCE_TMP_PATH);
			}
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Copy(item, text);
			AddNamespaceToFile(namespaceName, text);
		}
	}

	private static bool HasCustomScripts(GameObject go)
	{
		bool flag = false;
		DLLMaker dLLMaker = new DLLMaker();
		List<string> allSourceFromCSFilesInPathRecursive = dLLMaker.getAllSourceFromCSFilesInPathRecursive(SOURCE_FULL_PATH);
		for (int i = 0; i < allSourceFromCSFilesInPathRecursive.Count; i++)
		{
			allSourceFromCSFilesInPathRecursive[i] = Path.GetFileNameWithoutExtension(allSourceFromCSFilesInPathRecursive[i]);
		}
		MonoBehaviour[] componentsInChildren = go.GetComponentsInChildren<MonoBehaviour>(true);
		MonoBehaviour[] array = componentsInChildren;
		foreach (MonoBehaviour val in array)
		{
			foreach (string item in allSourceFromCSFilesInPathRecursive)
			{
				if (val != null && ((object)val).GetType() != null && !string.IsNullOrEmpty(item) && ((object)val).GetType().Name == item)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		return flag;
	}

	private static void CleanupNamespacedSourceFiles()
	{
		if (Directory.Exists(SOURCE_TMP_PATH))
		{
			Directory.Delete(SOURCE_TMP_PATH, recursive: true);
		}
	}

	private static void AddNamespaceToFile(string namespaceName, string filePath)
	{
		string str = string.Empty;
		if (File.Exists(filePath))
		{
			str = File.ReadAllText(filePath);
		}
		string str2 = "namespace " + namespaceName + "\n{ \n";
		string str3 = "\n} ";
		File.WriteAllText(filePath, str2 + str + str3);
	}

	private static string GetRandomString()
	{
		string randomFileName = Path.GetRandomFileName();
		return randomFileName.Replace(".", string.Empty);
	}

	public static bool DoesScriptDirExist()
	{
		return Directory.Exists(SOURCE_FULL_PATH);
	}

	public static void CreateDirectories()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		if (!DoesScriptDirExist() && APIUser.get_CurrentUser() != null && (int)APIUser.get_CurrentUser().get_developerType() >= 1)
		{
			Directory.CreateDirectory(SOURCE_FULL_PATH);
			AssetDatabase.Refresh();
		}
	}
}