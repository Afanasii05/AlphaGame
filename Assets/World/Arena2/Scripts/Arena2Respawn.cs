using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Alpha.Input;
using Alpha.Interfaces;
using Alpha.Player;
using UnityEngine;

namespace Alpha.World
{
    // Daca jucatorul cade in lupta cu cavalerul: ecran negru, revine la intrarea in curte cu viata plina,
    // cavalerul se intoarce pe tron, poarta se redeschide si lupta se poate relua.
    // Viata jucatorului e privata in HittableCharacter: o citim si o refacem prin reflexie, ca BossHealthReader.
    public class Arena2Respawn : MonoBehaviour
    {
        [SerializeField] Arena2Encounter encounter;
        [SerializeField] Arena2Screen screen;
        [SerializeField] Transform spawnPoint;
        [SerializeField] float deathDelay = 1.8f;
        [SerializeField] float fadeOut = 1f;
        [SerializeField] float hold = 0.6f;
        [SerializeField] float fadeIn = 1.2f;
        [SerializeField] float cameraPitch = 10f;
        [SerializeField] string retryHint = "The Oathbound Knight returns to his throne.";

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo HealthField = typeof(HittableCharacter).GetField("currentHealth", Private);
        static readonly FieldInfo MaxHealthField = typeof(HittableCharacter).GetField("Max_Health", Private);
        static readonly MethodInfo HealthLight = typeof(HittableCharacter).GetMethod("UpdateHealthLight", Private);

        HittableCharacter player;
        bool running;

        void Update()
        {
            if (running || encounter == null || !encounter.InFight || HealthField == null) return;
            if (player == null) player = FindPlayer();
            if (player == null || (float)HealthField.GetValue(player) > 0f) return;
            StartCoroutine(respawn());
        }

        IEnumerator respawn()
        {
            running = true;
            yield return new WaitForSeconds(deathDelay);
            InputHandler input = player.GetComponentInParent<InputHandler>();
            if (input != null) input.disableInputs();
            if (screen != null) yield return screen.FadeTo(1f, fadeOut);

            Revive(player);
            encounter.ResetEncounter();
            if (input != null) input.disableInputs();
            Camera cam = Camera.main;
            CameraController controller = cam != null ? cam.GetComponent<CameraController>() : null;
            if (controller != null && spawnPoint != null)
            {
                controller.xAngle = spawnPoint.eulerAngles.y;
                controller.yAngle = cameraPitch;
            }
            yield return new WaitForSecondsRealtime(hold);

            if (screen != null)
            {
                screen.Show(retryHint, 4f);
                yield return screen.FadeTo(0f, fadeIn);
            }
            if (input != null) input.enableInputs();
            running = false;
        }

        void Revive(HittableCharacter target)
        {
            if (MaxHealthField != null) HealthField.SetValue(target, MaxHealthField.GetValue(target));
            if (HealthLight != null) HealthLight.Invoke(target, null);

            // animatorul iese din moarte; parametrii (arma in mana etc.) raman cum erau
            Animator animator = target.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                var saved = new List<KeyValuePair<AnimatorControllerParameter, float>>();
                foreach (AnimatorControllerParameter p in animator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Float) saved.Add(new KeyValuePair<AnimatorControllerParameter, float>(p, animator.GetFloat(p.nameHash)));
                    else if (p.type == AnimatorControllerParameterType.Int) saved.Add(new KeyValuePair<AnimatorControllerParameter, float>(p, animator.GetInteger(p.nameHash)));
                    else if (p.type == AnimatorControllerParameterType.Bool) saved.Add(new KeyValuePair<AnimatorControllerParameter, float>(p, animator.GetBool(p.nameHash) ? 1f : 0f));
                }
                animator.Rebind();
                foreach (var pair in saved)
                {
                    AnimatorControllerParameter p = pair.Key;
                    if (p.type == AnimatorControllerParameterType.Float) animator.SetFloat(p.nameHash, pair.Value);
                    else if (p.type == AnimatorControllerParameterType.Int) animator.SetInteger(p.nameHash, (int)pair.Value);
                    else animator.SetBool(p.nameHash, pair.Value > 0.5f);
                }
                animator.Update(0f);
            }

            if (spawnPoint == null) return;
            Transform root = target.transform;
            Rigidbody rb = root.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.position = spawnPoint.position;
                rb.rotation = spawnPoint.rotation;
            }
            root.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            Physics.SyncTransforms();
        }

        static HittableCharacter FindPlayer()
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go == null) return null;
            HittableCharacter h = go.GetComponentInParent<HittableCharacter>();
            return h != null ? h : go.GetComponentInChildren<HittableCharacter>();
        }
    }
}
