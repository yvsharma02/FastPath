using UnityEngine;

using Vector = UnityEngine.Vector3;

namespace FastPath
{
	public class Node
	{
		#region Methods

		public Int2D Index;
		public Vector Position;
		public float MoveCost;
		public bool Walkable;

		public Node Parent;
		public bool OnOpenList;
		public bool OnClosedList;
		public float F;
		public float G;
		public float H;
		public int BHIndex = -1;

		#endregion

		#region Constructors

		public Node() {}

		public Node(Int2D Index)
		{
			this.Index = Index;
		}

		public Node(Int2D Index, Vector Position) : this(Index)
		{
			this.Position = Position;
		}

		public Node(Int2D Index, Vector Position, bool Walkable) : this(Index, Position)
		{
			this.Walkable = Walkable;
		}

		public Node(Int2D Index, Vector Position, bool Walkable, float MoveCost) : this(Index, Position, Walkable)
		{
			this.MoveCost = MoveCost;
		}

		public Node(Int2D Index, Vector Position, bool Walkable, float MoveCost, Node Parent, bool OnOpenList, bool OnClosedList, float F, float G, float H,
					int BHIndex) : this(Index, Position, Walkable, MoveCost)
		{
			this.Parent = Parent;
			this.OnOpenList = OnOpenList;
			this.OnClosedList = OnClosedList;
			this.F = F;
			this.G = G;
			this.H = H;
			this.BHIndex = BHIndex;
		}

		public Node(Node node, bool deep)
		{
			this.Index = node.Index;
			this.Position = node.Position;
			this.Walkable = node.Walkable;
			this.MoveCost = node.MoveCost;

			if(deep)
			{
				this.Parent = node.Parent;
				this.OnOpenList = node.OnOpenList;
				this.OnClosedList = node.OnClosedList;
				this.F = node.F;
				this.G = node.G;
				this.H = node.H;
				this.BHIndex = node.BHIndex;
			}
		}

		#endregion

		#region Methods

		public void CalculateFGH(Int2D End, Map map, float aggression)
		{
			const float scale = 1f;
			float multiplier = scale * ((aggression < 0f) ? -aggression : aggression);

			G = Parent != null ? Parent.G + (Parent.MoveCost * scale * ((Parent.Index.x != Index.x && Parent.Index.y != Index.y) ? 1.414f : 1f)) : 0f;
			H = (Index.x < End.x ? End.x - Index.x : Index.x - End.x) * multiplier + (Index.y < End.y ? End.y - Index.y : Index.y - End.y) * multiplier;
			F = G + H;
		}

		/// <summary>
		/// This calculates the difference in the dept (Z or Y axis) as cost too
		/// </summary>
		public void CalculateFGH(Int2D End, Map map, float aggression, float depthAggression, Generator.Config config)
		{
			const float scale = 1f;
			float multiplier = scale * ((aggression < 0f) ? -aggression : aggression);

			Vector parentPos = Parent.Position;
			int depthDimension = config.XYGrid ? 2 : 1;
			float depthDiff = parentPos[depthDimension] < Position[depthDimension] ? Position[depthDimension] - parentPos[depthDimension] : parentPos[depthDimension] - Position[depthDimension];

			G = Parent != null ? (Parent.G + (Parent.MoveCost * scale * ((Parent.Index.x != Index.x && Parent.Index.y != Index.y) ? 1.414f : 1f))) + (depthAggression * depthDiff)  : 0f;
			H = (Index.x < End.x ? End.x - Index.x : Index.x - End.x) * multiplier + (Index.y < End.y ? End.y - Index.y : Index.y - End.y) * multiplier;
			F = G + H;
		}

		private float GetEstimatedCost(Int2D End, Map map, float estimateAggression)
		{
			const float scale = 1f;
			float multiplier = scale * ((estimateAggression < 0f) ? -estimateAggression : estimateAggression);

			return (Index.x < End.x ? End.x - Index.x : Index.x - End.x) * multiplier + (Index.y < End.y ? End.y - Index.y : Index.y - End.y) * multiplier;
		}

		public void Reset()
		{
			this.Parent = null;
			this.OnOpenList = false;
			this.OnClosedList = false;
			this.F = 0f;
			this.G = 0f;
			this.H = 0f;
			this.BHIndex = -1;
		}

		#endregion

		#region Operators

		/* Directly copmaring F is giving a HUGE Performance boost ;)

		public static bool operator <(Node to, Node from)
		{
			return to.F < from.F ? true : to.F > from.F ? false : to.H < from.H ? true : false;
		}

		public static bool operator >(Node to, Node from)
		{
			return to.F > from.F ? true : to.F < from.F ? false : to.H > from.H ? true : false;
		}

		public static bool operator >=(Node to, Node from)
		{
			return to.F >= from.F;
		}

		public static bool operator <=(Node to, Node from)
		{
			return to.F <= from.F;
		}

		*/

		#endregion
	}
}