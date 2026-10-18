# TASKS.md — Space Explorer

One prompt = one task by default.

- `[ ]` not done
- `[x]` done
- Keep unfinished/blocked tasks unchecked; record `PARTIAL` or `BLOCKED` in `CHANGELOG.md`.

## Current

**NEXT:** `FIX-PLAYER-BOUNDS-01`

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
- [x] **COMBAT-02** — Bullet-Asteroid collision, damage, death path. **DONE** · `Depends: COMBAT-01,AST-01` · `Reads: P4,P5,P9`.
- [x] **STAR-01** — Star drop, pool, drift, collect, off-screen recycle. **DONE** · `Depends: COMBAT-02` · `Reads: P6,P11`.
- [x] **SCORE-01** — Score, survival time, clean run reset, result snapshot. **DONE** — the project owner confirmed successful manual Unity verification of the implemented SCORE-01 behavior. The EndGame result UI/display remains scoped to SCENE-02. · `Depends: COMBAT-02` · `Reads: P8`.
- [x] **SCORE-DEBUG-01** — Temporary Editor Play Mode overlay for Score/XP diagnostics; removed after the diagnostic task. **DONE** · `Depends: SCORE-01`.

## Leveling & Upgrades

- [x] **PROG-01** — Configurable Star XP thresholds, level progression, overflow, and new-run reset. **DONE** · `Depends: STAR-01` · `Reads: P6,P7`.
- [x] **UPG-01** — Upgrade definition/registration contract; queue `StarXPCollector` level-up events and show one sequential panel with up to 3 distinct registered, implemented, eligible choices; filter capped/ineligible options; apply the selected effect exactly once; safely dismiss requests with no eligible choices without an empty panel or permanent pause; own/release only its pause request, clear pending choices on Game Over, reset run-specific upgrade state, and resume only when allowed by `GameManager`. Resolve the authoritative persistent `GameManager` at runtime; do not rely on a serialized scene reference. Test UI display/selection with mock definitions only in an isolated harness or Unity test assembly; never register mocks, fake choices, placeholders, or empty handlers in production. Choice Buttons must not have conflicting serialized Inspector `On Click()` handlers. Wire the Gameplay UI and verify sequential level-ups, single application, capped/empty choices, Game Over while open, and new-run reset. **DONE** — Gameplay wiring and required UPG-01 flow manually verified by the project owner in Unity Play Mode; no concrete upgrade effects were part of this verification. `Depends: STAR-01,PROG-01` · `Reads: P6,P7,P9,P12,P13`.
- [x] **UPG-02** — Implement Multi Shot (one additional symmetric projectile per selection, cap 5 including base), Rapid Fire (-0.04s per selection, minimum 0.14s, preserve configured base interval), and Damage Up (+1 damage per projectile, cap 5 from base 1), including eligibility and visible effects. **DONE** — the project owner manually verified all three upgrades in Unity. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [x] **UPG-RANDOM-01** — Uniformly sample up to three distinct eligible upgrades without replacement; show all when three or fewer are eligible; avoid the same unordered ID set in consecutive rounds whenever multiple valid sets exist; recalculate for queued levels and reset tracking on run start/finish. **DONE** — the project owner confirmed successful Unity Play Mode verification. This is a follow-up behavior and was not part of the initial UPG-01 implementation. `Depends: UPG-01` · `Reads: P7`.
- [x] **UPG-03** — Add one follow-and-fire Drone per selection, maximum 2; fire right through shared Bullet pool; inherit Damage Up only, not Multi Shot or Rapid Fire. **DONE** — the project owner confirmed successful Unity Play Mode verification. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [x] **UPG-03-VISUAL-01** — Allow a dedicated right-facing Drone Wing sprite while preserving Player-sprite fallback, configurable size, tint, and sorting. **DONE** — the project owner confirmed the dedicated sprite, fallback, formation spacing, follow movement, and firing in Unity Play Mode. `Depends: UPG-03` · `Reads: P7,P12`.
- [x] **UPG-04** — Shield absorbs one Player-Asteroid collision and automatically reactivates after cooldown; levels use 12s/9s/6s cooldowns. Upgrading during cooldown leaves the current timer unchanged and affects the next cycle; unavailable Shield follows Game Over. **DONE** — setup and Play Mode behavior manually verified by the project owner in Unity. `Depends: UPG-01` · `Reads: P7,P9,P12`.
- [x] **UPG-05** — Smoothly attract Stars within level radii 3/4/5 world units; maximum level 3 and no teleporting. **DONE** — the project owner confirmed successful Unity Play Mode verification after attaching `StarMagnetController` and registering `StarMagnetUpgradeDefinition`. `Depends: UPG-01,STAR-01` · `Reads: P6,P7,P12`.
- [x] **UPG-UI-01** — Bind each randomized upgrade choice card to its selected definition's Display Name, Description, and optional Icon; clear stale card data, hide missing icons, and preserve existing button callbacks and selection flow. **DONE** — the project owner assigned all six upgrade icons and verified the displayed icons, names, descriptions, choices, and selection behavior in Unity Play Mode. `Depends: UPG-01,UPG-02,UPG-03,UPG-04,UPG-05` · `Reads: P7,P12,P13`.

## Stage 1 — Core Gameplay Revisions

