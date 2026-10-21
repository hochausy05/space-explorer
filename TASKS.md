# TASKS.md — Space Explorer

One prompt = one task by default.

- `[ ]` not done
- `[x]` done
- Keep unfinished/blocked tasks unchecked; record `PARTIAL` or `BLOCKED` in `CHANGELOG.md`.

## Current

**NEXT:** `GAMEPLAY-PAUSE-MENU-01`

## Foundation

- [x] **SETUP-01** — Unity baseline: folders, 3 scenes, 16:9, build config. `Reads: P1,P2,P14` · Verify: compile + open scenes.
- [x] **ARCH-01** — Core runtime skeleton: run state, RunResult, SceneLoader, pooling base. `Depends: SETUP-01` · `Reads: P2,P8,P11,P12` · Verify: compile.
- [x] **GM-INTEGRATION-01** — Integrate the persistent GameManager with Gameplay run start and asteroid spawning. **DONE** — runtime behavior manually verified by the project owner in Unity. `Reads: P2,P8,P9,P12`.

## Player & Shooting

- [x] **PLAYER-01** — Smooth 4-direction movement, acceleration/deceleration, bounds, Rigidbody2D. **DONE** · `Depends: SETUP-01` · `Reads: P3` · Verify: movement playtest.
- [x] **COMBAT-01** — Hold-to-fire + Bullet pool + off-screen/lifetime recycle. **DONE** · `Depends: PLAYER-01,ARCH-01` · `Reads: P4,P11` · Verify: fire spam + pool observation.
- [x] **FIX-BULLET-COLLIDER-01** — Save Bullet trigger collider in the prefab and validate its configuration. **DONE** · `Reads: P4`.

## Asteroids & Stars

- [x] **AST-01** — Small/Large Asteroid model, HP, movement, rotation, pooling, off-screen recycle. **DONE** · `Depends: ARCH-01` · `Reads: P5,P11`.
- [x] **AST-02** — Asteroid spawner with controlled spawn randomness. **DONE** · `Depends: AST-01` · `Reads: P5,P8`.
- [ ] **AST-EVOL-01** — Evolve Small/Large Asteroid maximum HP by 60-second active Gameplay stages (Small 1–6; Large 3–13), assigning health only at spawn from the DIFF-01 elapsed clock and resetting with each run. **PARTIAL** — the owner confirmed ordinary Asteroid HP increases over time in Play Mode; caps, new-run reset, pause behavior, and existing-asteroid HP stability still need verification. Special asteroid variants are outside this phase. `Depends: AST-02,DIFF-01` · `Reads: P5,P8,P11`.
- [x] **AST-SPEED-01** — Thêm Speed Special có thể spawn thử: bay thẳng với tốc độ ngẫu nhiên `1.5x–2.0x` Small thường tại cùng giai đoạn DIFF-01; dùng HP Special riêng theo bảng phút (HP khởi đầu 3, tối đa 6); va Player `5 HP`/`-25 Score`, phá hủy thưởng `+40 Score`/đúng 8 XP Star dùng pool. Không bật tự động spawn Special. **DONE** — chủ dự án xác nhận các Special hoạt động trong Unity Play Mode; kiểm tra chi tiết số reward vẫn thuộc QA còn lại. `Depends: AST-EVOL-01,AST-02,COMBAT-02,PLAYER-HP-01,COLLISION-HP-01,UPG-04,HIT-PROTECTION-01` · `Reads: P5,P8,P9,P11`.
- [x] **AST-SPECIAL-CODE-01** — Hoàn thiện nền runtime dùng chung cho Speed, Homing, Erratic, Splitter và Splitter Fragment không split; dùng đồng hồ active DIFF-01/AST-EVOL-01, luồng damage/reward/pickup hiện có, spawn thử từng loại và bảng HP Special độc lập theo phút trong P5. **DONE** — chủ dự án xác nhận bốn loại Special và Splitter Fragments hoạt động trong Unity Play Mode; các edge case QA chưa xác nhận vẫn được theo dõi ở AST-SPECIAL-QA-01. `Depends: AST-EVOL-01,AST-SPEED-01,COMBAT-02,PLAYER-HP-01,COLLISION-HP-01,HIT-PROTECTION-01` · `Reads: P5,P8,P9,P11`.
- [x] **AST-SPECIAL-PREFAB-01** — Tạo và nối prefab Speed, Homing, Erratic, Splitter, Splitter Fragment với `AsteroidPool` và Player target trong Unity Editor. **DONE** — chủ dự án xác nhận các loại Special và Fragment hoạt động trong Play Mode. `Depends: AST-SPECIAL-CODE-01` · `Reads: P5,P11`.
- [x] **AST-SPECIAL-SPAWN-01** — Dùng xác suất Special theo thời gian `6/10/14/18/20%` từ phút 1, phân phối `35/30/20/15%` cho Speed/Erratic/Homing/Splitter; giữ cadence DIFF-01, không thêm active-count/cooldown gate. **DONE** — chủ dự án xác nhận automatic time-based Special spawning hoạt động trong Unity Play Mode; các xác suất từng mốc chưa được xác nhận exhaustively. `Depends: AST-SPECIAL-PREFAB-01` · `Reads: P5,P8,P11`.
- [ ] **AST-SPECIAL-QA-01** — Kiểm tra chủ dự án trong Play Mode: HP từng stage, movement/hit/reward của bốn loại, Splitter fragments, pool reuse, xác suất Special, pause và reset run. **PARTIAL** — chủ dự án xác nhận bốn loại Special, Fragment và automatic time-based spawning hoạt động; còn cần xác minh đầy đủ HP/cap, pause/reset, reward counts, pool reuse/stress và xác suất tại từng mốc. `Depends: AST-SPECIAL-SPAWN-01` · `Reads: P5,P8,P9,P11`.
- [x] **COMBAT-02** — Bullet-Asteroid collision, damage, death path. **DONE** · `Depends: COMBAT-01,AST-01` · `Reads: P4,P5,P9`.
- [x] **STAR-01** — Star drop, pool, drift, collect, off-screen recycle. **DONE** · `Depends: COMBAT-02` · `Reads: P6,P11`.
- [x] **SCORE-01** — Score, survival time, clean run reset, result snapshot. **DONE** — the project owner confirmed successful manual Unity verification of the implemented SCORE-01 behavior. The EndGame result UI/display remains scoped to SCENE-02. · `Depends: COMBAT-02` · `Reads: P8`.
- [x] **SCORE-DEBUG-01** — Temporary Editor Play Mode overlay for Score/XP diagnostics; removed after the diagnostic task. **DONE** · `Depends: SCORE-01`.

