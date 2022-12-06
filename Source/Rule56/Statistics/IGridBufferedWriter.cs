using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace CombatAI.Statistics
{
	public class IGridBufferedWriter
	{
		private readonly int blockSize;
		private readonly string writeDir;
		private readonly string name;
		private readonly string filePrefix;

		private int opCounter;
		private byte[] buffer;

		public readonly Map map;
		public readonly string[] fields;
		public readonly Type[] types;

		public readonly Dictionary<string, Array> grids = new Dictionary<string, Array>();

		public int OpCounter => opCounter;

		public IGridBufferedWriter(Map map, string name, string filePrefix, string[] fields, Type[] types)
		{
			Assert.IsNotNull(map);
			Assert.AreEqual(fields.Length, types.Length);
			this.map = map;
			this.fields = fields;
			this.types = types;
			this.name = name;
			this.filePrefix = filePrefix;
			blockSize = map.cellIndices.NumGridCells * 4;
			for (var i = 0; i < fields.Length; i++)
				grids[fields[i]] = Array.CreateInstance(types[i], map.cellIndices.NumGridCells);
			buffer = new byte[8 + sizeof(float) * fields.Length * map.cellIndices.NumGridCells];
			Array.Copy(BitConverter.GetBytes(map.Size.x), 0, buffer, 0, 4);
			Array.Copy(BitConverter.GetBytes(map.Size.z), 0, buffer, 4, 4);
			var dataPath = Path.Combine(GenFilePaths.ConfigFolderPath, $"data");
			if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
			writeDir = Path.Combine(dataPath, name);
			if (!Directory.Exists(writeDir))
				Directory.CreateDirectory(writeDir);
			else
				opCounter = Directory.GetFiles(writeDir)?.Count(s => s.EndsWith(".bin")) ?? 0;
		}

		public Array this[string field] => grids[field];

		public void Clear()
		{
			for (var i = 0; i < fields.Length; i++) grids[fields[i]].Initialize();
		}

		public void Write()
		{
			var offset = 8;
			for (var i = 0; i < fields.Length; i++)
			{
				var grid = grids[fields[i]];
				Buffer.BlockCopy(grid, 0, buffer, offset, blockSize);
				offset += blockSize;
			}

			var f = Path.Combine(writeDir, $"{filePrefix}_{opCounter++}.bin");
			while (File.Exists(f)) f = Path.Combine(writeDir, $"{filePrefix}_{opCounter++}.bin");
			File.WriteAllBytes(f, buffer);
		}
	}
}