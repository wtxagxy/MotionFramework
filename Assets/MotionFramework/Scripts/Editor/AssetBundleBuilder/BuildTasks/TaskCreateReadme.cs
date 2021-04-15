﻿//--------------------------------------------------
// Motion Framework
// Copyright©2021-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MotionFramework.Patch;

namespace MotionFramework.Editor
{
	/// <summary>
	/// 创建说明文件
	/// </summary>
	internal class TaskCreateReadme : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParameters = context.GetContextObject<AssetBundleBuilder.BuildParametersContext>();
			var buildOptions = context.GetContextObject<AssetBundleBuilder.BuildOptionsContext>();
			var unityManifestContext = context.GetContextObject<TaskBuilding.UnityManifestContext>();
			CreateReadmeFile(buildParameters, buildOptions, unityManifestContext.Manifest);
		}

		/// <summary>
		/// 创建Readme文件到输出目录
		/// </summary>
		private void CreateReadmeFile(AssetBundleBuilder.BuildParametersContext buildParameters, AssetBundleBuilder.BuildOptionsContext buildOptions, AssetBundleManifest unityManifest)
		{
			string[] allAssetBundles = unityManifest.GetAllAssetBundles();

			// 删除旧文件
			string filePath = $"{buildParameters.OutputDirectory}/{PatchDefine.ReadmeFileName}";
			if (File.Exists(filePath))
				File.Delete(filePath);

			BuildLogger.Log($"创建说明文件：{filePath}");

			StringBuilder content = new StringBuilder();
			AppendData(content, $"构建平台：{buildParameters.BuildTarget}");
			AppendData(content, $"构建版本：{buildParameters.BuildVersion}");
			AppendData(content, $"构建时间：{DateTime.Now}");

			AppendData(content, "");
			AppendData(content, $"--着色器--");
			AppendData(content, $"IsCollectAllShaders：{AssetBundleCollectorSettingData.Setting.IsCollectAllShaders}");
			AppendData(content, $"ShadersBundleName：{AssetBundleCollectorSettingData.Setting.ShadersBundleName}");

			AppendData(content, "");
			AppendData(content, $"--配置信息--");
			for (int i = 0; i < AssetBundleCollectorSettingData.Setting.Collectors.Count; i++)
			{
				AssetBundleCollectorSetting.Collector wrapper = AssetBundleCollectorSettingData.Setting.Collectors[i];
				AppendData(content, $"Directory : {wrapper.CollectDirectory} | {wrapper.BundleLabelClassName} | {wrapper.SearchFilterClassName}");
			}

			AppendData(content, "");
			AppendData(content, $"--构建参数--");
			AppendData(content, $"CompressOption：{buildOptions.CompressOption}");
			AppendData(content, $"IsForceRebuild：{buildOptions.IsForceRebuild}");
			AppendData(content, $"IsDisableWriteTypeTree：{buildOptions.IsDisableWriteTypeTree}");
			AppendData(content, $"IsIgnoreTypeTreeChanges：{buildOptions.IsIgnoreTypeTreeChanges}");

			AppendData(content, "");
			AppendData(content, $"--构建清单--");
			for (int i = 0; i < allAssetBundles.Length; i++)
			{
				AppendData(content, allAssetBundles[i]);
			}

			PatchManifest patchFile = AssetBundleBuilder.LoadPatchManifestFile(buildParameters);
			{
				AppendData(content, "");
				AppendData(content, $"--更新清单--");
				foreach (var element in patchFile.ElementList)
				{
					if (element.Version == buildParameters.BuildVersion)
					{
						AppendData(content, element.BundleName);
					}
				}

				AppendData(content, "");
				AppendData(content, $"--变体列表--");
				foreach (var variant in patchFile.VariantList)
				{
					AppendData(content, variant.ToString());
				}
			}

			// 创建新文件
			File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);
		}
		private void AppendData(StringBuilder sb, string data)
		{
			sb.Append(data);
			sb.Append("\r\n");
		}
	}
}