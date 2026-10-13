# AlphaGame: specificații detaliate T0–T4 (bază: commit c599ca7)

> Ordinea de lucru (cronologică, cu puncte de test) e în `Docs/ROADMAP.md`. Aici sunt detaliile fiecărui task.

---

## 0. Reguli care se aplică la TOATE taskurile

| Regulă | Ce înseamnă concret aici |
|---|---|
| **Nu se modifică structura scripturilor** | `InputHandler`, `PlayerController`, `CameraController` și `PlayerDirectionalAnimation` rămân **neatinse**. Totul nou intră în scripturi noi, cu namespace-uri noi: `Alpha.Cutscenes`, `Alpha.Combat`, `Alpha.Enemies`, `Alpha.UI`. Ele se abonează la evenimentele `InputHandler` sau activează și dezactivează componentele existente (`enabled = false/true`), fără să le schimbe codul. |
| Niciodată pe `main` | Branch-uri: `feature/roland-sword-socket`, `feature/roland-cutscene-swordpull`, `feature/roland-hammer-hold`, `feature/roland-attack-slam`, `feature/roland-enemy-golem` |
| O singură persoană pe scenă | Tot ce se poate merge în **Prefab-uri**. Modificările în `SampleScene.unity` se strâng într-o singură fereastră de lucru, anunțată în chat. |
| Nu mutăm sau redenumim fără anunț | Fișiere noi doar în foldere noi (vezi fiecare task). Nu mutăm nimic existent. |
| Asset-uri modificate (nu scripturi) | `PlayerAnimation.controller`, `PlayerActions.inputactions`, `Cutscene_SwordPullingTimeline.playable`, `Lich_Character_Prefab.prefab`, `Packages/manifest.json` și `ProjectSettings/TagManager.asset` sunt fișiere comune. Fiecare modificare se anunță în chat-ul echipei. |

### Constrângeri tehnice descoperite (importante pentru toate taskurile)

- `PlayerController` și `PlayerDirectionalAnimation` **păstrează în cache Animator-ul în `Awake()`** (`GetComponentInChildren<Animator>()` și `model.GetComponent<Animator>()`).
  - Dacă înlocuim GameObject-ul `MainCharacter` cu alt model, scripturile rămân legate de Animator-ul vechi.
  - **Concluzie:** „modelul nou” trebuie să folosească **același GameObject `MainCharacter` și același Animator**. Schimbăm doar mesh-ul sau materialele și atașăm sabia. Detalii la T1.6.
- `CameraController.LateUpdate` scrie `transform.position` în fiecare frame, deci se bate cu `CinemachineBrain`. Îl dezactivăm cu `enabled = false` pe durata cutscene-ului, dintr-un script nou.
- `PlayerController` nu are blocare pentru cutscene. Îl dezactivăm temporar, împreună cu `PlayerDirectionalAnimation` și `PlayerInput`, și facem `Rigidbody.isKinematic = true`.
- Pachete care **lipsesc**:
  - `com.unity.animation.rigging` (IK pentru mâini);
  - `com.unity.ai.navigation` (NavMeshSurface pentru inamici).
  - Instalarea modifică `Packages/manifest.json` și trebuie anunțată.
- Proiectul nu are tag-uri custom, layere pentru combat, sistem audio sau UI.
- `Assets/Settings/SampleSceneProfile.asset` are deja Bloom și Vignette. Le folosim pentru dramatism.

### Ordinea recomandată și dependențele

```
T0 Sabie-ciocan prefab + socket-uri ─┬─> T1 Cutscene (perfecționare) ──> T2 Ținere sabie ──> T3 Atac slam
                                     └──────────────────────────────────────────────────────> T4 Monstru (integrare)
T4-Blender (model monstru) poate începe ORICÂND, în paralel.
```

---

## T0. Fundație comună: prefab „sabie-ciocan” + socket-uri pe mâini

> **Stadiu (2026-10-14): implementat și testat manual (OK).** Branch `feature/roland-t0-swordhammer`.
> - `SwordHammer.prefab`: root scale 1, pivot pe mâner (y 0.145 din mesh), `Visual` cu scara coaptă, Grip_R/Grip_L (6.9 cm, limitat de mânerul de ~11 cm), ImpactPoint, HeadHitbox (trigger, oprit).
> - Socket-uri pe `Lich_Character_Prefab`: `Socket_Weapon_R` (0.01, 0.062, 0.012) / rot (0,0,90) + `WeaponSocket`; `Socket_Weapon_L` (referință IK); `DebugEquip` (F1) pe root.
> - Scripturi noi: `Assets/Scripts/Combat/Weapon.cs`, `WeaponSocket.cs`, `DebugEquip.cs`. Niciun script existent atins; scena neatinsă.
> - Rămân deschise: T0-B (cap de piatră / mâner mai lung pentru două mâini), degete (doar lanț Index).

**De ce e primul:** cutscene-ul (atașarea sabiei în mână), ținerea sabiei și atacul folosesc toate aceeași armă și aceleași socket-uri.

### Starea actuală
- Sabia din scenă este o instanță brută de `Assets/Weapons/Sword_Unity/Sword.fbx`.
  - Poziție (-7.914, 2.818, -4.779), rotație Euler ≈ (120.7, 60.8, 183.7).
  - **Scale neuniform (0.7955, 0.8086, 1.3208).** Dacă o legăm de un os, rotațiile copiilor se deformează.
- Are un Animator inutil (fără controller), animat doar de Timeline.
- FBX-ul conține:
  - `Sword_mesh`: 3,350 vertecși, lamă pe axa +Y de la 0 la 1.2, mânerul la y ≈ 0.15;
  - `Stone_01/02/03`, adică pietrele de la vârf (y 0.99–1.70, x ±0.6). Ele formează **capul de „ciocan”**.
- Copii existenți:
  - `GripPoint`, cu `RightHandTarget` (+0.042 y) și `LeftHandTarget` (-0.038 y);
  - `MagicMotes` (particule);
  - `RuneLight` (verde, 2.6);
  - `SwordStones_Collider` (MeshCollider);
  - BoxCollider (nu e trigger).
- `Sword_Prefab.prefab` există, dar nu e folosit.
- Lich-ul (`Lich_Character_Prefab`) are oase `mixamorig:*`, dar **niciun socket**.

### Pași
1. **Prefab nou** `Assets/Weapons/Sword_Unity/SwordHammer.prefab`. Fișier nou; nu atingem `Sword_Prefab`.
   - Root `SwordHammer` are scale (1,1,1), iar pivotul e în **centrul mânerului**, acolo unde se închide mâna dreaptă.
   - Copilul `Visual` conține FBX-ul, cu scale-ul corectat uniform. Dacă proporțiile 0.8/0.81/1.32 sunt intenționate, le „coacem” în copilul `Visual`, nu în root.
   - Copil `Grip_R`: punctul exact al mâinii drepte. Aliniat cu root-ul, deci offset 0.
   - Copil `Grip_L`: mâna stângă, la ~0.10–0.12 m mai jos pe mâner, spre capul de piatră. E prinderea pe două mâini a unui ciocan greu.
   - Copil `ImpactPoint`: centrul feței de lovire a capului de piatră. Îl folosește T3.
   - Copil `HeadHitbox`: SphereCollider, isTrigger, cu raza egală cu capul de piatră. Inițial dezactivat.
   - Colliderele vechi (`SwordStones_Collider` și BoxCollider) se dezactivează când arma e în mână, ca să nu lovească capsula player-ului.
   - `RuneLight` și `MagicMotes` rămân. Le folosește cutscene-ul.
