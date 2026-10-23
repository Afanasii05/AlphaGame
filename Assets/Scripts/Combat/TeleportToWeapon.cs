
using Alpha.Player;
using System;
using System.Collections;
using UnityEngine;
namespace Alpha.Combat
{
    public class TeleportToWeapon : MonoBehaviour
    {
        [SerializeField] Transform playerTr;
        [SerializeField] float minTeleportDist=2f;
        [SerializeField] float playerTeleportSpeed = 10f;
        [SerializeField] LayerMask groundLayer;
        Animator anim;
        CapsuleCollider capsule;
        Rigidbody playerRb;
        Renderer[] bodyRenderers;

        [SerializeField] ParticleSystem teleportFx;
        [SerializeField] ParticleSystem teleportSmoke;
        [SerializeField] int smokeAmount = 40;
        [SerializeField] float smokeDelay = 0.4f;   
        [SerializeField] float revealDelay = 0.1f;  

        bool teleporting;
        public event Action<bool> onTriggerTeleport;
        Camera cam;
        CameraController cameraController;
        EnquipWeapon eq;

        void Awake()
        {
            playerRb = playerTr.GetComponent<Rigidbody>();
            bodyRenderers = playerTr.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            cam = Camera.main;
            cameraController = cam.GetComponent<CameraController>();
            capsule = playerTr.GetComponent<CapsuleCollider>();
            eq = GetComponentInChildren<EnquipWeapon>();
            anim = GetComponentInChildren<Animator>();
        }

        public bool canTeleport(Weapon wp)
        {
            if (wp == null)
                return false;
            Rigidbody wrb = wp.GetComponent<Rigidbody>();
            if (teleporting  ||  wrb.linearVelocity.magnitude > 1f)
                return false;
            return true;
        }
        public void teleport(Weapon wp)
        {
            Rigidbody weaponRb = wp.GetComponent<Rigidbody>();
            Vector3 target = FindSafeSpot(weaponRb.position, playerRb.position);
            StartCoroutine(teleportation(target, wp));
            
        }
        private IEnumerator teleportation(Vector3 target, Weapon wp)
        {
            anim.SetBool("Teleport", true);
            onTriggerTeleport?.Invoke(true);    //trimit in playerController pentru fov la camera
            teleporting = true;
            playerRb.isKinematic = true;
            AimCameraAt(target);                //Ma uit la sabie
            Vector3 initial = playerRb.position;
            float total = Vector3.Distance(initial, target);
            float ds = cameraController.distance;
            

            if (teleportSmoke) teleportSmoke.Emit(smokeAmount); 
            yield return new WaitForSeconds(smokeDelay);
            cameraController.distance = -total / 7; //Apropii distanta catre sabie

            SetVisible(false);
            if (teleportFx) teleportFx.Play();

            while ((playerTr.position - target).sqrMagnitude >= 0.01f)
            {
                float catAmMers =  1f - Vector3.Distance(playerRb.position, target) / total;
                cameraController.distance = Mathf.Lerp(-total/10, 5, catAmMers);
                playerRb.MovePosition(Vector3.MoveTowards(playerRb.position, target, playerTeleportSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
            if (teleportFx) teleportFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            eq.Equip(wp);
            cameraController.distance = ds;

            if (teleportSmoke) teleportSmoke.Emit(smokeAmount);   
            yield return new WaitForSeconds(revealDelay);

            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
            SetVisible(true);
            onTriggerTeleport?.Invoke(false);
            teleporting = false;
            anim.SetBool("Teleport", false);
            

        }
        void SetVisible(bool visible)
        {
            foreach (var r in bodyRenderers) r.enabled = visible;
        }
        void AimCameraAt(Vector3 point)
        {
            Vector3 dir = point - cam.transform.position;
            if (dir.sqrMagnitude < 0.0001f) return;

            Vector3 e = Quaternion.LookRotation(dir).eulerAngles;
            cameraController.xAngle = e.y;                                                  
            cameraController.yAngle = Mathf.Clamp(e.x > 180f ? e.x - 360f : e.x, -89.9f, 89f);
            cam.transform.rotation = Quaternion.Euler(cameraController.yAngle, cameraController.xAngle, 0f);
        }
        Vector3 FindSafeSpot(Vector3 weaponPos, Vector3 fromPos)
        {
            float r = capsule.radius * 0.95f;
            float half = Mathf.Max(capsule.height * 0.5f - capsule.radius, 0f);
            Vector3 back = fromPos - weaponPos; back.y = 0f; back.Normalize();

            for (int i = 0; i < 6; i++)   
            {
                Vector3 probe = weaponPos + back * (i * 0.5f);
                if (!Physics.Raycast(probe + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f, groundLayer, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 center = hit.point + Vector3.up * (capsule.height * 0.5f + 0.02f);
                if (!Physics.CheckCapsule(center + Vector3.up * half, center - Vector3.up * half, r, groundLayer, QueryTriggerInteraction.Ignore))
                    return center - capsule.center;   
            }
            return fromPos;   
        }
    }
}