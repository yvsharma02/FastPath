using UnityEngine;

using Vector = UnityEngine.Vector3;

namespace FastPath
{
	public static class Generator
	{
		/*
		 * The cost of a tile = cost of its layer * cost of its tag.
		 * 
		 * *REMOVED* Ignoring is treated as nothing (empty)
		 * 
		 * Non Walkable > Walkable > *REMOVED* Ignored.
		 * 
		 * Layer > Tag (as it is usually cheaper to test)
		 */

		#region Inner Classes

		#region Config

		/// <summary>
		/// The parameters on the bases on which the map will be generated.
		/// </summary>
		[System.Serializable]
		public class Config
		{
			#region Inner Classes

			#region Cost Layer

			[System.Serializable]
			public class CostLayer
			{
				#region Members

				[Range(0, 31)]
				public int LayerNo;
				public float Cost;
			
				#endregion

				#region Conversions

				public static implicit operator int(CostLayer costLayer)
				{
					return costLayer != null ? costLayer.LayerNo : -1;
				}

				public static implicit operator float(CostLayer costLayer)
				{
					return costLayer != null ? costLayer.Cost : 1f;
				}

				#endregion
			}

			#endregion

			#region Cost Tag

			[System.Serializable]
			public class CostTag
			{
				#region Members

				public string Tag;
				public float Cost;

				#endregion

				#region Conversions

				public static implicit operator string(CostTag tag)
				{
					return tag != null ? tag.Tag : "";
				}

				public static implicit operator float(CostTag tag)
				{
					return tag != null ? tag.Cost : 0f;
				}

				#endregion

			}

			#endregion

			#region Cost Object

			[System.Serializable]
			public class CostObject
			{
				#region Members

				public GameObject gameObject;
				public float Cost;

				#endregion

				#region Conversions

				public static implicit operator GameObject(CostObject obj)
				{
					return obj != null ? obj.gameObject : null;
				}

				public static implicit operator float(CostObject obj)
				{
					return obj != null ? obj.Cost : 0f;
				}

				#endregion
			}

			#endregion

			#endregion

			#region Constants (Readonlies)

			public readonly bool EmptySpaceWalkable = false;
			public readonly float EmptySpaceMoveCost = 0f;

			#endregion

			#region Members

			private bool verified = false;

			#endregion

			#region Inspector

			[Header("Map Shape")]
			public Vector2 TileSize;
			public Vector2 Start;
			public Vector2 End;

			[Header("Generation")]
			public CostTag[] WalkableTags;
			public CostLayer[] WalkableLayers;
			public string[] NonWalkableTags;
			public LayerMask NonWalkableLayers;
			public bool SingleCast; // Only checks the collider at highest depth
			// If this is false then both layers and tags will be considered else any one of them will be considered as match
			public bool MatchAny;
			// If this is true a larger area (euqal to Tilesize / 2 on both left and right) will be checked to mark the Node walkable rather than just a point
			public bool CheckLargerArea;
			// The layers and tags that do not occour in the above field will be ignored.

			[Header("Depth")] // Z if !Use3DPhysics or XYGrid else Y
			public bool IgnoreDepth; // If this is true then for 3D physics the Depth of path (Y if !XYGrid otherwise X) will be DefaultDepth otherwise it will be the depth of the gameObject
			public float MinDepth;
			public float MaxDepth;
			public float DefaultDepth;

			[Header("3D Settings")]
			public bool Use3DPhysics;
			public bool XYGrid;

			[Header("Forced")]
			public CostObject[] ForceWalkable;
			public GameObject[] ForceNonWalkable;
//			public GameObject[] ForceIgnore;

			#endregion

			#region Properties

			public bool Verified
			{
				get
				{
					return verified;
				}
			}

			public int TilesX
			{
				get
				{
					return (int) ((End.x - Start.x) / TileSize.x);
				}
			}

			public int TilesY
			{
				get
				{
					return (int) ((End.y - Start.y) / TileSize.y);
				}
			}

			#endregion

			#region Methods

			public Config Clone()
			{
				return new Config()
				{
					TileSize = TileSize,
					Start = Start,
					End = End,
					WalkableTags = WalkableTags,
					WalkableLayers = WalkableLayers,
					NonWalkableTags = NonWalkableTags,
					NonWalkableLayers = NonWalkableLayers,
					SingleCast = SingleCast,
					MatchAny = MatchAny,
					CheckLargerArea = CheckLargerArea,
					IgnoreDepth = IgnoreDepth,
					MinDepth = MinDepth,
					MaxDepth = MaxDepth,
					DefaultDepth = DefaultDepth,
					Use3DPhysics = Use3DPhysics,
					XYGrid = XYGrid,
					ForceWalkable = ForceWalkable,
					ForceNonWalkable = ForceNonWalkable
				};
			}

