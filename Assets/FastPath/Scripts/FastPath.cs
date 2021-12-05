using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;
using Vector = UnityEngine.Vector3;

namespace FastPath
{
	/*
	 * Replace Exceptions with BringInBounds whereever possible
	 */

	public static class FastPath
	{
		#region Static Members

		#region Defaults

		private static Map defaultMap;
		private static float defaultAgression = 1f;
		private static bool defaultDiognalMovement = true;

		private static float defaultDepthCostMultiplier = 0f;
		private static float defaultMaxDepthDiff = float.PositiveInfinity;

		#endregion

		#endregion

		#region Static Properties

		public static bool PathfinderBusy
		{
			get
			{
				return Pathfinder.IsBusy;
			}
		}

		public static ThreadPriority PathfinderPriority
		{
			get
			{
				return Pathfinder.PathfinderThreadPriority;	
			}
			set
			{
				Pathfinder.PathfinderThreadPriority = value;
			}
		}

		public static int QueLength
		{
			get
			{
				return Pathfinder.QueLength;
			}
		}

		#region Defaults

		public static Map DefaultMap
		{
			get
			{
				return defaultMap;
			}
			set
			{
				defaultMap = value;
			}
		}

		public static float DefaultDepthCostMultiplier
		{
			get
			{
				return defaultDepthCostMultiplier;
			}
			set
			{
				defaultDepthCostMultiplier = value;
			}
		}

		public static float DefaultMaxDepthDifference
		{
			get
			{
				return defaultMaxDepthDiff;
			}
			set
			{
				defaultMaxDepthDiff = value;
			}
		}

		public static float DefaultEstimateAggression
		{
			get
			{
				return defaultAgression;
			}
			set
			{
				defaultAgression = value;
			}
		}

		public static bool DefaultMoveDiognal
		{
			get
			{
				return defaultDiognalMovement;
			}
			set
			{
				defaultDiognalMovement = value;
			}
		}

		#endregion

		#endregion

		#region Static Methods

		public static bool IsMapBusy(Map map)
		{
			return map.IsBusy;
		}

		public static Map Generate(Generator.Config config)
		{
			return config.Build();
		}

		private static void PerformOnTree(GameObject gameObject, System.Action<GameObject> action)
		{
			int childCount = gameObject.transform.childCount;

			for(int i = 0; i < childCount; i++)
				PerformOnTree(gameObject.transform.GetChild(i).gameObject, action);

			action(gameObject);
		}

		#region Indexes Between

