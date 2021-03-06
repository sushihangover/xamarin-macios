﻿using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Xamarin.MacDev.Tasks;
using Xamarin.MacDev;

namespace Xamarin.iOS.Tasks
{
	public abstract class FindWatchAppBundleTaskBase : Task
	{
		#region Inputs

		public string SessionId { get; set; }

		[Required]
		public ITaskItem[] AppExtensionReferences { get; set; }

		#endregion Inputs

		#region Outputs

		[Output]
		public string WatchAppBundle { get; set; }

		#endregion

		public override bool Execute ()
		{
			var pwd = PathUtils.ResolveSymbolicLink (Environment.CurrentDirectory);

			Log.LogTaskName ("FindWatchAppBundle");
			Log.LogTaskProperty ("AppExtensionReferences", AppExtensionReferences);

			for (int i = 0; i < AppExtensionReferences.Length; i++) {
				var plist = PDictionary.FromFile (Path.Combine (AppExtensionReferences[i].ItemSpec, "Info.plist"));
				PString expectedBundleIdentifier, bundleIdentifier, extensionPoint;
				PDictionary extension, attributes;

				if (!plist.TryGetValue ("NSExtension", out extension))
					continue;

				if (!extension.TryGetValue ("NSExtensionPointIdentifier", out extensionPoint))
					continue;

				if (extensionPoint.Value != "com.apple.watchkit")
					continue;

				// Okay, we've found the WatchKit App Extension...
				if (!extension.TryGetValue ("NSExtensionAttributes", out attributes))
					continue;

				if (!attributes.TryGetValue ("WKAppBundleIdentifier", out expectedBundleIdentifier))
					continue;

				// Scan the *.app subdirectories to find the WatchApp bundle...
				foreach (var bundle in Directory.GetDirectories (AppExtensionReferences[i].ItemSpec, "*.app")) {
					if (!File.Exists (Path.Combine (bundle, "Info.plist")))
						continue;

					plist = PDictionary.FromFile (Path.Combine (bundle, "Info.plist"));

					if (!plist.TryGetValue ("CFBundleIdentifier", out bundleIdentifier))
						continue;

					if (bundleIdentifier.Value != expectedBundleIdentifier.Value)
						continue;
					
					WatchAppBundle = PathUtils.AbsoluteToRelative (pwd, PathUtils.ResolveSymbolicLink (bundle));

					return true;
				}
			}

			return !Log.HasLoggedErrors;
		}
	}
}
