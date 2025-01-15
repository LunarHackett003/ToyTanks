using Unity.Netcode;
using UnityEngine;
using ToyTanks;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
namespace ToyTanks
{
    [RequireComponent(typeof(PlayerEntity))]
    public class TankController : NetworkBehaviour
    {
        public static Dictionary<ulong, TankController> TankControllers = new();

        public PlayerEntity playerEntity;

        public Rigidbody rootRB;

        public HingeJoint turretAzimuth, turretPitch;
        public Transform barrel;

        public Transform cameraTransform;

        public AnimationCurve turretRecoilCurve;
        public float turretRecoilSpeed;
        public Vector3 turretRecoilStartPos, turretRecoilRearPos;

        public float timeBetweenShots;
        bool fireBlocked;

        public ParticleSystem muzzleEffect;
        public CinemachineCamera cam;

        [System.Serializable]
        public struct Wheel
        {
            public WheelCollider wheel;
            public Transform transform;
        }

        public Wheel[] leftWheels, rightWheels;
        public float driveTorque, brakeTorque;
        public float maxTurnTorque;

        public ControlScheme controls;

        bool brakeInput, fireInput;
        Vector2 lookInput, moveInput;

        public Vector2 aimAngle;
        public Vector2 aimPitchClamp;
        JointSpring azimuthSpring, pitchSpring;
        public float aimSpring, aimDamper;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            turretRecoilStartPos = barrel.localPosition;
            TankControllers.Add(OwnerClientId, this);
            rootRB.isKinematic = true;
            if (IsOwner)
            {
                InitialiseInput();

                azimuthSpring = new JointSpring()
                {
                    damper = aimDamper,
                    spring = aimSpring
                };
                pitchSpring = new JointSpring()
                {
                    damper = aimDamper,
                    spring = aimSpring
                };
            }
            else
            {
                cam.enabled = false;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            TankControllers.Remove(OwnerClientId);
        }
        [Rpc(SendTo.Owner)]
        public void FindSpawnAndTeleport_RPC()
        {
            SpawnpointHolder sph = FindFirstObjectByType<SpawnpointHolder>();
            if(sph != null)
            {
                (Vector3 pos, Quaternion rot) = sph.FindSpawnpoint(MatchManager.Instance.clientsOnTeams.Value[OwnerClientId]);
                playerEntity.Teleport_RPC(pos + Vector3.up, rot);
                rootRB.isKinematic = false;
                rootRB.linearVelocity = Vector3.zero;
                rootRB.angularVelocity = Vector3.zero;
                rootRB.Move(pos, rot * Quaternion.Euler(90, 0, 0));
            }
        }
        void InitialiseInput()
        {
            controls = new();
            controls.Enable();

            controls.Player.Fire.performed += Fire_performed;
            controls.Player.Fire.canceled += Fire_performed;

            controls.Player.Brake.performed += Brake_performed;
            controls.Player.Brake.canceled += Brake_performed;

            controls.Player.Move.performed += Move_performed;
            controls.Player.Move.canceled += Move_performed;

            controls.Player.LookMouse.performed += LookMouse_performed;
            controls.Player.LookMouse.canceled += LookMouse_performed;

            controls.Player.LookGamepad.performed += LookGamepad_performed;
            controls.Player.LookGamepad.canceled += LookGamepad_performed;
        }

        private void LookGamepad_performed(InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();

            aimAngle.y += lookInput.x * PlayerSettings.Instance.settingsContainer.padLookSpeedX * Time.deltaTime;
            aimAngle.x += lookInput.y * PlayerSettings.Instance.settingsContainer.padLookSpeedY * Time.deltaTime;
            aimAngle.x = Mathf.Clamp(aimAngle.x, -85, 85);
        }

        private void LookMouse_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();

            aimAngle.y += lookInput.x * PlayerSettings.Instance.settingsContainer.mouseLookSpeedX * Time.deltaTime;
            aimAngle.x += lookInput.y * PlayerSettings.Instance.settingsContainer.mouseLookSpeedY * Time.deltaTime;
            aimAngle.x = Mathf.Clamp(aimAngle.x, -85, 85);
        }

        private void Move_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            moveInput = obj.ReadValue<Vector2>();
        }

        private void Brake_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            brakeInput = obj.ReadValueAsButton();
        }
        private void Fire_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            fireInput = obj.ReadValueAsButton();
        }
        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            azimuthSpring.targetPosition = cameraTransform.eulerAngles.y;
            pitchSpring.targetPosition = Mathf.Clamp(aimAngle.x, aimPitchClamp.x, aimPitchClamp.y);

            turretAzimuth.spring = azimuthSpring;
            turretPitch.spring = pitchSpring;
            foreach (var item in leftWheels)
            {
                item.wheel.motorTorque = (moveInput.y * driveTorque) + (moveInput.x * maxTurnTorque);
            }
            foreach (var item in rightWheels)
            {
                item.wheel.motorTorque = (moveInput.y * driveTorque) - (moveInput.x * maxTurnTorque);
            }
            if (fireInput && !fireBlocked)
            {
                TryFire();
                fireInput = false;
            }
        }
        private void Update()
        {
            cameraTransform.rotation = Quaternion.Euler(aimAngle.x, aimAngle.y, 0);
        }

        void TryFire()
        {
            StartCoroutine(FireDelay());
            FireOnServer_RPC();
            FireOnClient_RPC();
        }

        [Rpc(SendTo.Server)]
        void FireOnServer_RPC()
        {

        }
        [Rpc(SendTo.ClientsAndHost)]
        void FireOnClient_RPC()
        {
            StartCoroutine(ReciprocateBarrel());
            if(muzzleEffect != null)
            {
                muzzleEffect.Play(true);
            }
        }
        IEnumerator ReciprocateBarrel()
        {
            float t = 0;
            while (t > 1)
            {
                t += Time.fixedDeltaTime * turretRecoilSpeed;
                barrel.localPosition = Vector3.Lerp(turretRecoilStartPos, turretRecoilRearPos, turretRecoilCurve.Evaluate(t));
                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator FireDelay()
        {
            fireBlocked = true;
            yield return new WaitForSeconds(timeBetweenShots);
            fireBlocked = false;
            yield break;
        }
    }
}