2. **Verificare proporții** (în Unity, lângă player-ul de 1.86 m): lungimea totală a armei ar trebui să fie ~1.3–1.5 m, iar capul de piatră ~0.45–0.6 m lățime.
   - Capul are acum ~1.2 m lățime în FBX (x ±0.6). Dacă arată exagerat, se deschide subtaskul Blender **T0-B**: refacem capul de piatră ca un bloc compact, lipit de lamă, păstrând materialul `M_Gravel043_Stone` și runele.
   - Export după regulile de pipeline (vezi `Docs/ROADMAP.md`, „Pipeline Blender → Unity”): pivot la mâner, FBX, fără `.blend` în Assets.
3. **Socket-uri pe Lich**: se editează `Lich_Character_Prefab.prefab`, nu scena.
   - `mixamorig:RightHand/Socket_Weapon_R`, cu un offset local de pornire de ≈ (0, 0.08, 0.03). Se reglează vizual în Play Mode.
   - `mixamorig:LeftHand/Socket_Weapon_L`, doar ca referință pentru IK.
   - Mod de reglare: în Play Mode cu pose-ul de hold, mutăm socket-ul, copiem valorile și le lipim în prefab.
4. **Script nou** `Assets/Scripts/Combat/WeaponSocket.cs` (`Alpha.Combat`):
   - `Attach(Transform weapon)`: setează parent pe socket, `localPosition` și `localRotation` din offset-uri serializate, `localScale = 1`, dezactivează colliderele armei și setează Rigidbody-ul pe kinematic, dacă există;
   - `Detach()`.
   - **Nu folosim ParentConstraint pe scale neuniform.** Rezolvăm scale-ul la pasul 1.

### Criterii de acceptare
- În Play Mode, `SwordHammer` apare în mâna dreaptă și urmărește mâna fără niciun tremur (idle, walk, run, jump).
- Nu e nicio deformare sau forfecare a armei la rotația mâinii.
- Player-ul nu e împins de colliderele armei.

---

## T1. Perfecționarea cutscene-ului „scoaterea sabiei din piatră”

### Starea actuală
- **Obiecte și setări**
  - GameObject: `Cutscene_SwordPulling`, aflat în (-4.153, 0.500, -3.693).
  - `PlayableDirector`: componenta e **dezactivată** (m_Enabled 0). PlayOnAwake e oprit, Wrap None, Update GameTime.
  - Timeline: `Assets/Cutscene_SwordPullingTimeline.playable`, 60 fps, durată ~20.6 s.
  - Clipuri: `Assets/Cutscenes/SwordPull/Animations/SP_*.anim`.

**Beat sheet actual (secunde)**

| Timp | Ce se întâmplă |
|---|---|
| 0–1.7 | SP_Reach: se apleacă spre mâner. Dolly 0, FOV 46. |
| 0.9–4.0 | SP_PullStruggle #1 |
| 3.2–4.7 | SP_Release: se îndreaptă și se uită în sus |
| 4.3–6.4 | SP_PullStruggle #2 |
| 6.0–12.2 | SP_SunPose, în buclă. Dolly 0→1 (2.47→6.92). **ZOOM: FOV 44 (8.03) → 22 (9.95) → 15 (10.4) → 42 (12.28).** Noise 0.06. |
| 11.6–12.8 | SP_Regrab |
| 12.5–14.6 | SP_PullStruggle #3 |
| 14.28–15.9 | SP_PullOut (mișcare 0.4 s). Spike Perlin 2.2 la 14.34. PullBurst VFX **mort** (referință nulă). Sabia începe să se miște la 14.43. |
| 15.48–17.25 | SP_FallSit. **Sabia „sare” la ~15.95.** |
| 16.37–18.27 | SP_StandUp |
| 17.07 | Dolly revine la 0 |
| 17.38 | Shot-ul Cinemachine se termină |
| 18.4 | Clipul sabiei se termină |
| 19.6 | Clipul camerei se termină |
| 20.63 | Track-ul Activation se termină |

### Buguri de cablaj care trebuie reparate înainte de orice (T1.0)

| # | Bug | Fix |
|---|---|---|
| 1 | Cinemachine Track e legat de un **al doilea `CinemachineBrain`**, pus pe GO-ul `CinemachineCamera` (fileID 615733230). | Ștergem brain-ul de pe CinemachineCamera. Legăm track-ul de brain-ul de pe **Main Camera** (330585550). |
| 2 | Componenta `CinemachineCamera` (615733233) e `m_Enabled 0` și are Priority dezactivat. | Componenta rămâne activă. Ea devine „live” doar când o activează Timeline-ul sau scriptul de cutscene. Setăm Priority 100 pe durata cutscene-ului. |
| 3 | `CameraController` mută camera în fiecare frame. | Dezactivat cu `enabled=false` pe durata cutscene-ului (T1.1). |
| 4 | Control Track „PullBurst” are `sourceGameObject` = null. | Creăm un sistem de particule `VFX_PullBurst` (T1.5) și îl asignăm. |
| 5 | Track-ul de animație al sabiei (Animation Track (2), clipul „Recorded (1)”) e sursa tremurului. | Track-ul se **mutează sau se șterge** în T1.3. Animator-ul de pe sabie se scoate. |
| 6 | Player-ul are rotația salvată înclinată, Euler ≈ (9.7, -125, -0.35). | Setăm X și Z la 0. |

**Recomandare structurală (opțională, se anunță):** transformăm `Cutscene_SwordPulling` într-un prefab, `Assets/Cutscenes/SwordPull/Cutscene_SwordPull.prefab`.
- Bindings-urile către Player și Main Camera se pierd într-un prefab. De aceea scriptul de cutscene le **re-leagă la runtime** cu `director.SetGenericBinding(track, obj)`.
- Câștig: lucrul pe cutscene nu mai blochează scena pentru restul echipei.

---

### T1.1 Trigger pe podium și pornirea din poziția curentă a player-ului

**Obiecte noi**
- GO `SwordPull_Trigger`, copil al `SwordShrine` (sau al prefab-ului cutscene):
  - `BoxCollider` cu **isTrigger = true**;
  - centru ≈ la baza sabiei, în world (-7.93, 2.3, -4.78);
  - size ≈ (2.2, 2.0, 2.2), adică o zonă puțin mai mare decât inelul de rune. Vârful podiumului e la y ≈ 1.76.
  - Notă: în scenă nu există acum **niciun** trigger collider.
- GO `CutsceneStartMark`, empty:
  - poziția **în care animațiile se potrivesc cu mânerul**. Poziția salvată a player-ului, (-7.5725, 2.6417, -4.4452) cu yaw ≈ -125…-134°, e exact cea autorată pentru cutscene;
  - privește spre sabie.