			public void Verify()
			{
				if(verified)
					return;

				const int dimensions = 2;
				const bool strict = false;

				for(int i = 0; i < dimensions; i++)
				{
					if(Start[i] > End[i])
					{
						float temp = Start[i];
						Start[i] = End[i];
						End[i] = temp;
					}
				}

				if(!Use3DPhysics)
					XYGrid = true;

				End += TileSize;

				#pragma warning disable 0162

				if(strict)
				{
					//Although this will be ignored by the generator but still warn them.
					for(int i = 0; i < ForceWalkable.Length; i++)
						for(int j = 0; j < ForceNonWalkable.Length; j++)
//							for(int k = 0; k < ForceIgnore.Length; k++)
							if(ForceWalkable[i] == ForceNonWalkable[j])// || ForceNonWalkable[j] == ForceIgnore[k] || ForceIgnore[k] == ForceWalkable[i])
									Debug.LogError("Same Objected forced in multiple fields");

					int nonWalkableLayers = NonWalkableLayers;
					int walkableLayers = 0;

					for(int i = 0; i < WalkableLayers.Length; i++)
					{
						int layer = 1 << WalkableLayers[i];
						if((walkableLayers & layer) != 0 || (nonWalkableLayers & layer) != 0)
							Debug.LogError("Same Layer occuring in multiple fields");
						walkableLayers |= layer;
					}

					for(int i = 0; i < WalkableTags.Length; i++)
						for(int j = 0; j < NonWalkableTags.Length; j++)
							if(WalkableTags[i] == NonWalkableTags[i])
								Debug.LogError("Same Tag occuring in multiple fields");

				}			
					
				#pragma warning restore 0162

				verified = true;
			}

			public Map Build()
			{
				return Generator.Generate(this);
			}

			#endregion
		}

		#endregion

		#endregion

		#region Static Methods

		#region Casting

		private static GameObject[] Cast2D(Vector Position, Config config)
		{
			if(config.SingleCast)
			{
				if(config.CheckLargerArea)
					return new GameObject[] { Physics2D.OverlapArea((Vector2) Position - (Vector2) (config.TileSize / 2f), (Vector2) Position + (Vector2) (config.TileSize / 2f), -1, config.MinDepth, config.MaxDepth).gameObject };
				else
					return new GameObject[] { Physics2D.OverlapPoint(Position, -1, config.MinDepth, config.MaxDepth).gameObject };
			}

			Collider2D[] hits = null;

			if(config.CheckLargerArea)
				hits = Physics2D.OverlapAreaAll((Vector2) Position - (Vector2) (config.TileSize / 2f), (Vector2) Position + (Vector2) (config.TileSize / 2f), -1, config.MinDepth, config.MaxDepth);
			else
				hits = Physics2D.OverlapPointAll(Position, -1, config.MinDepth, config.MaxDepth);

			GameObject[] gameObjects = new GameObject[hits.Length];

			for(int i = 0; i < gameObjects.Length; i++)
				gameObjects[i] = hits[i].gameObject;

			return gameObjects;
		}

		private static GameObject[] Cast3D(Vector Position, Config config, bool generateDepth, out float[] depth)
		{
			Vector origin = config.XYGrid ? new Vector(Position.x, Position.y, config.MinDepth) : new Vector(Position.x, config.MaxDepth, Position.z);
			Vector direction = config.XYGrid ? Vector.forward : Vector.down;
			
			if(config.SingleCast)
			{
				RaycastHit hit = default(RaycastHit);

				if(config.CheckLargerArea)
					Physics.BoxCast(origin, config.TileSize / 2f, direction, out hit, Quaternion.identity, config.MaxDepth - config.MinDepth, -1, QueryTriggerInteraction.UseGlobal);
				else
					Physics.Raycast(origin, direction, out hit, config.MaxDepth - config.MinDepth, -1, QueryTriggerInteraction.UseGlobal);
		
				if(hit.collider != null)
				{
					if(generateDepth)
						depth = new float[] { config.XYGrid ? hit.point.z : hit.point.y };
					else
						depth = null;

					return new GameObject[] { hit.collider.gameObject };
				}
				else
				{
					depth = (generateDepth ? new float[0] : null);
					return new GameObject[0];
				}
			}

			RaycastHit[] hits = null;

			if(config.CheckLargerArea)
				hits = Physics.BoxCastAll(origin, config.TileSize / 2f, direction, Quaternion.identity, config.MaxDepth - config.MinDepth, -1);
			else
				hits = Physics.RaycastAll(origin, direction, config.MaxDepth - config.MinDepth, -1);

			GameObject[] gameObjects = new GameObject[hits.Length];
			depth = generateDepth ? new float[hits.Length] : null;

			if(generateDepth)
			{
				for(int i = 0; i < hits.Length; i++)
				{
					gameObjects[i] = hits[i].collider.gameObject;
					depth[i] = config.XYGrid ? hits[i].point.z : hits[i].point.y;
				}
			}
			else
				for(int i = 0; i < hits.Length; i++)
					gameObjects[i] = hits[i].collider.gameObject;
				
			return gameObjects;
		}

