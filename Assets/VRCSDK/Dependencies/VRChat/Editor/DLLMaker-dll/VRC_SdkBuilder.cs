using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC;
using VRC.Core;
using VRCSDK2;

public class VRC_SdkBuilder : MonoBehaviour
{
	private static string vrcPath;

	public static bool shouldBuildUnityPackage;

	public static int mNumClientsToLaunch = 1;

	private static string RandomRoomDigits;

	public static int numClientsToLaunch
	{
		get
		{
			mNumClientsToLaunch = EditorPrefs.GetInt("VRC_numClientsToLaunch");
			return mNumClientsToLaunch;
		}
		set
		{
			mNumClientsToLaunch = value;
			EditorPrefs.SetInt("VRC_numClientsToLaunch", mNumClientsToLaunch);
		}
	}

	public VRC_SdkBuilder()
		: this()
	{
	}

	private static bool VerifyCredentials()
	{
		if (!RemoteConfig.IsInitialized())
		{
			RemoteConfig.Init((Action)null, (Action)null);
		}
		if (!APIUser.get_IsLoggedInWithCredentials() && ApiCredentials.Load())
		{
			APIUser.FetchCurrentUser((Action<ApiModelContainer<APIUser>>)delegate(ApiModelContainer<APIUser> c)
			{
				AnalyticsSDK.LoggedInUserChanged(c.get_Model() as APIUser);
			}, (Action<ApiModelContainer<APIUser>>)null);
		}
		if (!APIUser.get_IsLoggedInWithCredentials())
		{
			GUILayout.Label("Please use the \"VRChat SDK/Settings\" window to log in.", EditorStyles.get_boldLabel(), (GUILayoutOption[])new GUILayoutOption[0]);
			return false;
		}
		return true;
	}

	public static void ExportAndUploadAvatarBlueprint(GameObject externalReference = null)
	{
		if (VerifyCredentials())
		{
			EditorPrefs.DeleteKey("externalPluginPath");
			ExportCurrentAvatarResource(externalReference);
			UploadAvatarResource();
		}
	}