**Scripturi noi** (`Assets/Scripts/Cutscenes/`, namespace `Alpha.Cutscenes`)
1. `SwordPullTrigger.cs`
   - În `OnTriggerEnter`, verificăm `other.GetComponentInParent<PlayerController>() != null`. Nu avem tag-uri.
   - Pornește doar dacă player-ul e pe sol, adică nu sare.
   - E **one-shot**: după pornire, collider-ul se dezactivează.
   - Apelează `SwordPullCutscene.Begin(player)`.
2. `SwordPullCutscene.cs` (orchestratorul cutscene-ului). Secvența din `Begin`:
   1. **Blocarea gameplay-ului.** Dezactivăm `PlayerInput`, `PlayerController`, `PlayerDirectionalAnimation` și `CameraController`, setăm `rb.velocity = 0` și `rb.isKinematic = true`, iar pe Animator `Speed = 0`.
   2. **Pornirea din poziția curentă.** Facem o tranziție scurtă, de 0.6–0.9 s: rotim player-ul spre sabie (Slerp) și îl deplasăm lin (Lerp, cu ease-out) de unde se află până la `CutsceneStartMark`.
      - Pe durata tranziției, Animator `Speed` merge 0.3 → 0, ca să pară că face pași.
      - Resetăm rotația locală a `MainCharacter` la identity. `PlayerDirectionalAnimation` o rotea.
      - Dacă player-ul e la peste 2.5 m de mark, tranziția se face sub fade (T1.6).
   3. **Fade.** Ecranul trece la negru (0.35 s) → comutăm camera → `director.Play()` → fade-in (0.6 s). Detalii la T1.6.
   4. **Camera.** Activăm `CinemachineCamera` cu Priority 100. Brain-ul de pe Main Camera preia controlul.
   5. **Bindings la runtime.** Ne asigurăm că track-urile sunt legate de Animator-ul real al `MainCharacter` și de brain-ul real.
   6. **Final.** Ne abonăm la `director.stopped` și la un Signal `SIG_End` pentru secvența de final (T1.6).

**De ce așa:** track-ul de animație folosește `ApplySceneOffsets`, adică clipurile pornesc din poziția personajului. Dacă pornim din orice punct, mâinile nu nimeresc mânerul. Tranziția scurtă păstrează senzația că animația „pornește de unde e player-ul”, dar aliniază corect corpul.

**Criterii de acceptare**
- Intrând pe podium din **orice direcție**, cutscene-ul pornește o singură dată.
- Player-ul se întoarce spre sabie și se așază lin, fără teleport vizibil.
- WASD, mouse-ul și tasta P nu au niciun efect în timpul cutscene-ului.
- Camera nu mai tremură între CameraController și Cinemachine.

---

### T1.2 SP_PullStruggle: mâinile sunt prea depărtate

**Cauza exactă** (din curbele de mușchi):
- Ambele brațe au **aceleași valori**: Arm Down-Up = -0.3, Arm Front-Back = -0.45 → -0.6, Forearm Stretch 0.7 → 0.95.
- Asta înseamnă brațe **depărtate lateral și întinse**, ca un „A/T-pose” spre înainte. Așa apare în screenshot-urile 165215, 165222 și 165319.
- Twist-urile au rămas cele din idle, deci diferă stânga față de dreapta. Mâinile nu converg niciodată pe un punct comun.
- Aceeași familie de pose apare în `SP_Reach`, `SP_Regrab` și `SP_PullOut`, deci problema e la toate.

**Fix în 2 straturi.** Le facem pe amândouă.

**Strat A: corectarea pose-ului de bază** (ca mâinile să fie *aproape* de mâner fără IK)
- **NU rulăm** `SwordPullClipBuilder.cs` din backup. Ar suprascrie `SP_PullStruggle`-ul live (4 s, 23 de chei, reglat manual) cu versiunea veche (2.4 s), plus toate celelalte clipuri.
- Duplicăm clipurile ca `SP_PullStruggle_v2`, `SP_Reach_v2`, `SP_Regrab_v2` și `SP_PullOut_v2`, în același folder (fișiere noi).
- Valori de pornire pentru prinderea pe 2 mâini (se reglează din fereastra Animation, cu preview Humanoid):

| Mușchi | Acum (L și R) | Țintă (pornire) |
|---|---|---|
| Arm Down-Up | -0.3 | **-0.55 … -0.65** (brațe mai jos și mai aproape de corp) |
| Arm Front-Back | -0.45 … -0.6 | **-0.85 … -1.0** (brațe aduse în față și spre interior) |
| Shoulder Front-Back | ~0 | **+0.2 … +0.3** (umerii vin în față) |
| Forearm Stretch | 0.7 → 0.95 | **0.45 … 0.65** (coate ușor îndoite, ca la tras) |
| Arm Twist / Forearm Twist | idle (diferă L/R) | egalizate pe cele 2 părți, apoi reglate fin |
| Hand In-Out | ~0 | ±0.2, cu încheieturile spre interior |
| Degete (Index, deoarece doar indexul e mapat) | 0 | -0.5 … -0.7 (închis pe mâner) |

- Oscilația de efort (jitter-ul -0.52 … -0.6 pe Front-Back) se păstrează, ca delta peste noile valori.
- Opțional: un editor script nou, `Assets/Editor/Cutscenes/SwordPullArmFix.cs`, aplică delta-urile automat pe cele 4 clipuri `_v2`. Așa repetăm reglajul fără muncă manuală.
- În Timeline înlocuim clipurile cu versiunile `_v2`. Păstrăm timpii și blend-urile.

**Strat B: IK pe mâini (precizie)**
- Instalăm `com.unity.animation.rigging` (pachet nou, se anunță).
- În `Lich_Character_Prefab` (sau doar pe instanța din cutscene):
  - `RigBuilder` pe `MainCharacter`;
  - copil `Rig_Hands`, cu 2 constrângeri `TwoBoneIKConstraint`:
    - pentru mâna dreaptă: Root `mixamorig:RightArm`, Mid `RightForeArm`, Tip `RightHand`, cu target `Grip_R`;
    - pentru mâna stângă: Root `LeftArm`, Mid `LeftForeArm`, Tip `LeftHand`, cu target `Grip_L` de pe `SwordHammer`;
  - Hint-uri pentru coate, ca să nu se întoarcă invers.
- Script nou `CutsceneRigWeightDriver.cs`. Citește `director.time` și setează `rig.weight` după o `AnimationCurve` serializată:
  - 0 la t = 0;
  - urcă la 1 până la 1.2 (Reach);
  - 1 pe struggle #1 și #2;
  - **coboară la 0 pe 3.2–6.4 și pe SunPose (6–11.6)**, când mâinile sunt sus sau libere;
  - urcă la 1 până la 12.3 (Regrab);
  - 1 până la final, cu sabia în mână.
- **Spike de verificare (30 min) înainte de restul lucrului:** confirmăm că Animation Rigging se aplică peste clipurile Humanoid redate din Timeline. Ar trebui. Fallback: `OnAnimatorIK` cu IK Pass bifat, sau un solver two-bone simplu în `LateUpdate`.

**Criterii de acceptare**
- La struggle, ambele palme stau pe mâner, una sub alta, la ~10 cm. Coatele sunt îndoite, iar corpul se lasă pe spate.
- Mâinile nu „înoată” prin mâner și nu se încrucișează.

