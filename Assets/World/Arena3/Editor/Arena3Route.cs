using System.Collections.Generic;
using UnityEngine;

namespace Alpha.World.EditorTools
{
    // Traseul Drumului 3: de la posterna castelului spre est, in jos, intr-un bazin cu apa neagra si insula arenei.
    // Forma terenului si vopseaua drumului se calculeaza aici, ca terenul din Arena 2 si cel din Arena 3 sa dea
    // exact aceeasi inaltime pe cusatura (x = 550). Trei etape care se topesc una in alta:
    // a) dalele castelului se scufunda in pamant ud, b) camp de lupta inecat, c) mlastina adanca cu un dig tot mai ingust.
    public static class Arena3Route
    {
        public struct Hit { public float dist, s, lat, y, behind; }
        public struct Layers { public int grass, mud, gravel, rock, paving, trampled, bog, peat, moss; }
        public struct Pool { public Vector2 centre; public float radius, depth, s; }

        public static readonly Vector3[] Ctrl =
        {
            new Vector3(463f, 6.88f, 415.5f), new Vector3(477f, 5.7f, 413.5f), new Vector3(492f, 5.4f, 418f), new Vector3(507f, 5.15f, 428f),
            new Vector3(524f, 4.85f, 433f), new Vector3(542f, 4.45f, 429f), new Vector3(558f, 3.95f, 418f), new Vector3(574f, 3.45f, 409f),
            new Vector3(591f, 2.95f, 408f), new Vector3(607f, 2.45f, 415f), new Vector3(623f, 2.05f, 424f), new Vector3(639f, 1.75f, 427f),
            new Vector3(653f, 1.55f, 425f), new Vector3(672f, 1.55f, 425f)
        };
        const int Steps = 16;

        public const float WaterY = 0.6f, IslandR = 21.5f, IslandY = 1.6f;
        public static readonly Vector2 IslandCentre = new Vector2(692f, 425f);
        public static readonly Vector2 BasinCentre = new Vector2(685f, 425f), BasinRadii = new Vector2(88f, 56f);
        // drumul spre Arena 4: iese pe partea de est a insulei, inchis deocamdata
        public static readonly Vector2 StubFrom = new Vector2(712f, 425f), StubTo = new Vector2(765f, 425f);
        const float StubY = 1.45f;

        static List<Vector3> pts;
        static List<float> ss;
        static float[] ctrlS;
        static List<Pool> pools;

        public static float Length { get; private set; }
        public static float CausewayS => ctrlS[ctrlS.Length - 2];
        public static IList<Pool> Pools { get { Prepare(); return pools; } }

        public static void Prepare()
        {
            if (pts != null) return;
            pts = new List<Vector3>();
            ss = new List<float>();
            ctrlS = new float[Ctrl.Length];
            float s = 0f;
            for (int i = 0; i < Ctrl.Length - 1; i++)
            {
                Vector3 p0 = Ctrl[Mathf.Max(i - 1, 0)], p1 = Ctrl[i], p2 = Ctrl[i + 1], p3 = Ctrl[Mathf.Min(i + 2, Ctrl.Length - 1)];
                for (int k = 0; k < Steps; k++)
                {
                    Vector3 p = CatmullRom(p0, p1, p2, p3, k / (float)Steps);
                    if (pts.Count > 0) s += Flat(p - pts[pts.Count - 1]).magnitude;
                    if (k == 0) ctrlS[i] = s;
                    pts.Add(p);
                    ss.Add(s);
                }
            }
            s += Flat(Ctrl[Ctrl.Length - 1] - pts[pts.Count - 1]).magnitude;
            pts.Add(Ctrl[Ctrl.Length - 1]);
            ss.Add(s);
            ctrlS[Ctrl.Length - 1] = s;
            Length = s;
            BuildPools();
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) =>
            0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t);

        // ------------------------------------------------------------------ geometrie

