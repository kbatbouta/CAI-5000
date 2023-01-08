using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatAI
{
	public static class ITKeyedHelper
	{
		private static readonly HashSet<Type> processed = new HashSet<Type>();

		public static void Initialize()
		{
			Type[] types = typeof(ITKeyedHelper).Assembly.GetTypes();
			for(int i = 0; i < types.Length; i++)
			{
				Type t = types[i];
				if (processed.Contains(t))
				{
					continue;
				}				
				if (t.HasAttribute<ITKeyed>())
				{
					ApplyKeys(t);
				}
				processed.Add(t);
			}
		}

		private static void ApplyKeys(Type type)
		{
			List<FieldInfo> fields = type.GetFields(AccessTools.all).ToList();
			fields.SortBy(f => f.Name);
			int index = 1;
			for (int i = 0; i < fields.Count; i++)
			{
				FieldInfo f = fields[i];
				if (!f.IsStatic || f.FieldType != typeof(ITKeyed))
				{
					continue;
				}
				f.SetValue(null, (ITKey)index);
				index++;
			}
		}
	}
}

