using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.Cutscenes.EditorTools
{
    // Genereaza clipurile Humanoid ale cutscene-ului SwordPull din poze definite pe muschi.
    // Meniu: Tools > Cutscenes > Rebuild Sword Pull Clips
    public static class SwordPullClipBuilder
    {
        const string Folder = "Assets/Cutscenes/SwordPull/Animations/";
        const string IdleSource = "Assets/Character/Animations/Warrior Idle.fbx";

        class Key
        {
            public float time;
            public Dictionary<string, float> pose;
            public Key(float t, Dictionary<string, float> p) { time = t; pose = p; }
        }

        // ---------- POZE (valori absolute de muschi, -1..1; RootT.y/z in unitati humanoid) ----------
        // Conventii masurate pe acest avatar: Upper Leg / Arm Front-Back NEGATIV = in fata; Lower Leg / Forearm Stretch 1 = intins;
        // Spine/Chest Front-Back POZITIV = aplecat in fata; Neck Nod POZITIV = privire in sus. Idle: picior 0.46, brat 0.26.
        static Dictionary<string, float> Reach() => new Dictionary<string, float> {
            {"Spine Front-Back", 0.35f}, {"Chest Front-Back", 0.25f}, {"UpperChest Front-Back", 0.15f},
            {"Neck Nod Down-Up", -0.2f}, {"Head Nod Down-Up", -0.1f},
            {"Left Upper Leg Front-Back", 0.25f}, {"Right Upper Leg Front-Back", 0.3f},
            {"Left Lower Leg Stretch", 0.7f}, {"Right Lower Leg Stretch", 0.75f},
            {"Left Arm Down-Up", -0.3f}, {"Right Arm Down-Up", -0.3f},
            {"Left Arm Front-Back", -0.45f}, {"Right Arm Front-Back", -0.45f},
            {"Left Forearm Stretch", 0.7f}, {"Right Forearm Stretch", 0.7f},
            {"RootT.y", 0.93f}
        };
        static Dictionary<string, float> Strain() => new Dictionary<string, float> {
            {"Spine Front-Back", -0.3f}, {"Chest Front-Back", -0.25f}, {"UpperChest Front-Back", -0.15f},
            {"Neck Nod Down-Up", 0.25f}, {"Head Nod Down-Up", 0.1f},
            {"Left Upper Leg Front-Back", -0.05f}, {"Right Upper Leg Front-Back", 0.1f},
            {"Left Lower Leg Stretch", 0.3f}, {"Right Lower Leg Stretch", 0.4f},
            {"Left Arm Down-Up", -0.3f}, {"Right Arm Down-Up", -0.3f},
            {"Left Arm Front-Back", -0.6f}, {"Right Arm Front-Back", -0.6f},
            {"Left Forearm Stretch", 0.95f}, {"Right Forearm Stretch", 0.95f},
            {"RootT.y", 0.8f}, {"RootT.z", -0.05f}
        };
        static Dictionary<string, float> Upright() => new Dictionary<string, float> {
            {"Spine Front-Back", -0.1f}, {"Chest Front-Back", -0.15f}, {"UpperChest Front-Back", -0.1f},
            {"Neck Nod Down-Up", 0.3f}, {"Head Nod Down-Up", 0.2f}
        };
        static Dictionary<string, float> Lift() => new Dictionary<string, float> {
            {"Spine Front-Back", -0.35f}, {"Chest Front-Back", -0.25f}, {"UpperChest Front-Back", -0.15f},
            {"Neck Nod Down-Up", 0.3f}, {"Head Nod Down-Up", 0.1f},
            {"Left Upper Leg Front-Back", 0.45f}, {"Right Upper Leg Front-Back", 0.45f},
            {"Left Lower Leg Stretch", 0.95f}, {"Right Lower Leg Stretch", 0.95f},
            {"Left Arm Down-Up", 0.1f}, {"Right Arm Down-Up", 0.1f},
            {"Left Arm Front-Back", -0.8f}, {"Right Arm Front-Back", -0.8f},
            {"RootT.y", 0.99f}
        };
        static Dictionary<string, float> Sit() => new Dictionary<string, float> {
            {"Spine Front-Back", 0.3f}, {"Chest Front-Back", 0.15f}, {"UpperChest Front-Back", 0.05f},
            {"Neck Nod Down-Up", -0.1f}, {"Head Nod Down-Up", 0.0f},
            {"Left Upper Leg Front-Back", -0.75f}, {"Right Upper Leg Front-Back", -0.65f},
            {"Left Lower Leg Stretch", 0.85f}, {"Right Lower Leg Stretch", 0.45f},
            {"Left Arm Down-Up", -0.4f}, {"Right Arm Down-Up", -0.4f},
            {"Left Arm Front-Back", -0.4f}, {"Right Arm Front-Back", -0.4f},
            {"Left Forearm Stretch", 0.5f}, {"Right Forearm Stretch", 0.5f},
            {"RootT.y", 0.36f}, {"RootT.z", -0.25f}
        };
        static Dictionary<string, float> SitImpact() { var p = Sit(); p["Spine Front-Back"] = -0.25f; p["Chest Front-Back"] = -0.2f; p["Neck Nod Down-Up"] = 0.35f; p["RootT.y"] = 0.33f; return p; }
        static Dictionary<string, float> Crouch() { var p = Reach(); p["Left Upper Leg Front-Back"] = -0.15f; p["Right Upper Leg Front-Back"] = 0.2f; p["Left Lower Leg Stretch"] = 0.2f; p["Right Lower Leg Stretch"] = 0.5f; p["RootT.y"] = 0.75f; p["RootT.z"] = -0.05f; return p; }
        static Dictionary<string, float> Idle() => new Dictionary<string, float>();

        static Dictionary<string, float> With(Dictionary<string, float> p, params object[] kv)
        {
            var r = new Dictionary<string, float>(p);
            for (int i = 0; i < kv.Length; i += 2) r[(string)kv[i]] = (float)kv[i + 1];
            return r;
        }

        [MenuItem("Tools/Cutscenes/Rebuild Sword Pull Clips")]
        public static void Build()
        {
            var basePose = ReadBaseline();
            // 1. Aplecare si prinderea manerului
            Save("SP_Reach", basePose, false, new Key(0f, Idle()), new Key(0.9f, Reach()));
            // 2. Efortul: se lasa pe spate si tremura, sabia nu iese
            var strain = new List<Key> { new Key(0f, Reach()), new Key(0.5f, Strain()) };
            for (int i = 0; i < 10; i++) { float t = 0.6f + i * 0.17f; float s = (i % 2 == 0 ? 1f : -1f); strain.Add(new Key(t, With(Strain(), "Spine Twist Left-Right", 0.06f * s, "Chest Left-Right", 0.04f * s, "Spine Front-Back", -0.25f - 0.05f * (i % 3), "RootT.y", 0.8f - 0.01f * (i % 2)))); }
            strain.Add(new Key(2.4f, Strain()));
            Save("SP_PullStruggle", basePose, false, strain.ToArray());
            // 3. Da drumul si se indreapta, privind in sus
            Save("SP_Release", basePose, false, new Key(0f, Strain()), new Key(0.35f, With(Reach(), "Spine Front-Back", 0.1f)), new Key(1.0f, Upright()));
            // 4. Statea cu mana spre soare (bratul il pune IK-ul), piept arcuit, respiratie
            Save("SP_SunPose", basePose, true, new Key(0f, Upright()), new Key(1f, With(Upright(), "Chest Front-Back", -0.2f, "RootT.y", 0.985f)), new Key(2f, Upright()));
            // 5. A doua incercare: prinde din nou, se incordeaza
            Save("SP_Regrab", basePose, false, new Key(0f, Upright()), new Key(0.6f, Reach()), new Key(1.2f, Strain()));
            // 6. Smulgerea: impins exploziv in sus
            Save("SP_PullOut", basePose, false, new Key(0f, Strain()), new Key(0.18f, Crouch()), new Key(0.4f, Lift()));
            // 7. Aruncat in fund cu sabia in brate
            Save("SP_FallSit", basePose, false, new Key(0f, Lift()), new Key(0.25f, With(Lift(), "RootT.y", 0.75f, "RootT.z", -0.15f, "Spine Front-Back", -0.4f)), new Key(0.5f, SitImpact()), new Key(0.9f, Sit()));
            // 8. Sta jos si respira greu
            Save("SP_SitHold", basePose, true, new Key(0f, Sit()), new Key(0.8f, With(Sit(), "Chest Front-Back", 0.2f, "Spine Front-Back", 0.3f)), new Key(1.6f, Sit()));
            // 9. Se ridica in picioare
            Save("SP_StandUp", basePose, false, new Key(0f, Sit()), new Key(0.6f, Crouch()), new Key(1.3f, With(Reach(), "Spine Front-Back", 0.15f, "RootT.y", 0.95f)), new Key(1.9f, Idle()));
            AssetDatabase.SaveAssets();
            Debug.Log("[SwordPull] Clipuri regenerate in " + Folder);
        }

        static Dictionary<string, float> ReadBaseline()
        {
            var d = new Dictionary<string, float>();
            AnimationClip idle = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(IdleSource)) if (a is AnimationClip && !a.name.StartsWith("__")) idle = (AnimationClip)a;
            foreach (var b in AnimationUtility.GetCurveBindings(idle))
            {
                string n = b.propertyName;
                if (n.Contains("FootT") || n.Contains("FootQ") || n.Contains("HandT") || n.Contains("HandQ")) continue;
                d[n] = AnimationUtility.GetEditorCurve(idle, b).Evaluate(0.5f);
            }
            return d;
        }

        static void Save(string name, Dictionary<string, float> basePose, bool loop, params Key[] keys)
        {
            var clip = new AnimationClip { name = name, frameRate = 30 };
            foreach (var prop in basePose.Keys)
            {
                var curve = new AnimationCurve();
                foreach (var k in keys) { float v; if (!k.pose.TryGetValue(prop, out v)) v = basePose[prop]; curve.AddKey(new Keyframe(k.time, v)); }
                for (int i = 0; i < curve.keys.Length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                for (int i = 0; i < curve.keys.Length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), prop), curve);
            }
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = loop;
            s.loopBlendOrientation = true; s.keepOriginalOrientation = true;
            s.loopBlendPositionY = true; s.keepOriginalPositionY = true;
            s.loopBlendPositionXZ = true; s.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            string path = Folder + name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) { EditorUtility.CopySerialized(clip, existing); EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(clip, path);
        }
    }
}
