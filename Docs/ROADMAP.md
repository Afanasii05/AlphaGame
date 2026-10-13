# AlphaGame: roadmap cronologic (bază: commit c599ca7, 2026-10-13)

Ordinea este gândită așa încât **fiecare pas să poată fi testat imediat în Play Mode**. La fiecare pas jocul trebuie să rămână jucabil.
Detaliile tehnice ale taskurilor T0–T4 sunt în [`TASKS_DETALIAT.md`](TASKS_DETALIAT.md). Aici sunt ordinea, fix-urile noi și punctele de test.

## Reguli (se aplică la tot)

1. **Niciodată pe `main`.** Fiecare fază are branch-ul ei (`feature/roland-<subiect>`). Înainte de lucru: `git checkout main && git pull && git checkout <branch> && git merge main`.
2. **O singură persoană pe scenă.** Se lucrează în prefab-uri. Modificările în `SampleScene.unity` se anunță în chat înainte.
3. **Nu mutăm și nu redenumim fără anunț.** Fișierele se mută împreună cu `.meta`, ideal din Unity Editor. Restructurările merg în commit separat.
4. **Nu modificăm structura scripturilor existente.** `InputHandler`, `PlayerController`, `CameraController` și `PlayerDirectionalAnimation` rămân neatinse. Funcționalitatea nouă intră în **scripturi noi**, care se abonează la evenimentele `InputHandler` sau activează și dezactivează componentele existente.
5. **Fișiere comune** (se anunță la fiecare modificare): `PlayerAnimation.controller`, `PlayerActions.inputactions`, `Cutscene_SwordPullingTimeline.playable`, `Lich_Character_Prefab.prefab`, `Packages/manifest.json`, `ProjectSettings/*`, `.gitignore`.

## Privire de ansamblu

| Fază | Ce | Branch | Rezultat testabil |
|---|---|---|---|
| **F0** | Reparații și curățenie pe ce există deja | `feature/roland-fixes` | Mișcarea și camera merg corect, repo-ul e curat |
| **F1** | Cutscene: cablaj reparat + trigger + fade | `feature/roland-cutscene-base` | Cutscene-ul pornește la podium și se termină curat |
| **F2** | Arma: prefab `SwordHammer` + socket-uri (T0) | `feature/roland-sword-socket` | Sabia stă corect în mână |
| **F3** | Cutscene: mâini pe mâner + atașarea sabiei (T1.2, T1.3) | `feature/roland-cutscene-hands` | Nu mai pâlpâie, mâinile sunt pe mâner |
| **F4** | Cutscene: shake, ochi roșii, dramatism, final (T1.4–T1.6) | `feature/roland-cutscene-polish` | Cutscene complet |
| **F5** | Ținerea sabiei ca un ciocan (T2) | `feature/roland-hammer-hold` | Locomoție cu arma în mână |
| **F6** | Atac slam + bazele de combat + manechin de test (T3) | `feature/roland-attack-slam` | Lovitura se simte, face damage pe manechin |
| **F7** | Monstrul Stone Warden (T4) | `feature/roland-enemy-golem` | Luptă completă după cutscene |
| **F8** | Viața player-ului, respawn, audio, polish → merge în `main` | `feature/roland-loop-polish` | Bucla de joc completă |

**În paralel, oricând după F0:** modelul golemului în Blender (T4-A) și, dacă e nevoie, capul de piatră al ciocanului (T0-B).

```
F0 ─> F1 ─> F2 ─> F3 ─> F4 ─> F5 ─> F6 ─> F7 ─> F8
            └── T0-B (Blender, opțional)
      └──────────── T4-A Blender golem (paralel) ─────┘
```

---

## F0. Reparații pe ce e deja implementat

Starea verificată: consola Unity nu are erori sau warning-uri. Rigidbody-ul player-ului e corect (Interpolate on, rotații blocate). Problemele de mai jos sunt de setări, de asset-uri sau lipsuri, nu erori de compilare.