---

### T1.3 Urmărirea sabiei după scoatere „pâlpâie”

**Cauza exactă:**
1. Sabia **nu e legată de mână**. E animată separat, printr-un clip înregistrat cu doar 35 de chei între 14.43 și 18.4, cu spațiere inegală (de la 1 frame la 0.3 s).
2. La ~15.95 apare un **salt de poziție într-un singur frame**: de la (0.08, -0.32, -0.07) la (0.13, 0.52, 0.78).
3. Mâinile vin din clipuri de mușchi cu blend-uri diferite, deci nu pot coincide niciodată cu cheile sabiei.
4. Scale-ul neuniform al sabiei agravează problema.

**Fix:**
1. Sabia din cutscene devine o instanță a `SwordHammer.prefab` (T0). Scale-ul e corect, iar pivotul e la mâner.
2. **Între 0 și 14.4 s** sabia stă fixă în podium, fără animație. Mâinile vin la ea prin IK (T1.2-B).
3. **La 14.40** (începutul ridicării din SP_PullOut) se declanșează Signal-ul `SIG_SwordAttach`.
   - Un `SignalReceiver` de pe orchestrator apelează `WeaponSocket.Attach(sword)` pe `Socket_Weapon_R` (T0).
   - Sabia trece din podium în mâna dreaptă **în același frame** în care mâinile sunt deja pe mâner prin IK, deci nu se vede niciun salt.
   - Opțional: o tranziție de 0.08 s pe offset, ca să absoarbă eventuale diferențe de 1–2 cm.
4. Mâna stângă rămâne pe IK către `Grip_L`. Sabia urmează mâna dreaptă, iar stânga urmează sabia, deci ambele mâini rămân pe armă la FallSit și la StandUp.
5. **Ștergem sau mutăm Animation Track (2)** (clipul „Recorded (1)”) și scoatem Animator-ul de pe sabie.
6. Coliziunile sabiei se dezactivează la atașare (în `WeaponSocket.Attach`).
7. Pietricele și praf: `DisturbedDirt` și `VFX_PullBurst` (T1.5) acoperă momentul ruperii.

**Criterii de acceptare**
- Scrubbing frame cu frame pe 14.0–18.5: sabia nu sare, nu tremură și nu „alunecă” față de palme.
- La căzătura pe fund (FallSit) sabia rămâne în mână.

---

### T1.4 Camera shake

**Acum:** există doar zgomotul Perlin `Handheld_normal_mild` din `BasicMultiChannelPerlin`, cu o curbă de gain (0.35 normal, 0.06 în zoom, spike 2.2 la 14.34). Nu există Impulse Source sau Listener.

**Fix:**
1. Pe `CinemachineCamera`: adăugăm extensia **`CinemachineImpulseListener`** (gain 1, use 2D distance off).
2. Pe `Cutscene_SwordPulling`: adăugăm **`CinemachineImpulseSource`**, cu Impulse Shape *Rumble* pentru efort și un al doilea source cu shape *Explosion* pentru scoatere.
3. **Track de Signal-uri** nou în Timeline, cu `SignalReceiver` care apelează `GenerateImpulseWithForce(float)` direct din UnityEvent (nu e nevoie de script):

| Timp | Signal | Forță | Ce simbolizează |
|---|---|---|---|
| 1.9, 2.8, 3.6 | SIG_Shake_Small | 0.25 | smucituri în struggle #1 |
| 5.0, 5.8 | SIG_Shake_Small | 0.35 | struggle #2 |
| 10.4 | SIG_Shake_Pulse | 0.2 (rumble, 0.8 s) | „bătaia de inimă” la ochii roșii (T1.5) |
| 13.0, 13.6, 14.1 | SIG_Shake_Medium | 0.5–0.7, crescător | struggle #3, pământul cedează |
| **14.40** | SIG_Shake_Big | **1.5** (explosion, 0.6 s) | sabia iese |
| ~16.0 | SIG_Shake_Medium | 0.6 | impactul căderii pe fund |

4. Ajustăm curba Perlin AmplitudeGain: 0.6–0.8 pe struggle-uri (constant, „tremurul efortului”) și 0.06 în zoom (liniștea dinaintea furtunii). Spike-ul de la 14.34 se poate reduce la ~1.2, pentru că impulsul preia lovitura.
5. **Pre-condiție:** buggul #1 din T1.0 (al doilea brain) trebuie reparat. Altfel shake-ul nu ajunge pe Main Camera.

**Criterii de acceptare:** shake-ul se simte progresiv (mic → mare), lovitura de la 14.4 e clar cea mai puternică, iar shake-ul nu dă rău de mișcare (fără tremur continuu peste 1.0 gain).

---

### T1.5 Ochii devin roșii în zoom + mai mult dramatism

**Ochii.** Sunt submesh-ul 0 al `Character_Body`, cu materialul `Glow_Green`:
- base (0.424, 1, 0.584);
- emisie HDR (0.6, 4, 1.2).

**Nu modificăm materialul** (e asset comun, iar schimbarea ar rămâne permanentă în editor).

Script nou `Assets/Scripts/Cutscenes/EyeGlowController.cs`, pe `MainCharacter`:
- folosește `MaterialPropertyBlock` doar pe submesh-ul 0: `renderer.SetPropertyBlock(block, 0)`, cu `_BaseColor` și `_EmissionColor`;
- `TurnRed(float duration)`, care interpolează verde → **flash alb-fierbinte (8, 8, 8) timp de 0.1 s** → roșu HDR ≈ (6, 0.25, 0.15) pe 0.6 s;
- `SetRed(bool)` setează roșul instant, pentru final și pentru modelul nou;
- opțional: un mic `Point Light` roșu (range 0.4, intensitate 0→1.5) în fața ochilor, care colorează interiorul glugii (materialul `Void` e aproape negru, deci strălucirea se va vedea bine).

**Momentul:** zoom-ul are FOV 22 la 9.95 și FOV 15 la 10.4, iar ținta camerei e la +0.81 m, adică pe față. Signal-ul `SIG_EyesRed` vine **la 10.3**, astfel încât schimbarea se face exact la vârful zoom-ului. Zoom-ul trebuie ținut 0.6–0.8 s după schimbare: mutăm cheia FOV 42 de la 12.28 la ~12.6, ca să nu iasă prea repede.

**Dramatism, în ordinea impactului:**
1. **Volume de cutscene.** GO nou `CutsceneVolume` (local Volume, priority 10, profil nou `Assets/Cutscenes/SwordPull/VP_SwordPull.asset`). Ponderea `weight` e animată din Timeline, printr-un Animator pe GO.
   - Vignette: 0.25 → 0.45, în zoom.
   - Color Adjustments: contrast +15, saturație -25, iar după ochii roșii un color filter ușor roșiatic.
   - Chromatic Aberration: 0 → 0.5, doar 0.3 s la 14.4.
   - Film Grain: 0.15.
   - Bloom: intensitate 1.2 în zoom.
