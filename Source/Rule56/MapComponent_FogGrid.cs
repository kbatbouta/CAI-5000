using System;
using System.Diagnostics;
using System.Reflection;
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
			public float s_color;			
			private readonly Material mat;
			private readonly Mesh mesh;
			private readonly Vector3 pos;
			private readonly MapComponent_FogGrid comp;

			public void ApplyColor()
			{				
				mat.SetVector("_Color", new Vector4(0.1f, 0.1f, 0.1f, this.s_color = Finder.Settings.FogOfWar_FogColor));
			}

			public void ApplyFogged()
			{
				mat.SetFloatArray("_Fog", cells);
				dirty = false;
			}

			public void Update()
			{
				CellIndices indices = comp.map?.cellIndices;
				if (indices == null)
				{
					return;
				}				
				int numGridCells = indices.NumGridCells;		
				WallGrid walls = comp.walls;
				ITFloatGrid fogGrid = comp.sight.gridFog;				
				IntVec3 pos = this.pos.ToIntVec3();
				IntVec3 loc;
			
				ColorInt[] glowGrid = comp.glow.glowGrid;				
				float glowSky = comp.SkyGlow;
				bool changed = false;
				if (fogGrid != null)
				{
					for (int x = 0; x < SECTION_SIZE; x++)
					{
						for (int z = 0; z < SECTION_SIZE; z++)
						{																					
							int index = indices.CellToIndex(loc = pos + new IntVec3(x, 0, z));
							if (index >= 0 && index < numGridCells)
							{
								float val;
								float old = cells[x * SECTION_SIZE + z];								
								if (!walls.CanBeSeenOver(index))
								{
									val = 0.5f;
									comp.grid[index] = false;
								}
								else
								{									
									float visRLimit = Mathf.Lerp(0, 0.15f, 1 - glowSky);
									float visibility = fogGrid.Get(index);									
									if (glowSky < 1)
									{
										ColorInt glow = glowGrid[index];										
										visRLimit = Maths.Max(Mathf.Lerp(0, 0.15f, 1 - Maths.Min(Maths.Max(glow.r, glow.g, glow.b) / 255f * 3.6f, 1.0f)), visRLimit);
									}
									if (visibility < visRLimit)
									{
										visibility = 0f;
									}
									if (visibility > 0)
									{
										val = Maths.Max(cells[x * SECTION_SIZE + z] - 0.3f, 1 - visibility, 0f);
										comp.grid[index] = false;
									}
									else
									{
										val = Maths.Min(cells[x * SECTION_SIZE + z] + 0.3f, 1.0f);
										comp.grid[index] = true;
									}
								}
								if (old != val)
								{
									changed = true;
								}
								cells[x * SECTION_SIZE + z] = val;
							}
							else
							{
								cells[x * SECTION_SIZE + z] = 0f;
							}
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
		private float glowSky;

		private AsyncActions asyncActions;
		private ISection[][] grid2d;
		private static Shader fogShader;
		private readonly Rect mapRect;

		public bool[] grid;
		public SightGrid sight;
		public GlowGrid glow;
		public WallGrid walls;
		public CellIndices cellIndices;

		public float SkyGlow
		{
			get => glowSky;
		}
		

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

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			asyncActions.Start();
		}

		public bool IsFogged(IntVec3 cell) => IsFogged(cellIndices.CellToIndex(cell));
		public bool IsFogged(int index)
		{
			if (!Finder.Settings.FogOfWar_Enabled)
			{
				return false;
			}
			if (index >= 0 && index < cellIndices.NumGridCells)
			{
				return grid[index];
			}
			return false;
		}

		public override void MapComponentUpdate()
		{
			if (!alive || !Finder.Settings.FogOfWar_Enabled)
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
				walls = map.GetComp_Fast<WallGrid>();
				glow = map.glowGrid;
				ready = sight != null;				
			}
			base.MapComponentUpdate();
			if (ready)
			{
				glowSky = map.skyManager.CurSkyGlow;
				Rect rect = new Rect();
				CellRect cellRect = Find.CameraDriver.CurrentViewRect;
				rect.xMin = Mathf.Clamp(cellRect.minX - SECTION_SIZE, 0, cellIndices.mapSizeX);
				rect.xMax = Mathf.Clamp(cellRect.maxX + SECTION_SIZE, 0, cellIndices.mapSizeX);
				rect.yMin = Mathf.Clamp(cellRect.minZ - SECTION_SIZE, 0, cellIndices.mapSizeZ);
				rect.yMax = Mathf.Clamp(cellRect.maxZ + SECTION_SIZE, 0, cellIndices.mapSizeZ);
				mapScreenRect = rect;
				//mapScreenRect.ExpandedBy(32, 32);
				asyncActions.ExecuteMainThreadActions();
				zoom = Mathf.CeilToInt(Mathf.Clamp(Find.CameraDriver?.rootPos.y ?? 30, 15, 30f));
				DrawFog(Mathf.FloorToInt(mapScreenRect.xMin / SECTION_SIZE), Mathf.FloorToInt(mapScreenRect.yMin / SECTION_SIZE), Mathf.FloorToInt(mapScreenRect.xMax / SECTION_SIZE), Mathf.FloorToInt(mapScreenRect.yMax / SECTION_SIZE));
			}
		}

		//public override void MapComponentOnGUI()
		//{
		//	base.MapComponentOnGUI();
		//	//
		//	//Widgets.Label(new Rect(0, 0, 100, 25), $"{zoom} {Find.CameraDriver.rootPos}");
		//}

		public override void MapRemoved()
		{
			alive = false;
			asyncActions.Kill();
			base.MapRemoved();						
		}

		private void DrawFog(int minU, int minV, int maxU, int maxV)
		{			
			maxU = Mathf.Clamp(Maths.Max(maxU, minU + 1), 0, grid2d.Length - 1);
			minU = Mathf.Clamp(minU - 1, 0, grid2d.Length - 1);
			maxV = Mathf.Clamp(Maths.Max(maxV, minV + 1), 0, grid2d[0].Length - 1);
			minV = Mathf.Clamp(minV - 1, 0, grid2d[0].Length - 1);
			bool update = updateNum % 4 == 0;
			bool updateForced = updateNum % 8 == 0;
			float color = Finder.Settings.FogOfWar_FogColor;
			for (int u = minU; u <= maxU; u++)
			{
				for (int v = minV; v <= maxV; v++)
				{
					ISection section = grid2d[u][v];
					if (section.s_color != color)
					{
						section.ApplyColor();
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
				if (ready && Finder.Settings.FogOfWar_Enabled)
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

