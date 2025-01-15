using Unity.Netcode;
using UnityEngine;

namespace ToyTanks
{
    public class PlayerMotorV2 : NetworkBehaviour
    {
        public Rigidbody rb;
        public Transform headTransform;
        public PlayerEntity entity;
        Vector2 oldAimAngle, aimAngle, aimDelta;

        Vector3 lookSwayPos, lookSwayEuler, v_lookswaypos, v_lookswayeuler;
        //Sway and rotation based on movement
        Vector3 moveSwayPos, moveSwayEuler, v_moveswaypos, v_moveswayeuler;
        Quaternion swayInitialRotation;
        public Vector3 lookSwayPosScale, lookSwayEulerScale;
        public float lookSwayPosDampTime, lookSwayEulerDampTime, maxLookSwayPos, maxLookSwayEuler;

        public Transform weaponOffset;
        public MovementState moveState;
        Vector3 moveVec;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
            

        }

        private void Update()
        {
            if (IsOwner && entity.Alive)
            {
                UpdateAim();
            }   
        }
        private void FixedUpdate()
        {
            if(IsOwner)
            {
                rb.isKinematic = !entity.Alive;
                if (entity.Alive)
                {
                    MovePlayer();
                }
            }
        }

        void UpdateAim()
        {
            oldAimAngle = aimAngle;
            if (Vector2.one != Vector2.zero)
            {
                //aimAngle += entity.playerManager.lookInput * new Vector2(PlayerSettings.Instance.settingsContainer.mouseLookSpeedX, PlayerSettings.Instance.settingsContainer.mouseLookSpeedY) * Time.deltaTime;
                aimAngle.y = Mathf.Clamp(aimAngle.y, -85f, 85f);
                
                if (headTransform)
                {
                    headTransform.localRotation = Quaternion.Euler(-aimAngle.y, 0, 0);
                }
                transform.localRotation = Quaternion.Euler(0, aimAngle.x, 0);
            }
            aimDelta = oldAimAngle - aimAngle;
            aimDelta.x %= 360;
            aimAngle.x %= 360;
        }

        void MovePlayer()
        {

        }
    }
}
