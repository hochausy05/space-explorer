# PLAN.md — Space Explorer

Source of truth for game logic. Keep this file compact; task details live in `TASKS.md`.

## P1 — Game Identity

- Unity 2D, landscape 16:9.
- Genre: horizontal endless space shooter + light roguelite upgrades.
- Core loop: `Shoot -> Destroy Asteroid -> Gain Score + Drop Stars -> Collect XP -> Level Up -> Choose Upgrade -> Harder Sector -> Repeat`.
- Each run starts with 10/10 Player HP. A ready Energy Shield absorbs one asteroid impact; otherwise the Player takes asteroid-type damage. Game Over occurs at 0 HP.

## P2 — Scenes

- `MainMenu`: Play, Instructions panel, Quit.
- `Gameplay`: Player, shooting, Asteroids, Stars, upgrades, difficulty, HUD, background.
- `EndGame`: Final Score, Level, Survival Time, Play Again, Main Menu, Quit.

SCENE-02 implementation note (PARTIAL): `EndGamePresenter` reads the persistent `GameManager.LastResult`, formats Score/Level/Survival Time, and routes Play Again/Main Menu through `SceneLoader`; Quit calls `Application.Quit()` in a player build. It displays placeholders if no snapshot exists and guards repeated scene transitions. The EndGame scene still needs Canvas/UI object wiring and Unity Play Mode/standalone verification.

## P3 — Player Movement

- Arrow Keys, 4 directions.
- Player faces right and may move across the full visible camera area.
- Normalize diagonal input.
- Use short acceleration/deceleration; responsive, not slippery.
- Clamp the Player's existing `Collider2D.bounds` to the full visible camera area; account for the collider bounds center offset from the Rigidbody2D position. Keep the complete collider inside the camera on both axes.
- Prefer Rigidbody2D with interpolation.
- Suggested tuning: speed `6.5–8`, acceleration `18–26`, deceleration `22–32`.

## P4 — Shooting & Bullet Pool

- Hold Space to auto-fire right.
- Base: 1 bullet, damage 1. The current serialized firing interval is a tuning starting point; increase it slightly to reduce the current firing rate, then approve the final value in Play Mode.
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

The owner reports current encounter pacing is too fast. Reduce initial asteroid speed and/or spawn pressure slightly, retain Small/Large distinctions and spawn variation, and tune in Unity; do not treat an unverified exact balance value as final. HP damage defaults are Small `1` and Large `3`, Inspector-configurable and owner-approved.

## P6 — Stars

- Stars are upgrade XP, not Score.
- Standard design: Stars only drop from destroyed Asteroids.
- Star uses pooling.
- On collect: add XP -> return to pool.
- Off-screen left: return to pool.
- Star Magnet pulls nearby Stars smoothly; no teleport.
- Stars remain XP only; do not add direct Score rewards or ambient Star spawning. This intentionally differs from the assignment's literal Star-for-points wording.

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

Upgrade choice presentation (UPG-UI-01): production `UpgradeDefinition` assets provide a display name, concise static description, and optional Sprite icon. `UpgradeSelectionPanel` binds metadata from the selected randomized definition to the matching card, clears old content on each show/hide, and hides an icon whose Sprite is missing. This presentation layer does not change eligibility, choice generation, effect application, or callback ownership.

The project owner confirmed successful Unity Play Mode verification of UPG-UI-01: all six icons were assigned, and icons, names, descriptions, dynamic choices, and selection behavior worked. This is manual owner confirmation; no automated test or build result is implied.

UPG-03-VISUAL-01 is DONE by owner-reported Unity Play Mode verification: the dedicated Drone sprite and Player-sprite fallback work, and the adjusted formation avoids overlap while drones continue to follow and fire.

Concrete upgrade definitions and ownership:

- **Multi Shot (UPG-02)** — each selection adds one projectile per volley with symmetric spread; maximum 5 projectiles including the base projectile.
- **Rapid Fire (UPG-02)** — each selection reduces the configured firing interval by `0.04s`; minimum `0.14s`. Exclude when another reduction cannot improve the interval.
- **Damage Up (UPG-02)** — each selection adds 1 damage per projectile; maximum 5 damage, from base damage 1.
- **Drone Wing (UPG-03)** — each selection adds one follow-and-fire Drone; maximum 2. Drones fire right using the shared Bullet pool and inherit Damage Up only, not Multi Shot or Rapid Fire.
- **Energy Shield (UPG-04)** — absorbs one Player-Asteroid collision, then reactivates automatically after cooldown. Upgrade levels use cooldowns `12s`, `9s`, and `6s`. Upgrading during cooldown does not change the current timer; the new level applies to the next cycle.
- **Star Magnet (UPG-05)** — smoothly attracts Stars within radii of `3`, `4`, and `5` world units at levels 1–3; exclude at level 3. Stars must never teleport to the Player.

