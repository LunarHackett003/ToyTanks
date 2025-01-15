using System.Collections.Generic;
using ToyTanks;
using Unity.Netcode;
using UnityEngine;

namespace ToyTanks
{
    public class MatchManager : NetworkBehaviour
    {

        public static MatchManager Instance;

        public NetworkVariable<Dictionary<ulong, uint>> clientsOnTeams = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public int numberOfTeamsAllowed;
        public NetworkVariable<Dictionary<int, int>> teamScores = new(new(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkList<int> playersOnTeam = new(new int[20], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public SpawnpointHolder spawnpointHolder;

        public int maxRespawnTime = 3;
        public override void OnNetworkSpawn()
        {
            Instance = this;
            base.OnNetworkSpawn();
            NetworkManager.OnConnectionEvent += ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

            TeamsChanged(new(), clientsOnTeams.Value);
        }
        public override void OnNetworkDespawn()
        {
            NetworkManager.OnConnectionEvent -= ConnectionEvent;
            NetworkManager.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;

            Instance = null;
            base.OnNetworkDespawn();
        }

        public void TeamsChanged(Dictionary<ulong, uint> previous, Dictionary<ulong, uint> current)
        {
            playersOnTeam.Clear();
            for (int i = 0; i < numberOfTeamsAllowed; i++)
            {
                playersOnTeam.Add(0);
            };
            print("Updating teams!");
            foreach (var item in current)
            {
                print($"found a player on team {item.Value}");
                playersOnTeam[(int)item.Value]++;
            }
            for (int i = 0; i < numberOfTeamsAllowed; i++)
            {

            }
        }


        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if(sceneEvent.SceneEventType == SceneEventType.LoadComplete)
            {
                if (IsServer)
                {
                    spawnpointHolder = FindAnyObjectByType<SpawnpointHolder>();
                    SetPlayerTeam(sceneEvent.ClientId);
                }
            }
        }
        [Rpc(SendTo.Server)]
        public void RequestSpawn_RPC(ulong clientID, bool firstSpawn)
        {
            //if (PlayerManager.playersByID.TryGetValue(clientID, out PlayerManager p))
            //{
            //    if (p.LivingPlayer == null)
            //    {
            //        p.LivingPlayer = NetworkManager.SpawnManager.InstantiateAndSpawn(p.playerPrefab, clientID).GetComponent<PlayerEntity>();
            //    }
            //    p.LivingPlayer.currentHealth.Value = p.LivingPlayer.MaxHealth;
            //    p.timeUntilSpawn.Value = maxRespawnTime;
            //    if (!revived)
            //    {
            //        (Vector3 pos, Quaternion rot) = spawnpointHolder.FindSpawnpoint(p.teamIndex.Value);

            //        p.LivingPlayer.Teleport_RPC(pos, Quaternion.identity);

            //        p.SpawnPlayer_RPC();
            //    }
            //    else
            //    {
            //        p.LivingPlayer.Teleport_RPC(position, Quaternion.identity);
            //        p.SpawnPlayer_RPC();
            //    }
            //}
            PlayerManager pm = PlayerManager.PlayerManagers[OwnerClientId];
            if (firstSpawn)
            {
                pm.myTankController = NetworkManager.SpawnManager.InstantiateAndSpawn(pm.tank, clientID, false, false, false).GetComponent<TankController>();
            }
            pm.myTankController.FindSpawnAndTeleport_RPC();
        }

        void SetPlayerTeam(ulong clientID)
        {
            uint team = FindSmallestTeam();
            if (clientsOnTeams.Value.TryAdd(clientID, team))
            {
                print($"added client {clientID} to {team}");
            }
            else
            {
                print($"failed to add client {clientID} to team {team}");
            }

            NetworkObject n = NetworkManager.ConnectedClients[clientID].PlayerObject;
            TeamsChanged(clientsOnTeams.Value, clientsOnTeams.Value);
        }
        private void ConnectionEvent(NetworkManager manager, ConnectionEventData eventData)
        {
            if (!IsServer)
                return;

            if(eventData.EventType == Unity.Netcode.ConnectionEvent.ClientConnected)
            {

            }
            else if(eventData.EventType == Unity.Netcode.ConnectionEvent.ClientDisconnected)
            {
                if (clientsOnTeams.Value.ContainsKey(eventData.ClientId))
                {
                    clientsOnTeams.Value.Remove(eventData.ClientId);
                }
            }
        }
        uint FindSmallestTeam()
        {
            if(NetworkManager.ConnectedClients.Count == 0)
            {
                return 0;
            }
            else
            {
                //Where key is the team and value is the number of players on the team
                Dictionary<uint, uint> playersOnTeams = new();
                uint smallestTeamIndex = 0;
                uint smallestTeamPlayers = 100;
                for (uint i = 0; i < numberOfTeamsAllowed; i++)
                {
                    playersOnTeams.Add(i, 0);
                }
                foreach (KeyValuePair<ulong, uint> item in clientsOnTeams.Value)
                {
                    playersOnTeams[item.Value]++;
                }
                for (uint i = 0; i < numberOfTeamsAllowed; i++)
                {
                    if (playersOnTeams[i] < smallestTeamPlayers)
                    {
                        smallestTeamIndex = i;
                        smallestTeamPlayers = playersOnTeams[i];
                    }
                }
                return smallestTeamIndex;
            }
        }
        float specialSyncTime;
        bool syncingSpecialTime;
        private void FixedUpdate()
        {
            if (!IsHost && !IsServer)
                return;

            specialSyncTime += Time.fixedDeltaTime;
            if (specialSyncTime > 10)
            {
                specialSyncTime = 0;
                syncingSpecialTime = true;
            }
            else
                syncingSpecialTime = false;
        }
    }
}
