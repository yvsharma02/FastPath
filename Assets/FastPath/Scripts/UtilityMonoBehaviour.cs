using UnityEngine;

namespace FastPath
{
	public class UtilityMonoBehaviour : MonoBehaviour
	{
		#region Static Members

		private static UtilityMonoBehaviour instance;

		#pragma warning disable 0414

		private static Coroutine runner;

		#pragma warning restore 0414

		private static System.Collections.Generic.Queue<System.Collections.IEnumerator> Que;

		#endregion

		#region Static Methods

		public static void Start(System.Collections.IEnumerator coroutine)
		{
			instance.StartCoroutine(coroutine);
		}

		public static void Enque(System.Collections.IEnumerator coroutine)
		{
			Que.Enqueue(coroutine);
		}

		public static void CreateInstance()
		{
			if(instance == null)
			{
				instance = new GameObject().AddComponent<UtilityMonoBehaviour>();
				instance.gameObject.name = "Fast Path";
				Que = new System.Collections.Generic.Queue<System.Collections.IEnumerator>();
				runner = instance.StartCoroutine(FakeUpdate());
			}
		}

		private static System.Collections.IEnumerator FakeUpdate()
		{
			while (true)
			{
				while (Que.Count > 0)
					instance.StartCoroutine(Que.Dequeue());

				yield return null;
			}
		}

		#endregion
	}
}