		/* DISCARDED

		public static void CollectCastDataImmediate(Map map, bool generateDepth, System.Action<GameObject[,][], float[,][]> OutputMethod)
		{
			bool finished = false;

			UtilityMonoBehaviour.CreateInstance();
			UtilityMonoBehaviour.Start(CollectCastData(map, generateDepth, 0, (gameObjects, depth) =>
			{
				OutputMethod(gameObjects, depth);
				finished = true;
			}));

			while (finished);
		}

		public static void CollectCastData(Map map, bool generateDepth, System.Action<GameObject[,][], float[,][]> OutputMethod)
		{
			int updateAfterFrames = (map.TilesX * map.TilesY) / 10;
			UtilityMonoBehaviour.CreateInstance();
			UtilityMonoBehaviour.Start(CollectCastData(map, generateDepth, updateAfterFrames < 1 ? 1 : updateAfterFrames, OutputMethod));
		}

		#region Coroutines

		private static System.Collections.IEnumerator CollectCastData(Map map, bool generateDepth, int updatesPerFrame, System.Action<GameObject[,][], float[,][]> OutputMethod)
		{
			map.MakeBusy();

			Generator.Config config = map.GetConfig();
			Node[,] nodes = map.GetNodeArrayReference();
			float[,][] depth = null;

			int LengthX = config.TilesX;
			int LengthY = config.TilesY;

			GameObject[,][] updateObjects = new GameObject[LengthX, LengthY][];

			if(generateDepth)
				depth = new float[updateObjects.GetLength(0), updateObjects.GetLength(1)][];
			else
				depth = null;

			if(updatesPerFrame == 0)
			{
				for(int i = 0; i < LengthX; i++)
					for(int j = 0; j < LengthY; j++)
						updateObjects[i, j] = config.Use3DPhysics ? Generator.Cast3D(nodes[i, j].Position, config, generateDepth, out depth[i, j]) : Generator.Cast2D(nodes[i, j].Position, config);
			}
			else
			{
				int updatesDone = 0;
				for(int i = 0; i < LengthX; i++)
				{
					for(int j = 0; j < LengthY; j++)
					{
						updateObjects[i, j] = config.Use3DPhysics ? Generator.Cast3D(nodes[i, j].Position, config, generateDepth, out depth[i, j]) : Generator.Cast2D(nodes[i, j].Position, config);
						if(++updatesDone % updatesPerFrame == 0)
							yield return null;
					}
				}
			}

			map.MakeFree();
			OutputMethod(updateObjects, depth);
		}

		#endregion


		*/

		#endregion

		#region Updating

		private static void UpdateNode(Node Node, Config config)
		{
			UpdateNode(Node, config, true);
		}

		private static void UpdateNode(Node node, Config config, bool checkForcedObjects)
		{
			float[] depth = null;
			GameObject[] gameObjects = config.Use3DPhysics ? Cast3D(node.Position, config, true, out depth) : Cast2D(node.Position, config);
			UpdateNodeActual(node, config, gameObjects, depth, checkForcedObjects);
		}

		private static void UpdateNodeActual(Node node, Config config, GameObject[] hits, float[] depth)
		{
			UpdateNodeActual(node, config, hits, depth, true);
		}