## Leveling & Upgrades

- [x] **PROG-01** — Configurable Star XP thresholds, level progression, overflow, and new-run reset. **DONE** · `Depends: STAR-01` · `Reads: P6,P7`.
- [x] **UPG-01** — Upgrade definition/registration contract; queue `StarXPCollector` level-up events and show one sequential panel with up to 3 distinct registered, implemented, eligible choices; filter capped/ineligible options; apply the selected effect exactly once; safely dismiss requests with no eligible choices without an empty panel or permanent pause; own/release only its pause request, clear pending choices on Game Over, reset run-specific upgrade state, and resume only when allowed by `GameManager`. Resolve the authoritative persistent `GameManager` at runtime; do not rely on a serialized scene reference. Test UI display/selection with mock definitions only in an isolated harness or Unity test assembly; never register mocks, fake choices, placeholders, or empty handlers in production. Choice Buttons must not have conflicting serialized Inspector `On Click()` handlers. Wire the Gameplay UI and verify sequential level-ups, single application, capped/empty choices, Game Over while open, and new-run reset. **DONE** — Gameplay wiring and required UPG-01 flow manually verified by the project owner in Unity Play Mode; no concrete upgrade effects were part of this verification. `Depends: STAR-01,PROG-01` · `Reads: P6,P7,P9,P12,P13`.
- [x] **UPG-02** — Implement Multi Shot (one additional symmetric projectile per selection, cap 5 including base), Rapid Fire (-0.04s per selection, minimum 0.14s, preserve configured base interval), and Damage Up (+1 damage per projectile with no gameplay-level cap), including eligibility and visible effects. **DONE** — original upgrade behavior was manually verified earlier; the owner later confirmed unlimited Damage Up under DIFF-01 in Unity Play Mode. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [x] **UPG-RANDOM-01** — Uniformly sample up to three distinct eligible upgrades without replacement; show all when three or fewer are eligible; avoid the same unordered ID set in consecutive rounds whenever multiple valid sets exist; recalculate for queued levels and reset tracking on run start/finish. **DONE** — the project owner confirmed successful Unity Play Mode verification. This is a follow-up behavior and was not part of the initial UPG-01 implementation. `Depends: UPG-01` · `Reads: P7`.
- [x] **UPG-03** — Add one follow-and-fire Drone per selection, maximum 2; fire right through shared Bullet pool; inherit Damage Up only, not Multi Shot or Rapid Fire. **DONE** — the project owner confirmed successful Unity Play Mode verification. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [x] **UPG-03-VISUAL-01** — Allow a dedicated right-facing Drone Wing sprite while preserving Player-sprite fallback, configurable size, tint, and sorting. **DONE** — the project owner confirmed the dedicated sprite, fallback, formation spacing, follow movement, and firing in Unity Play Mode. `Depends: UPG-03` · `Reads: P7,P12`.
- [x] **UPG-04** — Shield absorbs one Player-Asteroid collision and automatically reactivates after cooldown; levels use 12s/9s/6s cooldowns. Upgrading during cooldown leaves the current timer unchanged and affects the next cycle; unavailable Shield follows Game Over. **DONE** — setup and Play Mode behavior manually verified by the project owner in Unity. `Depends: UPG-01` · `Reads: P7,P9,P12`.
- [x] **UPG-05** — Smoothly attract Stars within level radii 3/4/5 world units; maximum level 3 and no teleporting. **DONE** — the project owner confirmed successful Unity Play Mode verification after attaching `StarMagnetController` and registering `StarMagnetUpgradeDefinition`. `Depends: UPG-01,STAR-01` · `Reads: P6,P7,P12`.
- [x] **UPG-UI-01** — Bind each randomized upgrade choice card to its selected definition's Display Name, Description, and optional Icon; clear stale card data, hide missing icons, and preserve existing button callbacks and selection flow. **DONE** — the project owner assigned all six upgrade icons and verified the displayed icons, names, descriptions, choices, and selection behavior in Unity Play Mode. `Depends: UPG-01,UPG-02,UPG-03,UPG-04,UPG-05` · `Reads: P7,P12,P13`.

