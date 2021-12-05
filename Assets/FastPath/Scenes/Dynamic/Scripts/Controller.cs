using UnityEngine;
using FastPath;

using Show = UnityEngine.SerializeField;

namespace FastPath.Demos.Dynamic
{
	public class Controller : MonoBehaviour
	{
		#region Static Members

		private static Controller instance;

		#endregion

		#region Static Properties

		public static Controller Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion

		#region Memebers

		private MeshRenderer ObstacleRenderer;

		#endregion

		#region Inspector

		[Show] Generator.Config config;

		[Header("Extra")]
		[Show] float enemyY;
		[Show] Collider Obstacle;
		[Show] bool DrawPath;
		[Show] bool DrawMap;
		[Show] float speed;
		[Show] GameObject enemyObject;
		[Show] float spawnTime;
		[Show] Vector3 Start;
		[Show] Vector3 End;
		[Show] UnityEngine.UI.Text msText;
		[Show] UnityEngine.UI.Text tileSizeText;
		[Show] UnityEngine.UI.Text totalNodes;
		[Show] UnityEngine.UI.Text EstimateAggressionText;
		[Show] UnityEngine.UI.Text invalidPathIndicator;

		#endregion

		#region Properties

		public UnityEngine.UI.Text InvalidPathIndicator
		{
			get
			{
				return invalidPathIndicator;
			}
		}

		public float EnemyY
		{
			get
			{
				return enemyY;
			}
		}

		public float SpawnTime
		{
			get
			{
				return spawnTime;
			}
			set
			{
				spawnTime = value;
			}
		}

		public Vector3 StartPosition
		{
			get
			{
				return Start;	
			}
			set
			{
				Start = value;
			}
		}

		public Vector3 EndPosition
		{
			get
			{
				return End;
			}
			set
			{
				End = value;
			}
		}

		public GameObject EnemyObject
		{
			get
			{
				return enemyObject;
			}
		}

		public float Speed
		{
			get
			{
				return speed;
			}
			set
			{
				speed = value;
			}
		}

		public float TileSize
		{
			get
			{
				return config.TileSize.x = config.TileSize.y;
			}
			set
			{
				config.TileSize.x = config.TileSize.y = value;
			}
		}


		public float EstimateAggression
		{
			get
			{
				return FastPath.DefaultEstimateAggression;
			}
			set
			{
				FastPath.DefaultEstimateAggression = value;
			}
		}

		#endregion

		#region Methods

		public void Reset()
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
		}

		public void Regen()
		{
			FastPath.DefaultMap = FastPath.Generate(config); // Generates the map which contains nodes (walkalbe/non-walkable points).	Path will be found based on this map.
			totalNodes.text = (FastPath.DefaultMap.TilesX * FastPath.DefaultMap.TilesX).ToString();
		}

		#endregion

		#region Monobehaviour Methods

		//1123200 Nodes!

		void Awake()
		{
			instance = this;
			Regen();
			StartCoroutine(GenerateEnemy());
			ObstacleRenderer = Obstacle.GetComponent<MeshRenderer>();
		}

		void Update()
		{
			tileSizeText.text = TileSize.ToString("F1");
			EstimateAggressionText.text = EstimateAggression.ToString("F2");

			Path path = FastPath.FindPathImmediate(Start, End);

			if(path.ValidPath)
				InvalidPathIndicator.gameObject.SetActive(false);
			else
				InvalidPathIndicator.gameObject.SetActive(true);

			if(DrawMap)
				FastPath.DrawMapInEditor(FastPath.DefaultMap);	

			RaycastHit hit;
			if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
			{
				if(hit.collider.tag != "Enemy")// && (hit.collider.tag != "NonWalkable" || hit.collider.gameObject == Obstacle.gameObject))
				{
					Obstacle.transform.position = new Vector3(hit.point.x, enemyY, hit.point.z);

					Path p = FastPath.FindPathImmediate(Start, End, FastPath.IndexesBetween(FastPath.DefaultMap, Obstacle.bounds.min, Obstacle.bounds.max));

					if(p.ValidPath)
					{
						ObstacleRenderer.material.color = Color.green;
						if(Input.GetMouseButtonDown(0))
						{
							GameObject instance = (GameObject) Object.Instantiate(Obstacle.gameObject, new Vector3(hit.point.x, enemyY, hit.point.z), Quaternion.identity);
							instance.GetComponent<MeshRenderer>().material = new Material(ObstacleRenderer.material);
							FastPath.Update(FastPath.DefaultMap, FastPath.IndexesBetween(FastPath.DefaultMap, Obstacle.bounds.min, Obstacle.bounds.max));
						}
					}
					else
						ObstacleRenderer.material.color = Color.red;
					if(DrawPath)
						p.DrawPath();
				}
				else
					ObstacleRenderer.material.color = Color.red;
			}
		}

		#endregion

		#region Coroutines

		System.Collections.IEnumerator GenerateEnemy()
		{
			while (true)
			{
				yield return new WaitForSeconds(spawnTime);
				Enemy.Create(); // Creates an enemy
				msText.text = Enemy.LastPathTime + "ms";
			}
		}

		#endregion
	}
}