Owner-reported Unity Play Mode verification status (2026-10-18): UPG-02 (Multi Shot, Rapid Fire, Damage Up), UPG-RANDOM-01 (random upgrade selection), UPG-03 (Drone Wing), UPG-04 (Energy Shield), and UPG-05 (Star Magnet) are verified DONE. For UPG-05, the project owner confirmed the Player component and production definition asset were wired and that the three attraction levels work with Star pickups, XP, pooling, and randomized upgrade selection. This is manual owner confirmation; no automated test or build result is implied. The approved upgrade definitions and limits above remain unchanged.

UPG-04 implementation note: `EnergyShieldController` provides level, readiness/cooldown state, one-hit consumption, and new-run reset; `EnergyShieldUpgradeDefinition` registers its capped upgrade. `PlayerAsteroidCollisionHandler` consumes the shield through `TryAbsorbAsteroidCollision()` and sends an unabsorbed hit through the GAME-01 run-finish path. The project owner confirmed successful Unity setup and Play Mode verification for UPG-04; this is manual verification, not an automated test or build result. The controller does not duplicate collision or Game Over handling.

## P8 — Score, Run Result & Difficulty

Score and Star XP are separate.

EndGame receives:

- Final Score
- Level reached
- Survival Time

Sector increases over run time. Implement gradual difficulty scaling and tune its pacing so pressure does not become overwhelming immediately.

Difficulty may scale:

- spawn interval
- Asteroid HP
- speed
- Large Asteroid probability

Use a reasonable spawn-interval floor. Existing values may be used as tuning references, not as verified final balance; test initial spawn pressure, movement speed, and later Sector escalation in Unity.

A new run resets all run-scoped state.

Preserve current asteroid-destruction Score (`+10` Small, `+30` Large). Apply an Inspector-configurable Score penalty for each genuine Player-Asteroid impact, whether or not Shield absorbs it: Small `5`, Large `15`. Clamp Score at zero. This intentionally differs from the assignment's collision-deducts/Stars-add-points wording: asteroid destruction continues to award Score, Stars remain XP.

SCORE-01 implementation note (DONE): Existing code awards `+10` for Small and `+30` for Large through the asteroid destruction callback; `GameManager.TryBeginRun()` resets `CurrentScore` and clears `LastResult`. `PlayerAsteroidCollisionHandler` measures run time from `RunStarted` using scaled Unity time and submits the final score, level, and survival time once through `TryFinishRun()`. `RunResult` is an immutable value and `GameManager.LastResult` retains it through the EndGame scene load; the next Gameplay run clears it. The project owner confirmed successful manual Unity verification of the implemented SCORE-01 behavior; this records owner-reported runtime verification, not an automated test or build. EndGame result presentation remains part of SCENE-02.

## P9 — Collision & Game Over

Required collision pairs:

- Player <-> Asteroid
- Player <-> Star
- Bullet <-> Asteroid

Avoid unnecessary pairs such as Bullet-Bullet or Star-Star.

Player-Asteroid:

- Start each new run at 10/10 HP.
- Resolve each genuine impact once and apply one configurable Score penalty (clamp Score at zero); do not treat continuous overlap or duplicate callbacks as new impacts.
- Ready Shield -> consume one charge, prevent HP damage, and enter the existing cooldown. Keep the current 12/9/6 second level behavior and do not alter cooldown upgrades.
- Without a ready Shield -> apply the asteroid type's configurable HP damage: Small `1`, Large `3` (owner-approved starting values).
- A proposed, configurable ~1 second post-hit protection window suppresses repeat HP damage only. It does not suppress a distinct impact's one Score penalty; a ready Shield still absorbs a genuine impact while protection is active. Repeated callbacks or continued overlap from the same impact must not cause repeated penalties or Shield consumption.
- At 0 HP -> finish the run through the existing GameManager/result/EndGame flow, capturing Score, Level, and Survival Time.
- Shield cooldown levels and upgrade-during-cooldown behavior are defined in P7.

