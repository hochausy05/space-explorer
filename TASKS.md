# TASKS.md — Space Explorer

One prompt = one task by default.

- `[ ]` not done
- `[x]` done
- Keep unfinished/blocked tasks unchecked; record `PARTIAL` or `BLOCKED` in `CHANGELOG.md`.

## Current

**NEXT:** `COMBAT-02`

## Foundation

- [x] **SETUP-01** — Unity baseline: folders, 3 scenes, 16:9, build config. `Reads: P1,P2,P14` · Verify: compile + open scenes.
- [x] **ARCH-01** — Core runtime skeleton: run state, RunResult, SceneLoader, pooling base. `Depends: SETUP-01` · `Reads: P2,P8,P11,P12` · Verify: compile.

## Player & Shooting

- [x] **PLAYER-01** — Smooth 4-direction movement, acceleration/deceleration, bounds, Rigidbody2D. **DONE** · `Depends: SETUP-01` · `Reads: P3` · Verify: movement playtest.
- [x] **COMBAT-01** — Hold-to-fire + Bullet pool + off-screen/lifetime recycle. **DONE** · `Depends: PLAYER-01,ARCH-01` · `Reads: P4,P11` · Verify: fire spam + pool observation.

## Asteroids & Stars

- [x] **AST-01** — Small/Large Asteroid model, HP, movement, rotation, pooling, off-screen recycle. **DONE** · `Depends: ARCH-01` · `Reads: P5,P11`.
- [x] **AST-02** — Asteroid spawner with controlled spawn randomness. **DONE** · `Depends: AST-01` · `Reads: P5,P8`.
- [ ] **COMBAT-02** — Bullet-Asteroid collision, damage, death path. `Depends: COMBAT-01,AST-01` · `Reads: P4,P5,P9`.
- [ ] **STAR-01** — Star drop, pool, drift, collect, off-screen recycle. `Depends: COMBAT-02` · `Reads: P6,P11`.
- [ ] **SCORE-01** — Score, survival time, clean run reset, result snapshot. `Depends: COMBAT-02` · `Reads: P8`.

## Leveling & Upgrades

- [ ] **UPG-01** — Star XP, level-up pause, up to 3 unique valid choices. `Depends: STAR-01` · `Reads: P6,P7,P13`.
- [ ] **UPG-02** — Multi Shot, Rapid Fire, Damage Up. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7`.
- [ ] **UPG-03** — Up to 2 smooth-follow auto-fire Drones using shared Bullet pool. `Depends: UPG-01,COMBAT-01` · `Reads: P4,P7,P12`.
- [ ] **UPG-04** — Energy Shield with 1-hit absorb and cooldown. `Depends: UPG-01` · `Reads: P7,P9`.
- [ ] **UPG-05** — Star Magnet with smooth attraction and light distance checks. `Depends: UPG-01,STAR-01` · `Reads: P6,P7`.

## Difficulty & Scene Flow

- [ ] **DIFF-01** — Sector timer and difficulty scaling with spawn floor. `Depends: AST-02` · `Reads: P8`.
- [ ] **GAME-01** — Player-Asteroid Shield/death branches + EndGame transition. `Depends: SCORE-01,UPG-04` · `Reads: P8,P9`.
- [ ] **UI-01** — HUD: Score, Level, XP bar, Sector; optional Shield/Time. `Depends: SCORE-01,UPG-01,DIFF-01` · `Reads: P13`.
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
