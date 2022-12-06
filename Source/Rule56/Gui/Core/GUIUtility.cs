using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatAI.Gui
{
	public static partial class GUIUtility
	{
		/// <summary>
		/// Execute the provided function safely by saving and restoring GUI states regardless of whether the function exited normally or not.
		/// This allows for a safe stack like behavior where GUI states like color, font and wordwarp are always restored to their original state. 
		/// </summary>
		/// <param name="function">Function to be executed safely</param>
		/// <param name="fallbackAction">Fallback function</param>
		/// <param name="catchExceptions">Whether to catch (not rethrow) any exceptions</param>
		/// <returns></returns>
		public static Exception ExecuteSafeGUIAction(Action function, Action fallbackAction = null,
			bool catchExceptions = false)
		{
			StashGUIState();
			Exception exception = null;
			try
			{
				function.Invoke();
			}
			catch (Exception er)
			{
				Log.Error($"RocketEngine.Gui:UI error in ExecuteSafeGUIAction {er}");
				exception = er;
			}
			finally
			{
				RestoreGUIState();
			}

			if (exception != null && !catchExceptions)
			{
				if (fallbackAction != null)
					exception = ExecuteSafeGUIAction(
						fallbackAction,
						catchExceptions: false);
				if (exception != null)
					throw exception;
			}

			return exception;
		}

		private static bool _scrolling = false;
		private static readonly Color _altGray = new Color(0.2f, 0.2f, 0.2f);
		private static float[] _heights = new float[5000];

		public static void ScrollView<T>(Rect rect, ref Vector2 scrollPosition, IEnumerable<T> elements,
			Func<T, float> heightLambda, Action<Rect, T> elementLambda, Func<T, IComparable> orderByLambda = null,
			bool drawBackground = true, bool showScrollbars = true, bool catchExceptions = false,
			bool drawMouseOverHighlights = true)
		{
			var prevScrolling = _scrolling;
			_scrolling = true;
			StashGUIState();
			Exception exception = null;
			try
			{
				if (prevScrolling) throw new InvalidOperationException("Nested scroll views aren't allowed.");
				if (drawBackground)
				{
					Widgets.DrawMenuSection(rect);
					rect = rect.ContractedBy(2);
				}

				var contentRect = new Rect(0, 0, showScrollbars ? rect.width - 23 : rect.width, 0);
				var elementsInt = orderByLambda == null ? elements : elements.OrderBy(orderByLambda);
				if (_heights.Length < elementsInt.Count())
					_heights = new float[elementsInt.Count() * 2];
				float h;
				var w = showScrollbars ? rect.width - 16 : rect.width;
				var j = 0;
				var k = 0;
				var inView = true;
				foreach (var element in elementsInt)
				{
					h = heightLambda.Invoke(element);
					_heights[j++] = h;
					contentRect.height += Maths.Max(h, 0f);
				}

				j = 0;
				Widgets.BeginScrollView(rect, ref scrollPosition, contentRect, showScrollbars);
				var currentRect = new Rect(1, 0, w, 0);
				foreach (var element in elementsInt)
				{
					if (_heights[j] <= 0.00f)
					{
						j++;
						continue;
					}

					currentRect.height = _heights[j];
					if (false
					    || scrollPosition.y - 50 > currentRect.yMax
					    || scrollPosition.y + 50 + rect.height < currentRect.yMin)
						inView = false;
					if (inView)
					{
						if (drawBackground && k % 2 == 0)
							Widgets.DrawBoxSolid(currentRect, _altGray);
						if (drawMouseOverHighlights)
							Widgets.DrawHighlightIfMouseover(currentRect);

						GUIFont.Anchor = TextAnchor.MiddleLeft;
						GUIFont.Font = GUIFontSize.Tiny;
						elementLambda.Invoke(currentRect, element);
					}

					currentRect.y += _heights[j];
					k++;
					j++;
					inView = true;
				}
			}
			catch (Exception er)
			{
				Log.Error($"RocketEngine.Gui:UI error in ScrollView {er}");
				exception = er;
			}
			finally
			{
				_scrolling = prevScrolling;
				RestoreGUIState();
				Widgets.EndScrollView();
			}

			if (exception != null && !catchExceptions)
				throw exception;
		}

		private static readonly Dictionary<string, Vector2> _scrollPositions = new Dictionary<string, Vector2>();

		public static void ScrollView<T>(Rect rect, string id, IEnumerable<T> elements, Func<T, float> heightLambda,
			Action<Rect, T> elementLambda, Func<T, IComparable> orderByLambda = null, bool drawBackground = true,
			bool showScrollbars = true, bool catchExceptions = false, bool drawMouseOverHighlights = true)
		{
			if (!_scrollPositions.TryGetValue(id, out var scrollPosition)) scrollPosition = Vector2.zero;
			Exception exception = null;
			try
			{
				ScrollView(rect, ref scrollPosition, elements, heightLambda, elementLambda, orderByLambda,
					drawBackground, showScrollbars, catchExceptions, drawMouseOverHighlights);
			}
			catch (Exception er)
			{
				exception = er;
			}
			finally
			{
				_scrollPositions[id] = scrollPosition;
			}

			if (exception != null && !catchExceptions)
				throw exception;
		}


		public static void GridView<T>(Rect rect, int columns, List<T> elements, Action<Rect, T> cellLambda,
			bool drawBackground = true, bool drawVerticalDivider = false)
		{
			if (drawBackground) Widgets.DrawMenuSection(rect);
			rect = rect.ContractedBy(1);
			var rows = (int)Math.Ceiling((decimal)elements.Count / columns);
			var columnStep = rect.width / columns;
			var rowStep = rect.height / rows;
			var curRect = new Rect(0, 0, columnStep, rowStep);
			var k = 0;
			for (var i = 0; i < columns && k < elements.Count; i++)
			{
				curRect.x = i * columnStep + rect.x;
				for (var j = 0; j < rows && k < elements.Count; j++)
				{
					curRect.y = j * rowStep + rect.y;
					cellLambda(curRect, elements[k++]);
				}
			}
		}

		public static void DropDownMenu<T>(Func<T, string> labelLambda, Action<T> selectedLambda, T[] options)
		{
			DropDownMenu(labelLambda, selectedLambda, options.AsEnumerable());
		}

		public static void DropDownMenu<T>(Func<T, string> labelLambda, Action<T> selectedLambda,
			IEnumerable<T> options)
		{
			var useCustomFonts = GUIFont.UseCustomFonts;
			GUIFont.UseCustomFonts = false;
			var font = Text.Font;
			try
			{
				Text.Font = GameFont.Small;
				FloatMenuUtility.MakeMenu(options,
					(option) => { return labelLambda(option); },
					(option) => { return () => selectedLambda(option); }
				);
			}
			finally
			{
				Text.Font = font;
				GUIFont.UseCustomFonts = useCustomFonts;
			}
		}

		public static void Row(Rect rect, List<Action<Rect>> contentLambdas, bool drawDivider = true,
			bool drawBackground = false)
		{
			if (drawBackground) Widgets.DrawMenuSection(rect);
			var step = rect.width / contentLambdas.Count;
			var curRect = new Rect(rect.x - 5, rect.y, step - 10, rect.height);
			for (var i = 0; i < contentLambdas.Count; i++)
			{
				var lambda = contentLambdas[i];
				if (drawDivider && i + 1 < contentLambdas.Count)
				{
					var start = new Vector2(curRect.xMax + 5, curRect.yMin + 1);
					var end = new Vector2(curRect.xMax + 5, curRect.yMax - 1);
					Widgets.DrawLine(start, end, Color.white, 1);
				}

				ExecuteSafeGUIAction(() =>
				{
					lambda.Invoke(curRect);
					curRect.x += step;
				});
			}
		}

		public static void CheckBoxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false,
			bool monotone = false, float iconWidth = 20, GUIFontSize font = GUIFontSize.Tiny,
			FontStyle fontStyle = FontStyle.Normal, bool placeCheckboxNearText = false,
			bool drawHighlightIfMouseover = true, Texture2D texChecked = null, Texture2D texUnchecked = null)
		{
			var checkOnInt = checkOn;
			GUIFont.Font = font;
			GUIFont.Anchor = TextAnchor.MiddleLeft;
			GUIFont.CurFontStyle.fontStyle = fontStyle;
			if (placeCheckboxNearText) rect.width = Maths.Min(rect.width, GUIFont.CalcSize(label).x + 24f + 10f);
			Widgets.Label(rect, label);
			if (!disabled && Widgets.ButtonInvisible(rect))
			{
				checkOnInt = !checkOnInt;
				if (checkOnInt)
					SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
				else
					SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
			}

			var iconRect = new Rect(0f, 0f, iconWidth, iconWidth);
			iconRect.center = rect.RightPartPixels(iconWidth).center;
			var color = GUI.color;
			if (disabled || monotone) GUI.color = Widgets.InactiveColor;
			GUI.DrawTexture(
				image: checkOnInt ? texChecked != null ? texChecked : Widgets.CheckboxOnTex :
				texUnchecked != null ? texUnchecked : Widgets.CheckboxOffTex, position: iconRect);
			if (disabled || monotone) GUI.color = color;
			if (drawHighlightIfMouseover) Widgets.DrawHighlightIfMouseover(rect);
			checkOn = checkOnInt;
		}

		public static void ColorBoxDescription(Rect rect, Color color, string description)
		{
			var textRect = new Rect(rect.x + 30, rect.y, rect.width - 30, rect.height);
			var boxRect = new Rect(0, 0, 10, 10);
			boxRect.center = new Vector2(rect.xMin + 15, rect.yMin + rect.height / 2);
			ExecuteSafeGUIAction(() =>
			{
				GUIFont.Anchor = TextAnchor.MiddleLeft;
				GUIFont.Font = GUIFontSize.Tiny;
				GUIFont.CurFontStyle.fontStyle = FontStyle.Normal;
				Widgets.DrawBoxSolid(boxRect, color);
				Widgets.Label(textRect, description);
			});
		}
	}
}