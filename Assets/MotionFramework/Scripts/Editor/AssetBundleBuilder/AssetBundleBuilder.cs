﻿//--------------------------------------------------
// Motion Framework
// Copyright©2018-2021 何冠峰
// Licensed under the MIT license
//--------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using MotionFramework.Patch;
using MotionFramework.Utility;

namespace MotionFramework.Editor
{
	public class AssetBundleBuilder
	{
		/// <summary>
		/// 构建参数
		/// </summary>
		public class BuildParameters
		{
			/// <summary>
			/// 输出目录根路径
			/// </summary>
			public string OutputRoot;

			/// <summary>
			/// 构建平台
			/// </summary>
			public BuildTarget BuildTarget;

			/// <summary>
			/// 构建版本
			/// </summary>
			public int BuildVersion;

			/// <summary>
			/// 验证资源包的哈希类型
			/// </summary>
			public EHashType HashType;

			#region 构建选项
			/// <summary>
			/// 压缩选项
			/// </summary>
			public ECompressOption CompressOption;

			/// <summary>
			/// 是否强制重新构建整个项目，如果为FALSE则是增量打包
			/// </summary>
			public bool IsForceRebuild;

			// 高级选项
			public bool IsAppendHash = false;
			public bool IsDisableWriteTypeTree = false;
			public bool IsIgnoreTypeTreeChanges = true;
			#endregion
		}

		/// <summary>
		/// 构建选项
		/// </summary>
		public class BuildOptionsContext : IContextObject
		{
			public ECompressOption CompressOption = ECompressOption.Uncompressed;
			public bool IsForceRebuild = false;
			public bool IsAppendHash = false;
			public bool IsDisableWriteTypeTree = false;
			public bool IsIgnoreTypeTreeChanges = false;
		}

		/// <summary>
		/// 构建参数
		/// </summary>
		public class BuildParametersContext : IContextObject
		{
			/// <summary>
			/// 输出的根目录
			/// </summary>
			public string OutputRoot { private set; get; }

			/// <summary>
			/// 构建的平台
			/// </summary>
			public BuildTarget BuildTarget { private set; get; }

			/// <summary>
			/// 构建的资源版本号
			/// </summary>
			public int BuildVersion { private set; get; }

			/// <summary>
			/// 用于验证资源包的哈希类型
			/// </summary>
			public EHashType HashType { private set; get; }

			/// <summary>
			/// 最终的输出目录
			/// </summary>
			public string OutputDirectory { private set; get; }

			public BuildParametersContext(string outputRoot, BuildTarget buildTarget, int buildVersion, EHashType hashType)
			{
				OutputRoot = outputRoot;
				BuildTarget = buildTarget;
				BuildVersion = buildVersion;
				HashType = hashType;
				OutputDirectory = MakeOutputDirectory(outputRoot, buildTarget);
			}

			/// <summary>
			/// 获取本次构建的补丁目录
			/// </summary>
			public string GetPackageDirectory()
			{
				return $"{OutputRoot}/{BuildTarget}/{BuildVersion}";
			}
		}


		private readonly BuildContext _buildContext = new BuildContext();

		/// <summary>
		/// 开始构建
		/// </summary>
		public void Run(BuildParameters buildParameters)
		{
			// 清空旧数据
			_buildContext.ClearAllContext();

			// 构建参数
			var buildParametersContext = new BuildParametersContext(buildParameters.OutputRoot, buildParameters.BuildTarget, buildParameters.BuildVersion, buildParameters.HashType);
			_buildContext.SetContextObject(buildParametersContext);

			// 构建选项
			var buildOptionsContext = new BuildOptionsContext();
			buildOptionsContext.CompressOption = buildParameters.CompressOption;
			buildOptionsContext.IsForceRebuild = buildParameters.IsForceRebuild;
			buildOptionsContext.IsAppendHash = buildParameters.IsAppendHash;
			buildOptionsContext.IsDisableWriteTypeTree = buildParameters.IsDisableWriteTypeTree;
			buildOptionsContext.IsIgnoreTypeTreeChanges = buildParameters.IsIgnoreTypeTreeChanges;
			_buildContext.SetContextObject(buildOptionsContext);

			List<IBuildTask> pipeline = new List<IBuildTask>
			{
				new TaskPrepare(), //前期准备工作
				new TaskGetBuildMap(), //获取构建列表
				new TaskBuilding(), //开始构建
				new TaskEncryption(), //加密资源文件
				new TaskCheckCycle(), //检测循环依赖
				new TaskCreatePatchManifest(), //创建补丁文件
				new TaskCreateReadme(), //创建说明文件
				new TaskCopyUpdateFiles() //复制更新文件
			};
			BuildRunner.Run(pipeline, _buildContext);
			BuildLogger.Log($"构建完成！");
		}

		/// <summary>
		/// 从输出目录加载补丁清单文件
		/// </summary>
		public static PatchManifest LoadPatchManifestFile(BuildParametersContext buildParameters)
		{
			string filePath = $"{buildParameters.OutputDirectory}/{PatchDefine.PatchManifestFileName}";
			if (File.Exists(filePath) == false)
				return new PatchManifest();

			string jsonData = FileUtility.ReadFile(filePath);
			return PatchManifest.Deserialize(jsonData);
		}

		/// <summary>
		/// 获取配置的输出目录
		/// </summary>
		public static string MakeOutputDirectory(string outputRoot, BuildTarget buildTarget)
		{
			return $"{outputRoot}/{buildTarget}/{PatchDefine.UnityManifestFileName}";
		}
	}
}