### F0.1 Tastele Sprint și Crouch sunt inversate *(asset, nu script)*
- **Problema:** în `PlayerActions.inputactions`, Sprint = `leftCtrl` și Crouch = `leftShift`. Comentariile din `InputHandler` spun invers (Shift = sprint, Ctrl = crouch), iar convenția din jocuri e tot Shift = sprint.
- **Fix:** în editorul de Input Actions schimbăm binding-urile între ele. Numele acțiunilor rămân la fel, ca să nu se schimbe `OnSprint` și `OnCrouch`.
- **Test:** Shift + W aleargă, Ctrl + W merge ghemuit.

### F0.2 Player-ul are rotația înclinată în scenă
- **Problema:** player-ul e salvat cu Euler ≈ (9.7, -125, -0.35). `CameraController.Start` copiază rotația player-ului, deci camera pornește înclinată.
- **Fix:** X = 0 și Z = 0 pe transform-ul Player (scenă, anunțat).
- **Test:** la Play, orizontul e drept din primul frame.

### F0.3 Valorile din scenă diferă de cele din cod
- Codul are jump 15 și crouch ×0.1. Scena are jump 13 și crouch ×0.2. **Contează valorile din scenă** (Inspector).
- **Fix:** nu schimbăm codul. Stabilim valorile finale în Inspector și le notăm aici:

| Câmp | Cod | Scenă | Final (de decis la test) |
|---|---|---|---|
| moveSpeed | 5 | 5 | 5 |
| sprintMultiplier | 2.5 | 2.5 | 2.0–2.5 |
| crouchMultiplier | 0.1 | 0.2 | **0.4–0.5**: 0.2 înseamnă 1 m/s, aproape pe loc |
| jumpForce | 15 | 13 | 13 |

- **Test:** crouch-walk se simte natural, iar animația de crouch nu „patinează”.

### F0.4 Camera trece prin pereți și teren *(script nou)*
- **Problema:** `CameraController` pune camera mereu la 5 m în spate, fără coliziune.
- **Fix:** script nou `Assets/Scripts/Player/Camera/CameraCollision.cs` (`Alpha.Player.Camera`), pe Main Camera, cu `[DefaultExecutionOrder(1000)]`. Rulează **după** `CameraController.LateUpdate`:
  - face un SphereCast (rază 0.25) de la punctul de pivot (player + 1.5 m) spre poziția calculată a camerei, cu layerMask care exclude player-ul;
  - dacă lovește ceva, apropie camera la `hit.distance - 0.1`, cu revenire lină (SmoothDamp) când obstacolul dispare;
  - e inactiv când `cameraController.is3D == false` (first person). `is3D` e proprietate publică, deci nu schimbăm nimic.
- Același script va primi mai târziu shake-ul din F6, sau rămân 2 scripturi cu ordine 1000 și 1001.
- **Test:** cu spatele lipit de menhir sau de podium, camera nu mai intră în piatră.

### F0.5 Cursorul rămâne blocat pentru totdeauna *(script nou + acțiune nouă)*
- **Problema:** `CameraController.Start` blochează cursorul și nu există cale de a-l elibera. În Editor e incomod, iar pentru un meniu de pauză e obligatoriu.
- **Fix:**
  - acțiune nouă `UnlockCursor` (`<Keyboard>/escape`) în harta Player;
  - script nou `Assets/Scripts/UI/CursorManager.cs` (`Alpha.UI`), pe Player, cu `OnUnlockCursor(InputValue)`. Merge pentru că `PlayerInput` e pe Send Messages;
  - la Esc, cursorul se eliberează; la click în fereastră, se blochează la loc;
  - metode publice `Lock()` și `Unlock()`, folosite mai târziu de meniul de pauză și de UI.
- **Test:** Esc arată cursorul, iar click-ul îl ascunde. Între timp mouse-ul nu rotește camera.

