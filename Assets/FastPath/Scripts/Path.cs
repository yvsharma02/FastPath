using Vector = UnityEngine.Vector3;

namespace FastPath
{
	public class Path
	{
		#region Static Methods

		public static Path BuildImmediate(Map map, Vector start, Vector end, float aggression, bool moveDiognal, float maxDepthDiff, float depthDiffCostMultiplier, Int2D[] disallowedIndexes)
		{
			UtilityMonoBehaviour.CreateInstance();
			Path path = new Path();
			path.Initialise(Pathfinder.FindPathImmediate(start, end, map, aggression, moveDiognal, maxDepthDiff, depthDiffCostMultiplier, disallowedIndexes));
			return path;
		}

		public static Path BuildImmediate(Map map, Vector start, Vector end, float aggression, bool moveDiognal, Int2D[] disallowedIndexes)
		{
			return BuildImmediate(map, start, end, aggression, moveDiognal, FastPath.DefaultMaxDepthDifference, FastPath.DefaultDepthCostMultiplier, disallowedIndexes);
		}

		public static Path BuildImmediate(Vector start, Vector end)
		{
			return BuildImmediate(FastPath.DefaultMap, start, end, FastPath.DefaultEstimateAggression, FastPath.DefaultMoveDiognal, null);
		}

		public static Path BuildImmediate(Vector start, Vector end, Int2D[] disallowedIndexes)
		{
			return BuildImmediate(FastPath.DefaultMap, start, end, FastPath.DefaultEstimateAggression, FastPath.DefaultMoveDiognal, disallowedIndexes);
		}

		#endregion

		#region Events

		public event System.Action OnPathBuilt;

		#endregion

		#region Members

		private readonly Pathfinder.PathRequest request;
		private Vector[] path;
		private bool ready;

		#endregion

		#region Consturctors

		private Path() {}

		public Path(Map map, Vector start, Vector end, float aggression, bool moveDiognal, float maxDepthDiff, float depthDiffMultiplier, Int2D[] disallowedIndexes)
		{
			request = Pathfinder.RequestPath(start, end, map, aggression, moveDiognal, maxDepthDiff, depthDiffMultiplier, disallowedIndexes, Initialise);
		}

		public Path(Map map, Vector start, Vector end, float aggression, bool moveDiognal, Int2D[] disallowedIndexes)
		{
			request = Pathfinder.RequestPath(start, end, map, aggression, moveDiognal, FastPath.DefaultMaxDepthDifference, FastPath.DefaultDepthCostMultiplier, disallowedIndexes, Initialise);
		}

		public Path(Vector start, Vector end)
		{
			request = Pathfinder.RequestPath(start, end, FastPath.DefaultMap, FastPath.DefaultEstimateAggression, FastPath.DefaultMoveDiognal, FastPath.DefaultMaxDepthDifference, FastPath.DefaultDepthCostMultiplier, null, Initialise);
		}

		public Path(Vector start, Vector end, Int2D[] disallowedIndexes)
		{
			request = Pathfinder.RequestPath(start, end, FastPath.DefaultMap, FastPath.DefaultEstimateAggression, FastPath.DefaultMoveDiognal, FastPath.DefaultMaxDepthDifference, FastPath.DefaultDepthCostMultiplier, disallowedIndexes, Initialise);
		}

		#endregion

		#region Properties

		public bool IsReady
		{
			get
			{
				return ready;
			}
		}

		public bool ValidPath
		{
			get
			{
				if(!IsReady)
					throw new System.InvalidOperationException("Path is yet not ready!");
				return path != null;
			}
		}

		public int Length
		{
			get
			{
				if(!IsReady)
					throw new System.ArgumentException("Path is yet not build");
				if(path == null)
					return 0;
				return path.Length;
			}
		}

		/*

		public float TotalDistance
		{
			get
			{
				float len = 0f;
				for(int i = 0; i < path.Length - 1; i++)
					len += Vector.Distance(path[i], path[i + 1]);
				return len;
			}
		}

		*/

		#endregion

		#region Methods

		public void ForceBuild()
		{
			if(IsReady)
				throw new System.InvalidOperationException("Path is already built");
			this.request.BuildImmediate();
		}

		public void DrawPath()
		{
			if(!IsReady)
				throw new System.InvalidOperationException("Path is yet not ready!");
			if(path != null)
				for(int i = 0; i < path.Length - 1; i++)
					UnityEngine.Debug.DrawLine(path[i], path[i + 1], UnityEngine.Color.red);
		}

		private void Initialise(Vector[] path)
		{
			ready = true;
			this.path = path;
			UtilityMonoBehaviour.Enque(TriggerInitialiseEvent());
		}

		#endregion

		#region Coroutines

		private System.Collections.IEnumerator TriggerInitialiseEvent()
		{
			yield return null;
			if(OnPathBuilt != null)
				OnPathBuilt();
		}

		#endregion

		#region Indexers

		public Vector this[int index]
		{
			get
			{
				return path[index];
			}
		}

		#endregion
	}
}