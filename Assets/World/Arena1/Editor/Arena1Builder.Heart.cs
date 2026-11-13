using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Inima golemului (cristal verde cu pietricele pe orbita), etapele ciocanului si cinematica de cioplire,
    // legate de Arena1_Encounter: boss mort -> inima fuge -> prinsa cu ciocanul -> cioplire -> sigiliul si poarta se deschid.
    public static partial class Arena1Builder
    {
        const string HeartFolder = Folder + "/Art/Heart";
        static readonly Color HeartGlow = new Color(0.35f, 1f, 0.45f);

        static void BuildHeart(Transform arena, Vector3 c, ParticleSystem dust, Arena1FxBuilder.Result fx, Text hint, Arena1Encounter encounter)
        {
            EnsureFolder(Folder + "/Art", "Heart");
            GameObject root = new GameObject("GolemHeart");
            root.transform.SetParent(arena, false);
            root.transform.position = c + Vector3.up * 2f;
            root.layer = 2;
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;

            // miezul: cristal fatetat, emisiv
            GameObject core = new GameObject("Core");
            core.transform.SetParent(root.transform, false);
            core.layer = 2;
            core.AddComponent<MeshFilter>().sharedMesh = HeartMesh();
            MeshRenderer coreRenderer = core.AddComponent<MeshRenderer>();
            coreRenderer.sharedMaterial = HeartMaterial();
            coreRenderer.shadowCastingMode = ShadowCastingMode.Off;

            Light glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.type = LightType.Point;
            glow.color = HeartGlow;
            glow.range = 6f;
            glow.intensity = 3f;
            glow.shadows = LightShadows.None;

            ParticleSystem motes = Loop("Motes", root.transform, root.transform.position, Quaternion.identity, art.glow, 40, 0.8f, 1.6f, 0.03f, 0.08f, 18f);
            var main = motes.main;
            main.playOnAwake = false;
            main.prewarm = false;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.gravityModifier = -0.05f;
            Tint(motes, new Color(0.55f, 1f, 0.6f, 0.9f));
            Fade(motes, 0.3f);
            var shape = motes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            motes.gameObject.layer = 2;

            // pietricele care orbiteaza inima, resturi din trupul golemului
            List<Transform> pebbles = new List<Transform>();
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f + R(-12f, 12f);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * R(0.5f, 0.7f) + Vector3.up * R(-0.25f, 0.25f);
                GameObject pebble = Spawn(Rocks[i % Rocks.Length], root.transform, root.transform.position + offset, R(0f, 360f));
                if (pebble == null) continue;
                pebble.transform.rotation = Random3();
                pebble.transform.position = root.transform.position + offset;
                ScaleToSize(pebble, R(0.12f, 0.22f));
                StripColliders(pebble);
                foreach (Transform t in pebble.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
                foreach (MeshRenderer r in pebble.GetComponentsInChildren<MeshRenderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
                pebble.name = "Pebble_" + i;
                pebbles.Add(pebble.transform);
            }

            GolemHeart heart = root.AddComponent<GolemHeart>();
            SetRef(heart, "core", core.transform);
            SetRef(heart, "coreRenderer", coreRenderer);
            SetRef(heart, "glow", glow);
            SetRef(heart, "motes", motes);
            SetRefs(heart, "pebbles", pebbles.ToArray());
            SetRef(heart, "dustPrefab", dust);
            SetRef(heart, "sparkPrefab", fx.burst);
            SetRef(heart, "screenHint", hint);
            ClearStatic(root);

            // etapele ciocanului traiesc in scena; fara ele cioplirea doar seteaza etapa
            GameObject stagesGo = new GameObject("HammerStages");
            stagesGo.transform.SetParent(arena, false);
            HammerStages stages = stagesGo.AddComponent<HammerStages>();
            HammerStagesSetup.Configure(stages, fx.burst);

            Arena1Carving carving = Arena1BossUiBuilder.BuildCarving(arena, fx, dust);
            if (carving == null) Debug.LogError("[Arena1Builder] Arena1_BossCanvas lipseste, cinematica de cioplire nu a fost creata.");

            SetRef(encounter, "heart", heart);
            SetRef(encounter, "carving", carving);
        }

        static Quaternion Random3() => Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f));

        static Material HeartMaterial()
        {
            string path = HeartFolder + "/M_GolemHeart.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.1f, 0.35f, 0.15f));
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetColor("_EmissionColor", HeartGlow * 3f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // bipiramida hexagonala alungita, cu cateva cioburi lipite de ea; fete plate (vertexuri duplicate)
        static Mesh HeartMesh()
        {
            string path = HeartFolder + "/SM_GolemHeart.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.name = "SM_GolemHeart";
            List<Vector3> v = new List<Vector3>();
            List<int> tris = new List<int>();
            System.Random local = new System.Random(Seed + 51);
            Crystal(v, tris, Vector3.zero, Quaternion.identity, 0.2f, 0.3f, 0.26f, 6, local);
            Vector3[] shardDirs = { new Vector3(0.8f, 0.4f, 0.2f), new Vector3(-0.7f, -0.3f, 0.5f), new Vector3(0.1f, 0.5f, -0.9f), new Vector3(-0.3f, -0.6f, -0.6f) };
            for (int i = 0; i < shardDirs.Length; i++)
            {
                Vector3 d = shardDirs[i].normalized;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, d);
                Crystal(v, tris, d * 0.13f, rot, 0.06f + 0.015f * i, 0.16f, 0.05f, 5, local);
            }
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static void Crystal(List<Vector3> v, List<int> tris, Vector3 offset, Quaternion rot, float radius, float top, float bottom, int sides, System.Random local)
        {
            Vector3 apex = offset + rot * (Vector3.up * top), foot = offset + rot * (Vector3.down * bottom);
            Vector3[] ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (i + (float)local.NextDouble() * 0.25f) / sides * Mathf.PI * 2f;
                float r = radius * (0.85f + 0.3f * (float)local.NextDouble());
                ring[i] = offset + rot * new Vector3(Mathf.Cos(a) * r, ((float)local.NextDouble() - 0.5f) * radius * 0.4f, Mathf.Sin(a) * r);
            }
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = ring[i], b = ring[(i + 1) % sides];
                Tri(v, tris, apex, b, a);
                Tri(v, tris, foot, a, b);
            }
        }

        static void Tri(List<Vector3> v, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            int n = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            tris.Add(n); tris.Add(n + 1); tris.Add(n + 2);
        }
    }
}