### F0.6 Prefab pentru Player *(structural, se anunță)*
- **Problema:** Player-ul, Main Camera și cutscene-ul există doar în scenă. Orice modificare la player blochează scena pentru toată echipa.
- **Fix:** facem `Assets/Character/Player.prefab` din GameObject-ul Player (cu `MainCharacter` în el). Referința `cameraController` rămâne ca override pe instanța din scenă.
- De acum, F2, F5 și F6 se lucrează în prefab, nu în scenă.
- **Test:** Play Mode e identic. Modificările făcute în prefab apar în scenă.

### F0.7 Animator: stări cu același nume
- **Problema:** în `PlayerAnimation.controller`, ambele stări de crouch se numesc `mixamo_com`. Se încurcă la debug și la tranziții.
- **Fix:** redenumim stările (doar numele stării din controller, nu clipurile) în `Crouch_Idle` și `Crouch_Walk`. Codul folosește parametri, nu nume de stări, deci nu se strică nimic.
- **Test:** crouch merge ca înainte. În fereastra Animator se vede clar ce stare e activă.

### F0.8 Sabia apare roz în Blender
- **Cauza:** `Assets/Weapons/Sword_Unity/Sword_Source.blend` caută 11 texturi la `//../Gravel043_2K-PNG/…`, `//../Metal046B_2K-PNG/…` și `//../Metal062C_2K-PNG/…`, adică în `Assets/Weapons/`. Texturile sunt de fapt în `Assets/Materials/<Nume>_2K-PNG/`. Blender colorează **magenta** orice textură lipsă. În Unity sabia e corectă, pentru că folosește materialele Unity.
- **Fix:** mutăm întâi sursa în afara Assets (F0.9), apoi în Blender: **File → External Data → Find Missing Files**, alegem folderul `Assets/Materials` (caută recursiv), **File → External Data → Make Paths Relative**, apoi **Save**.
- **Test:** în Material Preview (tasta Z → Material Preview) sabia are piatră și metal, nu roz.

### F0.9 Curățenie în repo *(structural, commit separat, se anunță)*
Fiecare punct are nevoie de acordul echipei, pentru că mută sau șterge fișiere urmărite de git.

| # | Ce | Propunere |
|---|---|---|
| a | `.blend` în Assets: `Sword_Source.blend`, `Bushes.blend`, `*_2K-PNG.blend` | Mutăm sursele (`Sword_Source`, `Bushes`) în `SourceArt/Blender/` la rădăcina repo-ului, în afara Assets, ca Unity să nu le importe. `.blend`-urile de material (ambientCG) se pot șterge, pentru că PNG-urile rămân. |
| b | Backup-uri urmărite: `_Backup/`, `CutsceneBackup_20261013_1940/`, `PlayerAnimation_Backup.controller` | Le scoatem din repo (git le păstrează oricum în istoric) sau le mutăm în `SourceArt/_Backup/`. |
| c | Foldere imbricate `Assets/Character/MainPlayerModel/Assets/...` și `Assets/Weapons/Sword_Unity/Assets/...` | Aplatizare din Unity Editor, ca `.meta`-urile să meargă cu fișierele. Se face o singură dată, când nimeni nu lucrează pe acele prefab-uri. |
| d | `SetupCharacterMaterials1.cs` și `SetupSwordMaterials1.cs` (scripturi de editor, folosite o singură dată) | Se șterg doar cu acordul autorului. Sunt scripturi existente. |
| e | `My project.slnx` (urmărit), `AlphaGame.slnx`, `.vscode/`, `.DS_Store` (neurmărite) | Le adăugăm în `.gitignore`: `*.slnx`, `.vscode/`, `.DS_Store`. Scoatem `My project.slnx` din index. |
| f | `productName: My project`, `companyName: DefaultCompany` | `AlphaGame` și numele echipei (Project Settings → Player). |
| g | `DefaultVolumeProfile` are componente de test rămase din template | Le curățăm. Post-processing-ul real e în `SampleSceneProfile`. |
| h | `Sword_Prefab.prefab` e nefolosit | Rămâne până la F2. Acolo e înlocuit de `SwordHammer.prefab`, apoi se șterge. |