		private static void UpdateNodeActual(Node node, Config config, GameObject[] hits, float[] depth, bool checkForcedObjects)
		{
			#region Forced

			if(checkForcedObjects)
			{	
				for(int i = 0; i < config.ForceNonWalkable.Length; i++)
				{
					for(int j = 0; j < hits.Length; j++)
					{
						if(config.ForceNonWalkable[i] == hits[j])
						{
							MakeNonWalkable(node);
							return;
						}
					}
				}

				for(int i = 0; i < config.ForceWalkable.Length; i++)
				{
					for(int j = 0; j < hits.Length; j++)
					{
						if(config.ForceWalkable[i] == hits[j])
						{
							MakeWalkable(node, config.ForceWalkable[i].Cost, depth != null ? depth[j] : config.DefaultDepth, config);
							return;
						}
					}
				}

				/*

				for(int i = 0; i < config.ForceIgnore.Length; i++)
				{
					for(int j = 0; j < config.ForceIgnore.Length; j++)
					{
						if(config.ForceIgnore[i] == hits[j])
						{
							IgnoreNode(node, config.EmptySpaceMoveCost, config.DefaultDepth, config);
							return;
						}
					}
				}
				
				*/

			}

			

			#endregion

			#region Non Walkable Check

			if(!config.MatchAny)
			{
				for(int i = 0; i < hits.Length; i++)
				{
					if(((1 << hits[i].layer) & config.NonWalkableLayers.value) != 0)
					{
						for(int j = 0; j < config.NonWalkableTags.Length; j++)
						{
							if(hits[i].tag == (config.NonWalkableTags[j]))
							{
								MakeNonWalkable(node);
								return;
							}
						}
					}
				}
			}
			else
			{
				for(int i = 0; i < hits.Length; i++)
				{
					if(((1 << hits[i].layer) & config.NonWalkableLayers.value) != 0)
					{
						MakeNonWalkable(node);
						return;
					}
					for(int j = 0; j < config.NonWalkableTags.Length; j++)
					{
						if(hits[i].tag == (config.NonWalkableTags[j]))
						{
							MakeNonWalkable(node);
							return;
						}
					}
				}
			}

			#endregion

			#region Walkable Check

			if(!config.MatchAny)
			{
				for(int i = 0; i < config.WalkableLayers.Length; i++)
				{
					for(int j = 0; j < hits.Length; j++)
					{
						if(((1 << hits[j].layer) & (1 << config.WalkableLayers[i].LayerNo)) == 0)
							continue;

						for(int k = 0; k < config.WalkableTags.Length; k++)
						{
							if(hits[j].tag == config.WalkableTags[k])
							{
								MakeWalkable(node, config.WalkableTags[k] * config.WalkableLayers[i], depth != null ? depth[j] : config.DefaultDepth, config);
								return;
							}
						}
					}
				}
			}
			else
			{
				for(int i = 0; i < hits.Length; i++)
				{
					for(int j = 0; j < config.WalkableLayers.Length; j++)
					{
						if(((1 << hits[i].layer) & (1 << config.WalkableLayers[j].LayerNo)) != 0)
						{
							MakeWalkable(node, config.WalkableLayers[j].Cost, depth != null ? depth[i] : config.DefaultDepth, config);
							return;
						}
					}

					for(int k = 0; k < config.WalkableTags.Length; k++)
					{
						if(hits[i].tag == (config.WalkableTags[k]))
						{
							MakeWalkable(node, config.WalkableTags[k].Cost, depth != null ? depth[i] : config.DefaultDepth, config);
							return;
						}
					}
				}
			}

			#endregion

//			IgnoreNode(node, config.EmptySpaceMoveCost, config.DefaultDepth, config);
		}

		public static void UpdateNodeRuntime(Map map, Int2D index, Config config)
		{
			map.MakeBusy();

			UpdateNode(map.GetNode(index), config, false);

			map.MakeFree();

			map.TriggerUpdateEvent();
		}

		public static void UpdateMapRuntime(Map map)
		{
			UpdateNodesRuntime(map, new Int2D(0, 0), new Int2D(map.TilesX, map.TilesY));
		}

		public static void UpdateNodesRuntime(Map map, Int2D[] indexes)
		{
			map.MakeBusy();
			
			Node[,] nodes = map.GetNodeArrayReference();
			Config config = map.GetConfig();

			for(int i = 0; i < indexes.Length; i++)
				UpdateNode(nodes[indexes[i].x, indexes[i].y], config, false);

			map.MakeFree();

			map.TriggerUpdateEvent();
		}

		public static void UpdateNodesRuntime(Map map, Int2D Start, Int2D End)
		{
			map.MakeBusy();

			Node[,] nodes = map.GetNodeArrayReference();
			Generator.Config config = map.GetConfig();

			if(End.x < Start.x)
			{
				int startX = Start.x;
				Start.x = End.x;
				End.x = startX;
			}
			if(End.y < Start.y)
			{
				int startY = Start.y;
				Start.y = End.y;
				End.y = startY;
			}

			if(Start.x < 0 || Start.y < 0 || End.x > map.TilesX || End.y > map.TilesY)
				throw new System.IndexOutOfRangeException("Node index is out of range");

			for(int i = Start.x; i < End.x; i++)
				for(int j = Start.y; j < End.y; j++)
					UpdateNode(nodes[i, j], config, false);

			map.MakeFree();

			map.TriggerUpdateEvent();
		}