## Stage 1 — Core Gameplay Revisions

- [x] **FIX-PLAYER-BOUNDS-01** — Use Player Collider2D world bounds, including collider-center offset from Rigidbody2D, to clamp movement to the full visible camera on both axes; remove the former left-half restriction. **DONE** — the project owner confirmed all five movement checks passed in Unity Play Mode. `Depends: PLAYER-01` · `Reads: P3`.
- [x] **PLAYER-HP-01** — Add run-scoped Player health, starting at 10/10 and resetting on each new run; apply configurable damage by Asteroid type and show HP using only the existing Gameplay XP bar. **DONE** — the project owner confirmed Player HP, asteroid damage, the repurposed HP bar, XP text, and existing gameplay work correctly in Unity Play Mode. `Depends: FIX-PLAYER-BOUNDS-01,AST-01` · `Reads: P5,P9,P12,P13`.
- [x] **COLLISION-HP-01** — Resolve each genuine Player-Asteroid impact once. Apply configurable Score penalties (Small 5, Large 15) on every impact, including Shield-absorbed impacts; clamp Score at zero; route the existing configurable HP damage (Small 1, Large 3) through PlayerHealth only when no ready Shield absorbs the impact; finish only at 0 HP through existing GameManager/RunResult/EndGame flow. Preserve the existing same-impact duplicate guard; do not implement the separate timed post-hit protection task. **DONE** — the project owner confirmed all COLLISION-HP-01 Unity Play Mode checks passed. `Depends: PLAYER-HP-01,GAME-01,SCORE-01,UPG-04` · `Reads: P8,P9,P12`.
- [x] **HIT-PROTECTION-01** — Add a configurable 1.0-second post-hit protection window after Shield absorption or surviving HP damage; protected collisions skip Score/HP/Shield effects. Use scaled time and reset on new run. Show the existing `ShieldVisual` SpriteRenderer only for Shield-absorbed hits; hide it on expiry/new run/run end. **DONE** — the project owner confirmed successful Unity Play Mode verification of protection and the PNG effect. `Depends: COLLISION-HP-01` · `Reads: P9,P13`.
- [x] **SHIELD-FX-01** — Shield impact visual is implemented through the existing `ShieldVisual` SpriteRenderer under HIT-PROTECTION-01. **DONE — merged and owner-verified with HIT-PROTECTION-01.** `Depends: HIT-PROTECTION-01` · `Reads: P13`.
- [x] **SHIELD-ANIM-01** — Refine the existing ShieldVisual during the 1-second protection window: fade in from 0–0.15s, subtle pulse from 0.15–0.65s, two smooth blinks from 0.65–0.90s, and fade fully out by 1.0s. Preserve/restore original sprite tint, follow scaled time, and keep the visual hidden on startup/new run/run end. **DONE** — the project owner confirmed successful Unity Play Mode testing. `Depends: HIT-PROTECTION-01` · `Reads: P13`.

## Stage 2 — Difficulty and Combat Balancing