2. **Lumini.**
   - `RuneLight` de pe sabie: 2.6 → 5–6 pe struggle-uri, pâlpâind (curbă noise).
   - La ochii roșii, `RuneLight` trece și el din verde în **roșu** (sabia „îl alege”). Opțional, `M_Rune_Glow` primește emisie roșie prin property block.
   - La 14.4, flash scurt de 0.15 s la intensitate 15.
   - Directional Light: intensitatea scade cu ~35% între 12.5 și 14.4 („cerul se întunecă”) și revine la final.
3. **Particule.**
   - `MagicMotes`: rata de emisie crește progresiv pe struggle-uri.
   - `VFX_PullBurst` (nou): burst radial de pietricele, praf și scântei de rune la 14.28–14.4. Se leagă de Control Track-ul existent, reparând referința nulă.
   - Praf în inelul de rune pe struggle #3.
4. **Slow-motion.** Signal `SIG_SlowMo` la 14.40: `Time.timeScale` 1 → 0.3 → 1 în 0.5 s real (script nou `CutsceneTimeScale.cs`). Directorul e pe GameTime, deci încetinește împreună cu tot.
5. **Cameră.** Dutch tilt -4…-6° pe struggle #3 (12.5–14.4) și revenire la 0 la pull-out. Zoom-ul către ochi pornește puțin mai agresiv: FOV 22 la ~9.6 în loc de 9.95.
6. **Audio** (proiectul nu are niciun sunet). Audio Track în Timeline:
   - rumble de pământ (struggle-uri);
   - bătăi de inimă (8–11 s);
   - șoaptă sau „whoosh” la ochii roșii;
   - BOOM și sunet de piatră spartă la 14.4;
   - thud la FallSit.
   - Sursă: fișiere CC0 (ex. freesound, Kenney). **Le descarcă Roland**, apoi intră în `Assets/Audio/Cutscenes/`.

**Criterii de acceptare:** în zoom se vede clar trecerea verde → alb → roșu a ochilor, iar lovitura de la 14.4 combină shake, flash, burst, slow-mo și sunet. Ochii rămân roșii după cutscene.

---

### T1.6 Fade in / fade out + final: player în centrul podiumului, cu „modelul nou” care ține sabia

**Fader** (nou, reutilizabil):
- Prefab `Assets/UI/ScreenFader.prefab`: Canvas Screen Space Overlay, sort order 100, Image negru full-screen și CanvasGroup.
- `Assets/Scripts/UI/ScreenFader.cs` (`Alpha.UI`):
  - `FadeOut(float d)` și `FadeIn(float d)`, ca coroutine sau eveniment de final;
  - folosește **unscaled time**, ca să meargă și cu slow-mo;
  - curba e ease-in-out.

**Secvența de start:** trigger → tranziția de aliniere (T1.1) → FadeOut 0.35 s → în negru se comută pe camera de cutscene și se dă `Play()` → FadeIn 0.6 s.
- Motivul: gameplay-ul nu folosește Cinemachine, deci trecerea pe CinemachineCamera e o tăietură bruscă. Fade-ul o ascunde.

**Secvența de final:**
1. Shot-ul Cinemachine se prelungește de la 17.38 la ~19.2, ca să nu rămână camera „orfană” după 17.38.
2. **18.3:** Signal `SIG_End`, apoi FadeOut 0.8 s.
3. În negru (`SwordPullCutscene.End()`):
   - `director.Stop()` și CinemachineCamera dezactivată (Priority 0);
   - player-ul e teleportat în **centrul podiumului**:
     - GO `Podium_SpawnPoint`, la ≈ (-8.157, 2.64, -4.975). Vârful podiumului e la y ≈ 1.76, iar root-ul player-ului stă la ~+0.88 față de sol, ca în poziția salvată;
     - privește spre drumul hub-ului;
     - poziția se setează cu `rb.position` și `transform.position`, iar velocity devine 0;
   - **„modelul nou”** se aplică (vezi mai jos);
   - se reactivează `rb.isKinematic=false`, `PlayerController`, `PlayerDirectionalAnimation`, `PlayerInput` și `CameraController`. Camera se repoziționează singură în spatele player-ului;
   - (T4) se dă semnalul `OnSwordClaimed` pentru spawn-ul primilor inamici, „gardienii trezesc”.
4. FadeIn 1.0 s.

**„Modelul nou care ține sabia”: decizie de design.** Nu putem schimba GameObject-ul `MainCharacter`, pentru că scripturile țin Animator-ul în cache. Avem 2 variante:

| Variantă | Ce înseamnă | Efort |
|---|---|---|
| **A (recomandat pentru început)** | Același Lich + sabia în `Socket_Weapon_R` + ochi roșii permanenți + layer-ul de „hold” activ (T2) + mici accente „corupte”: rune roșii pe armură, emisie magenta → roșu prin property block | mic |
| B (v2) | Variantă de mesh nouă, „Lich Ascended”, din Blender: armură nouă și crăpături roșii, **skinned pe același schelet `mixamorig`**. E importată ca SkinnedMeshRenderer în plus sub `MainCharacter`, cu același Avatar; la final dezactivăm `Character_Body` și activăm `Character_Body_Ascended`. | mare (Blender) |

Varianta B nu atinge niciun script existent, pentru că Animator-ul rămâne același.

**Criterii de acceptare**
- Cutscene-ul începe și se termină cu fade curat, fără tăieturi bruște.
- La final player-ul stă în centrul podiumului, cu sabia-ciocan în mână și ochii roșii, iar controlul revine imediat. Camera e în spatele lui, iar WASD funcționează.
- Trigger-ul nu se mai poate porni a doua oară.

---

### T1.7 Ordinea de lucru pe cutscene și ce se modifică

1. T1.0: fix-uri de cablaj (scenă și timeline).
2. T1.1: trigger și blocare (scripturi noi; GO-uri noi în scenă sau în prefab-ul de cutscene).
3. T1.3 + T1.2-B: atașarea sabiei și IK-ul (după T0).
4. T1.2-A: clipurile `_v2`.
5. T1.4: shake.
6. T1.5: ochi și dramatism.
7. T1.6: fade și final.

**Fișiere atinse:** `SampleScene.unity` (anunțat), `Cutscene_SwordPullingTimeline.playable`, clipuri noi `_v2`, `Lich_Character_Prefab.prefab` (socket-uri și rig), `Packages/manifest.json` (Animation Rigging) și scripturi **noi** în `Assets/Scripts/Cutscenes/`, `Assets/Scripts/UI/` și `Assets/Scripts/Combat/`.

---

## T2. Ținerea sabiei în mână ca un ciocan (logică + animație de bază)

**Concept:** după cutscene, sabia are capul de piatră lipit de vârf, deci e grea în cap. Lich-ul o poartă **ca pe un baros pe două mâini**:
- mâna dreaptă pe mâner, sus;
- mâna stângă mai jos pe mâner, spre lamă;
- capul de piatră sprijinit pe **umărul drept** în idle și la mers;
- la alergare, ciocanul e ținut în diagonală, cu capul în spate.

### Animator: modificăm asset-ul, nu codul
- **Avatar Mask nou:** `Assets/Character/Animations/Masks/AM_UpperBody.mask`. Include Body (Spine, Chest și UpperChest prin humanoid), Head, Left/Right Arm, Left/Right Hand și IK-urile de mână. Exclude picioarele și root-ul.
- **Layer nou în `PlayerAnimation.controller`:** `UpperBody_Weapon`.
  - Mask `AM_UpperBody`, blending Override, **Weight 0** implicit.
  - IK Pass bifat, ca fallback.