		public static Int2D[] IndexBetween(Map map, GameObject obj)
		{
			bool ThreeD = map.GetConfig().Use3DPhysics;

			Vector3 min = new Vector(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			Vector3 max = new Vector(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

			PerformOnTree(obj, (gameObj) =>
			{
				Bounds? bounds = null;

				if(ThreeD)
				{
					Collider col = gameObj.GetComponent<Collider>();

					if(col)
						bounds = col.bounds;
				}
				else
				{
					Collider2D col = gameObj.GetComponent<Collider2D>();

					if(col)
						bounds = col.bounds;
				}

				if(bounds.HasValue)
				{
					for(int i = 0; i < 2; i++)
					{
						if(bounds.Value.min[i] < min[i])
							min[i] = bounds.Value.min[i];
						if(bounds.Value.max[i] > max[i])
							max[i] = bounds.Value.max[i];
					}
				}
			});

			return IndexesBetween(map, min, max);
		}

		public static Int2D[] IndexesBetween(Map map, Vector start, Vector end)
		{
			return IndexesBetween(map, map.PositionToIndexCeil(start), map.PositionToIndexFloor(end));
		}

		public static Int2D[] IndexesBetween(Map map, Int2D start, Int2D end) // Start and end both are inclusive
		{
			if(start.x > end.x)
			{
				int StartX = start.x;

				start.x = end.x;
				end.x = StartX;
			}
			if(start.y > end.y)
			{
				int StartY = start.y;

				start.y = end.y;
				end.y = StartY;
			}

			start = BringInBounds(map, start);
			end = BringInBounds(map, end);

			Int2D[] collectedNodes = new Int2D[((end.x - start.x) + 1) * ((end.y - start.y) + 1)];
			int current = 0;

			for(int i = start.x; i <= end.x; i++)
				for(int j = start.y; j <= end.y; j++)
					collectedNodes[current++] = new Int2D(i, j);

			return collectedNodes;
		}

		#endregion

		#region Drawing

		public static void DrawMapInEditor(Map map)
		{
			map.DrawMapInEditor();
		}

		public static void DrawPathInEditor(Path path)
		{
			path.DrawPath();
		}

		#endregion

		#region To Index

		public static Int2D PositionToIndexFloor(Map map, Vector position)
		{
			return map.PositionToIndexFloor(position);
		}

		public static Int2D PositionToIndexCeil(Map map, Vector position)
		{
			return map.PositionToIndexCeil(position);	
		}

		#endregion

		#region Bounds

		public static Vector BringInBounds(Map map, Vector position)
		{
			return map.BringInBounds(position);
		}

		public static Int2D BringInBounds(Map map, Int2D index)
		{
			return map.BringInBounds(index);
		}

		public static Bounds BringInBounds(Map map, Bounds bounds)
		{
			return new Bounds()
			{
				min = map.BringInBounds(bounds.min),
				max = map.BringInBounds(bounds.max)
			};
		}

		#endregion

		#region Updating Map

		public static void Update(Map map, Int2D[] indexes)
		{
			Generator.UpdateNodesRuntime(map, indexes);
		}

		public static void Update(Map map, Bounds bounds)
		{
			Update(map, bounds.min, bounds.max);
		}

		public static void Update(Map map)
		{
			Generator.UpdateMapRuntime(map);
		}

		public static void Update(Map map, Vector position)
		{
			Update(map, map.PositionToIndexFloor(position));
		}

		public static void Update(Map map, Int2D index)
		{
			index = map.BringInBounds(index);
			
			Generator.UpdateNodeRuntime(map, index, map.GetConfig());
		}

		public static void Update(Map map, Vector start, Vector end)
		{
			Generator.UpdateNodesRuntime(map, map.PositionToIndexFloor(start), map.PositionToIndexCeil(end));
		}

		public static void Update(Map map, Int2D start, Int2D end)
		{
			start = map.BringInBounds(start);
			end = map.BringInBounds(end);
			Generator.UpdateNodesRuntime(map, start, end);
		}
		
		#endregion

		#region Pathfinding

		public static void FindPathImmediate(Path path)
		{
			path.ForceBuild();
		}

		public static Path FindPathImmediate(Map map, Vector start, Vector end, float aggression, bool moveDiognal, float maxDepthDiff, float depthDiffCostMultiplier, Int2D[] disallowedIndexes)
		{
			return Path.BuildImmediate(map, start, end, aggression, moveDiognal, maxDepthDiff, depthDiffCostMultiplier, disallowedIndexes);
		}

		public static Path FindPathImmediate(Map map, Vector start, Vector end, float aggression, bool moveDiognal, Int2D[] disallowedIndexes)
		{
			return Path.BuildImmediate(map, start, end, aggression, moveDiognal, disallowedIndexes);
		}

		public static Path FindPathImmediate(Vector start, Vector end)
		{
			return Path.BuildImmediate(start, end);
		}

		public static Path FindPathImmediate(Vector start, Vector end, Int2D[] disallowedIndexes)
		{
			return Path.BuildImmediate(start, end, disallowedIndexes);
		}

		public static Path FindPath(Map map, Vector start, Vector end, float aggression, bool moveDiognal, float maxDepthDiff, float depthDiffMultiplier, Int2D[] disallowedIndexes)
		{
			return new Path(map, start, end, aggression, moveDiognal, maxDepthDiff, depthDiffMultiplier, disallowedIndexes);
		}

		public static Path FindPath(Map map, Vector start, Vector end, float aggression, bool moveDiognal, Int2D[] disallowedIndexes)
		{
			return new Path(map, start, end, aggression, moveDiognal, disallowedIndexes);
		}

		public static Path FindPath(Vector start, Vector end)
		{
			return new Path(start, end);
		}

		public static Path FindPath(Vector start, Vector end, Int2D[] disallowedIndexes)
		{
			return new Path(start, end, disallowedIndexes);
		}

		#endregion

		#endregion
	}
}