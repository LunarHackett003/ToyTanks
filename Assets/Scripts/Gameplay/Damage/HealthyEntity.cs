using Unity.Netcode;
using UnityEngine;

namespace ToyTanks
{
    public class HealthyEntity : Entity
    {

        [SerializeField] float maxHealth;
        public float MaxHealth => maxHealth;
        /// <summary>
        /// Current Health is only modifiable by the server.
        /// </summary>
        public NetworkVariable<float> currentHealth = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public float CurrentHealth => currentHealth.Value;
        public bool healable;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                currentHealth.Value = maxHealth;
            }
            
        }

        public override void ReceiveDamage(float damageIn, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, incomingCritMultiply);
        }
        public override void ReceiveDamage(float damageIn, ulong sourceClientID, float incomingCritMultiply)
        {
            base.ReceiveDamage(damageIn, sourceClientID, incomingCritMultiply);
            currentHealth.Value -= damageIn;

            if (scoreBehaviour != 0)
            {
                uint value = (uint)Mathf.RoundToInt(damageIn * 10);
                //PlayerManager target;
                switch (scoreBehaviour)
                {
                    case ScoreAwardingBehaviour.none:
                        break;
                    case ScoreAwardingBehaviour.sourceCombat:
                        break;
                    case ScoreAwardingBehaviour.sourceSupport:
                        break;
                    case ScoreAwardingBehaviour.ownerCombat:
                        break;
                    case ScoreAwardingBehaviour.ownerSupport:
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Restoring health to an entity will ALWAYS reward the one who performed the action with support points.
        /// </summary>
        /// <param name="healthIn"></param>
        /// <param name="sourceClientID"></param>
        public virtual void RestoreHealth(float healthIn, ulong sourceClientID)
        {
            currentHealth.Value += healthIn;
            //uint value = (uint)Mathf.RoundToInt(healthIn * 10);
        }
    }
}
