using UnityEngine;
using FastPath;

using System.Collections.Generic;

namespace FastPath.Demos.Dynamic
{
	public class Enemy : MonoBehaviour
	{
		#region Static Memebers

		private static List<Enemy> enemies;

		private static double lastPathTime;

		#endregion

		#region Static Properties

		public static double LastPathTime
		{
			get
			{
				return lastPathTime;
			}
		}

		#endregion

		#region Static Methods

		public static bool PathForAll(Vector3 end, Int2D[] ignoreNodes)
		{
			for(int i = 0; i < enemies.Count; i++)
				if(!Path.BuildImmediate(enemies[i].transform.position, end, ignoreNodes).ValidPath)
					return false;
			return true;
		}

		public static Enemy Create()
		{
			return Create(true);
		}

		public static Enemy Create(bool findPathImmediate)
		{
			GameObject gameObject = (GameObject) Object.Instantiate(Controller.Instance.EnemyObject.gameObject, Controller.Instance.StartPosition, Quaternion.identity);
			Enemy enemy = gameObject.GetComponent<Enemy>();

			if(!enemy)
				enemy = gameObject.AddComponent<Enemy>();

			FastPath.DefaultMap.OnUpdate += () =>
			{
				if(enemy != null)
					enemy.FindPath();
			};

			if(enemies == null)
				enemies = new List<Enemy>();
			
			enemies.Add(enemy);

			enemy.findImmediate = findPathImmediate;
			enemy.FindPath();

			return enemy;
		}

		#endregion

		#region Members

		private int currentIndex;

		private Path path;
		private bool findImmediate;

		#endregion

		#region MonoBehaviour Methods

		void Update()
		{
			if(path == null || !path.IsReady)
				return;

			if(!path.ValidPath)
			{
				enemies.Remove(this);
				Object.Destroy(gameObject);
				return;
			}

			if(currentIndex >= path.Length)
			{
				enemies.Remove(this);
				Object.Destroy(gameObject);
				return;
			}

			transform.position = Vector3.MoveTowards(transform.position, new Vector3(path[currentIndex].x, Controller.Instance.EnemyY, path[currentIndex].z), Time.deltaTime * Controller.Instance.Speed);

			if(Vector3.Distance(transform.position, new Vector3(path[currentIndex].x, Controller.Instance.EnemyY, path[currentIndex].z)) < 0.1f)
				currentIndex += 1;
		}

		#endregion

		#region Methods

		private void FindPath()
		{
			path = null;
			Vector3 startPos = new Vector3(transform.position.x, Controller.Instance.EnemyY, transform.position.z);

			System.DateTime start = System.DateTime.Now;

			// This lines actually finds the path. If findImmediate is true it finds it immediately otherwise it puts it in ther que.
			path = findImmediate ? FastPath.FindPathImmediate(startPos, Controller.Instance.EndPosition) : new Path(transform.position, Controller.Instance.EndPosition);

			lastPathTime = (System.DateTime.Now - start).TotalMilliseconds;

			currentIndex = 1;
		}

		#endregion
	}
}