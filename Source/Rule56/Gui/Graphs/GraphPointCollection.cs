using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CombatAI.Gui.Grapher;

namespace CombatAI.Gui
{
    public class GraphPointCollection
    {

        private readonly List<GraphPoint> points = new List<GraphPoint>();

        private int _maxAge;

        private int _minAge;

        private int   _streak;
        private float timeWindow = 250;

        public bool Ready
        {
            get => Count > 16;
        }

        public int Count
        {
            get => points.Count;
        }

        public float TargetTimeWindowSize
        {
            get => timeWindow;
            set
            {
                if (timeWindow != value)
                {
                    timeWindow = value;
                    Rebuild();
                }
            }
        }

        public float MinY
        {
            get;
            private set;
        } = float.MaxValue;

        public float MaxY
        {
            get;
            private set;
        } = float.MinValue;

        public float MinT
        {
            get => First.t;
        }

        public float MaxT
        {
            get => Last.t;
        }

        public float RangeT
        {
            get => Maths.Min(Last.t - First.t, timeWindow);
        }

        public float RangeY
        {
            get => MaxY - MinY;
        }

        public GraphPoint First
        {
            get => points.First();
        }

        public GraphPoint Last
        {
            get => points.Last();
        }

        public IEnumerable<GraphPoint> Points
        {
            get => points;
        }

        public void Add(GraphPoint point)
        {
            if (Count < 16)
            {
                Commit(point);
                return;
            }
            if (points.Count >= 1500)
            {
                points.RemoveAt(0);
            }
            if (Last.t == point.t)
            {
                point.y += Last.y;
                if (point.y > MaxY)
                {
                    _maxAge = Maths.Min(15, points.Count);
                    MaxY    = point.y;
                }
                if (point.y < MinY)
                {
                    _minAge = Maths.Min(15, points.Count);
                    MinY    = point.y;
                }
                points[points.Count - 1] = point;
                return;
            }

            GraphPoint pNm1 = Last;
            GraphPoint pNm2 = points[points.Count - 2];

            if (pNm1.t == pNm2.t)
            {
                Commit(point);
                return;
            }
            float m1 = (pNm1.y - pNm2.y) / (pNm1.t - pNm2.t);
            float m0 = (point.y - pNm1.y) / (point.t - pNm1.t);

            if (Mathf.Abs(m1 - m0) < 1e-3)
            {
                if (_streak++ > 1 && point.color == pNm1.color)
                {
                    points[points.Count - 1] = point;
                    return;
                }
                Commit(point);
                return;
            }
            _streak = 0;

            Commit(point);
        }

        public void Rebuild()
        {
            if (Count < 3)
            {
                return;
            }

            int position = 0;

            while (position < points.Count - 3 && Last.t - points[position].t > timeWindow)
                position++;

            if (position > 0 && position < points.Count)
            {
                GraphPoint p0 = points[position - 1];
                GraphPoint p1 = points[position];

                if (p0.t != p1.t)
                {
                    float t1 = Last.t - timeWindow;
                    float m  = (p1.y - p0.y) / (p1.t - p0.t);

                    p0.y = m * (t1 - p0.t) + p0.y;
                    p0.t = t1;

                    points[position - 1] = p0;
                }
                position -= 2;

                while (position >= 0)
                {
                    points.RemoveAt(position);
                    position--;
                }
            }

            if (_maxAge > 0 && _minAge > 0)
            {
                _maxAge = Maths.Max(_maxAge - 1, 0);
                _minAge = Maths.Max(_minAge - 1, 0);
            }
            else
            {
                UpdateCriticalPoints();
            }
        }

        private void Commit(GraphPoint point)
        {
            points.Add(point);
            if (point.y > MaxY)
            {
                _maxAge = Maths.Min(15, points.Count);
                MaxY    = point.y;
            }
            if (point.y < MinY)
            {
                _minAge = Maths.Min(15, points.Count);
                MinY    = point.y;
            }
        }

        private void UpdateCriticalPoints()
        {
            GraphPoint last = Last;

            MinY = last.y;
            MaxY = last.y;

            for (int i = 0; i < Count; i++)
            {
                GraphPoint point = points[i];
                if (MinY > point.y)
                {
                    _minAge = Maths.Min(i, 15);
                    MinY    = point.y;
                }
                if (MaxY < point.y)
                {
                    _maxAge = Maths.Min(i, 15);
                    MaxY    = point.y;
                }
            }
        }
    }
}
