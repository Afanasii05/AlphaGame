using Alpha.Player;
using UnityEngine;

namespace Alpha.World
{
    // Cadru cinematic la aparitia boss-ului: camera jos, in fata lui, care se apropie incet,
    // apoi revine lin pe camera de joc, orientata spre boss.
    // Ruleaza dupa CameraController si inainte de CameraShake / CameraRumble.
    [DefaultExecutionOrder(50)]
    public class BossIntro : MonoBehaviour
    {
        [SerializeField] BossTitleCard titleCard;
        [SerializeField] RectTransform[] letterbox;
        [SerializeField] float duration = 4f;
        [SerializeField] float blendIn = 0.6f;
        [SerializeField] float blendOut = 1.1f;
        [SerializeField] float titleDelay = 0.9f;

        [Header("Cadru")]
        [SerializeField] float startDistance = 8f;
        [SerializeField] float endDistance = 5.2f;
        [SerializeField] float height = 1.1f;
        [SerializeField] float sideOffset = 1.8f;
        [SerializeField] float lookHeight = 2.4f;
        [SerializeField] float fovFrom = 48f;
        [SerializeField] float fovTo = 38f;
        [SerializeField] float gamePitch = 12f;

        const int BlockedLayers = (1 << 2) | (1 << 6) | (1 << 7) | (1 << 8);

        Camera cam;
        CameraController controller;
        Vector3 focus, facing, side;
        Quaternion startRotation, gameRotation;
        float time, baseFov;
        bool titleShown;

        public bool IsPlaying { get; private set; }
        public float Duration => duration;

        void Awake() => SetLetterbox(0f);

        // point = locul boss-ului, facing = directia in care priveste boss-ul
        public void Play(Vector3 point, Vector3 facing)
        {
            cam = Camera.main;
            if (cam == null) return;
            controller = cam.GetComponent<CameraController>();
            if (controller == null) return;

            focus = point + Vector3.up * lookHeight;
            this.facing = Flat(facing, cam.transform.forward);
            side = Vector3.Cross(Vector3.up, this.facing);
            baseFov = cam.fieldOfView;
            startRotation = cam.transform.rotation;

            // la final camera de joc priveste spre boss
            Vector3 pivot = cam.transform.position + cam.transform.forward * controller.distance;
            Vector3 toBoss = Flat(point - pivot, cam.transform.forward);
            gameRotation = Quaternion.Euler(gamePitch, Quaternion.LookRotation(toBoss).eulerAngles.y, 0f);

            time = 0f;
            titleShown = false;
            IsPlaying = true;
        }

        void LateUpdate()
        {
            if (!IsPlaying) return;
            time += Time.deltaTime;
            Transform t = cam.transform;

            // CameraController tocmai a pus camera la distance in spatele jucatorului, pe rotatia curenta
            float distance = controller.distance;
            Vector3 pivot = t.position + t.forward * distance;
            Quaternion gameRot = Quaternion.Slerp(startRotation, gameRotation, Smooth(time / blendIn));
            Vector3 gamePos = pivot - gameRot * Vector3.forward * distance;

            float k = Mathf.Clamp01(time / duration);
            Vector3 shotPos = ShotPosition(Mathf.Lerp(startDistance, endDistance, Smooth(k)));
            Quaternion shotRot = Quaternion.LookRotation(focus - shotPos);
            float w = Smooth(time / blendIn) * (1f - Smooth((time - (duration - blendOut)) / blendOut));

            t.SetPositionAndRotation(Vector3.Lerp(gamePos, shotPos, w), Quaternion.Slerp(gameRot, shotRot, w));
            cam.fieldOfView = Mathf.Lerp(baseFov, Mathf.Lerp(fovFrom, fovTo, k), w);
            SetLetterbox(w);

            if (!titleShown && time >= titleDelay && titleCard != null)
            {
                titleShown = true;
                titleCard.Show();
            }
            if (time >= duration) Finish(gamePos);
        }

        void Finish(Vector3 gamePos)
        {
            IsPlaying = false;
            Vector3 euler = gameRotation.eulerAngles;
            controller.xAngle = euler.y;
            controller.yAngle = Mathf.DeltaAngle(0f, euler.x);
            cam.transform.SetPositionAndRotation(gamePos, gameRotation);
            cam.fieldOfView = baseFov;
            SetLetterbox(0f);
        }

        Vector3 ShotPosition(float distance)
        {
            Vector3 pos = focus - Vector3.up * lookHeight + facing * distance + side * sideOffset;
            // camera ramane mereu putin deasupra solului
            if (Physics.Raycast(pos + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 12f, ~BlockedLayers, QueryTriggerInteraction.Ignore))
                pos.y = Mathf.Max(pos.y, hit.point.y);
            return pos + Vector3.up * height;
        }

        void SetLetterbox(float amount)
        {
            if (letterbox == null) return;
            foreach (RectTransform bar in letterbox)
                if (bar != null) bar.localScale = new Vector3(1f, amount, 1f);
        }

        static Vector3 Flat(Vector3 v, Vector3 fallback)
        {
            v.y = 0f;
            if (v.sqrMagnitude < 0.0001f)
            {
                v = fallback;
                v.y = 0f;
            }
            return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
        }

        static float Smooth(float k)
        {
            k = Mathf.Clamp01(k);
            return k * k * (3f - 2f * k);
        }
    }
}
