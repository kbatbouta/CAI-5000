using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
namespace CombatAI
{
    public class CellMetrics
    {
        private SightTracker.SightReader         sightReader;
        private AvoidanceTracker.AvoidanceReader avoidanceReader;
        private Map                              map;
            
        private readonly List<Func<IntVec3, float>> metrics       = new List<Func<IntVec3, float>>();
        private readonly List<float>                metricsVal    = new List<float>();
        private readonly List<float>                metricsWeight = new List<float>();
        private readonly List<bool>                 metricsComperative = new List<bool>();
#pragma warning disable CS0649
        private readonly List<string>               metricKeys = new List<string>();
#pragma warning restore CS0649

        public CellMetrics()
        { 
        }

        public void Begin(Map map, SightTracker.SightReader sightReader, AvoidanceTracker.AvoidanceReader avoidanceReader, IntVec3 root)
        {
            this.map             = map;
            this.sightReader     = sightReader;
            this.avoidanceReader = avoidanceReader;
            this.metricsVal.Clear();
            for (int i = 0; i < metrics.Count; i++)
            {
                if (this.metricsComperative[i])
                {
                    this.metricsVal.Add(metrics[i](root));
                }
                else
                {
                    this.metricsVal.Add(0);
                }
            }
        }

        public void Add(string key, Func<SightTracker.SightReader, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            this.metricKeys.Add(key);
            this.metricsWeight.Add(weight);
            this.metrics.Add((cell) => func(this.sightReader, cell));
            this.metricsComperative.Add(comperative);
        }
        
        public void Add(string key, Func<AvoidanceTracker.AvoidanceReader, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            this.metricKeys.Add(key);
            this.metricsWeight.Add(weight);
            this.metrics.Add((cell) => func(this.avoidanceReader, cell));
            this.metricsComperative.Add(comperative);
        }
        
        public void Add(string key, Func<SightTracker.SightReader, AvoidanceTracker.AvoidanceReader, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            this.metricKeys.Add(key);
            this.metricsWeight.Add(weight);
            this.metrics.Add((cell) => func(this.sightReader, this.avoidanceReader, cell));
            this.metricsComperative.Add(comperative);
        }
        
        public void Add(string key, Func<Map, IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            this.metricKeys.Add(key);
            this.metricsWeight.Add(weight);
            this.metrics.Add((cell) => func(map, cell));
            this.metricsComperative.Add(comperative);
        }

        public void Add(string key, Func<IntVec3, float> func, float weight = 1f, bool comperative = true)
        {
            this.metricKeys.Add(key);
            this.metricsWeight.Add(weight);
            this.metrics.Add(func);
            this.metricsComperative.Add(comperative);
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
            this.sightReader     = null;
            this.avoidanceReader = null;
            this.map             = null;
            this.metricsVal.Clear();
        }
    }
}