- [ ] **FIX-PLAYER-BOUNDS-01** — Use Player Collider2D world bounds, including collider-center offset from Rigidbody2D, to clamp movement to the full visible camera on both axes; remove the former left-half restriction. **PARTIAL** — collider-based bounds are integrated and the owner confirms the updated gameplay setup works, but full-screen edge-clamp and weapon firing-reach checks were not specifically confirmed. `Depends: PLAYER-01` · `Reads: P3`.
- [ ] **PLAYER-HP-01** — Add run-scoped Player health, starting at 10/10 and resetting on each new run; apply configurable damage by Asteroid type. Proposed defaults are Small 1 and Large 3, pending owner approval; do not treat these as final values. `Depends: FIX-PLAYER-BOUNDS-01,AST-01` · `Reads: P5,P9,P12`.
- [ ] **COLLISION-HP-01** — Resolve each genuine Player-Asteroid impact once. Apply a configurable Score penalty on every impact (including a Shield-absorbed impact), clamp Score at zero, route HP damage through PlayerHealth only when no ready Shield absorbs the impact, and finish at 0 HP through existing GameManager/RunResult/EndGame flow. Penalty value is TBD; do not invent a final balancing value. `Depends: PLAYER-HP-01,GAME-01,SCORE-01,UPG-04` · `Reads: P8,P9,P12`.
- [ ] **HIT-PROTECTION-01** — Add a configurable post-hit protection window, with ~1 second only a proposed starting value pending approval. It suppresses repeat HP loss during protection, not the Score penalty for a distinct genuine impact; a ready Shield still absorbs a genuine impact. Continuous overlap and duplicate callbacks must not count as new impacts. `Depends: COLLISION-HP-01` · `Reads: P9`.
- [ ] **SHIELD-FX-01** — Show a brief cyan-blue circular/hexagonal barrier around the Player on a Shield-absorbed impact, flash then fade. Suggested duration ~0.8 seconds is not final; this effect is visual only and does not change physics or cooldown. `Depends: COLLISION-HP-01,UPG-04` · `Reads: P13`.

## Stage 2 — Difficulty and Combat Balancing

- [ ] **DIFF-01** — Increase Sector over run time and gradually scale difficulty with a reasonable spawn-interval floor. Tune initial spawn pressure and Asteroid movement to be slightly less aggressive while retaining type distinctions and variation; exact final balance values require Play Mode tuning. `Depends: AST-02` · `Reads: P5,P8`.
- [ ] **WEAPON-PACING-01** — Slightly increase the existing serialized base firing interval to reduce firing rate; preserve hold-to-fire, pooling, and Multi Shot/Rapid Fire behavior. Choose the final interval through Play Mode tuning rather than inventing a fixed value here. `Depends: COMBAT-01,UPG-02` · `Reads: P4,P7`.
- [ ] **FIRE-MOVE-PENALTY-01** — Reduce Player movement speed by 10% while the Player holds fire and restore it on release; apply once while held, not per projectile. Drone firing does not affect Player movement. `Depends: PLAYER-01,COMBAT-01` · `Reads: P3,P4`.

## Stage 3 — Required Gameplay UI and Scenes

- [x] **GAME-01** — Existing Player-Asteroid Shield/death integration and EndGame transition. **DONE** — the project owner manually verified the original one-hit completion flow. This completed infrastructure remains the basis for the revised 10 HP flow; type damage, per-impact penalties, and Game Over at 0 HP are planned under Stage 1. `Depends: SCORE-01,UPG-04` · `Reads: P8,P9`.
- [x] **UI-BUILD-01** — Display-only text panel for Drone count, Shield readiness/cooldown, projectile Damage, effective Player fire interval, and Player volley projectile count; refresh from existing gameplay APIs without modifying gameplay values. **DONE** — the project owner configured BuildStatusPanel in the existing Gameplay Canvas and confirmed stat updates, Shield cooldown, and new-run reset in Unity Play Mode. The text-based design is intentional; no icon conversion is required. `Depends: UPG-02,UPG-03,UPG-04` · `Reads: P7,P12,P13`.
- [ ] **UI-01** — Gameplay HUD for Score, Level, XP progress, Sector, and Player HP; reuse any existing HP bar rather than adding a duplicate. **PARTIAL** — Level/XP HUD and serialized references exist; layout and contrast still need improvement for readability and unobstructed gameplay, and HP/Sector integration remains to be completed. `Depends: SCORE-01,PROG-01,DIFF-01,PLAYER-HP-01` · `Reads: P13`.
- [ ] **SCENE-01** — MainMenu: Play, Instructions, Quit. `Depends: SETUP-01,ARCH-01` · `Reads: P2`.
- [ ] **SCENE-02** — EndGame: display final Score, Level reached, and Survival Time; provide Play Again, Main Menu, and Quit actions. **PARTIAL** — added `EndGamePresenter`; attach it to the EndGame Canvas, assign three result labels and three buttons, clear persistent Inspector `On Click()` callbacks, save, and verify in Unity. `Depends: GAME-01` · `Reads: P2,P8`.
- [ ] **TRANS-01** — Optional short scene fade. `Depends: SCENE-01,SCENE-02` · `Reads: P2,P13`.

## Stage 4 — World and Polish

- [ ] **WORLD-01** — Infinite recycled background + optional parallax. `Depends: SETUP-01` · `Reads: P10,P11,P13`.
- [ ] **POLISH-01** — Shooting/hit/explosion/Star/level-up feedback, including brief Player hit feedback where appropriate; do not duplicate the dedicated Shield impact effect. `Depends: core gameplay` · `Reads: P13`.

## Stage 5 — QA, Documentation and Submission

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