- Stări:
  - `Hold_Idle`: hammer pe umăr;
  - `Hold_Move`: blend tree pe `Speed`, cu ciocanul legănat ușor la mers și ținut în diagonală la alergare;
  - `Hold_Crouch`, opțional.
- **Parametru nou** `IsArmed` (bool). Ca structură de date, adăugarea unui parametru nu e o schimbare a scripturilor.
- Picioarele rămân pe layer-ul de bază (locomoția existentă). Doar trunchiul și brațele se schimbă.

### Animații: sursa
- **Mixamo** (descărcat de Roland, care e logat). Setări: FBX for Unity, 30 fps, **Without Skin**, „In Place” pentru locomoție.
- Căutări sugerate:
  - „Great Sword Idle”, „Great Sword Walk”, „Great Sword Run”;
  - alternativ „Two Handed Idle”, „Heavy Weapon Idle” sau „Holding Hammer” (alegem după cum arată pe ciocan).
- Import:
  - Rig Humanoid, **Copy From Other Avatar** = avatarul `Lich_Character`;
  - Loop Time pentru idle și locomoție;
  - Root Transform Rotation și Position (Y): Bake Into Pose.
- Destinație: `Assets/Character/Animations/Weapon/` (folder nou).
- Fallback fără Mixamo: un pose static de hold, creat din fereastra Animation (clip de un frame) + IK (de mai jos). Arată decent dacă îl combinăm cu un script de „sway”.

### Scripturi noi (`Assets/Scripts/Combat/`, `Alpha.Combat`)
1. `WeaponHolder.cs`, pe Player.
   - Referințe: Animator (din `MainCharacter`), `WeaponSocket` și prefab-ul `SwordHammer`.
   - `Equip()`: instanțiază sau atașează arma la socket, `animator.SetBool("IsArmed", true)` și trece layer weight-ul `UpperBody_Weapon` de la 0 la 1 în 0.25 s.
   - `Unequip()`.
   - Proprietăți și evenimente: `bool IsArmed`, `event Action<bool> onArmedChanged`.
   - Apelat de `SwordPullCutscene.End()`. Opțional, salvează starea „are sabia”.
2. `WeaponHandIK.cs`, pe `MainCharacter` (lângă Animator).
   - Reutilizează rig-ul din T1.2-B (`Rig_Hands`).
   - Cât timp `IsArmed`, mâna stângă e permanent pe `Grip_L` (weight 1), iar mâna dreaptă vine din animație, fiind ea socket-ul.
   - În timpul atacului (T3), weight-ul stângii rămâne 1, ca ambele mâini să fie pe coadă.
3. `HeavyWeaponSway.cs` (opțional, „senzația de greutate”):
   - un offset mic de rotație pe socket, în `LateUpdate`, după viteza player-ului (inerție cu spring-damper);
   - la oprire bruscă, capul de piatră „mai merge” 2–3° și revine.
   - E pur vizual și nu atinge mișcarea.

### Ce NU facem
- Nu încetinim player-ul cât timp ține ciocanul: viteza e în `PlayerController`, privată, deci ar însemna să modificăm scriptul. Dacă vrei asta, cerem voie explicit.

### Criterii de acceptare
- După cutscene, idle, walk, run, jump și crouch funcționează, iar trunchiul și brațele țin ciocanul corect, pe ambele mâini.
- Capul de piatră nu intră în corp sau în glugă, iar sabia nu tremură.
- Fără armă (`IsArmed=false`) totul arată exact ca acum.

---

## T3. Animația de atac: ciocanul lovește pământul (ground slam)

### Input: asset nou, cod nou, `InputHandler` neatins
- În `Assets/PlayerInputActions/PlayerActions.inputactions` (harta „Player”) se adaugă acțiunea **`Attack`**: Button, `<Mouse>/leftButton`, plus opțional `<Gamepad>/buttonWest`.
- `PlayerInput` e pe **Send Messages**, deci Unity apelează automat `OnAttack(InputValue)` pe **orice** componentă de pe GO-ul Player.
- Script nou `Assets/Scripts/Combat/PlayerCombat.cs` (`Alpha.Combat`), pe Player:
  - `OnAttack(InputValue v)`: dacă `v.isPressed && weaponHolder.IsArmed && !isAttacking && grounded && cooldown ok`, atunci `animator.SetTrigger("Attack")`;
  - trigger-ul `Attack` există deja în controller și e nefolosit, deci îl folosim pe el;
  - opțional, ascultă `inputHandler.onMoveUpdate` ca să știe direcția pentru rotația spre țintă.

### Animator
- Starea `Attack_Slam` merge **pe tot corpul**, pentru că picioarele participă (genuflexiune la impact):
  - fie un layer nou `FullBody_Action` (fără mask, Override, weight 1 doar în timpul atacului),
  - fie în Base Layer: `Any State → Attack_Slam`, cu condițiile `Attack` + `IsArmed`, fără „Can Transition To Self”, iar ieșirea pe Exit Time (~0.9) către Idle sau locomoție.
- Clip Mixamo. Căutări: „Great Sword Slash” (overhead), „Standing Melee Attack Downward”, „Great Sword Jump Attack” (variantă de săritură, pentru mai târziu). Import Humanoid, Copy Avatar Lich și **In Place**.
- **Fazele și timpii țintă** (se ajustează pe clipul ales):

| Fază | Timp | Ce se vede |
|---|---|---|
| Anticipare (windup) | 0.00–0.45 s | ridică ciocanul deasupra capului, se lasă pe spate, rune-le de pe armă se încarcă |
| Lovitura (swing) | 0.45–0.55 s | coborâre foarte rapidă, cu smear sau trail pe capul de piatră |
| **Impact** | **~0.55 s** | piatra lovește pământul, cu genuflexiune |
| Ținere | 0.55–0.85 s | ciocanul rămâne înfipt, praful se ridică |
| Recuperare | 0.85–1.40 s | smulge ciocanul și revine la pose-ul de hold |

- **Animation Events** în setările de import ale clipului: `SlamWindup` (0.10), `SlamImpact` (cadrul de impact) și `SlamEnd` (1.30).
- Event-urile ajung pe GO-ul cu Animator (`MainCharacter`), deci avem nevoie de un **releu**:
  - script nou `AnimationEventRelay.cs` pe `MainCharacter`;
  - metodele `SlamWindup()`, `SlamImpact()` și `SlamEnd()` ridică evenimente C# la care se abonează `PlayerCombat`.

### Ce se întâmplă la impact (`PlayerCombat.OnSlamImpact`)
1. **Damage în arie:** `Physics.OverlapSphere(ImpactPoint.position, radius 2.0, layerMask Enemy)`.
   - Damage 30 în centru, scade liniar până la 15 la margine.
   - Prin `IDamageable.TakeDamage(DamageInfo)` (din T4).
   - Fiecare țintă e lovită o singură dată per atac.
