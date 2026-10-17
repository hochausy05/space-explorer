# PLAN.md — Space Explorer

Source of truth for game logic. Keep this file compact; task details live in `TASKS.md`.

## P1 — Game Identity

- Unity 2D, landscape 16:9.
- Genre: horizontal endless space shooter + light roguelite upgrades.
- Core loop: `Shoot -> Destroy Asteroid -> Gain Score + Drop Stars -> Collect XP -> Level Up -> Choose Upgrade -> Harder Sector -> Repeat`.
- Game over: Player hits an Asteroid without an active Shield.

## P2 — Scenes

- `MainMenu`: Play, Instructions panel, Quit.
- `Gameplay`: Player, shooting, Asteroids, Stars, upgrades, difficulty, HUD, background.
- `EndGame`: Final Score, Level, Survival Time, Play Again, Main Menu, Quit.

SCENE-02 implementation note (PARTIAL): `EndGamePresenter` reads the persistent `GameManager.LastResult`, formats Score/Level/Survival Time, and routes Play Again/Main Menu through `SceneLoader`; Quit calls `Application.Quit()` in a player build. It displays placeholders if no snapshot exists and guards repeated scene transitions. The EndGame scene still needs Canvas/UI object wiring and Unity Play Mode/standalone verification.

## P3 — Player Movement

- Arrow Keys, 4 directions.
- Player faces right and mainly stays in the left half of the screen.
- Normalize diagonal input.
- Use short acceleration/deceleration; responsive, not slippery.
- Clamp the Player's existing `Collider2D.bounds` to the visible camera area; account for the collider bounds center offset from the Rigidbody2D position. Keep the horizontal limit at the screen midpoint.
- Prefer Rigidbody2D with interpolation.
- Suggested tuning: speed `6.5–8`, acceleration `18–26`, deceleration `22–32`.

## P4 — Shooting & Bullet Pool

- Hold Space to auto-fire right.
- Base: 1 bullet, damage 1, fire interval about `0.30–0.40s`.
- Multi Shot uses symmetric spread.
- Keep the configured base firing interval; Rapid Fire reduces it by `0.04s` per upgrade to a minimum of `0.14s`.
- Damage Up adds 1 damage per projectile, to a maximum of 5 damage.
- Bullets use Object Pooling.
- Return Bullet on hit, off-screen right, or lifetime expiry.
- Player and Drone share the same Bullet pool when possible.
- Never Instantiate/Destroy per shot after warm-up.

## P5 — Asteroids

Two standard types:

- Small: faster, low HP, `+10 Score`, drops 1 Star.
- Large: slower, higher HP, `+30 Score`, drops 2–3 Stars.

Spawn outside the right edge. Randomize once on spawn:

- Y position
- speed
- rotation speed
- type
- optional small vertical drift

On death: Score -> Star drop -> optional effect -> return to pool.

If off-screen left: return to pool with no penalty.

## P6 — Stars

- Stars are upgrade XP, not Score.
- Standard design: Stars only drop from destroyed Asteroids.
- Star uses pooling.
- On collect: add XP -> return to pool.
- Off-screen left: return to pool.
- Star Magnet pulls nearby Stars smoothly; no teleport.

## P7 — Leveling & Upgrades

XP thresholds, overflow, and level state are owned by `StarXPCollector` and defined by PROG-01. Upgrade flow consumes its level-up event; it must not recalculate XP or levels.

Shared level-up flow:

1. Queue every level-up event and process requests sequentially.
2. Pause through a GameManager-owned request/token; only one selection panel may be active.
3. Offer up to 3 distinct upgrades that are registered with implemented effects and remain eligible. Fewer choices are valid when fewer eligible upgrades remain.
4. Never show capped or otherwise ineligible upgrades. Do not invent rarity, weights, or extra effects.
5. Apply the selected registered effect exactly once, close the panel, then process the next queued level-up.
6. Resume only while the run is active and no other pause owner remains. On Game Over, close the panel, clear queued choices, and do not resume.
7. Reset run-specific upgrade state when a new run starts.
8. If a queued level-up has no eligible registered effect, dismiss that request without showing an empty panel or leaving gameplay paused; continue processing the queue and release only the upgrade flow's pause request when appropriate.

