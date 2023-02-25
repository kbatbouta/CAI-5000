using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;
namespace CombatAI
{
    /*
     * Shamelessly stolen from Telefonmast
     * Source:
     * https://github.com/RealTelefonmast/TeleCore/blob/main/Source/TeleCore/Static/TeleContentDB.cs#L26     
     */
    [StaticConstructorOnStartup]
    public static class AssetBundleDatabase
    {
        private static readonly List<AssetBundle> bundles = new List<AssetBundle>(10);

        static AssetBundleDatabase()
        {
            string path = GetCurrentSystemPath;
            if (!Directory.Exists(path))
            {
                Log.Warning($"ISMA: {path} doesn't exists.");
                return;
            }
            string[] files = Directory.GetFiles(path);
            if (files.NullOrEmpty())
            {
                Log.Warning($"ISMA: {path} is empty.");
                return;
            }
            foreach (string file in Directory.GetFiles(path))
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(file);

                if (bundle == null)
                {
                    Log.Warning($"ISMA: Could not load AssetBundle at {file}");
                }
                else
                {
                    bundles.Add(bundle);
                    Log.Message($"ISMA: Loaded bundle {bundle.GetAllAssetNames().ToLineList()}");
                }
            }
        }

        private static string GetCurrentSystemPath
        {
            get
            {
                string pathPart = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    pathPart = "StandaloneOSX";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    pathPart = "StandaloneWindows";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    pathPart = "StandaloneLinux64";
                }
                return Path.Combine(Finder.Mod.Content.RootDir, $@"Resources/Bundles/{pathPart}");
            }
        }

        public static T Get<T>(string name) where T : Object
        {
            return AssetCache<T>.Get(name);
        }

        private static class AssetCache<T> where T : Object
        {
            private static readonly Dictionary<string, T> assetByName = new Dictionary<string, T>();

            public static T Get(string name)
            {
                if (!assetByName.TryGetValue(name, out T asset) || asset == null)
                {
                    foreach (AssetBundle bundle in bundles)
                    {
                        asset = bundle.LoadAsset<T>(name);
                        if (asset != null)
                        {
                            assetByName[name] = asset;
                            break;
                        }
                    }
                    if (asset == null)
                    {
                        Log.Warning($"ISMA: Could not load {typeof(T).Name} '{name}'");
                    }
                }
                return asset;
            }
        }
    }
}
