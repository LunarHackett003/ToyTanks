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
    public class VehicleController : NetworkBehaviour
    {
        public static Dictionary<ulong, VehicleController> VehicleControllers = new();

        public PlayerEntity playerEntity;

        public Rigidbody rootRB;

        public Transform turretAzimuth, turretPitch;

        public Transform cameraTransform;

        public List<Cannon> cannons;

        public float timeBetweenShots;
        bool fireBlocked;

        public CinemachineCamera cam;


        public float maxTurnAngle, turnSpeed, driveTorque, idleBrakeTorque, idleBrakeVelocityThreshold, eBrakeTorque;
        public bool idleBrake;
        float turnAngle;
        public ControlScheme controls;
        
        bool eBrakeInput, fireInput, secondaryInput;
        Vector2 lookInput, moveInput;

        public Vector2 aimAngle;
        public Vector2 aimPitchClamp;
        public float aimPitchOffset;
        public Vector2 targetAimAngle;
        public float maxTurretTurnSpeed;
        public float airSpinPower;
        [System.Serializable]
        public struct Wheel
        {
            public WheelCollider wheel;
            public Transform wheelVis;
            public Quaternion startRotation;
        }
        public Wheel[] frontWheels, rearWheels;

        public int cannonIndex;
        public bool onGround;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            VehicleControllers.Add(OwnerClientId, this);
            rootRB.isKinematic = true;
            for (int i = 0; i < frontWheels.Length; i++)
            {
                Wheel item = frontWheels[i];
                item.startRotation = item.wheelVis.rotation;
                frontWheels[i] = item;
            }
            for (int i = 0; i < rearWheels.Length; i++)
            {
                Wheel item = rearWheels[i];
                item.startRotation = item.wheelVis.rotation;
                rearWheels[i] = item;
            }
            if (IsOwner)
            {
                InitialiseInput();
            }
            else
            {
                cam.enabled = false;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            VehicleControllers.Remove(OwnerClientId);
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
                rootRB.Move(pos, rot);
                PlayerManager.PlayerManagers[OwnerClientId].deadUI.alpha = 0;
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

            controls.Player.SecondaryInput.performed += SecondaryInput_performed;
            controls.Player.SecondaryInput.canceled += SecondaryInput_performed;
        }

        private void SecondaryInput_performed(InputAction.CallbackContext obj)
        {
            secondaryInput = obj.ReadValueAsButton();
        }

        private void LookGamepad_performed(InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();

            aimAngle.y += lookInput.x * PlayerSettings.Instance.settingsContainer.padLookSpeedX * Time.deltaTime;
            aimAngle.x += lookInput.y * PlayerSettings.Instance.settingsContainer.padLookSpeedY * Time.deltaTime;
            aimAngle.x = Mathf.Clamp(aimAngle.x, -45, 45);
        }

        private void LookMouse_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            lookInput = obj.ReadValue<Vector2>();

            aimAngle.y += lookInput.x * PlayerSettings.Instance.settingsContainer.mouseLookSpeedX * Time.deltaTime;
            aimAngle.x += lookInput.y * PlayerSettings.Instance.settingsContainer.mouseLookSpeedY * Time.deltaTime;
            aimAngle.x = Mathf.Clamp(aimAngle.x, -45, 45);
        }

        private void Move_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            moveInput = obj.ReadValue<Vector2>();
        }

        private void Brake_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            eBrakeInput = obj.ReadValueAsButton();
        }
        private void Fire_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            fireInput = obj.ReadValueAsButton();
        }
        private void FixedUpdate()
        {
            if (IsOwner)
            {
                targetAimAngle = new()
                {
                    x = Mathf.MoveTowardsAngle(targetAimAngle.x, cameraTransform.eulerAngles.y - rootRB.rotation.eulerAngles.y, maxTurretTurnSpeed * Time.fixedDeltaTime),
                    y = Mathf.MoveTowards(targetAimAngle.y, -Mathf.Clamp(aimAngle.x, aimPitchClamp.x, aimPitchClamp.y), maxTurretTurnSpeed * Time.fixedDeltaTime)
                };
                if (targetAimAngle.x > 179.99f)
                {
                    targetAimAngle.x -= 360;
                }
                if(targetAimAngle.x < -179.99f)
                {
                    targetAimAngle.x += 360;
                }

                turretAzimuth.localEulerAngles = new(0, targetAimAngle.x);
                turretPitch.localEulerAngles = new(-targetAimAngle.y + aimPitchOffset, 0);
                idleBrake = Mathf.Approximately(moveInput.y, 0) && rootRB.linearVelocity.magnitude < idleBrakeVelocityThreshold;

                onGround = false;

                turnAngle = Mathf.MoveTowards(turnAngle, moveInput.x * maxTurnAngle, turnSpeed * Time.fixedDeltaTime);
                foreach (var item in frontWheels)
                {
                    if(item.wheel.GetGroundHit(out WheelHit hit))
                    {
                        onGround |= hit.collider != null;
                        item.wheel.steerAngle = turnAngle;
                        item.wheel.brakeTorque = eBrakeInput ? eBrakeTorque : (idleBrake ? idleBrakeTorque : 0);
                        item.wheel.motorTorque = driveTorque * moveInput.y;
                    }
                    item.wheel.GetWorldPose(out Vector3 pos, out Quaternion rot);
                    item.wheelVis.SetPositionAndRotation(pos, rot * item.startRotation);
                }
                foreach (var item in rearWheels)
                {
                    if (item.wheel.GetGroundHit(out WheelHit hit))
                    {
                        onGround |= hit.collider != null;
                        item.wheel.motorTorque = driveTorque * moveInput.y * 0.25f;
                    }
                    item.wheel.GetWorldPose(out Vector3 pos, out Quaternion rot);
                    item.wheelVis.SetPositionAndRotation(pos, rot * item.startRotation);
                }
                
                if(!onGround)
                {
                    rootRB.AddTorque((airSpinPower * moveInput.y * rootRB.transform.right) + (airSpinPower * moveInput.x * rootRB.transform.up));
                }
                if (fireInput && !fireBlocked)
                {
                    TryFire();
                    fireInput = false;
                }
                if (cannons.Count > 0)
                {
                    for (int i = 0; i < cannons.Count; i++)
                    {
                        cannons[i].aiming = secondaryInput && i == cannonIndex;
                    }
                }
            }
        }
        private void Update()
        {
            aimAngle.y = Mathf.Repeat(aimAngle.y, 360);
            cameraTransform.rotation = Quaternion.Euler(aimAngle.x, aimAngle.y, 0);
        }

        void TryFire()
        {
            StartCoroutine(FireDelay());
            cannons[cannonIndex].Fire();
            cannonIndex++;
            cannonIndex %= cannons.Count;
            print($"Fired tank!");
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