### F0.10 Git LFS *(decizie de echipă)*
- `.git` are ~456 MB, din cauza PNG-urilor 2K și a FBX-urilor.
- **Propunere:** activăm Git LFS **doar pentru fișierele noi** (`.gitattributes` cu `*.png`, `*.fbx`, `*.blend`, `*.wav`, `*.psd`). Nu rescriem istoria: `git lfs migrate` ar obliga pe toți să re-cloneze.
- Toți colegii trebuie să ruleze o singură dată `git lfs install`.

### Punct de test F0
Play Mode complet: mers, alergare (Shift), ghemuire (Ctrl), săritură, comutarea perspectivei (P), camera lângă obstacole, Esc pentru cursor. Consola fără erori. → **Merge în `main` după test.**

---

## F1. Cutscene: bază funcțională (T1.0 + T1.1 + fader-ul din T1.6)

Detalii: `TASKS_DETALIAT.md` → T1.0, T1.1 și T1.6.

1. **T1.0 Cablaj** (scenă și timeline):
   - ștergem al doilea `CinemachineBrain` și legăm track-ul de brain-ul de pe Main Camera;
   - componenta `CinemachineCamera` rămâne activă, cu Priority controlat din script;
   - scoatem referința nulă din PullBurst (provizoriu: dezactivăm clipul);
   - **Test:** pornind manual Timeline-ul în Play Mode, camera de cutscene preia imaginea fără tremur.
2. **Prefab de cutscene** (opțional, recomandat): `Assets/Cutscenes/SwordPull/Cutscene_SwordPull.prefab`, cu re-binding la runtime.
3. **ScreenFader:** `Assets/UI/ScreenFader.prefab` și `Assets/Scripts/UI/ScreenFader.cs`, pe unscaled time.
   - **Test:** un buton temporar din Inspector face fade-out și fade-in.
4. **T1.1 Trigger + blocare:**
   - `SwordPullTrigger.cs`, `SwordPullCutscene.cs`, `SwordPull_Trigger` și `CutsceneStartMark`;
   - blocarea input-ului, tranziția de aliniere și fade;
   - **Test:** intri pe podium din 4 direcții. Cutscene-ul pornește o singură dată, iar WASD, mouse-ul și P nu au efect.
5. **Final minimal** (fără „modelul nou” încă): `SIG_End` → fade → player-ul în `Podium_SpawnPoint` → controlul revine.
   - **Test:** după cutscene, poți juca normal, iar camera e în spatele player-ului.

**Punct de test F1:** bucla „mergi la podium → cutscene → controlul revine” funcționează cap-coadă, chiar dacă mâinile și sabia arată încă greșit. → Merge.

---

## F2. Arma: `SwordHammer.prefab` + socket-uri (T0)

Detalii: `TASKS_DETALIAT.md` → T0.

> **Stadiu (2026-10-14): ✅ implementat și testat manual** pe branch-ul `feature/roland-t0-swordhammer`. Rezumat: `Docs/SESIUNE_2026-10-14_T0.md`. Rămâne deschis T0-B (mâner mai lung / cap de piatră).

1. Facem prefab-ul `SwordHammer`: scale uniform, pivot la mâner, `Grip_R`, `Grip_L`, `ImpactPoint` și `HeadHitbox`.
2. Verificăm proporțiile lângă player. Dacă capul de piatră e prea mare → **T0-B în Blender** (pe `Sword_Source.blend` mutat în `SourceArt/`).
3. Adăugăm socket-urile `Socket_Weapon_R` și `Socket_Weapon_L` în `Lich_Character_Prefab`.
4. Script nou: `Assets/Scripts/Combat/WeaponSocket.cs`.
5. **Unealtă de test** (nouă, doar pentru dezvoltare): `Assets/Scripts/Combat/DebugEquip.cs` (pus lângă `Weapon`/`WeaponSocket`; rulează doar în Editor și în build-uri de development), cu o tastă de debug (F1) care atașează sau detașează arma la socket, fără cutscene. Așa F5 și F6 se pot testa fără să rulăm cutscene-ul de fiecare dată.