		/* DISCARDED
		 * No unity api can be called from outside of main unity thread so this method is useless

		private static void UpdateMapRuntime(Map map, GameObject[,][] gameObjects, float[,][] depth)//, Int2D start = Int2D(0, 0))
		{
			Config config = map.GetConfig();
			Node[,] nodes = map.GetNodeArrayReference();
			int lengthX = nodes.GetLength(0);
			int lengthY = nodes.GetLength(1);

			if(gameObjects.GetLength(0) != lengthX || gameObjects.GetLength(1) != lengthY || depth == null ? true : (depth.GetLength(0) != lengthX || depth.GetLength(1) != lengthY))
				throw new System.ArgumentException("Invalid Argument Dimensions");

			while (map.IsBusy);
			map.MakeBusy();

			for(int i = 0; i < lengthX; i++)
				for(int j = 0; j < lengthY; j++)
					UpdateNodeActual(nodes[i, j], config, gameObjects[i, j], depth[i, j]);

			map.MakeFree();
		}

		public static void UpdateMapRuntime(Map map, bool generateDepth, System.Threading.ThreadPriority priority)
		{
			CollectCastData(map, generateDepth, (gameObjects, depth) =>
			{
				System.Threading.Thread thread = new System.Threading.Thread(() => UpdateMapRuntime(map, gameObjects, depth));
				thread.Priority = priority;
				thread.Start();
			});
		}

		public static void UpdateMapRuntime(Map map, bool generateDepth)
		{
			UpdateMapRuntime(map, true, System.Threading.ThreadPriority.Normal);
		}

		public static void UpdateMapRuntime(Map map, System.Threading.ThreadPriority priority)
		{
			UpdateMapRuntime(map, true, priority);
		}

		public static void UpdateMapRuntime(Map map)
		{
			UpdateMapRuntime(map, true, System.Threading.ThreadPriority.Normal);
		}

		public static void UpdateMapRuntimeImmediate(Map map, bool generateDepth)
		{
			CollectCastDataImmediate(map, generateDepth, (gameObjects, depth) => UpdateMapRuntime(map, gameObjects, depth));
		}

		public static void UpdateMapRuntimeImmediate(Map map)
		{
			UpdateMapRuntimeImmediate(map, true);
		}

		*/

		#endregion

		#region Node Editing

		private static void SetDepth(Node node, float depth, Config config)
		{
			if(config.XYGrid)
				node.Position.z = config.IgnoreDepth ? config.DefaultDepth : depth;
			else
				node.Position.y = config.IgnoreDepth ? config.DefaultDepth : depth;
		}

		private static void MakeWalkable(Node node, float cost, float depth, Config config)
		{
			node.MoveCost = cost;
			node.Walkable = true;
			SetDepth(node, depth, config);
		}

		private static void MakeNonWalkable(Node node)
		{
			node.Walkable = false;
		}

		/*

		private static void IgnoreNode(Node node, float cost, float depth, Config config)
		{
			if(config.EmptySpaceWalkable)
				MakeWalkable(node, cost, depth, config);
			else
				MakeNonWalkable(node);
		}

		*/

		#endregion

		#region Creation

		private static Node[,] CreateNodes(Config config)
		{
			int LengthX = config.TilesX;
			int LengthY = config.TilesY;

			Node[,] nodes = new Node[LengthX, LengthY];

			if(config.XYGrid)
			{
				for(int i = 0; i < LengthX; i++)
					for(int j = 0; j < LengthY; j++)
						UpdateNode(nodes[i, j] = new Node(new Int2D(i, j), new Vector(config.Start.x + config.TileSize.x * i, config.Start.y + config.TileSize.y * j, 0f)), config);
			}
			else
			{
				for(int i = 0; i < LengthX; i++)
					for(int j = 0; j < LengthY; j++)
						UpdateNode(nodes[i, j] = new Node(new Int2D(i, j), new Vector(config.Start.x + config.TileSize.x * i, 0f, config.Start.y + config.TileSize.y * j)), config);
			}

			return nodes;
		}
			
		public static Map Generate(Config config)
		{
			config.Verify();
			return new Map(CreateNodes(config), config.Clone());
		}

		#endregion

		#endregion

	}
}