- [x] **DIFF-01** — Reduce initial asteroid speed by 20% and spawn frequency by 25%; every 30 active gameplay seconds add +5% speed and +8% spawn frequency relative to the balanced baseline, capped at 1.6x/2.0x. Apply speed only to newly spawned asteroids, pause/reset progression with the run lifecycle, and retain asteroid-to-asteroid physical interactions. Reduce Player movement to 90% while Space is held, make the current base firing interval 10% longer while preserving Rapid Fire's -0.04s step and 0.14s floor, and make Damage Up unlimited at +1 damage per successful choice with accurate display/reset. **DONE** — all 10 acceptance checks were confirmed by the project owner in Unity Play Mode. `Depends: AST-02,COMBAT-01,UPG-02,PLAYER-01` · `Reads: P3,P4,P5,P7,P8,P12`.
- [x] **WEAPON-PACING-01** — Increase the base firing interval by 10% while preserving hold-to-fire, pooling, Multi Shot, Rapid Fire's 0.04s reduction, and the 0.14s floor. **DONE as part of DIFF-01**, covered by the owner's Play Mode confirmation. `Depends: COMBAT-01,UPG-02` · `Reads: P4,P7`.
- [x] **FIRE-MOVE-PENALTY-01** — Reduce Player movement speed by 10% while Space is held and restore it on release using WeaponController's fire-action state. **DONE as part of DIFF-01**, covered by the owner's Play Mode confirmation. `Depends: PLAYER-01,COMBAT-01` · `Reads: P3,P4`.

## Stage 3 — Required Gameplay UI and Scenes

- [x] **GAME-01** — Existing Player-Asteroid Shield/death integration and EndGame transition. **DONE** — the project owner manually verified the original one-hit completion flow. This completed infrastructure remains the basis for the revised 10 HP flow; type damage, per-impact penalties, and Game Over at 0 HP are planned under Stage 1. `Depends: SCORE-01,UPG-04` · `Reads: P8,P9`.
- [x] **UI-BUILD-01** — Display-only text panel for Drone count, Shield readiness/cooldown, projectile Damage, effective Player fire interval, and Player volley projectile count; refresh from existing gameplay APIs without modifying gameplay values. **DONE** — the project owner configured BuildStatusPanel in the existing Gameplay Canvas and confirmed stat updates, Shield cooldown, and new-run reset in Unity Play Mode. The text-based design is intentional; no icon conversion is required. `Depends: UPG-02,UPG-03,UPG-04` · `Reads: P7,P12,P13`.
- [x] **UI-01** — Gameplay HUD showing Score, Level, numeric XP text only, and Player HP using the existing HP bar; preserve the approved layout and existing BuildStatusHUD. Sector and an XP progress bar are explicitly outside the scope. **DONE** — the project owner approved the current HUD and confirmed BuildStatusHUD was tested successfully in Unity Play Mode. `Depends: SCORE-01,PROG-01,PLAYER-HP-01` · `Reads: P13`.
- [x] **SCENE-01** — MainMenu: Play directly to a fresh Gameplay run; toggle the existing right-side InfoPanel between InstructionsTitle/InstructionsText and RecordsContent; Quit safely in Editor/standalone; persist and display the newest 20 completed runs (Final Score, survival duration, Player Level), newest first, with an empty state. **DONE** — the project owner confirmed SCENE-01 works correctly in Unity Play Mode. Standalone-build Quit behavior remains for final QA. `Depends: SETUP-01,ARCH-01,SCORE-01,GAME-01` · `Reads: P2,P8,P12`.
- [x] **SCENE-02** — EndGame: display final Score, Level reached, and Survival Time; provide Play Again and Main Menu actions. **DONE** — the project owner confirmed all EndGame functionality and visual layout checks passed in Unity Play Mode. Final layout: GAME OVER logo, Score, Level, Survival Time, Play Again, and Main Menu; Quit was intentionally removed. `EndGamePresenter` validates the three result labels and these two navigation buttons. `Depends: GAME-01` · `Reads: P2,P8`.
- [ ] **TRANS-01** — Optional short scene fade. `Depends: SCENE-01,SCENE-02` · `Reads: P2,P13`.

## Stage 4 — World and Polish

