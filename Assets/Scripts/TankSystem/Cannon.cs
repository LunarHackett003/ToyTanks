using System.Collections;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace ToyTanks
{
    public class Cannon : NetworkBehaviour
    {
        public AnimationCurve turretRecoilCurve;
        public float turretBarrelRecip;
        public float turretRecoilSpeed;
        public Vector3 turretRecoilStartPos, turretRecoilRearPos;
        public Transform barrel;
        public ParticleSystem muzzleEffect;
        public bool aiming;
        public LineRenderer trajectoryRenderer;
        public float launchVelocity;
        public Transform launchOrigin;
        public bool projectileHasGravity;
        public int maxIterations = 20;
        Vector3[] linePositions;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            turretRecoilStartPos = barrel.localPosition;

            VehicleController.VehicleControllers[OwnerClientId].cannons.Add(this);
            linePositions = new Vector3[maxIterations + 1];
        }
        public override void OnNetworkDespawn()
        {
            VehicleController.VehicleControllers[OwnerClientId].cannons.Remove(this);
            base.OnNetworkDespawn();

        }
        private void FixedUpdate()
        {
            trajectoryRenderer.enabled = aiming;
            if (aiming)
            {
                CalculateLaunchTrajectory();
            }
        }
        public void Fire()
        {
            VehicleController.VehicleControllers[OwnerClientId].rootRB.AddForceAtPosition(-launchOrigin.forward * launchVelocity, launchOrigin.position);
            FireOnClient_RPC();
            FireOnServer_RPC();
        }

        [Rpc(SendTo.Server)]
        void FireOnServer_RPC()
        {

        }
        [Rpc(SendTo.ClientsAndHost)]
        void FireOnClient_RPC()
        {
            StartCoroutine(ReciprocateBarrel());
            if (muzzleEffect != null)
            {
                muzzleEffect.Play(true);
            }
        }

        IEnumerator ReciprocateBarrel()
        {
            turretBarrelRecip= 0;
            while (turretBarrelRecip < 1)
            {
                turretBarrelRecip += Time.fixedDeltaTime * turretRecoilSpeed;
                barrel.localPosition = Vector3.Lerp(turretRecoilStartPos, turretRecoilRearPos, turretRecoilCurve.Evaluate(turretBarrelRecip));
                yield return new WaitForFixedUpdate();
            }
        }
        public void CalculateLaunchTrajectory()
        {
            RaycastHit hit = new();
            Vector3 lastDirection;
            float timeElapsed = 0;
            int iterationsReached = 0;
            Vector3 lastPos = launchOrigin.position, nextPos = launchOrigin.position + (launchVelocity * Time.fixedDeltaTime * launchOrigin.forward);
            linePositions[0] = lastPos;
            for (int i = 0; i < maxIterations; i++)
            {
                iterationsReached++;
                if (Physics.Linecast(lastPos, nextPos, out hit))
                {
                    if (hit.collider != null)
                    {
                        nextPos = hit.point;
                        linePositions[i + 1] = nextPos;
                        break;
                    }
                }
                else
                {
                    timeElapsed += Time.fixedDeltaTime;
                    lastDirection = (nextPos - lastPos).normalized;
                    lastPos = nextPos;
                    nextPos += (launchVelocity * Time.fixedDeltaTime * lastDirection) + (0.5f * timeElapsed * timeElapsed * Physics.gravity);
                    linePositions[i + 1] = nextPos;
                }
            }
            trajectoryRenderer.positionCount = iterationsReached;
            for (int i = 0; i < iterationsReached; i++)
            {
                trajectoryRenderer.SetPosition(i, linePositions[i]);
            }
            if (hit.collider != null)
            {

            }
        }
    }
}