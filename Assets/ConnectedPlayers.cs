using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ConnectedPlayers : NetworkBehaviour{

    [SyncVar]
    private int PlayersConnected = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void IncrementPlayers()
    {
        PlayersConnected += 1;
    }

    public int GetPlayerCount()
    {
        return PlayersConnected;
    }
}
