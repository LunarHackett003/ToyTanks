using System.Collections.Generic;
using System.Collections;
using TMPro;
using ToyTanks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace ToyTanks
{
    public class PlayerManager : NetworkBehaviour
    {
        public static Dictionary<ulong, PlayerManager> PlayerManagers = new();

        public VehicleController myTankController;
        public CanvasGroup deadUI;
        public Button spawnButton;
        public NetworkVariable<int> respawnTime = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public TMP_Text respawnCounter;
        public NetworkObject tank;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                respawnTime.OnValueChanged += RespawnTimeChanged;
            }
            if (IsServer)
            {
                respawnTime.Value = 0;
            }
            PlayerManagers.Add(OwnerClientId, this);
            deadUI.gameObject.SetActive(IsOwner);
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PlayerManagers.Remove(OwnerClientId);
        }
        public void TrySpawnPlayer()
        {
            if (MatchManager.Instance != null && respawnTime.Value <= 0)
            {
                MatchManager.Instance.RequestSpawn_RPC(OwnerClientId, myTankController == null);
                spawnButton.interactable = false;
            }
        }
        public void RespawnTimeChanged(int previous, int current)
        {
            if(current > 0)
            {
                respawnCounter.text = $"RESPAWN IN {current}";
                spawnButton.interactable = false;
            }
            else
            {
                respawnCounter.text = $"RESPAWN READY";
                spawnButton.interactable = true;
            }
        }
        public void TankEntityDamaged(PlayerEntity entity)
        {
            deadUI.alpha = entity.Alive ? 1 : 0;
            if (IsServer && entity.CurrentHealth <= 0)
            {
                respawnTime.Value = MatchManager.Instance.maxRespawnTime;
            }
        }
        public IEnumerator RespawnTimer()
        {
            while (respawnTime.Value > 0)
            {
                respawnTime.Value--;
                yield return new WaitForSeconds(1);
            }
        }
    }
}
