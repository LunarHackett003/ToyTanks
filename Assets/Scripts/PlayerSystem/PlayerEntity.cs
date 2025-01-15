using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace ToyTanks
{
    public class PlayerEntity : HealthyEntity
    {
        public NetworkTransform netTransform;

        public Outline outlineComponent;
        public CharacterRenderable cr;

        public delegate void OnHealthChanged(PlayerEntity entity);
        public OnHealthChanged onHealthChanged;


        public NetworkVariable<bool> stunned = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<bool> burning = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public bool Alive => CurrentHealth > 0;

        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, incomingCritMultiply);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply);
        }
        public override void RestoreHealth(float healthIn, ulong sourceClientID)
        {
            base.RestoreHealth(healthIn, sourceClientID);
        }
        [Rpc(SendTo.Owner)]
        public void Teleport_RPC(Vector3 pos, Quaternion rot)
        {
            netTransform.Teleport(pos, rot, Vector3.one);
            
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            cr.InitialiseViewable(this);
            if (IsOwner || IsServer)
            {
                onHealthChanged += PlayerManager.PlayerManagers[OwnerClientId].TankEntityDamaged;
                currentHealth.OnValueChanged += HealthChanged;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsOwner)
            {
                onHealthChanged -= PlayerManager.PlayerManagers[OwnerClientId].TankEntityDamaged;
            }
        }
        void HealthChanged(float previous, float current)
        {
            onHealthChanged?.Invoke(this);
        }
    }
}