        public static Hit Nearest(float x, float z)
        {
            Prepare();
            Vector2 p = new Vector2(x, z);
            // cautare grosiera din 4 in 4 puncte, apoi exacta in jurul celui mai bun
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < pts.Count; i += 4)
            {
                float d = (new Vector2(pts[i].x, pts[i].z) - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            Hit hit = new Hit { dist = float.MaxValue };
            int from = Mathf.Max(0, best - 8), to = Mathf.Min(pts.Count - 2, best + 8);
            for (int i = from; i <= to; i++)
            {
                Vector2 a = new Vector2(pts[i].x, pts[i].z), ab = new Vector2(pts[i + 1].x, pts[i + 1].z) - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                float d2 = (p - (a + ab * t)).sqrMagnitude;
                if (d2 >= hit.dist) continue;
                hit.dist = d2;
                hit.s = Mathf.Lerp(ss[i], ss[i + 1], t);
                hit.y = Mathf.Lerp(pts[i].y, pts[i + 1].y, t);
                hit.lat = ab.x * (p.y - a.y) - ab.y * (p.x - a.x) < 0f ? 1f : -1f;
                hit.behind = i == 0 && t <= 0f ? Mathf.Max(0f, -Vector2.Dot(p - a, ab.normalized)) : 0f;
            }
            hit.dist = Mathf.Sqrt(hit.dist);
            hit.lat *= hit.dist;
            return hit;
        }

        public static Vector3 Point(float s)
        {
            Prepare();
            s = Mathf.Clamp(s, 0f, Length);
            int i = ss.BinarySearch(s);
            if (i >= 0) return pts[i];
            i = ~i;
            if (i <= 0) return pts[0];
            if (i >= pts.Count) return pts[pts.Count - 1];
            return Vector3.Lerp(pts[i - 1], pts[i], Mathf.InverseLerp(ss[i - 1], ss[i], s));
        }

        public static Vector3 Forward(float s) => Flat(Point(Mathf.Min(s + 1f, Length)) - Point(Mathf.Max(s - 1f, 0f))).normalized;

        public static Vector3 Right(float s)
        {
            Vector3 f = Forward(s);
            return new Vector3(f.z, 0f, -f.x);
        }

        public static float Yaw(float s)
        {
            Vector3 f = Forward(s);
            return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        }

        // polilinia drumului (pentru atmosfera de la runtime), cu un punct la cativa metri
        public static Vector3[] Polyline(float step)
        {
            Prepare();
            var list = new List<Vector3>();
            for (float s = 0f; s < Length; s += step) list.Add(Point(s));
            list.Add(Point(Length));
            return list.ToArray();
        }

        public static float StageB(float s) => Smooth(55f, 82f, s);
        public static float StageC(float s) => Smooth(125f, 152f, s);
        public static float Causeway(float s) { Prepare(); return Smooth(CausewayS - 8f, CausewayS, s); }

        public static float HalfW(float s) => 2.6f - 0.2f * StageB(s) - 0.75f * Smooth(140f, CausewayS, s) - 0.05f * Causeway(s);

        public static float BasinE(float x, float z)
        {
            // elipsa cu marginea zdrentuita, ca malul sa nu para trasat cu compasul
            float dx = (x - BasinCentre.x) / BasinRadii.x, dz = (z - BasinCentre.y) / BasinRadii.y;
            return Mathf.Sqrt(dx * dx + dz * dz) + 0.22f * (Fbm(x * 0.03f + 17f, z * 0.03f + 5f) - 0.5f);
        }

        public static float StubDistance(float x, float z)
        {
            float t = Mathf.Clamp01((x - StubFrom.x) / (StubTo.x - StubFrom.x));
            return Vector2.Distance(new Vector2(x, z), Vector2.Lerp(StubFrom, StubTo, t));
        }

        static bool Inside(float x, float z) => x >= 440f && z >= 330f && z <= 530f;

        // ------------------------------------------------------------------ inaltimi

        // h = inaltimea terenului fara drum; intoarce inaltimea cu valea, baltile, bazinul, insula si digul
        public static float Shape(float x, float z, float h)
        {
            if (!Inside(x, z)) return h;
            Hit hit = Nearest(x, z);
            float s = hit.s, d = hit.dist;
            float n = Fbm(x * 0.06f, z * 0.06f);
            float c = StageC(s);
            float fade = Smooth(0f, 22f, s);   // langa posterna terenul ramane cum era

            // valea: fund ud putin sub drum, in etapa c apa neagra adanca
            float valleyY = ValleyY(hit, n);
            float inner = Mathf.Lerp(13f, 20f, c) + 6f * n, outer = inner + 26f + 10f * n;
            h = Mathf.Lerp(h, valleyY, (1f - Smooth(inner, outer, d)) * fade);

            // bazinul
            float basin = 1f - Smooth(0.88f, 1.28f, BasinE(x, z));
            h = Mathf.Lerp(h, WaterY - 1.7f + 0.8f * (n - 0.5f), basin);

            // balti pe campul inecat
            foreach (Pool pool in pools)
            {
                float pd = Vector2.Distance(new Vector2(x, z), pool.centre);
                if (pd < pool.radius) h -= pool.depth * (1f - Smooth(pool.radius * 0.35f, pool.radius, pd));
            }

            // insula: aproape plata, cu un inel de apa mica in jur
            float r = Vector2.Distance(new Vector2(x, z), IslandCentre);
            float top = IslandY + 0.3f * (Fbm(x * 0.12f + 5f, z * 0.12f) - 0.5f);
            float shore = WaterY - 0.35f + 0.12f * (n - 0.5f);
            float island = Mathf.Lerp(top, shore, Smooth(IslandR - 1.5f, IslandR + 1.5f, r));
            h = Mathf.Lerp(h, island, 1f - Smooth(IslandR + 6f, IslandR + 11f, r));

            // digul drumului: coroana la nivelul traseului, malul coboara spre fundul vaii
            float hw = HalfW(s), sw = Mathf.Lerp(2.6f, 3.6f, c);
            float roadFade = 1f - Smooth(0f, 6f, hit.behind);
            // in spatele startului coroana urca in continuarea potecii de la posterna, fara treapta
            float crown = hit.y + 0.24f * hit.behind - 0.05f * Smooth(0.2f, hw, d) * (1f - c);
            // fagase doar unde inca e drum de car
            if (d < hw) crown -= 0.07f * (1f - StageB(s)) * (1f - Smooth(0.1f, 0.4f, Mathf.Abs(d - 0.95f)));
            float bank = Mathf.Lerp(crown, valleyY, Smooth(hw, hw + sw, d)) - Smooth(hw + sw, hw + sw + 3f, d) * 6f - (1f - roadFade) * 20f;
            h = Mathf.Max(h, bank);
            h = Mathf.Lerp(h, crown, (1f - Smooth(hw - 0.2f, hw + 1.2f, d)) * roadFade);

            // capatul drumului spre Arena 4
            float sd = StubDistance(x, z);
            if (sd < 12f)
            {
                float stubY = Mathf.Lerp(StubY, 3.2f, Smooth(735f, StubTo.x, x));
                float stubBank = Mathf.Lerp(stubY, WaterY - 1.2f, Smooth(1.6f, 4.6f, sd)) - Smooth(4.6f, 7.6f, sd) * 6f;
                h = Mathf.Max(h, stubBank);
                h = Mathf.Lerp(h, stubY, 1f - Smooth(1.4f, 2.6f, sd));
            }
            return h;
        }

        static float ValleyY(Hit hit, float n) =>
            Mathf.Lerp(hit.y - 0.5f - 0.9f * n, WaterY - 1.3f - 0.5f * n, StageC(hit.s));

        // ------------------------------------------------------------------ vopsea

        // intoarce cat de "gol" e locul (0..1): drum, apa, balti -> fara iarba
        public static float Paint(float x, float z, float h, float steep, float[] w, Layers L)
        {
            if (!Inside(x, z)) return 0f;
            Hit hit = Nearest(x, z);
            float s = hit.s, d = hit.dist, b = StageB(s), c = StageC(s);
            float n = Fbm(x * 0.07f + 3f, z * 0.07f), n2 = Fbm(x * 0.19f + 9f, z * 0.19f), n3 = Fbm(x * 0.33f + 1f, z * 0.33f + 4f);
            float flat = 1f - Smooth(22f, 36f, steep);
            float fade = Smooth(0f, 22f, s);
            float roadFade = 1f - Smooth(0f, 4f, hit.behind);
            float hw = HalfW(s), off = d - hw;

            // fundul vaii: noroi, apoi muschi de mlastina
            float valley = (1f - Smooth(12f + 6f * n, 30f + 6f * n, d)) * fade * flat;
            Paint(w, L.mud, valley * Mathf.Lerp(0.3f, 0.75f, b) * Smooth(0.35f, 0.6f, n2));
            Paint(w, L.bog, valley * b * Smooth(0.42f, 0.62f, n));
            Paint(w, L.moss, valley * c * 0.75f * Smooth(0.38f, 0.6f, n2));

            // mal si fund de apa: turba neagra
            float wet = 1f - Smooth(WaterY + 0.05f, WaterY + 0.6f, h);
            Paint(w, L.peat, wet);

            // balti: turba in mijloc, noroi pe margine
            float pool = 0f;
            foreach (Pool p in pools)
            {
                float pd = Vector2.Distance(new Vector2(x, z), p.centre);
                if (pd < p.radius * 1.4f) pool = Mathf.Max(pool, 1f - Smooth(p.radius * 0.8f, p.radius * 1.4f, pd));
            }
            Paint(w, L.mud, pool);
            Paint(w, L.peat, Smooth(0.55f, 1f, pool) * 0.85f);

            // drumul: pamant batatorit, noroi in fagase, dale ramase din castel tot mai rare
            float road = (1f - Smooth(-0.3f, 0.9f + 0.8f * n2, off)) * roadFade;
            Paint(w, L.trampled, road);
            Paint(w, L.mud, road * Mathf.Lerp(0.3f, 0.65f, b) * Smooth(0.4f, 0.62f, n3));
            float thr = Mathf.Lerp(0.3f, 0.62f, Smooth(0f, 68f, s));
            Paint(w, L.paving, road * (1f - Smooth(40f, 72f, s)) * Smooth(thr, thr + 0.1f, n3));
            Paint(w, L.moss, road * c * 0.45f * Smooth(0.45f, 0.65f, n));

            // umerii digului: pietris langa castel, noroi, apoi turba si muschi in mlastina
            float shoulder = Smooth(-0.2f, 0.6f, off) * (1f - Smooth(1.5f, 3.5f + c, off)) * fade;
            Paint(w, L.gravel, shoulder * (1f - b) * 0.6f * Smooth(0.4f, 0.6f, n2));
            Paint(w, L.mud, shoulder * b * 0.7f);
            Paint(w, L.peat, shoulder * c * 0.6f * Smooth(0.3f, 0.6f, n));

            // insula: muschi ud, noroi si urme de lupta
            float r = Vector2.Distance(new Vector2(x, z), IslandCentre);
            float island = 1f - Smooth(IslandR - 1f, IslandR + 1f, r);
            Paint(w, L.moss, island * 0.7f * Smooth(0.35f, 0.6f, n));
            Paint(w, L.mud, island * 0.55f * Smooth(0.45f, 0.65f, n2));
            Paint(w, L.trampled, island * 0.45f * Smooth(0.5f, 0.7f, n3));

            // capatul spre Arena 4
            float stub = 1f - Smooth(1.2f, 2.6f + n2, StubDistance(x, z));
            Paint(w, L.trampled, stub);
            Paint(w, L.mud, stub * 0.5f * Smooth(0.4f, 0.6f, n3));

            // pe mijlocul insulei iarba e rara si joasa, sa se vada lupta; spre mal ramane mai deasa
            float floor = island * Mathf.Lerp(0.85f, 0.4f, Smooth(IslandR - 8f, IslandR - 1.5f, r));
            return Mathf.Clamp01(Mathf.Max(Mathf.Max(road, stub), Mathf.Max(wet, pool)) + floor);
        }

        public static void Paint(float[] w, int layer, float amount)
        {
            if (layer < 0 || layer >= w.Length) return;
            amount = Mathf.Clamp01(amount);
            if (amount <= 0f) return;
            for (int i = 0; i < w.Length; i++) w[i] *= 1f - amount;
            w[layer] += amount;
        }

        // ------------------------------------------------------------------ balti

        static void BuildPools()
        {
            pools = new List<Pool>();
            System.Random rng = new System.Random(3307);
            int side = 1;
            for (float s = 30f; s < 136f; s += 7f + (float)rng.NextDouble() * 5f)
            {
                side = rng.NextDouble() < 0.7 ? -side : side;
                float b = StageB(s);
                float radius = Mathf.Lerp(1.5f, 2.2f, b) + (float)rng.NextDouble() * Mathf.Lerp(1f, 2.6f, b);
                float hw = HalfW(s), sw = Mathf.Lerp(2.6f, 3.6f, StageC(s));
                float min = hw + sw + radius + 0.4f, max = Mathf.Max(min + 0.5f, 12.5f - radius);
                float lat = min + (float)rng.NextDouble() * (max - min);
                Vector3 c = Point(s) + Right(s) * side * lat;
                pools.Add(new Pool { centre = new Vector2(c.x, c.z), radius = radius, depth = 0.5f + 0.3f * (float)rng.NextDouble(), s = s });
            }
        }

        // ------------------------------------------------------------------ utilitare

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // acelasi zgomot ca in Arena2Builder (aceleasi decalaje), ca relieful sa continue peste cusatura
        public static float Fbm(float x, float z)
        {
            float sum = 0f, amp = 1f, norm = 0f, f = 1f;
            for (int o = 0; o < 4; o++)
            {
                sum += Mathf.PerlinNoise(x * f + 13.7f + o * 31.1f, z * f + 71.3f + o * 17.9f) * amp;
                norm += amp; amp *= 0.5f; f *= 2f;
            }
            return sum / norm;
        }
    }
}