Implementation note (GAME-01 — DONE for its original scope): `PlayerAsteroidCollisionHandler` is integrated with the Player's existing trigger collider, ignores non-Asteroid contacts, consumes one ready Shield charge, and otherwise disables movement/fire, captures score/level/survival time, finishes the run via `GameManager`, then loads `EndGame` through `SceneLoader`. The project owner confirmed successful Unity setup and Play Mode verification of that original one-hit flow; this is manual verification, not an automated test or build result. The revised 10 HP behavior will extend this integration through the planned Stage 1 health and collision tasks.

COLLISION-HP-01 implementation note (DONE): `PlayerAsteroidCollisionHandler` applies the configured 5/15 Score penalty once after its existing same-collider/same-physics-step guard and before trying Shield absorption. A ready Shield therefore prevents HP loss but not the penalty. Unshielded impacts use the existing configured 1/3 HP damage and only finish at zero. `GameManager.TryApplyScorePenalty()` clamps Score to zero and publishes `ScoreChanged`; Score awards, reset, final snapshot, and run ownership remain in GameManager. The project owner confirmed the COLLISION-HP-01 behavior in Unity Play Mode; this is manual owner verification, not an automated test or build result. The timed post-hit protection window remains in HIT-PROTECTION-01 and is not implemented here.

The proposed configurable ~1 second post-hit protection timing remains unapproved and belongs to HIT-PROTECTION-01.

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
- `PlayerHealth` — owns current/max HP, new-run reset, bounded damage, and a health-changed event for display consumers
- Player-Asteroid collision handler — one resolution per genuine impact, Score penalty, Shield routing, HP damage/protection, and GameManager finish at zero HP
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
- Player HP bar showing current HP accurately; reuse the existing HP bar if present and avoid a duplicate.

Level-up choices use a separate selection UI with one selectable control per offered upgrade. Keep the panel readable and avoid obscuring essential gameplay context.

On an Energy Shield impact, show a brief cyan-blue circular or hexagonal barrier around the Player, flash on impact, then fade. This is a visual-only effect with a suggested duration near `0.8s` pending Play Mode tuning; it adds no physics or collision behavior. Optional brief Player hit feedback belongs in POLISH-01 and must not duplicate the Shield impact effect.

Optional:

- Shield status
- Survival Time
- Build status panel (UI-BUILD-01): display Drone count, Shield readiness/cooldown, per-projectile damage, effective Player fire interval, and Player projectiles per volley from the existing gameplay APIs; display only and do not own or recalculate gameplay state.

UI-BUILD-01 is DONE by owner-reported Unity Play Mode verification. The panel intentionally remains text-based; no icon conversion is planned. The owner confirmed stat updates, Shield cooldown display, and new-run reset.

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
- defensive base/station, base HP, base damage, repair-base upgrade, or tower-defense gameplay
- pre-run Core Selection Hub; use the existing flow Main Menu -> Play -> Gameplay -> collect XP -> choose a randomized level-up upgrade -> continue -> End Game

## P16 — Midterm Done

Ready when all are true:

- MainMenu -> Gameplay -> EndGame -> MainMenu works.
- Instructions works.
- Player moves smoothly in four directions across the full camera; Collider2D remains inside the visible bounds.
- Player starts at 10/10 HP, asteroid types apply their configured damage, a ready Shield prevents HP damage, and Game Over occurs at 0 HP.
- Each genuine asteroid impact applies one configured Score penalty without duplicate resolution or negative Score; protection prevents repeat HP loss from overlap.
- Hold-to-fire works.
- Base firing interval and movement penalty while holding fire match their approved Play Mode tuning.
- Bullet/Asteroid/Star recycling works.
- Asteroids spawn, take damage, die, score, and drop Stars.
- Star XP and Level Up work.
- Six standard upgrades work within caps.
- Shield protection, cooldown, and impact feedback work.
- Sector difficulty increases.
- Score/Level/Time reach EndGame correctly.
- Assignment deviations are clear: destroyed Asteroids award Score rather than collision-only scoring; asteroid impacts incur a configurable penalty; Stars remain XP and do not add Score or spawn independently.
- Random asteroid movement and initial/later pacing are acceptable.
- Performance, pooling cleanup, and Unity Console are checked.
- No severe Console errors remain.
- README and CHANGELOG match the real project.