**Punct de test F2:** cu F1 (debug), sabia stă în mână la idle, walk, run, jump și crouch, fără tremur, fără deformare și fără să împingă player-ul. → Merge.

---

## F3. Cutscene: mâini pe mâner + sabia nu mai pâlpâie (T1.2 + T1.3)

Detalii: `TASKS_DETALIAT.md` → T1.2 și T1.3.

1. Instalăm `com.unity.animation.rigging` (se anunță; modifică `manifest.json`).
2. **Spike de 30 min:** verificăm că IK-ul se aplică peste clipurile Humanoid din Timeline. Dacă nu merge, trecem pe fallback-ul `OnAnimatorIK`.
3. Facem `Rig_Hands` (TwoBoneIK pe ambele mâini) și `CutsceneRigWeightDriver.cs`.
4. Sabia fixă în podium până la 14.40, apoi `SIG_SwordAttach` → `WeaponSocket.Attach`. Ștergem Animation Track (2) și Animator-ul de pe sabie.
5. Facem clipurile `_v2`, cu brațele aduse spre mâner (strat A).

**Punct de test F3:** scrubbing frame cu frame pe 0–18.5 s. Mâinile stau pe mâner la struggle, iar sabia nu sare la 14.4 și nici la ~15.95. → Merge.

---

## F4. Cutscene: dramatism și final (T1.4 + T1.5 + T1.6)

Detalii: `TASKS_DETALIAT.md` → T1.4, T1.5 și T1.6.

1. **Shake:** Impulse Listener și Impulse Source + track-ul de Signal-uri. **Test:** shake progresiv, cu vârful la 14.4.
2. **Ochi roșii:** `EyeGlowController.cs` + `SIG_EyesRed` la 10.3 + zoom-ul ținut mai mult. **Test:** în zoom se vede trecerea verde → alb → roșu.
3. **Dramatism:** `CutsceneVolume`, luminile, `VFX_PullBurst` (repară Control Track-ul), slow-mo și Dutch tilt. Le testăm pe rând, nu toate odată.
4. **Final complet:** varianta A a „modelului nou” (sabia în mână, ochii roșii permanenți) și evenimentul `OnSwordClaimed` (folosit în F7).
5. Audio, dacă avem deja fișierele. Altfel trece în F8.

**Punct de test F4:** cutscene-ul complet, de la trigger la control, cu fade curat și sabia în mână la final. → Merge.

---

## F5. Ținerea sabiei ca un ciocan (T2)

Detalii: `TASKS_DETALIAT.md` → T2.

1. Descărcăm animațiile Mixamo de hold (manual) și le importăm ca Humanoid, cu Copy Avatar Lich.
2. Facem `AM_UpperBody.mask`, layer-ul `UpperBody_Weapon` (weight 0) și parametrul `IsArmed`.
3. Scripturi noi: `WeaponHolder.cs` (Equip/Unequip, layer weight), `WeaponHandIK.cs` (mâna stângă pe `Grip_L`) și, opțional, `HeavyWeaponSway.cs`.
4. Legăm `SwordPullCutscene.End()` → `WeaponHolder.Equip()`. Unealta `DebugEquip` (F2) trece și ea prin `WeaponHolder`.

**Punct de test F5:** cu F1 (debug) sau după cutscene, idle, walk, run, jump și crouch arată corect cu ciocanul. Fără armă, totul e exact ca înainte. → Merge.

---

## F6. Atac slam + bazele de combat (T3, cu interfețele din T4 aduse mai devreme)

