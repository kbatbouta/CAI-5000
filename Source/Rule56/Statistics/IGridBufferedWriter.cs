using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Assertions;
using Verse;
namespace CombatAI.Statistics
{
    public class IGridBufferedWriter
    {
        private readonly int      blockSize;
        private readonly byte[]   buffer;
        public readonly  string[] fields;
        private readonly string   filePrefix;

        public readonly Dictionary<string, Array> grids = new Dictionary<string, Array>();

        public readonly  Map    map;
        private readonly string name;
        public readonly  Type[] types;
        private readonly string writeDir;

        public IGridBufferedWriter(Map map, string name, string filePrefix, string[] fields, Type[] types)
        {
            Assert.IsNotNull(map);
            Assert.AreEqual(fields.Length, types.Length);
            this.map        = map;
            this.fields     = fields;
            this.types      = types;
            this.name       = name;
            this.filePrefix = filePrefix;
            blockSize       = map.cellIndices.NumGridCells * 4;
            for (int i = 0; i < fields.Length; i++)
            {
                grids[fields[i]] = Array.CreateInstance(types[i], map.cellIndices.NumGridCells);

            }
            buffer = new byte[8 + sizeof(float) * fields.Length * map.cellIndices.NumGridCells];
            Array.Copy(BitConverter.GetBytes(map.Size.x), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(map.Size.z), 0, buffer, 4, 4);
            string dataPath = Path.Combine(GenFilePaths.ConfigFolderPath, "data");
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            writeDir = Path.Combine(dataPath, name);
            if (!Directory.Exists(writeDir))
            {
                Directory.CreateDirectory(writeDir);
            }
            else
            {
                OpCounter = Directory.GetFiles(writeDir)?.Count(s => s.EndsWith(".bin")) ?? 0;
            }
        }

        public int OpCounter
        {
            get;
            private set;
        }

        public Array this[string field]
        {
            get => grids[field];
        }

        public void Clear()
        {
            for (int i = 0; i < fields.Length; i++)
            {
                grids[fields[i]].Initialize();
            }
        }

        public void Write()
        {
            int offset = 8;
            for (int i = 0; i < fields.Length; i++)
            {
                Array grid = grids[fields[i]];
                Buffer.BlockCopy(grid, 0, buffer, offset, blockSize);
                offset += blockSize;
            }
            string f = Path.Combine(writeDir, $"{filePrefix}_{OpCounter++}.bin");
            while (File.Exists(f))
            {
                f = Path.Combine(writeDir, $"{filePrefix}_{OpCounter++}.bin");
            }
            File.WriteAllBytes(f, buffer);
        }
    }
}
