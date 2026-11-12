using System.Collections.Generic;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.World
{
    // Praf discret sub picioare cand lichul alearga pe sol si un nor mai mare la aterizare.
    // Culoarea vine din suprafata de sub picior (materialul sau stratul de teren), stinsa spre un ton neutru.
    public class PlayerDust : MonoBehaviour
    {
        [SerializeField] PlayerCombatState combat;
        [SerializeField] Rigidbody body;
        [SerializeField] Transform leftFoot;
        [SerializeField] Transform rightFoot;
        [SerializeField] Material material;
        [SerializeField] LayerMask groundMask = ~0;

        [Header("Pasi")]
        [SerializeField] float runSpeed = 3.2f;
        [SerializeField] float plantHeight = 0.45f;
        [SerializeField] float strideLength = 1.2f;
        [SerializeField] float stepCooldown = 0.18f;
        [SerializeField] int stepCount = 4;
        [SerializeField] Vector2 stepSize = new Vector2(0.3f, 0.55f);

        [Header("Aterizare")]
        [SerializeField] float landSpeed = 3.5f;
        [SerializeField] int landCount = 12;
        [SerializeField] Vector2 landSize = new Vector2(0.45f, 0.9f);

        [Header("Culoare")]
        [SerializeField] Color neutral = new Color(0.52f, 0.49f, 0.44f, 1f);
        [SerializeField, Range(0f, 1f)] float surfaceInfluence = 0.55f;
        [SerializeField, Range(0f, 1f)] float saturation = 0.45f;
        [SerializeField, Range(0f, 1f)] float alpha = 0.55f;
        [SerializeField] float softFade = 0.25f;
        [SerializeField, Range(0f, 1f)] float lighten = 0.4f;

        static readonly Dictionary<Texture, Color> TextureTint = new Dictionary<Texture, Color>();
        readonly Dictionary<Collider, Color> surfaceTint = new Dictionary<Collider, Color>();

        ParticleSystem dust;
        bool wasGrounded = true;
        float lastStep, fallSpeed, travelled;

        void Awake()
        {
            dust = BuildSystem();
        }

        void Update()
        {
            if (dust == null || combat == null || body == null) return;
            bool grounded = combat.Grounded;
            Vector3 velocity = body.linearVelocity;
            if (!grounded) fallSpeed = Mathf.Max(fallSpeed, -velocity.y);

            if (grounded && !wasGrounded && fallSpeed >= landSpeed) Land();
            if (grounded) fallSpeed = 0f;
            wasGrounded = grounded;

            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            if (grounded && speed >= runSpeed) Step(speed);
            else travelled = 0f;
        }

        // un pas la fiecare lungime de pas parcursa, sub piciorul care e mai jos in acel moment
        // (animatia de alergare ridica glezna foarte putin, deci contactul nu se poate citi sigur din inaltime)
        void Step(float speed)
        {
            travelled += speed * Time.deltaTime;
            if (travelled < strideLength || Time.time - lastStep < stepCooldown) return;
            travelled = 0f;
            if (leftFoot == null || rightFoot == null) return;
            bool leftHit = Ground(leftFoot.position, out RaycastHit leftGround);
            bool rightHit = Ground(rightFoot.position, out RaycastHit rightGround);
            if (!leftHit && !rightHit) return;
            float left = leftHit ? leftFoot.position.y - leftGround.point.y : float.MaxValue;
            float right = rightHit ? rightFoot.position.y - rightGround.point.y : float.MaxValue;
            RaycastHit ground = left <= right ? leftGround : rightGround;
            if (Mathf.Min(left, right) > plantHeight) return;
            lastStep = Time.time;
            Vector3 back = -new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z).normalized;
            Puff(ground.point, Tint(ground), stepCount, stepSize, 0.35f + 0.05f * speed, back);
        }

        void Land()
        {
            Vector3 center = leftFoot != null && rightFoot != null ? (leftFoot.position + rightFoot.position) * 0.5f : transform.position;
            if (!Ground(center, out RaycastHit ground)) return;
            Puff(ground.point, Tint(ground), landCount, landSize, 1.1f + 0.08f * fallSpeed, Vector3.zero);
        }

        bool Ground(Vector3 from, out RaycastHit hit)
        {
            return Physics.Raycast(from + Vector3.up * 0.35f, Vector3.down, out hit, 1.2f, groundMask, QueryTriggerInteraction.Ignore);
        }

        void Puff(Vector3 point, Color color, int count, Vector2 size, float spread, Vector3 push)
        {
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f);
                Vector3 dir = new Vector3(ring.x, 0f, ring.y);
                emit.position = point + dir * 0.12f + Vector3.up * 0.04f;
                emit.velocity = dir * spread + push * 0.5f + Vector3.up * Random.Range(0.15f, 0.45f);
                emit.startSize = Random.Range(size.x, size.y);
                emit.startLifetime = Random.Range(0.8f, 1.3f);
                emit.rotation = Random.Range(0f, 360f);
                emit.startColor = color;
                dust.Emit(emit, 1);
            }
        }

        // ------------------------------------------------------------------ culoare

        Color Tint(RaycastHit hit)
        {
            Color surface;
            if (!(hit.collider is TerrainCollider) && surfaceTint.TryGetValue(hit.collider, out surface)) return Mute(surface);
            surface = Surface(hit);
            if (!(hit.collider is TerrainCollider)) surfaceTint[hit.collider] = surface;
            return Mute(surface);
        }

        Color Mute(Color surface)
        {
            float grey = surface.grayscale;
            Color muted = Color.Lerp(new Color(grey, grey, grey), surface, saturation);
            Color c = Color.Lerp(neutral, muted, surfaceInfluence);
            // praful ridicat e mai deschis decat solul din care vine, altfel dispare pe el
            c = Color.Lerp(c, new Color(0.92f, 0.9f, 0.86f), lighten);
            c.a = alpha;
            return c;
        }

        Color Surface(RaycastHit hit)
        {
            TerrainCollider terrainCollider = hit.collider as TerrainCollider;
            if (terrainCollider != null)
            {
                Terrain terrain = terrainCollider.GetComponent<Terrain>();
                TerrainLayer layer = terrain != null ? DominantLayer(terrain, hit.point) : null;
                return layer != null ? Average(layer.diffuseTexture) * (Color)layer.diffuseRemapMax : neutral;
            }
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) rend = hit.collider.GetComponentInParent<Renderer>();
            Material mat = rend != null ? rend.sharedMaterial : null;
            if (mat == null) return neutral;
            Color tint = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            return tex != null ? tint * Average(tex) : tint;
        }

        static TerrainLayer DominantLayer(Terrain terrain, Vector3 point)
        {
            TerrainData data = terrain.terrainData;
            if (data == null || data.terrainLayers.Length == 0) return null;
            Vector3 local = point - terrain.transform.position;
            int x = Mathf.Clamp((int)(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp((int)(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
            float[,,] weights = data.GetAlphamaps(x, z, 1, 1);
            int best = 0;
            for (int i = 1; i < weights.GetLength(2); i++)
                if (weights[0, 0, i] > weights[0, 0, best]) best = i;
            return best < data.terrainLayers.Length ? data.terrainLayers[best] : null;
        }

        // media culorii unei texturi, o singura data per textura: blit pe 2x2 (ia ultimul mip) si citire
        static Color Average(Texture tex)
        {
            if (tex == null) return Color.white;
            Color avg;
            if (TextureTint.TryGetValue(tex, out avg)) return avg;
            RenderTexture rt = RenderTexture.GetTemporary(2, 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            Texture2D read = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, 2, 2), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            avg = Color.black;
            foreach (Color c in read.GetPixels()) avg += c * 0.25f;
            Destroy(read);
            avg.a = 1f;
            TextureTint[tex] = avg;
            return avg;
        }

        // ------------------------------------------------------------------ particule

        ParticleSystem BuildSystem()
        {
            GameObject go = new GameObject("FootDust");
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            main.gravityModifier = -0.03f;
            main.startSpeed = 0f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 2.5f;
            drag.multiplyDragByParticleSize = false;

            ParticleSystem.SizeOverLifetimeModule grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.5f));

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = SoftCopy(material);
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            ps.Play();
            return ps;
        }

        // materialul de praf comun se stinge pe ~1.2 m langa geometrie; pufurile de pas stau lipite de sol,
        // asa ca folosim o copie cu o zona de stingere scurta (asset-ul ramane neatins)
        Material SoftCopy(Material source)
        {
            if (source == null || !source.HasProperty("_SoftParticleFadeParams")) return source;
            Material copy = new Material(source) { name = source.name + "_Feet" };
            copy.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / Mathf.Max(0.01f, softFade), 0f, 0f));
            return copy;
        }
    }
}
