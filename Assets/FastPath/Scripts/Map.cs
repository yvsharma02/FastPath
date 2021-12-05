using UnityEngine;

using Vector = UnityEngine.Vector3;

namespace FastPath
{
	public class Map
	{
		#region Events

		public event System.Action OnUpdate;

		#endregion

		#region Members

		private Generator.Config config;
		private Node[,] Nodes;
		private Node[] OpenListBH;
		private Node[] ClosedList;
		private bool busy = false;

		#endregion

		#region Properties

		public Vector Start
		{
			get
			{
				return new Vector(config.Start.x, config.XYGrid ? config.Start.y : config.MinDepth, config.XYGrid ? config.MinDepth : config.Start.y);
//				return config.Start;
			}
		}

		public Vector End
		{
			get
			{
				return new Vector(config.End.x, config.XYGrid ? config.End.y : config.MaxDepth, config.XYGrid ? config.MaxDepth : config.End.y);
//				return config.End;
			}
		}

		public int TilesX
		{
			get
			{
				return Nodes.GetLength(0);
			}
		}

		public int TilesY
		{
			get
			{
				return Nodes.GetLength(1);
			}
		}

		public bool IsBusy
		{
			get
			{
				return busy;
			}
		}

		#endregion

		#region Constructors

		public Map(Node[,] nodes, Generator.Config config)
		{
			this.Nodes = nodes;
			this.config = config;
			this.OpenListBH = new Node[(nodes.GetLength(0) * nodes.GetLength(1)) + 1];
			this.ClosedList = new Node[nodes.GetLength(0) * nodes.GetLength(1)];

			if(FastPath.DefaultMap == null)
				FastPath.DefaultMap = this;
		}

		#endregion

		#region Methods

		public void TriggerUpdateEvent()
		{
			if(OnUpdate != null)
				OnUpdate();
		}

		public bool InBounds(Vector position)
		{
			return InBounds(position, false);
		}

		public bool InBounds(Vector position, bool depth)
		{
			Vector start = Start;
			Vector end = End;

			if(depth)
			{
				for(int i = 0; i < 3; i++)
					if(position[i] < start[i] || position[i] > end[i])
						return false;
				return true;
			}
			else
			{
				for(int i = 0; i < 3; i++)
				{
					if(config.XYGrid)
					{
						if(i == 2)
							return true;
					}
					else
					{
						if(i == 1)
							continue;
					}

					if(position[i] < start[i] || position[i] > end[i])
						return false;
				}
				return true;
			}
		}

		public bool InBounds(Int2D start)
		{
			return (start.x >= 0 && start.x < config.TilesX) && (start.y >= 0 && start.y < config.TilesY);	
		}

		public void MakeBusy()
		{
			while (busy);

			busy = true;
		}

		public void MakeFree()
		{
			busy = false;
		}

		public Generator.Config GetConfig()
		{
			return config;
		}

		public Int2D PositionToIndexFloor(Vector Position)
		{
			if(config.XYGrid)
				return new Int2D((int) ((Position.x - config.Start.x) / config.TileSize.x), (int) ((Position.y - config.Start.y) / config.TileSize.y));
			else
				return new Int2D((int) ((Position.x - config.Start.x) / config.TileSize.x), (int) ((Position.z - config.Start.y) / config.TileSize.y));
		}

		public Int2D PositionToIndexCeil(Vector position)
		{
			float x = (position.x - config.Start.x) / config.TileSize.x;
			float y = (config.XYGrid ? (position.y - config.Start.y) : (position.z - config.Start.y)) / config.TileSize.y;

			return new Int2D((int) ((x == (int) x) ? x : x + 1), (int) ((y == (int) y) ? y : y + 1));
		}

		public Vector BringInBounds(Vector position)
		{
			Vector start = Start;
			Vector end = End;

			for(int i = 0; i < 3; i++)
			{
				if(position[i] < start[i])
					position[i] = start[i];
				if(position[i] > end[i])
					position[i] = end[i];
			}

			return position;
		}

		public Int2D BringInBounds(Int2D index)
		{
			return new Int2D(index.x < 0 ? 0 : index.x >= TilesX ? TilesX - 1 : index.x, index.y < 0 ? 0 : index.y >= TilesY ? TilesY - 1 : index.y);
		}

		public Node[,] GetNodeArrayReference()
		{
			return Nodes;
		}

		public Node[] GetClosedListReference()
		{
			return ClosedList;
		}

		public Node[] GetOpenListReference()
		{
			return OpenListBH;
		}

		public Node GetNode(Int2D index)
		{
			return Nodes[index.x, index.y];
		}

		public void DrawMapInEditor()
		{
			int LengthX = TilesX;
			int LengthY = TilesY;

			for(int i = 0; i < LengthX; i++)
			{
				for(int j = 0; j < LengthY; j++)
				{
					if(!Nodes[i, j].Walkable)
						continue;

					if(i + 1 < LengthX && Nodes[i + 1, j].Walkable)
						Debug.DrawLine(Nodes[i, j].Position, Nodes[i + 1, j].Position, Color.green);
					
					if(i + 1 < LengthX && j + 1 < LengthY && Nodes[i + 1, j + 1].Walkable)
						Debug.DrawLine(Nodes[i, j].Position, Nodes[i + 1, j + 1].Position, Color.green);
					
					if(j + 1 < LengthY && Nodes[i, j + 1].Walkable)
						Debug.DrawLine(Nodes[i, j].Position, Nodes[i, j + 1].Position, Color.green);
					
					if(i + 1 < LengthX && j + 1 < LengthY && Nodes[i + 1, j].Walkable && Nodes[i, j + 1].Walkable)
						Debug.DrawLine(Nodes[i, j + 1].Position, Nodes[i + 1, j].Position, Color.green);
					
//					if(i + 1 < TilesX)
//					{
//						if(Nodes[i + 1, j].Walkable)
//							Debug.DrawLine(Nodes[i, j].Position, Nodes[i + 1, j].Position, Color.green);
//						if(j + 1 < TilesY) 
//						{
//							if(Nodes[i + 1, j + 1].Walkable)
//								Debug.DrawLine(Nodes[i, j].Position, Nodes[i + 1, j + 1].Position, Color.green);
//							if(Nodes[i + 1, j].Walkable)
//								Debug.DrawLine(Nodes[i, j + 1].Position, Nodes[i + 1, j].Position, Color.green);
//						}
//					}
//					if(j + 1 < TilesY && Nodes[i, j + 1].Walkable)
//						Debug.DrawLine(Nodes[i, j].Position, Nodes[i, j + 1].Position, Color.green);
				}
			}
		}

		#endregion
	}
}