using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	#region variables
	public static GameManager instance;

	public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();

	[SerializeField] private GameObject localPlayerPrefab;
	[SerializeField] private GameObject playerPrefab;
	#endregion

	private void Awake()
	{
		if (instance == null)
			instance = this;
		else
			Destroy(this);
	}

	public void SpawnPlayer(int id, Vector3 position, Quaternion rotation)
	{
		GameObject player;
		if (id == Client.instance.myId)
			player = Instantiate(localPlayerPrefab, position, rotation);
		else
			player = Instantiate(playerPrefab, position, rotation);

		player.GetComponent<PlayerManager>().id = id;
		players.Add(id, player.GetComponent<PlayerManager>());
	}
}
