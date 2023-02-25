using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public class CellMetrics
    {
#pragma warning disable CS0649
        private readonly List<string> metricKeys = new List<string>();
#pragma warning restore CS0649

        private readonly List<Func<IntVec3, float>>       metrics            = new List<Func<IntVec3, float>>();
        private readonly List<bool>                       metricsComperative = new List<bool>();
        private readonly List<float>                      metricsVal         = new List<float>();
        private readonly List<float>                      metricsWeight      = new List<float>();
        private          AvoidanceTracker.AvoidanceReader avoidanceReader;
        private          Map                              map;
        private          SightTracker.SightReader         sightReader;

        public void Begin(Map map, SightTracker.SightReader sightReader, AvoidanceTracker.AvoidanceReader avoidanceReader, IntVec3 root)
        {
            this.map             = map;
            this.sightReader     = sightReader;
            this.avoidanceReader = avoidanceReader;
            metricsVal.Clear();
            for (int i = 0; i < metrics.Count; i++)
            {
                if (metricsComperative[i])
                {
                    metricsVal.Add(metrics[i](root));
                }
                else
                {
                    metricsVal.Add(0);
                }
            }
        }

        public void Add(string key, Func<SightTracker.SightReader, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            metricKeys.Add(key);
            metricsWeight.Add(weight);
            metrics.Add(cell => func(sightReader, cell));
            metricsComperative.Add(comperative);
        }

        public void Add(string key, Func<AvoidanceTracker.AvoidanceReader, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            metricKeys.Add(key);
            metricsWeight.Add(weight);
            metrics.Add(cell => func(avoidanceReader, cell));
            metricsComperative.Add(comperative);
        }

        public void Add(string key, Func<SightTracker.SightReader, AvoidanceTracker.AvoidanceReader, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            metricKeys.Add(key);
            metricsWeight.Add(weight);
            metrics.Add(cell => func(sightReader, avoidanceReader, cell));
            metricsComperative.Add(comperative);
        }

        public void Add(string key, Func<Map, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            metricKeys.Add(key);
            metricsWeight.Add(weight);
            metrics.Add(cell => func(map, cell));
            metricsComperative.Add(comperative);
        }

        public void Add(string key, Func<IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            metricKeys.Add(key);
            metricsWeight.Add(weight);
            metrics.Add(func);
            metricsComperative.Add(comperative);
        }

        public float Score(IntVec3 cell)
        {
            float f = 0;
            for (int i = 0; i < metrics.Count; i++)
            {
                f += (metrics[i](cell) - metricsVal[i]) * metricsWeight[i];
            }
            return f;
        }

        public string MaxAbsKey(IntVec3 cell)
        {
            float  max   = float.MinValue;
            string key   = null;
            int    index = -1;
            for (int i = 0; i < metrics.Count; i++)
            {
                float val = Mathf.Abs(metrics[i](cell) - metricsVal[i]);
                if (max < val)
                {
                    key   = metricKeys[i];
                    max   = val;
                    index = i;
                }
            }
            if (index != -1)
            {
                return $"{key}={metrics[index](cell)},{Mathf.Abs(metrics[index](cell) - metricsVal[index])}";
            }
            return key;
        }

        public void Print(IntVec3 cell)
        {
            string message = "";
            for (int i = 0; i < metrics.Count; i++)
            {
                message += $"{metricKeys[i]}=({Mathf.Abs(metrics[i](cell) - metricsVal[i])}, {metrics[i](cell)}, {metricsVal[i]})\n";
            }
            Log.Message(message);
        }

        public string MinAbsKey(IntVec3 cell)
        {
            float  min   = float.MaxValue;
            string key   = null;
            int    index = -1;
            for (int i = 0; i < metrics.Count; i++)
            {
                float val = Mathf.Abs(metrics[i](cell) - metricsVal[i]);
                if (min > val)
                {
                    key   = metricKeys[i];
                    min   = val;
                    index = i;
                }
            }
            if (index != -1)
            {
                return $"{key}={metrics[index](cell)},{Mathf.Abs(metrics[index](cell) - metricsVal[index])}";
            }
            return string.Empty;
        }

        public void Reset()
        {
            sightReader     = null;
            avoidanceReader = null;
            map             = null;
            metricsVal.Clear();
        }
    }
}
