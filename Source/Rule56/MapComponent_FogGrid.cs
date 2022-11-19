using System;
using System.Diagnostics;
using System.Threading;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Noise;
using static UnityEngine.SpookyHash;

namespace CombatAI
{
	[StaticConstructorOnStartup]
	public class MapComponent_FogGrid : MapComponent
	{
		private const int SECTION_SIZE = 16;

		private class ISection
		{
			public float[] cells;
			public Rect rect;
			public bool dirty = true;
			public float zoom;
			private readonly Material mat;
			private readonly Mesh mesh;
			private readonly Vector3 pos;
			private readonly MapComponent_FogGrid comp;

			public void ApplyZoom()
			{
				this.zoom = MapComponent_FogGrid.zoom;
				mat.SetVector("_Color", new Vector4(0.1f, 0.1f, 0.1f, this.zoom / 120f));
			}

			public void ApplyFogged()
			{
				mat.SetFloatArray("_Fog", cells);
				dirty = false;
			}

			public void Update()
			{
				ITSignalGrid pawns = comp.sight.grid;
				ITSignalGrid turrets = comp.sightTurrets?.grid ?? null;
				IntVec3 pos = this.pos.ToIntVec3();
				IntVec3 loc;
				bool changed = false;
				if (pawns != null)
				{
					for (int x = 0; x < SECTION_SIZE; x++)
					{
						for (int z = 0; z < SECTION_SIZE; z++)
						{
							float visibility = pawns.GetSignalStrengthAt(loc = pos + new IntVec3(x, 0, z)) + (turrets?.GetSignalStrengthAt(loc) ?? 0f);
							float old = cells[x * SECTION_SIZE + z];
							float val;
							if (visibility > 0)
							{
								val =  Maths.Max((0.05f - visibility) / 0.05f, 0f);
								if (loc.InBounds(comp.map))
								{
									comp.grid[comp.cellIndices.CellToIndex(loc)] = false;
								}
							}
							else
							{
								val = Maths.Min(cells[x * SECTION_SIZE + z] + 0.1f, 1.0f);
								if (loc.InBounds(comp.map))
								{
									comp.grid[comp.cellIndices.CellToIndex(loc)] = true;
								}
							}
							if (old != val)
							{
								changed = true;
							}
							cells[x * SECTION_SIZE + z] = val;
						}
					}
				}
				dirty = changed;
			}

			public ISection(MapComponent_FogGrid comp, Rect rect, Rect mapRect, Mesh mesh, Texture2D tex, Shader shader)
			{
				this.comp = comp;
				this.rect = rect;
				this.mesh = mesh;
				pos = new Vector3(rect.position.x, 8, rect.position.y);
				mat = new Material(shader);
				mat.SetVector("_Color", new Vector4(0.1f, 0.1f, 0.1f, 0.8f));
				mat.SetTexture("_Tex", tex);
				cells = new float[256];
				cells.Initialize();
				mat.SetFloatArray("_Fog", cells);
			}

			public void Draw(Rect screenRect)
			{
				GenDraw.DrawMeshNowOrLater(mesh, pos, Quaternion.identity, mat, false);
			}
		}

		private static float zoom;
		private static readonly Texture2D fogTex;
		private static readonly Mesh mesh;

		private Rect mapScreenRect;
		private bool alive;
		private bool ready;
		private int updateNum;

		private AsyncActions asyncActions;
		private ISection[][] grid2d;
		private static Shader fogShader;
		private readonly Rect mapRect;

		public bool[] grid;
		public SightGrid sight;
		public SightGrid sightTurrets;
		public CellIndices cellIndices;

		static MapComponent_FogGrid()
		{
			fogTex = new Texture2D(SECTION_SIZE, SECTION_SIZE, TextureFormat.RGBAFloat, true);
			fogTex.Apply();
			mesh = CombatAI_MeshMaker.NewPlaneMesh(Vector2.zero, Vector2.one * SECTION_SIZE, 0);
			fogShader = AssetBundleDatabase.Get<Shader>("assets/fogshader.shader");
			Assert.IsNotNull(fogShader);
		}

		public MapComponent_FogGrid(Map map) : base(map)
		{
			this.alive = true;
			this.asyncActions = new AsyncActions();
			this.cellIndices = map.cellIndices;
			this.mapRect = new Rect(0, 0, cellIndices.mapSizeX, cellIndices.mapSizeZ);
			this.grid = new bool[map.cellIndices.NumGridCells];
			grid2d = new ISection[Mathf.CeilToInt(cellIndices.mapSizeX / (float)SECTION_SIZE)][];
			for (int i = 0; i < grid2d.Length; i++)
			{
				grid2d[i] = new ISection[Mathf.CeilToInt(cellIndices.mapSizeZ / (float)SECTION_SIZE)];
				for (int j = 0; j < grid2d[i].Length; j++)
				{
					grid2d[i][j] = new ISection(this, new Rect(new Vector2(i * SECTION_SIZE, j * SECTION_SIZE), Vector2.one * SECTION_SIZE), mapRect, mesh, fogTex, fogShader);
				}
			}
			this.asyncActions.EnqueueOffThreadAction(() =>
			{
				OffThreadLoop(0, 0, grid2d.Length, grid2d[0].Length);
			});
		}

