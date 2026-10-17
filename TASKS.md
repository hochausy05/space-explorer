# TASKS.md — Space Explorer

One prompt = one task by default.

- `[ ]` not done
- `[x]` done
- Keep unfinished/blocked tasks unchecked; record `PARTIAL` or `BLOCKED` in `CHANGELOG.md`.

## Current

**NEXT:** `UPG-04`

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
- [ ] **SCORE-01** — Score, survival time, clean run reset, result snapshot. **PARTIAL** · `Depends: COMBAT-02` · `Reads: P8`.
- [x] **SCORE-DEBUG-01** — Temporary Editor Play Mode overlay for Score/XP diagnostics; removed after the diagnostic task. **DONE** · `Depends: SCORE-01`.

## Leveling & Upgrades

- [x] **PROG-01** — Configurable Star XP thresholds, level progression, overflow, and new-run reset. **DONE** · `Depends: STAR-01` · `Reads: P6,P7`.
- [x] **UPG-01** — Upgrade definition/registration contract; queue `StarXPCollector` level-up events and show one sequential panel with up to 3 distinct registered, implemented, eligible choices; filter capped/ineligible options; apply the selected effect exactly once; safely dismiss requests with no eligible choices without an empty panel or permanent pause; own/release only its pause request, clear pending choices on Game Over, reset run-specific upgrade state, and resume only when allowed by `GameManager`. Resolve the authoritative persistent `GameManager` at runtime; do not rely on a serialized scene reference. Test UI display/selection with mock definitions only in an isolated harness or Unity test assembly; never register mocks, fake choices, placeholders, or empty handlers in production. Choice Buttons must not have conflicting serialized Inspector `On Click()` handlers. Wire the Gameplay UI and verify sequential level-ups, single application, capped/empty choices, Game Over while open, and new-run reset. **DONE** — Gameplay wiring and required UPG-01 flow manually verified by the project owner in Unity Play Mode; no concrete upgrade effects were part of this verification. `Depends: STAR-01,PROG-01` · `Reads: P6,P7,P9,P12,P13`.
- [x] **UPG-02** — Implement Multi Shot (one additional symmetric projectile per selection, cap 5 including base), Rapid Fire (-0.04s per selection, minimum 0.14s, preserve configured base interval), and Damage Up (+1 damage per projectile, cap 5 from base 1), including eligibility and visible effects. **DONE** — the project owner manually verified all three upgrades in Unity. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [x] **UPG-RANDOM-01** — Uniformly sample up to three distinct eligible upgrades without replacement; show all when three or fewer are eligible; avoid the same unordered ID set in consecutive rounds whenever multiple valid sets exist; recalculate for queued levels and reset tracking on run start/finish. **DONE** — the project owner confirmed successful Unity Play Mode verification. This is a follow-up behavior and was not part of the initial UPG-01 implementation. `Depends: UPG-01` · `Reads: P7`.
- [x] **UPG-03** — Add one follow-and-fire Drone per selection, maximum 2; fire right through shared Bullet pool; inherit Damage Up only, not Multi Shot or Rapid Fire. **DONE** — the project owner confirmed successful Unity Play Mode verification. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [ ] **UPG-04** — Shield absorbs one Player-Asteroid collision and automatically reactivates after cooldown; levels use 12s/9s/6s cooldowns. Upgrading during cooldown leaves the current timer unchanged and affects the next cycle; unavailable Shield follows Game Over. `Depends: UPG-01` · `Reads: P7,P9,P12`.
- [ ] **UPG-05** — Smoothly attract Stars within level radii 3/4/5 world units; maximum level 3 and no teleporting. `Depends: UPG-01,STAR-01` · `Reads: P6,P7,P12`.

## Difficulty & Scene Flow

- [ ] **DIFF-01** — Sector timer and difficulty scaling with spawn floor. `Depends: AST-02` · `Reads: P8`.
- [ ] **GAME-01** — Player-Asteroid Shield/death branches + EndGame transition. `Depends: SCORE-01,UPG-04` · `Reads: P8,P9`.
- [ ] **UI-01** — HUD: Score, Level, XP bar, Sector; optional Shield/Time. **PARTIAL** — Gameplay đã có Level/XP HUD và serialized references; cần chỉnh layout khỏi vùng giữa màn hình, tăng độ tương phản text để đáp ứng tiêu chí dễ đọc/không che gameplay. `Depends: SCORE-01,UPG-01,DIFF-01` · `Reads: P13`.
- [ ] **SCENE-01** — MainMenu: Play, Instructions, Quit. `Depends: SETUP-01,ARCH-01` · `Reads: P2`.
- [ ] **SCENE-02** — EndGame: Score, Level, Time, Play Again, Main Menu, Quit. `Depends: GAME-01` · `Reads: P2,P8`.

## World & Polish

- [ ] **WORLD-01** — Infinite recycled background + optional parallax. `Depends: SETUP-01` · `Reads: P10,P11,P13`.
- [ ] **POLISH-01** — Shooting/hit/explosion/Star/level-up feedback. `Depends: core gameplay` · `Reads: P13`.
- [ ] **TRANS-01** — Optional short scene fade. `Depends: SCENE-01,SCENE-02` · `Reads: P2,P13`.

## QA & Documentation

- [ ] **QA-01** — Performance/lifecycle audit: bounded pools, no off-screen buildup, no hot-loop Find/GetComponent. `Depends: gameplay complete` · `Reads: P4,P5,P6,P11`.
- [ ] **QA-02** — Full midterm functional checklist. `Depends: QA-01` · `Reads: P1,P16`.
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
