using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

    public int _playersToWaitFor = 2;
    GameObject networkManager;
    ConnectedPlayers connectedPlayers;

	// Use this for initialization
	void Start () {
        networkManager = GameObject.Find("NetworkManager");
        connectedPlayers = networkManager.GetComponent<ConnectedPlayers>();
	}

    private void WaitForPlayers(int playersToWaitFor)
    {

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