		public bool IsFogged(IntVec3 cell) => IsFogged(cellIndices.CellToIndex(cell));
		public bool IsFogged(int index)
		{
			if (index >= 0 && index < cellIndices.NumGridCells)
			{
				return grid[index];
			}
			return false;
		}

		public override void MapComponentUpdate()
		{
			if (!alive)
			{
				return;
			}
			if (Find.CurrentMap != map)
			{
				ready = false;
				return;
			}
			if (!ready)
			{
				sight = map.GetComp_Fast<SightTracker>().colonistsAndFriendlies;
				if (map.ParentFaction.IsPlayerSafe())
				{
					sightTurrets = map.GetComp_Fast<SightTracker>().settlementTurrets;
				}
				ready = sight != null;
			}
			base.MapComponentUpdate();
			if (ready)
			{
				if (updateNum % 10 == 0 && sightTurrets == null && map.ParentFaction.IsPlayerSafe())
				{
					sightTurrets = map.GetComp_Fast<SightTracker>().settlementTurrets;
				}
				Rect rect = new Rect();
				CellRect cellRect = Find.CameraDriver.CurrentViewRect;
				rect.xMin = Mathf.Clamp(cellRect.minX - SECTION_SIZE, 0, cellIndices.mapSizeX);
				rect.xMax = Mathf.Clamp(cellRect.maxX + SECTION_SIZE, 0, cellIndices.mapSizeX);
				rect.yMin = Mathf.Clamp(cellRect.minZ - SECTION_SIZE, 0, cellIndices.mapSizeZ);
				rect.yMax = Mathf.Clamp(cellRect.maxZ + SECTION_SIZE, 0, cellIndices.mapSizeZ);
				mapScreenRect = rect;
				//mapScreenRect.ExpandedBy(32, 32);
				asyncActions.ExecuteMainThreadActions();
				zoom = Mathf.CeilToInt(Mathf.Clamp(Find.CameraDriver?.rootPos.y ?? 30, 15, 25f));
				DrawFog(Mathf.FloorToInt(mapScreenRect.xMin / SECTION_SIZE), Mathf.FloorToInt(mapScreenRect.yMin / SECTION_SIZE), Mathf.FloorToInt(mapScreenRect.xMax / SECTION_SIZE), Mathf.FloorToInt(mapScreenRect.yMax / SECTION_SIZE));
			}
		}

		public override void MapComponentOnGUI()
		{
			base.MapComponentOnGUI();
			Widgets.Label(new Rect(0, 0, 100, 25), $"{zoom} {Find.CameraDriver.rootPos}");
		}

		public override void MapRemoved()
		{
			base.MapRemoved();
			asyncActions.Kill();
			alive = false;
		}

		private void DrawFog(int minU, int minV, int maxU, int maxV)
		{
			maxU = Mathf.Clamp(Maths.Max(maxU, minU + 1), 0, grid2d.Length - 1);
			minU = Mathf.Clamp(minU - 1, 0, grid2d.Length - 1);
			maxV = Mathf.Clamp(Maths.Max(maxV, minV + 1), 0, grid2d[0].Length - 1);
			minV = Mathf.Clamp(minV - 1, 0, grid2d[0].Length - 1);
			bool update = updateNum % 4 == 0;
			bool updateForced = updateNum % 8 == 0;
			for (int u = minU; u <= maxU; u++)
			{
				for (int v = minV; v <= maxV; v++)
				{
					ISection section = grid2d[u][v];
					if (zoom != section.zoom)
					{
						section.ApplyZoom();
					}
					if (updateForced || (update && section.dirty))
					{
						section.ApplyFogged();
					}
					section.Draw(mapScreenRect);
				}
			}
			updateNum++;
		}

		private void OffThreadLoop(int minU, int minV, int maxU, int maxV)
		{
			Stopwatch stopwatch = new Stopwatch();
			while (alive)
			{
				stopwatch.Restart();
				if (ready)
				{
					for (int u = minU; u < maxU; u++)
					{
						for (int v = minV; v < maxV; v++)
						{
							grid2d[u][v].Update();
						}
					}
				}
				stopwatch.Stop();
				float t = 0.021f - (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;
				if (t > 0f)
				{
					Thread.Sleep(Mathf.CeilToInt(t * 1000));
				}
			}
		}
	}
}

