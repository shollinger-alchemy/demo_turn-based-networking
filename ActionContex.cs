using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Alchemy;

/**
* Stores metadata and simple state information about a player's action.
*/
public class ActionContext : ScriptableObject
{
   public GameInteractable actor;
   public GameInteractable target;

   public Vector3 selectedPoint;
   public Action action;

   public Ability ability;
   public Stats statChange;

   public int actionPhaseIndex = 0;

/**
* Used by the server and each client to look up what another client's action should do.
*/
   public async static Task<ActionContext> FromPlayerAction(PlayerAction playerAction) {
      ActionContext context = new ActionContext();

      context.ability = await AssetCatalog.GetInstance().FindAssetByUUID<Ability>(playerAction.abilityUUID);
      context.actor = playerAction.source.gameObject.GetComponent<GameInteractable>();
      context.target = playerAction.requestedTarget != null? playerAction.requestedTarget.gameObject.GetComponent<GameInteractable>() : null;
      context.selectedPoint = playerAction.targetIndicatorLocation;

      return context;
   }

}