	private static void UploadAvatarResource()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!string.IsNullOrEmpty(vrcPath))
		{
			VRC_AvatarDescriptor component = Selection.get_activeObject().GetComponent<VRC_AvatarDescriptor>();
			PipelineSaver val = component.get_gameObject().AddComponent<PipelineSaver>();
			val.contentType = 0;
			EditorApplication.set_isPlaying(true);
			VRCPipelineManagerEditor.launchedFromSDKPipeline = true;
		}
	}

	private static void ExportCurrentAvatarResource(GameObject avatarResource = null)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		if (avatarResource != null)
		{
			Selection.set_activeObject(avatarResource);
		}
		vrcPath = null;
		if (Selection.get_activeObject() == null)
		{
			EditorUtility.DisplayDialog("Build Custom Avatar", "You must select the avatar root gameobject you want to build.", "Ok");
		}
		else
		{
			VRC_AvatarDescriptor component = (Selection.get_activeObject() as GameObject).GetComponent<VRC_AvatarDescriptor>();
			if (component == null)
			{
				EditorUtility.DisplayDialog("Build Custom Avatar", "You must place a VRC_AvatarDescriptor on the root of you custom scene", "Ok");
			}
			else
			{
				component.unityVersion = Application.get_unityVersion();
				EditorUtility.SetDirty(component);
				PipelineManager component2 = component.get_gameObject().GetComponent<PipelineManager>();
				if (component2 != null)
				{
					component2.contentType = 0;
					if (string.IsNullOrEmpty(component2.blueprintId))
					{
						component2.AssignId();
					}
					EditorPrefs.SetString("lastBuiltAssetBundleBlueprintID", component2.blueprintId);
					EditorUtility.SetDirty(component2);
					EditorSceneManager.MarkSceneDirty(component2.get_gameObject().get_scene());
					EditorSceneManager.SaveScene(component2.get_gameObject().get_scene());
					PrefabUtility.CreatePrefab("Assets/_CustomAvatar.prefab", Selection.get_activeObject() as GameObject);
					if (shouldBuildUnityPackage)
					{
						List<string> list = new List<string>();
						list.Add("Assets/_CustomAvatar.prefab");
						string dstPath = Application.get_dataPath() + "/../" + Selection.get_activeObject().get_name() + ".unitypackage";
						AssetExporter.ExportCurrentAssetAsUnityPackage(list, dstPath);
					}
					else
					{
						AssetExporter.CleanupUnityPackageExport();
					}
					AssetBundleBuild val = default(AssetBundleBuild);
					val.assetNames = new string[1]
					{
						"Assets/_CustomAvatar.prefab"
					};
					val.assetBundleName = "customAvatar.unity3d";
					string sourceFileName = Application.get_temporaryCachePath() + "/" + val.assetBundleName;
					vrcPath = Application.get_temporaryCachePath() + "/custom.vrca";
					BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.get_selectedBuildTargetGroup();
					BuildTarget activeBuildTarget = EditorUserBuildSettings.get_activeBuildTarget();
					BuildPipeline.BuildAssetBundles(Application.get_temporaryCachePath(), (AssetBundleBuild[])new AssetBundleBuild[1]
					{
						val
					}, 0, EditorUserBuildSettings.get_activeBuildTarget());
					if (File.Exists(vrcPath))
					{
						File.Delete(vrcPath);
					}
					File.Move(sourceFileName, vrcPath);
					EditorPrefs.SetString("currentBuildingAssetBundlePath", vrcPath);
					EditorUserBuildSettings.SwitchActiveBuildTarget(selectedBuildTargetGroup, activeBuildTarget);
					AssetDatabase.DeleteAsset("Assets/_CustomAvatar.prefab");
					AssetDatabase.Refresh();
					VRC_Editor.RecordActivity("avatar", Path.GetFileName(vrcPath));
				}
				else
				{
					EditorUtility.DisplayDialog("Could not find PipelineManager", "Your scene does not contain a PipelineManager. It should be auto added by the AvatarDescriptor.", "Ok");
				}
			}
		}
	}

	public static void UploadLastExportedSceneBlueprint()
	{
		if (VerifyCredentials())
		{
			string @string = EditorPrefs.GetString("lastExternalPluginPath");
			string text = WWW.UnEscapeURL(EditorPrefs.GetString("lastVRCPath"));
			bool flag = false;
			if (string.IsNullOrEmpty(text) || (!string.IsNullOrEmpty(text) && !File.Exists(text)))
			{
				flag = true;
			}
			else if (CustomDLLMaker.CustomScriptsAvailable() && !string.IsNullOrEmpty(@string) && !File.Exists(@string))
			{
				flag = true;
			}
			if (!flag)
			{
				EditorPrefs.SetString("currentBuildingAssetBundlePath", text);
				if (APIUser.get_CurrentUser().get_hasScriptingAccess() && CustomDLLMaker.CustomScriptsAvailable())
				{
					EditorPrefs.SetString("externalPluginPath", @string);
				}
				else
				{
					CustomDLLMaker.ClearSavedPluginPrefs();
				}
				if (shouldBuildUnityPackage)
				{
					AssetExporter.ExportCurrentSceneAsUnityPackageIfNotExist();
				}
				else
				{
					AssetExporter.CleanupUnityPackageExport();
				}
				AssetExporter.LaunchSceneBlueprintUploader();
			}
			else
			{
				EditorUtility.DisplayDialog("Could not run VRChat scene", "Last built VRChat scene could not be found. Please Test/Compile Full Scene (slow).", "OK");
			}
		}
	}

	public static void ExportAndUploadSceneBlueprint()
	{
		if (VerifyCredentials())
		{
			ExportSceneAndPrepareForUpload();
		}
	}

	private static void RecompileSceneScripts()
	{
		if (CustomDLLMaker.CustomScriptsAvailable())
		{
			string @string = EditorPrefs.GetString("lastExternalPluginPath");
			string text = WWW.UnEscapeURL(EditorPrefs.GetString("lastVRCPath"));
			bool flag = false;
			if (string.IsNullOrEmpty(text) || (!string.IsNullOrEmpty(text) && !File.Exists(text)))
			{
				flag = true;
			}
			else if (!string.IsNullOrEmpty(@string) && !File.Exists(@string))
			{
				flag = true;
			}
			if (!flag)
			{
				CustomDLLMaker.BuildAndLoadPlugin(recompileCurrentPlugin: true);
			}
			else
			{
				EditorUtility.DisplayDialog("Could not recompile scripts", "Something went wrong. Please Test/Compile Full Scene (slow)", "OK");
			}
		}
	}

	public static void RunLastExportedSceneResource()
	{
		string @string = EditorPrefs.GetString("lastExternalPluginPath");
		string string2 = EditorPrefs.GetString("lastVRCPath");
		bool flag = false;
		if (string.IsNullOrEmpty(string2) || (!string.IsNullOrEmpty(string2) && !File.Exists(string2)))
		{
			flag = true;
		}
		else if (CustomDLLMaker.CustomScriptsAvailable() && !string.IsNullOrEmpty(@string) && !File.Exists(@string))
		{
			flag = true;
		}
		if (!flag)
		{
			if (CustomDLLMaker.CustomScriptsAvailable())
			{
				RecompileSceneScripts();
			}
			AssetExporter.RunScene(string2, @string);
		}
		else
		{
			EditorUtility.DisplayDialog("Could not run VRChat scene", "Last built VRChat scene could not be found. Please Test/Compile Full Scene (slow).", "OK");
		}
	}

	public static void ExportSceneResourceAndRun()
	{
		try
		{
			if (ExportSceneResource())
			{
				EditorAssemblies.AddOnAssemblyReloadCallback("VRC.AssetExporter", "RunExportedSceneResourceAndCleanupPlugin");
			}
			else
			{
				AssetExporter.RunExportedSceneResourceAndCleanupPlugin();
			}
		}
		catch (Exception ex)
		{
			AssetExporter.CleanupTmpFiles();
			EditorAssemblies.ClearAssemblyReloadCallbacks();
			throw ex;
			IL_0038:;
		}
	}

	private static void ExportSceneAndPrepareForUpload()
	{
		try
		{
			if (shouldBuildUnityPackage)
			{
				AssetExporter.ExportCurrentSceneAsUnityPackage();
			}
			else
			{
				AssetExporter.CleanupUnityPackageExport();
			}
			if (ExportSceneResource())
			{
				if (!APIUser.get_CurrentUser().get_hasScriptingAccess() && CustomDLLMaker.CustomScriptsAvailable())
				{
					CustomDLLMaker.ClearSavedPluginPrefs();
				}
				EditorAssemblies.AddOnAssemblyReloadCallback("CustomDLLMaker", "Cleanup");
				EditorAssemblies.AddOnAssemblyReloadCallback("VRC.AssetExporter", "LaunchSceneBlueprintUploader");
			}
			else
			{
				CustomDLLMaker.ClearSavedPluginPrefs();
				AssetExporter.LaunchSceneBlueprintUploader();
			}
		}
		catch (Exception ex)
		{
			AssetExporter.CleanupTmpFiles();
			EditorAssemblies.ClearAssemblyReloadCallbacks();
			throw ex;
			IL_0082:;
		}
	}

	public static string GetLastUrl()
	{
		if (string.IsNullOrEmpty(EditorPrefs.GetString("lastVRCPath")))
		{
			return null;
		}
		string @string = EditorPrefs.GetString("lastExternalPluginPath");
		string string2 = EditorPrefs.GetString("lastVRCPath");
		string str = WWW.EscapeURL(string2).Replace("+", "%20");
		string text = WWW.EscapeURL(@string).Replace("+", "%20");
		if (string.IsNullOrEmpty(RandomRoomDigits))
		{
			RandomRoomDigits = Tools.GetRandomDigits(10);
		}
		string randomRoomDigits = RandomRoomDigits;
		string str2 = "vrchat://create?roomId=" + randomRoomDigits;
		str2 = str2 + "&url=file:///" + str;
		if (!string.IsNullOrEmpty(text))
		{
			str2 = str2 + "&pluginUrl=file:///" + text;
		}
		return str2;
	}

	public static bool ExportSceneResource()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		PipelineManager[] array = Object.FindObjectsOfType<PipelineManager>();
		if (array.Length > 0)
		{
			PipelineManager val = array[0];
			val.contentType = 1;
			if (string.IsNullOrEmpty(val.blueprintId))
			{
				val.AssignId();
			}
			EditorPrefs.SetString("lastBuiltAssetBundleBlueprintID", val.blueprintId);
			EditorUtility.SetDirty(array[0]);
			EditorSceneManager.MarkSceneDirty(val.get_gameObject().get_scene());
			EditorSceneManager.SaveScene(val.get_gameObject().get_scene());
		}
		if (CustomDLLMaker.CustomScriptsAvailable())
		{
			ExportCurrentSceneResourceWithPlugin();
			return true;
		}
		CustomDLLMaker.ClearSavedPluginPrefs();
		AssetExporter.ExportCurrentSceneResource();
		return false;
	}

	private static void ExportCurrentSceneResourceWithPlugin()
	{
		CustomDLLMaker.PrepareSceneForExport();
		EditorAssemblies.AddOnAssemblyReloadCallback("VRC.AssetExporter", "FinishExportCurrentSceneResourceWithPlugin");
	}

	[MenuItem("VRChat SDK/Clear Cache and PlayerPrefs")]
	private static void ClearPlayerPrefs()
	{
		Tools.ClearUserData();
	}

	public static void PreBuildBehaviourPackaging()
	{
		Debug.Log((object)"Importing web objects.");
		VRC_WebPanel[] array = Object.FindObjectsOfType<VRC_WebPanel>();
		foreach (VRC_WebPanel val in array)
		{
			val.ImportWebData();
			Debug.Log((object)(val.get_name() + " recorded " + val.webData.Count.ToString() + " objects."));
			Undo.RecordObject(val.get_gameObject(), "Store Web Data");
		}
		EditorSceneManager.SaveOpenScenes();
	}
}
