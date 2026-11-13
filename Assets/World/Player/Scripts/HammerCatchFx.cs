using Alpha.Combat;
using UnityEngine;

namespace Alpha.World
{
    // Cand ciocanul aruncat se intoarce in mana: o licarire scurta in palma si o scuturare usoara a camerei.
    // Dara ciocanului (WeaponTrail) e deja a echipei, aici e doar momentul prinderii.
    public class HammerCatchFx : MonoBehaviour
    {
        [SerializeField] Transform hand;
        [SerializeField] Color flashColor = new Color(0.55f, 1f, 0.7f);
        [SerializeField] float flashIntensity = 6f;
        [SerializeField] float flashRange = 2.4f;
        [SerializeField] float flashTime = 0.22f;
        [SerializeField] float minFlightTime = 0.25f;
        [SerializeField] float shakeStrength = 0.07f;
        [SerializeField] float shakeDuration = 0.12f;

        Weapon weapon;
        Light flash;
        float releasedAt = -1f;
        float flashLeft;
        CameraShake shake;
        WeaponSocket socket;

        void Awake()
        {
            socket = GetComponentInChildren<WeaponSocket>(true);
            flash = new GameObject("HammerCatchFlash").AddComponent<Light>();
            flash.transform.SetParent(transform, false);
            flash.type = LightType.Point;
            flash.color = flashColor;
            flash.range = flashRange;
            flash.shadows = LightShadows.None;
            flash.enabled = false;
        }

        void OnDisable()
        {
            Bind(null);
            flashLeft = 0f;
            if (flash != null) flash.enabled = false;
        }

        void Update()
        {
            Weapon current = HammerStages.Instance != null ? HammerStages.Instance.Hammer : null;
            if (current == null && socket != null) current = socket.CurrentWeapon;
            if (current != weapon && current != null) Bind(current);

            if (flashLeft <= 0f) return;
            flashLeft -= Time.deltaTime;
            float k = Mathf.Clamp01(flashLeft / flashTime);
            flash.intensity = flashIntensity * k * k;
            if (hand != null) flash.transform.position = hand.position;
            if (flashLeft <= 0f) flash.enabled = false;
        }

        void Bind(Weapon next)
        {
            if (weapon != null) weapon.onHeldChange -= HeldChanged;
            weapon = next;
            if (weapon != null) weapon.onHeldChange += HeldChanged;
        }

        void HeldChanged(bool held)
        {
            if (!held)
            {
                releasedAt = Time.time;
                return;
            }
            if (releasedAt < 0f || Time.time - releasedAt < minFlightTime) return;
            releasedAt = -1f;
            flashLeft = flashTime;
            flash.enabled = true;
            flash.transform.position = hand != null ? hand.position : weapon.transform.position;
            if (shake == null && Camera.main != null) shake = Camera.main.GetComponent<CameraShake>();
            if (shake != null) shake.Shake(shakeStrength, shakeDuration);
        }
    }
}
