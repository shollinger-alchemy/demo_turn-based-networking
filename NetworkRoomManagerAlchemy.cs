using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

/**
* A shell implementation for a match-making lobby, using Mirror's NetworkRoomManager.
* The user will select a formation of several game-characters in the lobby, which the
* manager uses to initialize game-state when the match has started.
*/
public class NetworkRoomManagerAlchemy : NetworkRoomManager
{
    private NetworkRoomPlayerAlchemy networkRoomPlayer;

    private List<CharacterSaveData> selectedFormation;

    public string[] names;

    public override void Awake() {
        base.Awake();
        UISignals.onStartMatchMaking += HandleStartMatchMaking;
        UISignals.onClientEnterRoom += HandleClientEnterRoom;
        UISignals.onFormationUpdated += SetFormation;
    }

    public override void Start() {
        base.Start();

        StartHost();
    }

    public override void OnStartServer() {
        base.OnStartServer();
        Debug.Log("Starting game server...");
    }

    public override void OnStartClient() {
        GameObject.FindObjectOfType<PlayerDataUI>().Init();
    }

    private void HandleStartMatchMaking(MatchContext matchContext) {
        if(NetworkIsReady()) {
            networkRoomPlayer.CmdChangeReadyState(true);
        }
    }

    private void HandleClientEnterRoom(NetworkRoomPlayerAlchemy networkRoomPlayer) {
        this.networkRoomPlayer = networkRoomPlayer;
    }

    private bool NetworkIsReady() {
        bool isReady = NetworkClient.isConnected && NetworkServer.active;
        return isReady;
    }

    /* Called when Mirror changes room to game.  Main hub of data exchange from loadout to game. */
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        NetworkPlayer gamePlayerComponent = gamePlayer.GetComponent<NetworkPlayer>();

        if(gamePlayerComponent == null) {
            return false;
        }
        
        gamePlayerComponent.formation.AddRange(selectedFormation);

        return true;
    }

    public void SetFormation(List<CharacterSaveData> formation) {
        this.selectedFormation = formation;
    }
}
