using UnityEngine;
using System.Threading;

using Vector = UnityEngine.Vector3;

namespace FastPath
{
	public static class Pathfinder
	{
		/*
		 * Maybe add MaxFallDown value (rather than using maxHeightDiff for both climbing and falling)
		 */

		#region Inner Classes

		#region PathRequest

		public class PathRequest
		{
			#region Constructors

			public PathRequest(Int2D Start, Int2D End, Map map, float EstimateAggression, bool MoveDiognal, float MaxDepthDiff, float DepthCostAggression, Int2D[] disallowedIndexes, System.Action<Vector[]> OutputMethod)
			{
				this.Start = Start;
				this.End = End;
				this.map = map;
				this.EstimateAggression = EstimateAggression;
				this.MoveDiognal = MoveDiognal;
				this.OutputMethod = OutputMethod;
				this.DepthCostAggression = DepthCostAggression;
				this.MaxDepthDifference = MaxDepthDiff;
				this.disallowedIndexes = disallowedIndexes;
			}

			#endregion

			#region Members

			private Int2D[] disallowedIndexes;

			public readonly Int2D Start;
			public readonly Int2D End;
			public readonly Map map;
			public readonly float EstimateAggression;
			public readonly bool MoveDiognal;
			public readonly float MaxDepthDifference;
			public readonly float DepthCostAggression;
			public readonly System.Action<Vector[]> OutputMethod;
			private bool built;

			#endregion

			#region Properties

			public bool IsBuilt
			{
				get
				{
					return built;
				}
			}

			#endregion

			#region Methods

			public void BuildImmediate()
			{
				if(built)
					return;

				built = true;
				OutputMethod(Pathfinder.FindPathImmediate(Start, End, map, EstimateAggression, MoveDiognal, MaxDepthDifference, DepthCostAggression, disallowedIndexes));
			}

			#endregion
		}

		#endregion

		#endregion

		#region Members

		#region Que

		private static System.Collections.Generic.Queue<PathRequest> RequestQue;
		private static Thread RequestProcessThread;
		private static System.Threading.ThreadPriority DefaultPriority = System.Threading.ThreadPriority.BelowNormal;

		#endregion

		#region Pathfinding

		private static bool busy;

		private static Generator.Config config;
		private static Map currentMap;

		private static Node[,] Nodes;
		private static Node[] OpenList;
		private static Node[] ClosedList;
	
		private static int OpenListLength;
		private static int ClosedListLength;

		private static int TilesX;
		private static int TilesY;

		#endregion

		#endregion

		#region Static Properties

		public static int QueLength
		{
			get
			{
				if(RequestQue == null)
					return 0;
				return RequestQue.Count;
			}
		}

		public static System.Threading.ThreadPriority PathfinderThreadPriority
		{
			get
			{
				return DefaultPriority;
			}
			set
			{
				DefaultPriority = value;
				if(RequestProcessThread != null)
					RequestProcessThread.Priority = DefaultPriority;
			}
		}

		public static bool IsBusy
		{
			get
			{
				return busy;
			}
		}

		#endregion

		#region Static Methods

		#region Cleanup and Initialization

		private static void Initialise(Map map)
		{
			while (busy);
			busy = true;

			map.MakeBusy();

			currentMap = map;
			config = map.GetConfig();

			TilesX = map.TilesX;
			TilesY = map.TilesY;

			Nodes = map.GetNodeArrayReference();
			OpenList = map.GetOpenListReference();
			ClosedList = map.GetClosedListReference();

			OpenListLength = 0;
			ClosedListLength = 0;
		}

		private static void Clean()
		{
			for(int i = 1; i < OpenListLength + 2; i++)
			{
				if(OpenList[i] != null)
					OpenList[i].Reset();
				OpenList[i] = null;
			}

			for(int i = 0; i < ClosedListLength; i++)
			{
				ClosedList[i].Reset();
				ClosedList[i] = null;
			}

			config = null;

			Nodes = null;
			ClosedList = null;
			OpenList = null;

			TilesX = -1;
			TilesY = -1;

			OpenListLength = 0;
			ClosedListLength = 0;

			currentMap.MakeFree();
			currentMap = null;

			busy = false;
		}

		#endregion

		#region Binary Heap