- [x] **AUDIO-SFX-01** — Thêm service SFX 2D dùng chung cho tám clip Shoot, Bullet Hit, Asteroid Break, Shield Hit, XP Collect, Level Up, Click và Game Over. Chỉ phát theo sự kiện thực, hỗ trợ nhiều âm thanh chồng có giới hạn, chỉnh master/per-effect volume và tồn tại xuyên MainMenu/Gameplay/EndGame. **DONE** — chủ dự án xác nhận đã cấu hình đủ tám SFX trong Unity và âm thanh hoạt động. Nhạc nền được cố ý loại khỏi phạm vi. `Reads: P4,P5,P6,P7,P8,P9,P13`.
- [ ] **GAMEPLAY-PAUSE-MENU-01** — Thêm Settings/Pause menu trong Gameplay với Settings góc trên phải và panel ban đầu ẩn gồm đúng Resume, SFX Mute/Unmute và Exit về MainMenu. Pause bằng token riêng của GameManager; không chồng lên pause chọn upgrade, Resume chỉ nhả token của menu, mute tám SFX dùng chung và lưu qua PlayerPrefs, Exit bỏ run không ghi result/history/Game Over rồi đưa run state về Ready. **PARTIAL** — code foundation hoàn tất; cần tạo/wire UI trong Unity, lưu scene và xác minh Play Mode. `Depends: ARCH-01,UPG-01,AUDIO-SFX-01,SCENE-01` · `Reads: P2,P5,P7,P8,P9,P13`.
- [ ] **WORLD-01** — Infinite recycled background + optional parallax. `Depends: SETUP-01` · `Reads: P10,P11,P13`.
- [ ] **POLISH-01** — Shooting/hit/explosion/Star/level-up feedback, including brief Player hit feedback where appropriate; do not duplicate the dedicated Shield impact effect. `Depends: core gameplay` · `Reads: P13`.
- [x] **POLISH-EXPLOSION-01** — Reuse `Assets/Prefabs/Effects/AsteroidExplosion.prefab` for one pooled, non-physical, pause-aware destruction animation at the last world position of every genuinely destroyed Small, Large, Speed, Homing, Erratic, Splitter, and Splitter Fragment asteroid. Trigger only from the shared destruction callback; never play on off-screen despawn, pool release, or scene cleanup; preserve existing rewards/SFX and Splitter's two real fragments. Expose per-kind Inspector scale multipliers (suggested initial values: `0.8/1.1/1.0/1.2/1.2/1.3/0.6`). **DONE** — the project owner confirmed the shared explosion effect works correctly in Unity Play Mode. `Depends: AST-01,AST-SPECIAL-CODE-01` · `Reads: P5,P11,P13`.
- [x] **WORLD-PARALLAX-01** — Keep the existing static space background unchanged and add exactly two runtime-generated moving star layers. Defaults: FarStars `45`, `0.2` world units/s; NearStars `20`, `0.6` world units/s. Expose count, speed, world size, color/opacity, camera and sorting controls; use a shared in-memory soft-circle sprite, no physics/assets, recycle left-to-right, adapt to orthographic viewport/aspect changes, and freeze on scaled-time pauses. Preserve the existing circular stars; no four-pointed star changes. **DONE** — the project owner confirmed successful Unity Play Mode testing. `Depends: SETUP-01` · `Reads: P10,P11,P13`.

## Stage 5 — QA, Documentation and Submission

- [x] **FIX-UNITY6-EDITOR-API-01** — Replace obsolete Unity 6 `FindFirstObjectByType<T>()` calls in the UPG-01 harness with `FindAnyObjectByType<T>()`; preserve behavior because deterministic instance ordering is not required. **DONE** — both start and stop lookups use any available `UpgradeManager`. `Reads: P12`.
- [x] **FIX-POOL-DISPOSE-01** — Make shared `PrefabPool<T>.Dispose()` safe when Unity has already destroyed its pool-root Transform/GameObject during scene unload or shutdown; use Unity-aware null checks and retain idempotent bookkeeping cleanup for asteroid, star, bullet, and effect pools. **DONE** — the project owner confirmed the MissingReferenceException is resolved in Unity. `Reads: P11,P12`.
- [ ] **QA-01** — Performance/lifecycle audit: bounded pools, no off-screen buildup, no hot-loop Find/GetComponent; verify the revised health, impact, protection, pacing, and input behaviors. `Depends: gameplay complete` · `Reads: P4,P5,P6,P9,P11`.
- [ ] **QA-02** — Full midterm functional checklist, including the documented differences from assignment scoring and Star behavior. `Depends: QA-01` · `Reads: P1,P16`.
- [ ] **DOC-01** — Finalize README to match actual build. `Depends: QA-02` · `Reads: P1,P2,P12,P16`.

## Parking Lot

Do not start unless explicitly requested after the core build:

- screen shake
- Asteroid warning indicator
- extra visual variants
- audio mixer
- local high score
- extra background themes
- pause menu