2. **Knockback:** direcția e dinspre impact spre inamic, cu forța 6 orizontal și 3 vertical (pe `NavMeshAgent` printr-un `EnemyKnockback`, pe Rigidbody prin `AddForce`).
3. **Hit-stop:** `Time.timeScale = 0.05` timp de 0.06 s real, apoi 1. Doar dacă a lovit ceva.
4. **Camera shake în gameplay.** Camera de gameplay **nu e Cinemachine** (e `CameraController`), deci Impulse nu merge aici.
   - Script nou `Assets/Scripts/Combat/CameraShake.cs` pe Main Camera, cu `[DefaultExecutionOrder(1000)]`. Rulează **după** `CameraController.LateUpdate` și adaugă un offset de shake (Perlin, decay exponențial, amplitudine 0.15 m și 1.5°, 0.35 s).
   - Nu atinge `CameraController`.
5. **VFX** (`Assets/VFX/Combat/`, folder nou):
   - inel de praf radial (`VFX_SlamDust`);
   - pietre și moloz sărind (`VFX_SlamDebris`, cu coliziune pe sol);
   - undă de șoc cu rune roșii pe sol (`VFX_SlamRuneWave`);
   - **decal** de crăpătură care dispare în 4 s (URP Decal Projector; necesită **Decal Renderer Feature** în `PC_Renderer.asset`, de anunțat);
   - opțional, trail pe capul de piatră în faza de swing (Trail Renderer activ doar 0.45–0.6 s).
6. **Sunete:** whoosh la swing și BOOM de piatră la impact (fișiere descărcate de Roland).

### Blocarea mișcării în timpul atacului: decizie
| Opțiune | Cum | Risc |
|---|---|---|
| A (recomandat) | La `SlamWindup` setăm `PlayerController.enabled=false` și `rb.velocity=Vector3.zero` (păstrăm Y), iar la `SlamEnd` îl reactivăm. Atacul e permis doar pe sol. | Gravitația custom e oprită ~1 s, dar fiind pe sol nu contează. |
| B | Fără blocare: poți ataca din mers. | Arată ciudat: alunecă în timp ce lovește. |

### Criterii de acceptare
- Click stânga cu sabia în mână declanșează animația completă (windup → impact → recovery).
- Spam-click nu repornește animația; există un cooldown de ~0.3 s după recovery.
- Impactul coincide **vizual** cu momentul în care piatra atinge solul. Shake-ul, praful, decal-ul și damage-ul pornesc toate în același frame.
- Fără sabie, click-ul nu face nimic.

---

## T4. Monștri / mobi: design + implementare + animații de bază

### Decizia de design (după tema jocului)
- **Tema:**
  - un Lich (schelet-necromant) în robă mov, cu armură întunecată și ochi verzi;
  - un altar de piatră pe un deal, cu menhiruri și **rune verzi**;
  - dealuri cu iarbă uscată, la apus;
  - o sabie cu cap de piatră folosită ca **ciocan**.
- **Propunere:** **„Străjerul de Menhir” (Stone Warden).** Un golem din lespezi de piatră, cu rune verzi, care păzește altarul. Când Lich-ul smulge sabia (și ochii lui devin roșii), gardienii se trezesc din pământ și din menhiruri.
- De ce acest monstru:
  1. **Narativ:** legătură directă cu cutscene-ul. Gardienii runelor verzi luptă contra Lich-ului care a devenit „roșu”, iar contrastul de culoare verde/roșu face lupta foarte lizibilă.
  2. **Gameplay:** un ciocan zdrobește piatra, deci lovitura are sens, iar moartea poate fi o **spargere în bucăți** foarte satisfăcătoare.
  3. **Asset:** reutilizează materialele de piatră existente (`M_Gravel043_Stone` și texturile de menhir) și `M_Rune_Glow`, deci se potrivește vizual cu altarul.
  4. **Tehnic:** e humanoid, deci merge cu animațiile Mixamo, iar skinning-ul e **rigid**: fiecare lespede e 100% pe un os, simplu și fără deformări urâte.
  5. Un schelet ar fi fost redundant: player-ul e deja un mort-viu.

**Fișa de design (MVP: 1 tip de mob)**

| Aspect | Specificație |
|---|---|
| Înălțime | 2.0–2.2 m (puțin peste player, care are 1.86 m), cocoșat |
| Siluetă | Umeri foarte lați (V), antebrațe și pumni masivi (~2× proporția umană), picioare scurte și groase, cap mic „înfundat” între umeri |
| Corp | Lespezi de piatră suprapuse, cu goluri între ele prin care se vede lumină verde. Smocuri de iarbă uscată și mușchi pe umeri, ca terenul din hub. |
| Punct focal | „Inima”: o piatră de rună în piept, cu emisie verde puternică, plus 2 fante de ochi verzi |
| Paletă | Gri-maro cald (ca menhirurile) + verde rune emisiv (aceeași nuanță ca `M_Rune_Glow`: (0.965, 3.93, 0.26) HDR). Fără mov sau roșu, culorile player-ului. |
| Polycount | LOD0 10–15k tris, LOD1 ~50%, LOD2 ~25% (sufixe `_LOD0/1/2`) |
| Materiale | 1 material URP Lit, set 2K: `T_StoneWarden_BaseColor`, `_Normal` (GL), `_MetallicSmoothness`, `_AO`, `_Emission` (masca runelor) |
| Variantă (v2) | „Străjer Mare” (Elite): 3 m, mai lent, cu atac slam propriu, aceeași bază recolorată sau scalată |

**Statistici de pornire (de echilibrat)**

| HP | Damage | Rază atac | Aggro | Pierde ținta | Viteză patrulare / urmărire | Cooldown atac | Telegraf |
|---|---|---|---|---|---|---|---|
| 60 (2–3 lovituri de ciocan) | 10 | 1.8 m | 12 m | 20 m | 1.5 / 3.2 m/s (player: 5) | 2.0 s | windup 0.6 s, runele se aprind mai tare |

### T4-A. Blender: modelul (poate începe oricând)
Etape: blockout → modelare → retopologie → UV → texturare PBR → rig → export → QA.

1. **Referință și blockout:** cuburi pentru proporții, plus o referință de 1.86 m (capsula player-ului) în scenă. Screenshot-uri din față, din lateral și din 3/4 pentru aprobare. **Aici se oprește pentru feedback de la Roland.**
2. **Modelare:** lespezi separate (hard-surface cu bevel), sculpt de crăpături și eroziune doar pe high-poly.
3. **Low-poly și retopo** în bugetul de mai sus, plus UV-uri cu texel density consistent.
4. **Bake** high → low (normal, AO și curvature), apoi texturare:
   - piatră PBR: din texturile existente din `Assets/Materials` sau Poly Haven (CC0);
   - masca de emisie pentru rune și crăpături;
   - packing URP (Metallic în R, Smoothness = 1 - roughness în A).
5. **Rig:**
   - schelet cu nume compatibile Mixamo/Humanoid: `Hips` ca root pentru spate și picioare, Spine, Chest, Neck, Head, Shoulder/Arm/ForeArm/Hand (L și R), UpLeg/Leg/Foot (L și R);
   - **skinning rigid**: fiecare lespede 100% pe osul ei;
   - T-pose.
