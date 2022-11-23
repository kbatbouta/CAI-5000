using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatAI
{
	public class CompatibilityManager
	{
		private static List<Type> types;

		public static void Initialize()
		{
			// find types
			types = typeof(CompatibilityManager).Assembly.GetTypes()
				.Where(t => t.HasAttribute<LoadIf>())
				.ToList();
			// start processing
			LoadIf attr;
			LoadNamed named;			
			foreach (Type type in types)
			{				
				if ((attr = type.TryGetAttribute<LoadIf>()) != null && attr.packageId != null && LoadedModManager.RunningMods.Any(m => m.PackageId.ToLower() == attr.packageId))
				{
					Log.Message($"CAI: attempting LoadIf for {attr.packageId}");
					// start loading defs
					foreach(FieldInfo field in AccessTools.GetDeclaredFields(type))
					{						 
						// check if the field is static and doesn't have unsaved attribute
						if(field.IsStatic && !field.HasAttribute<UnsavedAttribute>())
						{
							bool success = false;
							Type fieldType = field.FieldType;
							if (typeof(Def).IsAssignableFrom(fieldType))
							{
								field.SetValue(null, AccessTools.Method(typeof(DefDatabase<>).MakeGenericType(fieldType), "GetNamed").Invoke(null, new object[] { field.Name, false }));
								success = true;
							}
							else if (field.HasAttribute<LoadNamed>() && (named = field.TryGetAttribute<LoadNamed>())?.name != null)
							{
								if (typeof(Type).IsAssignableFrom(fieldType))
								{
									field.SetValue(null, AccessTools.TypeByName(named.name));
									success = true;
								}
								else if (typeof(FieldInfo).IsAssignableFrom(fieldType))
								{
									field.SetValue(null, AccessTools.Field(named.name));
									success = true;
								}
								else if (typeof(MethodInfo).IsAssignableFrom(fieldType))
								{
									field.SetValue(null, AccessTools.Method(named.name, parameters: named.prams));
									success = true;
								}
							}
							else if (typeof(Boolean).IsAssignableFrom(fieldType) && field.Name == "active")
							{
								field.SetValue(null, true);
								success = true;
							}
							if (!success || field.GetValue(null) == null)
							{
								Log.Error($"CAI: Failed to load '{field.Name}'({field.FieldType}) from '{attr.packageId}'");
							}
							else
							{
								Log.Message($"CAI: <color=green>Load success</color> '{field.Name}'t<{field.FieldType}> from '{attr.packageId}'");
							}
						}
					}
				}
			}			
		}

		private static T GetDef<T>(string name) where T : Def
		{
			return DefDatabase<T>.GetNamed(name);
		}
	}
}

