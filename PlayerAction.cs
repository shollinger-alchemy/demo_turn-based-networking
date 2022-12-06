using UnityEngine;
using System.Collections.Generic;
using Mirror;

namespace Alchemy{
    /**
    * Represents an action a player can take in the game.  Has a unique ID for referencing, a source of the action, a target of the action, 
    * and a location of where the player selected the action to take place.
    */
    public class PlayerAction {
        public string abilityUUID;
        public NetworkIdentity source;
        public NetworkIdentity requestedTarget;
        public Vector3 targetIndicatorLocation;
    }
}