		private static void SortFrom(int index)
		{
			int parent = index >> 1;

			while (true) // parent == none || parent.F > index.F
			{
				if(parent > 0 && OpenList[index].F <= OpenList[parent].F)
				{
					Node temp = OpenList[index];
					OpenList[index] = OpenList[parent];
					OpenList[parent] = temp;

					OpenList[index].BHIndex = index;
					OpenList[parent].BHIndex = parent;

					index >>= 1;
					parent >>= 1;
				}
				else
				{
					int childA = index << 1;
					int childB = (index << 1) | 1;

					if(childA > OpenListLength)
						return;

					int lowerChild = ((OpenListLength >= childB) ? ((OpenList[childA].F < OpenList[childB].F) ? childA : childB) : childA);

					if(OpenList[index].F > OpenList[lowerChild].F)
					{
						Node temp = OpenList[index];
						OpenList[index] = OpenList[lowerChild];
						OpenList[lowerChild] = temp;

						OpenList[index].BHIndex = index;
						OpenList[lowerChild].BHIndex = lowerChild;

						index = (lowerChild == childA ? index << 1 : (index << 1) | 1);
					}
					else
						return;
				}
			}
		}

		private static void AddToBH(Node node)
		{
			node.OnOpenList = true;
			node.BHIndex = ++OpenListLength;
			OpenList[node.BHIndex] = node;
			SortFrom(OpenListLength);
		}

		/*
		 
		private static void RemoveFromOpenList()
		{
			OpenList[1].OnClosedList = true;
			OpenList[1].OnOpenList = false;
			ClosedList[ClosedListLength++] = OpenList[1];

			OpenList[1].BHIndex = -1;
			OpenList[1] = OpenList[OpenListLength--];
			SortFrom(1);
		}

		*/

		#endregion

		#region A*

		public static Vector[] FindPathImmediate(Vector Start, Vector End, Map map, float estimateAggression, bool moveDiognal, float maxDepthDiff, float depthCostAggression, Int2D[] disallowedIndexes)
		{
			if(!map.InBounds(Start) || !map.InBounds(End))
				throw new System.ArgumentException("Start or End position are out of bounds");

			return FindPathImmediate(map.PositionToIndexFloor(Start), map.PositionToIndexFloor(End), map, estimateAggression, moveDiognal, maxDepthDiff, depthCostAggression, disallowedIndexes);	
		}

