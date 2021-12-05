using UnityEngine;

namespace FastPath
{
	[System.Serializable]
	public struct Int2D
	{
		#region Members

		public int x;
		public int y;

		#endregion

		#region Construcotrs

		public Int2D(int x)
		{
			this.x = x;
			this.y = 0;
		}

		public Int2D(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public Int2D(Int2D int2D)
		{
			this.x = int2D.x;
			this.y = int2D.y;
		}

		#endregion

		#region Methods

		public override string ToString()
		{
			return x.ToString() + ", " + y.ToString();
		}

		#endregion

		#region Indexers

		public int this[int index]
		{
			get
			{
				return index == 0 ? x : y;
			}
			set
			{
				if(index == 0)
					x = value;
				else
					y = value;
			}
		}

		#endregion
	}
}