6. **Mesh de moarte** `SM_StoneWarden_Debris`: aceleași lespezi ca obiecte separate, cu pivot pe centrul fiecăreia. În Unity primesc Rigidbody și MeshCollider convex.
7. **Export FBX** cu setările din pipeline (FBX_SCALE_UNITS, -Z forward, Y up, fără leaf bones, deform only) în `Assets/Enemies/StoneWarden/Models/` (folder nou).
8. **QA:** verificare în Unity la scară reală lângă player, cu screenshot-uri în lumina scenei.


### T4-B. Animații de bază (Mixamo, retarget Humanoid)
Pack-ul „Mutant” se potrivește perfect unui golem greoi. Proiectul folosește deja „Mutant Jumping”.

| Stare | Clip sugerat (Mixamo) | Note |
|---|---|---|
| Idle | Mutant Breathing Idle | loop |
| Walk | Mutant Walking | In Place, loop |
| Run / Chase | Mutant Run | In Place, loop |
| Aggro / Spawn | Mutant Roaring (sau ieșire din pământ: animație de ridicare + translație) | la prima vedere a player-ului |
| Attack | Mutant Punch sau Mutant Swiping | event `EnemyHit` la cadrul de contact |
| Hit | „Hit Reaction” sau „Big Hit” | scurt, ~0.4 s |
| Death | Mutant Dying, **sau** spargere în moloz (recomandat) | — |

- Import: Humanoid cu **Create From This Model** pe FBX-ul golemului, apoi animațiile cu Copy From Other Avatar (golem).
- **Animator Controller nou:** `Assets/Enemies/StoneWarden/AC_StoneWarden.controller`.
  - Parametri: `Speed` (float), `Attack` (trigger), `Hit` (trigger), `IsDead` (bool) și `Aggro` (trigger).
  - Blend tree pe `Speed`: Idle → Walk → Run.

### T4-C. Unity: implementare (scripturi noi)
**Pachete și setări** (se anunță în chat):
- `com.unity.ai.navigation`, pentru `NavMeshSurface`. Există doar modulul legacy `com.unity.modules.ai`.
- Layer nou `Enemy` și tag nou `Player` (`ProjectSettings/TagManager.asset`). Setăm layer-ul pe prefab-ul inamicului.
- `NavMeshSurface` pe `Environment` (HubTerrain și podium), cu Agent Type „Golem”: radius 0.5, height 2.1, step 0.4. **Bake în scenă** (lucru în scenă, anunțat).

**Scripturi noi**

| Fișier | Namespace | Rol |
|---|---|---|
| `Assets/Scripts/Combat/IDamageable.cs` | Alpha.Combat | `void TakeDamage(DamageInfo info)` |
| `Assets/Scripts/Combat/DamageInfo.cs` | Alpha.Combat | struct: amount, point, direction, source |
| `Assets/Scripts/Combat/Health.cs` | Alpha.Combat | maxHP, currentHP, `TakeDamage`, evenimentele `onDamaged` și `onDied`, i-frames opționale |
| `Assets/Scripts/Combat/PlayerHealth.cs` | Alpha.Combat | pe Player (componentă nouă, nu modifică nimic). La moarte: fade (`ScreenFader`) și respawn la `Podium_SpawnPoint`. |
| `Assets/Scripts/Enemies/EnemyBrain.cs` | Alpha.Enemies | FSM: Dormant → Spawn → Idle/Patrol → Chase → Attack → Hit → Dead. Folosește `NavMeshAgent`. Rotește spre player în Attack. Distanțele și timpii din tabelul de statistici. |
| `Assets/Scripts/Enemies/EnemyAnimatorBridge.cs` | Alpha.Enemies | `agent.velocity` → `Speed`, plus trigger-ele Attack, Hit și IsDead |
| `Assets/Scripts/Enemies/EnemyMeleeHit.cs` | Alpha.Enemies | primește event-ul `EnemyHit` din animație; OverlapSphere în fața pumnului, raza 0.8, damage pe player |
| `Assets/Scripts/Enemies/EnemyKnockback.cs` | Alpha.Enemies | primește impulsul de la slam și îl aplică prin `agent.Move` cu decay |
| `Assets/Scripts/Enemies/ShatterOnDeath.cs` | Alpha.Enemies | la moarte: ascunde mesh-ul skinned și instanțiază `StoneWarden_Debris` cu explozie de forță dinspre lovitură; după 4 s bucățile se scufundă în sol și se distrug |
| `Assets/Scripts/Enemies/EnemySpawner.cs` | Alpha.Enemies | puncte de spawn în jurul altarului (la baza menhirurilor). Pornește la evenimentul `OnSwordClaimed` din cutscene: un val de 3, apoi încă 2 după ce mor primii. Spawn = ieșire din pământ (translație -1.5 m → 0 în 1.2 s + praf). |

**Prefab:** `Assets/Enemies/StoneWarden/StoneWarden.prefab`, cu:
- modelul, Animator-ul cu `AC_StoneWarden`, `NavMeshAgent`;
- `CapsuleCollider` (r 0.5, h 2.1) și Rigidbody kinematic;
- `Health`, `EnemyBrain`, `EnemyAnimatorBridge`, `EnemyMeleeHit`, `EnemyKnockback` și `ShatterOnDeath`;
- layer `Enemy`.

**Feedback vizual de lovire** (important pentru „game feel”):
- flash alb al emisiei 0.08 s, prin property block;
- runele golemului pâlpâie;
- particule de așchii de piatră în punctul de impact.

### Criterii de acceptare T4
- După cutscene ies 3 golemi din pământ în jurul altarului.
- Te observă la 12 m, te urmăresc pe NavMesh (inclusiv pe/de pe podium) și atacă la 1.8 m cu un telegraf vizibil.
- Ciocanul (T3) îi lovește în arie, cu knockback. Mor din 2–3 lovituri și se sparg în bucăți.
- Lovitura lor scade din HP-ul player-ului; la 0 HP urmează fade și respawn.
- Arată coerent cu altarul: aceeași piatră, același verde de rune.

---

## 5. Ce se face manual (din contul propriu)
- Descărcarea animațiilor Mixamo (lista din T2, T3 și T4-B).
- Descărcarea sunetelor CC0.
- Testul de „feel” în Play Mode.
- Confirmarea deciziilor de mai jos.

## 6. Decizii deschise (am pus între paranteze recomandarea mea)
1. Ce înseamnă „model nou de caracter” la final? (**A**: același Lich, cu sabie + ochi roșii + accente; B: mesh nou din Blender, pe același schelet.)
2. Trigger-ul pornește automat la intrarea pe podium (**da**, cum ai cerut), sau cu tastă de interacțiune „E”? E presupune o acțiune nouă în input.
3. Mișcarea blocată în timpul atacului? (**A**: blocată.)
4. Monstrul: Străjerul de Menhir (**recomandat**) sau altceva?
5. Facem `Cutscene_SwordPulling` prefab, cu re-binding la runtime? (**da**, ca să nu blocăm scena; e o schimbare structurală, deci se anunță.)
6. Capul de piatră de ~1.2 m lățime rămâne sau îl refacem în Blender (T0-B)? Decidem după verificarea vizuală din T0.
