using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Alchemy
{
    /**
    * Represents a player in a match-making game lobby.  Uses hooks from NetworkRoomPlayer to
    * mutate player state during match setup.
    */
    public class DemoNetworkRoomPlayer : NetworkRoomPlayer
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(NetworkRoomPlayerAlchemy));

        public List<CharacterSaveData> formation;

        public  void Awake() {
            UISignals.onFormationUpdated += SetFormation;
        }

        public void SetFormation(List<CharacterSaveData> formation) {
            this.formation = formation;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        public override void OnClientEnterRoom()
        {
            UISignals.ClientEnterRoom(this);
        }

    }
}