Detalii: `TASKS_DETALIAT.md` → T3 și T4-C (doar interfețele).

1. **Bazele de combat** (aduse din T4, ca atacul să se poată testa fără monstru):
   - `IDamageable.cs`, `DamageInfo.cs` și `Health.cs`;
   - layer-ul `Enemy` (anunțat).
2. **Manechin de test:** `Assets/Prefabs/Dev/TrainingDummy.prefab`. E o capsulă pe layer-ul `Enemy`, cu `Health`, flash la lovire, un număr de damage în consolă și resetare la moarte.
3. Acțiunea nouă `Attack` (`<Mouse>/leftButton`), cu `PlayerCombat.cs` și `AnimationEventRelay.cs`.
4. Animația `Attack_Slam` (Mixamo) și Animation Events-urile ei.
5. La impact: OverlapSphere, damage, knockback (Rigidbody pe manechin) și hit-stop.
6. `CameraShake.cs` (ordine 1001, după `CameraCollision`), apoi VFX și decal (Decal Renderer Feature, anunțat).

**Punct de test F6:** 3 manechine în jurul player-ului. Un slam le lovește doar pe cele din rază, cu damage scăzut la margine, iar shake-ul, praful și decal-ul pornesc în același frame. Spam-click nu repornește atacul. → Merge.

---

## F7. Monstrul Stone Warden (T4)

Detalii: `TASKS_DETALIAT.md` → T4.

1. **T4-A (Blender, poate începe în paralel din F0):** blockout → **aprobare** → modelare → retopologie → UV → texturare → rig → debris → export.
2. Pachetul `com.unity.ai.navigation` + `NavMeshSurface` (bake în scenă, anunțat).
3. Scripturile `EnemyBrain`, `EnemyAnimatorBridge`, `EnemyMeleeHit`, `EnemyKnockback` și `ShatterOnDeath`, plus prefab-ul `StoneWarden`.
4. **Test intermediar:** un singur golem pus manual în scenă. Urmărește, atacă, ia damage de la slam, moare și se sparge.
5. `EnemySpawner` legat la `OnSwordClaimed`.

**Punct de test F7:** după cutscene ies 3 golemi, iar lupta merge cap-coadă. → Merge.

---

## F8. Bucla de joc + polish

1. `PlayerHealth.cs`: damage de la golemi, moarte → fade → respawn la `Podium_SpawnPoint`.
2. **HUD minimal** (nou, `Assets/Scripts/UI/`): bara de HP a player-ului și HP deasupra golemilor.
3. **Meniu de pauză** (Esc, folosește `CursorManager` din F0.5): Resume și Quit.
4. Audio pass: cutscene, atac, golemi, ambient.
5. Opțional: crouch care micșorează capsula (script nou abonat la `onCrouchHold`), ca să poți trece pe sub obstacole.
6. Opțional: în first person, capul și gluga se ascund (script nou, ShadowsOnly pe renderer-ul capului cât timp `is3D == false`).
7. Test complet, apoi merge final în `main`.

---

## Pipeline Blender → Unity (pe scurt)

- Sursele `.blend` stau în `SourceArt/Blender/`, **nu în Assets**. În Assets intră doar FBX-uri.
- Un fișier `.blend` per asset: `Lich_Source.blend`, `Sword_Source.blend`, `StoneWarden_Source.blend`. Pentru referință de scară se folosește **File → Link** (nu Append). Asset-ul legat apare read-only și nu se amestecă în fișierul curent.
- Export FBX:
  - Apply Scalings = FBX Units Scale, Forward = -Z, Up = Y;
  - Add Leaf Bones off, Only Deform Bones on.
- Rig Humanoid cu root-ul `Hips`.
- Materiale URP: Metallic în R, Smoothness (1 - roughness) în A.
- Texturile sursă sunt referite **relativ** (File → External Data → Make Paths Relative), ca să meargă pe orice calculator al echipei.