Testing strategy: production gameplay may register only upgrades with implemented effects. Never register fake choices, placeholder upgrades, or empty handlers in production. UI selection tests may use mock definitions/handlers only through an isolated test harness or Unity test assembly; test display and selection there without registering those mocks in normal gameplay.

Randomized selection policy (introduced separately under UPG-RANDOM-01; it was not part of the initial UPG-01 implementation):

- Gather all registered upgrades with implemented effects that are currently eligible, retaining unique-ID filtering.
- Uniformly select up to 3 distinct choices without replacement. If 3 or fewer are eligible, show all of them.
- Avoid the same unordered set of upgrade IDs in consecutive displayed selection rounds within a run whenever more than one valid set exists. If the first uniform sample repeats the previous set, uniformly choose among the remaining valid sets without retry loops.
- Recalculate eligibility for each queued level-up. Reset previous-set tracking on run start and clear it on run finish.
- Keep Editor-only mock selection state isolated from production selection state; mocks remain outside the production registry.

Concrete upgrade definitions and ownership:

- **Multi Shot (UPG-02)** — each selection adds one projectile per volley with symmetric spread; maximum 5 projectiles including the base projectile.
- **Rapid Fire (UPG-02)** — each selection reduces the configured firing interval by `0.04s`; minimum `0.14s`. Exclude when another reduction cannot improve the interval.
- **Damage Up (UPG-02)** — each selection adds 1 damage per projectile; maximum 5 damage, from base damage 1.
- **Drone Wing (UPG-03)** — each selection adds one follow-and-fire Drone; maximum 2. Drones fire right using the shared Bullet pool and inherit Damage Up only, not Multi Shot or Rapid Fire.
- **Energy Shield (UPG-04)** — absorbs one Player-Asteroid collision, then reactivates automatically after cooldown. Upgrade levels use cooldowns `12s`, `9s`, and `6s`. Upgrading during cooldown does not change the current timer; the new level applies to the next cycle.
- **Star Magnet (UPG-05)** — smoothly attracts Stars within radii of `3`, `4`, and `5` world units at levels 1–3; exclude at level 3. Stars must never teleport to the Player.

Owner-reported Unity Play Mode verification status (2026-10-17): UPG-02 (Multi Shot, Rapid Fire, Damage Up), UPG-RANDOM-01 (random upgrade selection), UPG-03 (Drone Wing), and UPG-04 (Energy Shield) are verified DONE. This records the project owner's manual confirmation; no automated test or build result is implied. UPG-05 remains a separate unfinished task with its specification above unchanged.

UPG-04 implementation note: `EnergyShieldController` provides level, readiness/cooldown state, one-hit consumption, and new-run reset; `EnergyShieldUpgradeDefinition` registers its capped upgrade. `PlayerAsteroidCollisionHandler` consumes the shield through `TryAbsorbAsteroidCollision()` and sends an unabsorbed hit through the GAME-01 run-finish path. The project owner confirmed successful Unity setup and Play Mode verification for UPG-04; this is manual verification, not an automated test or build result. The controller does not duplicate collision or Game Over handling.

## P8 — Score, Run Result & Difficulty

Score and Star XP are separate.

EndGame receives:

- Final Score
- Level reached
- Survival Time

Sector increases every ~30 seconds.

Difficulty may scale:

- spawn interval
- Asteroid HP
- speed
- Large Asteroid probability

Suggested spawn interval starts around `1.8s` and should not go below about `0.7–0.8s`.

A new run resets all run-scoped state.

SCORE-01 implementation note (DONE): Existing code awards `+10` for Small and `+30` for Large through the asteroid destruction callback; `GameManager.TryBeginRun()` resets `CurrentScore` and clears `LastResult`. `PlayerAsteroidCollisionHandler` measures run time from `RunStarted` using scaled Unity time and submits the final score, level, and survival time once through `TryFinishRun()`. `RunResult` is an immutable value and `GameManager.LastResult` retains it through the EndGame scene load; the next Gameplay run clears it. The project owner confirmed successful manual Unity verification of the implemented SCORE-01 behavior; this records owner-reported runtime verification, not an automated test or build. EndGame result presentation remains part of SCENE-02.

## P9 — Collision & Game Over

Required collision pairs:

- Player <-> Asteroid
- Player <-> Star
- Bullet <-> Asteroid

Avoid unnecessary pairs such as Bullet-Bullet or Star-Star.