		private static Vector[] FindPathImmediate(Int2D Start, Int2D End, Map map, float estimateAggression, bool moveDiognal, float maxDepthDiff, float depthCostAggression, Int2D[] disallowedIndexes)
		{
			Initialise(map);

			Start = map.BringInBounds(Start);
			End = map.BringInBounds(End);

			if(disallowedIndexes != null)
			{
				for(int i = 0; i < disallowedIndexes.Length; i++)
				{
					Node node = Nodes[disallowedIndexes[i].x, disallowedIndexes[i].y];
					ClosedList[ClosedListLength++] = node;
					node.OnOpenList = false;
					node.OnClosedList = true;
				}
			}

			int depthDimension = config.XYGrid ? 1 : 2;
																					 // We disallowed the target node!  // And starting one too....
			if(!Nodes[Start.x, Start.y].Walkable || !Nodes[End.x, End.y].Walkable || Nodes[End.x, End.y].OnClosedList || Nodes[Start.x, Start.y].OnClosedList)
			{
				Clean();
				return null;
			}

			Node current = Nodes[Start.x, Start.y];
			current.CalculateFGH(End, map, estimateAggression);
			AddToBH(current);

			while (!Nodes[End.x, End.y].OnClosedList)
			{
				if(OpenListLength < 0)
				{
					Clean();
					return null;
				}

				current = OpenList[1];

				OpenList[1].OnClosedList = true;
				OpenList[1].OnOpenList = false;
				ClosedList[ClosedListLength++] = OpenList[1];

				OpenList[1].BHIndex = -1;
				OpenList[1] = OpenList[OpenListLength--];
				SortFrom(1);

				Int2D currentIndex = current.Index;

				for(int i = 0; i < 8; i++)
				{
					if(!moveDiognal)
						if((i & 1) != 0) // if i is not even
							continue;

					Int2D nearbyIndex = currentIndex;

					switch(i)
					{
						case 0:
							nearbyIndex.x -= 1;
						break;
						case 1:
							nearbyIndex.x -= 1;
							nearbyIndex.y += 1;
						break;
						case 2:
							nearbyIndex.y += 1;
						break;
						case 3:
							nearbyIndex.y += 1;
							nearbyIndex.x += 1;
						break;
						case 4:
							nearbyIndex.x += 1;
						break;
						case 5:
							nearbyIndex.x += 1;
							nearbyIndex.y -= 1;
						break;
						case 6:
							nearbyIndex.y -= 1;
						break;
						case 7:
							nearbyIndex.x -= 1;
							nearbyIndex.y -= 1;
						break;
					}

					if(nearbyIndex.x < 0 || nearbyIndex.y < 0 || nearbyIndex.x >= TilesX || nearbyIndex.y >= TilesY)
						continue;

					Node nearbyNode = Nodes[nearbyIndex.x, nearbyIndex.y];

					float depthDiff =  nearbyNode.Position[depthDimension] - current.Position[depthDimension];
					depthDiff = depthDiff < 0f ? -depthDiff : depthDiff;

					if(depthDiff > maxDepthDiff)
						continue;

					if(nearbyNode.OnOpenList)
					{
						if(nearbyNode.G < current.G)
						{
							nearbyNode.Parent = current;
							nearbyNode.CalculateFGH(End, map, estimateAggression, depthCostAggression, config);
							SortFrom(nearbyNode.BHIndex);
						}
					}
					else if(!nearbyNode.OnClosedList && nearbyNode.Walkable)
					{
						nearbyNode.Parent = current;
						nearbyNode.CalculateFGH(End, map, estimateAggression, depthCostAggression, config);

						nearbyNode.OnOpenList = true;
						nearbyNode.BHIndex = ++OpenListLength;
						OpenList[nearbyNode.BHIndex] = nearbyNode;
						SortFrom(OpenListLength);
					}
				}
			}

			int len = 0;
			Node n = Nodes[End.x, End.y];
			while (n != null)
			{
				n = n.Parent;
				len += 1;
			}

			n = Nodes[End.x, End.y];
			Vector[] path = new Vector[len];
			int j = len - 1;

			while (n != null)
			{
				path[j--] = n.Position;
				n = n.Parent;
			}

			Clean();

			return path;
		}

		#endregion

		#region Que

		private static void ProcessRequests()
		{
			while (RequestQue.Count > 0)
			{
				PathRequest request = null;

				lock(RequestQue)
					request = RequestQue.Dequeue();

				request.BuildImmediate();
			}
		}

		public static PathRequest RequestPath(Vector Start, Vector End, Map map, float estimateAggresssion, bool moveDiognal, float maxDepthDiff, float depthDiffCostMultiplier, Int2D[] disallowedIndexes, System.Action<Vector[]> OutputMethod)
		{
			if(!map.InBounds(Start) || !map.InBounds(End))
				throw new System.ArgumentException("Start or End position are out of bounds");
			
			return RequestPath(map.PositionToIndexFloor(Start), map.PositionToIndexFloor(End), map, estimateAggresssion, moveDiognal, maxDepthDiff, depthDiffCostMultiplier, disallowedIndexes, OutputMethod);
		}

		private static PathRequest RequestPath(Int2D Start, Int2D End, Map map, float estimateAggression, bool moveDiognal, float maxDepthDiff, float depthDiffCostMultiplier, Int2D[] disallowedIndexes, System.Action<Vector[]> OutputMethod)
		{
			if(RequestQue == null)
				RequestQue = new System.Collections.Generic.Queue<PathRequest>();

			UtilityMonoBehaviour.CreateInstance();
			PathRequest request = new PathRequest(Start, End, map, estimateAggression, moveDiognal, maxDepthDiff, depthDiffCostMultiplier, disallowedIndexes, OutputMethod);
			RequestQue.Enqueue(request);

			if(RequestProcessThread == null || !RequestProcessThread.IsAlive)
			{
				RequestProcessThread = new Thread(ProcessRequests);
				RequestProcessThread.Priority = DefaultPriority;
				RequestProcessThread.Start();
			}

			return request;
		}

		#endregion

		#endregion
	}
}