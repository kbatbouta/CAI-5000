using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace CombatAI.Gui
{
	public class Grapher
	{
		public const int GraphMaxPointsNum = 1500;

		public const int Scales = 4;

		public struct GraphPoint
		{
			public float t;

			public float y;

			public Color color;

			public GraphPoint(float t, float y, Color color)
			{
				this.t = t;
				this.y = y;
				this.color = color;
			}
		}

		private List<GraphPoint> pointsQueue = new List<GraphPoint>();

		private GraphPointCollection points = new GraphPointCollection();

		private Listing_Collapsible collapsible = new Listing_Collapsible(scrollViewOnOverflow: false);

		private GraphPoint mouseIsOverPoint = new GraphPoint(0, 0, Color.white);

		private bool mouseIsOver = false;

		private List<Action<Rect>> header;

		public string description = string.Empty;

		public string title = string.Empty;

		private IEnumerable<GraphPoint> Range => points.Points;

		private float RangeT => points.RangeT;

		private float RangeY => points.RangeY;

		public float MinY => points.MinY;

		public float MaxY => points.MaxY;

		public float MinT => points.MinT;

		public float MaxT => points.MaxT;

		public float TimeWindowSize
		{
			get => points.TargetTimeWindowSize;
			set => points.TargetTimeWindowSize = value;
		}

		public Listing_Collapsible.Group_Collapsible Group
		{
			get => collapsible.Group;
			set => collapsible.Group = value;
		}

		public Grapher(string title, string description = null)
		{
			this.title = title;
			this.description = description ?? string.Empty;
			header = new List<Action<Rect>>()
			{
				(rect) =>
				{
					GUIFont.Font = GUIFontSize.Tiny;
					GUIFont.Anchor = TextAnchor.MiddleLeft;
					rect.xMin += 25;
					Widgets.Label(rect, $"Min T:<color=cyan>{Math.Round(MinT, 4)}</color>");
				},
				(rect) =>
				{
					if (mouseIsOver)
					{
						GUIFont.Font = GUIFontSize.Tiny;
						GUIFont.Anchor = TextAnchor.MiddleCenter;
						Widgets.Label(rect,
							$"Current:(<color=cyan>{Math.Round(mouseIsOverPoint.t, 4)}</color>,<color=cyan>{Math.Round(mouseIsOverPoint.y, 4)}</color>)");
					}
				},
				(rect) =>
				{
					GUIFont.Font = GUIFontSize.Tiny;
					GUIFont.Anchor = TextAnchor.MiddleRight;
					Widgets.Label(rect, $"Max T:<color=cyan>{Math.Round(MinT + RangeT, 4)}</color>");
				}
			};
		}

		public float this[float t]
		{
			set => Add(t, value);
		}

		public void Add(float t, float y)
		{
			Add(t, y, Color.cyan);
		}

		public void Add(float t, float y, Color color)
		{
			var point = new GraphPoint();
			point.t = t;
			point.y = y;
			point.color = color;
			pointsQueue.Add(point);
		}

		public void Dirty()
		{
			points.Rebuild();
		}

		public void Plot(ref Rect inRect)
		{
			if (pointsQueue.Count > 0 && collapsible.Expanded)
			{
				foreach (var point in pointsQueue) points.Add(point);
				points.Rebuild();
				pointsQueue.Clear();
			}

			collapsible.Begin(inRect, title);
			if (points.Ready && points.Count > 24)
			{
				GUI.color = Color.white;
				collapsible.Columns(15, header);
				collapsible.Line(1);
				collapsible.Lambda(100, Draw);

				if (!description.NullOrEmpty())
				{
					collapsible.Line(1);
					collapsible.Label(description);
				}
			}

			collapsible.End(ref inRect);
		}

		private void Draw(Rect rect)
		{
			Widgets.DrawBoxSolid(rect, Color.black);
			if (!points.Ready)
			{
				GUIFont.Font = GUIFontSize.Small;
				GUIFont.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect, "Preparing");
				return;
			}

			GUI.color = Color.white;
			GUIFont.Font = GUIFontSize.Tiny;
			GUIFont.Anchor = TextAnchor.MiddleLeft;

			rect = rect.ContractedBy(5);
			var width = rect.width;
			var height = rect.height;

			var textRect = new Rect(Vector2.zero, GUIFont.CalcSize("0.00000"));
			var textOffset = textRect.width + 5;
			var x0 = rect.xMin;
			var x1 = rect.xMax;
			for (var i = 0; i <= 5; i++)
			{
				var y = height * i / 5;
				textRect.x = x0;
				textRect.y = rect.yMax - y - textRect.height / 2;
				Widgets.DrawLine(new Vector2(x0 + 2 + textOffset, rect.yMax - y), new Vector2(x1 - 2, rect.yMax - y),
					Color.gray, 1);
				Widgets.Label(textRect, $"{Math.Round(MinY + RangeY * (i / 5f), 3)}");
			}

			width -= textOffset;
			rect.xMin += textOffset;

			mouseIsOver = false;

			var v0 = new Vector2();
			var v1 = new Vector2();

			v0.x = rect.xMin;
			v0.y = rect.yMax - (points.First.y - MinY) / RangeY * height;

			var hoverRect = new Rect(v0.x, rect.y + 2, 0, rect.height - 2);

			foreach (var p in Range)
			{
				v1.x = rect.xMin + (p.t - MinT) / RangeT * width;
				v1.y = rect.yMax - (p.y - MinY) / RangeY * height;

				hoverRect.xMin = v0.x;
				hoverRect.xMax = v1.x;
				Widgets.DrawLine(v0, v1, p.color, 1);

				if (Mouse.IsOver(hoverRect))
				{
					Widgets.DrawBoxSolid(hoverRect.RightPartPixels(1), Color.gray);
					mouseIsOverPoint = p;
					mouseIsOver = true;
				}

				v0 = v1;
			}
		}
	}
}