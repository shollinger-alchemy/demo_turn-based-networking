using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Mirror;

namespace Alchemy {

    /**
    *   Data marshaller for network ingress and egress.
    */
    public class ActionNetworkData {
        public string actionUUID;

        public NetworkIdentity actor;

        public NetworkIdentity target;

        public static ActionNetworkData FromActionContext(ActionContext actionContext) {
            ActionNetworkData actionNetworkData = new ActionNetworkData();
            actionNetworkData.actionUUID = actionContext.action.uuid;
            actionNetworkData.actor = actionContext.actor.netIdentity;
            actionNetworkData.target = actionContext.target.netIdentity;

            return actionNetworkData;
        }

        public async static Task<ActionContext> ToActionContext(ActionNetworkData actionNetworkData) {
            ActionContext actionContext = new ActionContext();
            actionContext.action = await AssetCatalog.GetInstance().FindAssetByUUID<Action>(actionNetworkData.actionUUID);

            actionContext.actor = actionNetworkData.actor.gameObject.GetComponent<GameInteractable>();
            actionContext.target = actionNetworkData.target.gameObject.GetComponent<GameInteractable>();

            return actionContext;
        }
    }

    /**
    *   An impelementation of Mirror's NetworkBehaviour that handles client/server communication and client event signaling.

    *   The general concept is the server owns game-state during the execution of an ability that a game character is performing.
    *   The client from the active player will request the server for a chosen action to take place.  
    *   The server will drive running a series of ActionPhases that make up an Action (eg. Unity Timelines or other sequenced actions), 
    *   so that they will occur in each player's client and game state stays synced across all clients.
    */
    public class NetworkAbilityManager : NetworkBehaviour {
        // Placeholder value for the demo.  This value would be tied to each Action individually in a full implementation.
        private static int BASE_STAT_CHANGE_MIN = -10;  

        private PlayableDirector playableDirector;

        private AbilitySignalReceiver signalReceiver;


        void Awake() {
            // Loosely interfaces with a singleton for event management of server/client signals
            ServerSignals.onPerformAbility += OnPerformAbility;
            Signals.onTimelinePhaseEnd += OnTimelinePhaseEnd;

            playableDirector = GetComponent<PlayableDirector>();

        }

        public void OnTimelinePhaseEnd(ActionContext actionContext) {
            actionContext.actionPhaseIndex++;
            PlayNextActionPhase(actionContext);
        }

        [ClientRpc]
        public async void RpcFireClientAction(ActionNetworkData actionNetworkData) {
           
            ActionContext actionContext = await ActionNetworkData.ToActionContext(actionNetworkData);

            actionContext.action.FireAction(actionContext);
        }

        [Server]
        public void OnPerformAbility(ActionContext actionContext) {
            this.GetComponent<AbilitySignalReceiver>().PrepAbility(actionContext);
            if(actionContext.ability.targetModifiers) {
                actionContext.statChange = CalculateStatChange(actionContext);
                actionContext.target.interactableGameData.ReflectStatChange(actionContext.statChange);
            }

            PlayNextActionPhase(actionContext);
        }

        [Server]
        public void PlayNextActionPhase(ActionContext actionContext) {
            List<ActionPhase> actionPhases = actionContext.ability.actionPhases;
            if(actionPhases != null &&  actionContext.actionPhaseIndex < actionPhases.Count) {
                foreach(Action action in actionPhases[actionContext.actionPhaseIndex].actions) {
                    if(action != null) {
                        actionContext.action = action;

                    if(action.runtimeEnvironment == RuntimeEnvironmentMode.SERVER ||
                        action.runtimeEnvironment == RuntimeEnvironmentMode.BOTH) {
                            action.FireAction(actionContext);
                        }

                        if(action.runtimeEnvironment == RuntimeEnvironmentMode.CLIENT ||
                        action.runtimeEnvironment == RuntimeEnvironmentMode.BOTH) {
                            RpcFireClientAction(ActionNetworkData.FromActionContext(actionContext));   
                        }
                    }
                }
            
            } else {
              OnAbilityComplete(actionContext);
            }
        }


        [Server]
        public void UpdateActionStats(ActionContext actionContext) {
            Stats actionCostStats = new Stats();
            actionCostStats.initalizeStat(TurnManagementSystem.getInstance().actionResourceStat, -1);

            actionContext.actor.interactableGameData.ReflectStatChange(actionCostStats);
        }

        [Server]
        public void OnAbilityComplete(ActionContext actionContext) {
            UpdateActionStats(actionContext);

            TurnBasedActor actor = (TurnBasedActor) actionContext.actor;
            if(actor.hasMoreAbilities()) {
                Signals.PromptAbility(actor);
            } else {
                Signals.TurnComplete(actor);
            } 
        }

        [Server]
        public Stats CalculateStatChange(ActionContext actionContext) {
            Stats statsDiff = new Stats();
            TurnBasedActor actor = (TurnBasedActor) actionContext.actor;

            foreach(StatDefinition.StatValue statValueContainer in actionContext.ability.targetModifiers.statValues) {
                int value = statValueContainer.statValue + Random.Range(BASE_STAT_CHANGE_MIN, 0);

                statsDiff.initalizeStat(statValueContainer.statDefinition, value);
            }

           return statsDiff;
        }
    }

}