Player-Asteroid:

- Active Shield -> absorb one collision, Player survives, and Shield enters its current cooldown.
- Unavailable Shield or no Shield -> disable movement/fire, capture run result, and transition to EndGame.
- Shield cooldown levels and upgrade-during-cooldown behavior are defined in P7.

Implementation note (GAME-01 — DONE): `PlayerAsteroidCollisionHandler` is integrated with the Player's existing trigger collider, ignores non-Asteroid contacts, consumes one ready Shield charge, and otherwise disables movement/fire, captures score/level/survival time, finishes the run via `GameManager`, then loads `EndGame` through `SceneLoader`. The project owner confirmed successful Unity setup and Play Mode verification; this is manual verification, not an automated test or build result.

No multi-HP Player system in the standard midterm scope.

## P10 — Background

- Fixed gameplay camera.
- World/background scrolls right-to-left.
- Use reusable background tiles instead of a huge map.
- Recycle tiles after they leave the left edge.
- Optional slower far-star layer for parallax.

## P11 — Runtime Lifecycle & Performance

Pool these repeated runtime objects:

- Bullet
- Asteroid
- Star
- repeated Effect

Rules:

- no gameplay object stays alive indefinitely outside camera;
- reset pooled state on reuse;
- cache hot-path component references;
- avoid scene-wide Find calls, repeated GetComponent, LINQ, and avoidable allocations in hot loops;
- randomize Asteroid movement on spawn, not every frame.

Suggested prewarm only:

- Bullet 96
- Asteroid 32
- Star 64
- Effect 24

Pools may grow safely if needed.

## P12 — Architecture

Recommended responsibilities:

- `GameManager` — run state / top-level coordination; sole owner of `Time.timeScale` and pause-request ownership
- `PlayerController` — input / movement / bounds
- `WeaponController` — fire timing / spread / weapon stats
- `Bullet` — projectile lifecycle
- `PoolManager` or reusable pool component
- `Asteroid` — HP / movement / damage / death
- `AsteroidSpawner` — spawning / difficulty input
- `StarPickup` — drift / magnet / collect
- `StarXPCollector` — XP, thresholds, level state, and level-up events (PROG-01)
- `UpgradeManager` — registered upgrade definitions/effects, queued level-up choices, and selection flow; it consumes `StarXPCollector` events without owning XP or level calculations
- `DroneController` — follow / auto-fire
- `ShieldController` — shield / cooldown
- `HUDController` — display state only
- `BackgroundScroller` — tile/parallax recycling
- `RunResult` — transfer final run data
- `SceneLoader` — scene transitions

Avoid God Objects and unnecessary frameworks.

## P13 — HUD & Feedback

HUD minimum:

- Score
- Level
- Star XP bar
- Sector

Level-up choices use a separate selection UI with one selectable control per offered upgrade. Keep the panel readable and avoid obscuring essential gameplay context.

Optional:

- Shield status
- Survival Time

Polish priority:

1. smooth movement
2. clear shooting
3. Asteroid hit feedback
4. explosion
5. Star burst/drop
6. clear Level Up choices
7. visible upgrade effect
8. parallax
9. short scene fade if time remains

## P14 — Project Structure

Recommended only; do not refactor a reasonable existing layout just to match it.

```text
Assets/
  Art/
  Audio/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Player/
    Combat/
    Asteroids/
    Pickups/
    Upgrades/
    UI/
    World/
  Tests/
```

## P15 — Non-Goals

Do not add unless explicitly requested:

- boss
- shop
- inventory
- permanent progression
- save profile
- online leaderboard
- multiplayer
- procedural galaxy
- large skill tree
- story/dialogue framework
- mobile controls
- gamepad support

## P16 — Midterm Done

Ready when all are true:

- MainMenu -> Gameplay -> EndGame -> MainMenu works.
- Instructions works.
- Player movement is smooth and bounded.
- Hold-to-fire works.
- Bullet/Asteroid/Star recycling works.
- Asteroids spawn, take damage, die, score, and drop Stars.
- Star XP and Level Up work.
- Six standard upgrades work within caps.
- Shield/game-over behavior works.
- Sector difficulty increases.
- Score/Level/Time reach EndGame correctly.
- No severe Console errors remain.
- README and CHANGELOG match the real project.
