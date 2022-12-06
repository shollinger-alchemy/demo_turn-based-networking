using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Mirror;
using Alchemy.Persistence.DynamoDb;
using UnityEngine.AI;


namespace Alchemy {
    public class CharacterList : SyncList<CharacterSaveData> {
    }

    /**
    * Represents a connected player in the game network.  Drives network communication for initializing a player on each client and sequencing a player's turn.
    * Performs input validation before user requests are forwarded to other areas of the server.
    */
    public class NetworkPlayer : NetworkBehaviour {
        
        public CharacterActor characterPrefab;

        private List<StartingZone> startingZones;


        // Game Data
        public CharacterList formation = new CharacterList();
        private StartingZone assignedStartingZone;
        
        [SyncVar]
        public bool isActivePlayer = false;

        private int charactersSpawned = 0;

        void Awake() {
            Signals.onRequestAbility += OnRequestAbility;
            Signals.onPromptAbility += OnPromptAbility;
            ServerSignals.onTurnStarted += OnTurnStarted;
        }

        void Start() {
            if(isServer) {
                startingZones = GameObject.FindObjectsOfType<StartingZone>().ToList();
            }
        }

        [Client]
        public override void OnStartLocalPlayer() {
           CmdInitializePlayer();
        }

       [Client]
       public void OnRequestAbility(ActionContext actionContext) {
           PlayerAction playerAction = new PlayerAction();

           // Init network information re: player action
           playerAction.abilityUUID = actionContext.ability.uuid;
           playerAction.source = actionContext.actor.netIdentity;
           playerAction.requestedTarget = actionContext.target != null ? actionContext.target.netIdentity : null;
           playerAction.targetIndicatorLocation = actionContext.selectedPoint;

           CmdRequestPlayerAction(playerAction);
       }

       [Client]
       public void OnPromptAbility(GameInteractable gameInteractable) {
           if(isActivePlayer) {
               RadialMenuSpawner.instance.SpawnMenu(gameInteractable);
           }
        }

       [ClientRpc]
       public void RpcCharacterSpawned(NetworkIdentity character) {
           CharacterActor characterActor = character.gameObject.GetComponent<CharacterActor>();
           GameManagementSystem.getInstance().gameInteractables.Add(characterActor);

           charactersSpawned++;

           if(charactersSpawned == formation.Count) {
               CmdPlayerClientInitialized();
           }
       }

       [ClientRpc]
       public void RpcTurnStarted(NetworkIdentity character) {
           UISignals.TurnStarted(character.GetComponent<TurnBasedActor>(), isActivePlayer);
       }

       [Command]
       public void CmdInitializePlayer() {           
           assignedStartingZone = AssignStartingZone();

           foreach(CharacterSaveData characterSaveData in formation) {
                SpawnCharacterActor(characterSaveData);
           }

       }

       [Command]
       public async void CmdRequestPlayerAction(PlayerAction playerAction) {
          ActionContext actionContext = await ActionContext.FromPlayerAction(playerAction);

          if(ValidActionRequest()) {
            ServerSignals.PerformAbility(actionContext);
          }
          
       }

       [Command]
       public void CmdPlayerClientInitialized() {
           ServerSignals.PlayerInitialized(this.netIdentity);
       }

       [Server]
       public void OnTurnStarted(TurnBasedActor actor) {
           isActivePlayer = actor.controllingPlayer.netId == this.netId;

           RpcTurnStarted(actor.netIdentity);
       }

       [Server]
       private bool ValidActionRequest() {
           return isActivePlayer;
       }

  
       [Server]
       private async void SpawnCharacterActor(CharacterSaveData characterSaveData) {
           CharacterActor spawned = Instantiate(characterPrefab.gameObject).GetComponent<CharacterActor>();
           spawned.transform.position = assignedStartingZone.GetCharacterActorLocation(spawned);
           spawned.controllingPlayer = this.netIdentity;

           spawned.transform.LookAt(GameObject.FindObjectOfType<Objective>().transform);

           spawned.GetComponent<NavMeshAgent>().enabled = true;

           await spawned.Init(characterSaveData);
        
           NetworkServer.Spawn(spawned.gameObject);

           Signals.ActorAdded(spawned);

           RpcCharacterSpawned(spawned.netIdentity);
                   
       }

       [Server]
       private StartingZone AssignStartingZone() {
           int chosenIndex = Random.Range(0, startingZones.Count - 1);
         
           StartingZone chosen = startingZones[chosenIndex];
           startingZones.RemoveAt(chosenIndex);

           return chosen;
       }
    }
}