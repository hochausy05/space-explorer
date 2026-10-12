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

## P3 — Player Movement

- Arrow Keys, 4 directions.
- Player faces right and mainly stays in the left half of the screen.
- Normalize diagonal input.
- Use short acceleration/deceleration; responsive, not slippery.
- Clamp to camera bounds.
- Prefer Rigidbody2D with interpolation.
- Suggested tuning: speed `6.5–8`, acceleration `18–26`, deceleration `22–32`.

## P4 — Shooting & Bullet Pool

- Hold Space to auto-fire right.
- Base: 1 bullet, damage 1, fire interval about `0.30–0.40s`.
- Multi Shot uses symmetric spread.
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

Suggested XP rule:

`RequiredStars = 5 + (Level - 1) * 3`

On level-up:

1. keep overflow XP;
2. pause gameplay;
3. show up to 3 unique valid upgrades;
4. exclude maxed upgrades;
5. apply selected upgrade;
6. resume.

Standard upgrades:

- **Multi Shot** — more bullets per volley; suggested cap 5.
- **Rapid Fire** — lower fire interval; keep a safe minimum around `0.12–0.16s`.
- **Damage Up** — increase bullet damage.
- **Drone Wing** — add up to 2 follow-and-fire Drones.
- **Energy Shield** — absorbs one collision; suggested cooldown levels `12s / 9s / 6s`.
- **Star Magnet** — increase attraction radius; suggested 3 levels.

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

## P9 — Collision & Game Over

Required collision pairs:

- Player <-> Asteroid
- Player <-> Star
- Bullet <-> Asteroid

Avoid unnecessary pairs such as Bullet-Bullet or Star-Star.

Player-Asteroid:

- Shield active -> consume Shield, Player survives.
- No Shield -> disable movement/fire, capture run result, transition to EndGame.

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

- `GameManager` — run state / top-level coordination
- `PlayerController` — input / movement / bounds
- `WeaponController` — fire timing / spread / weapon stats
- `Bullet` — projectile lifecycle
- `PoolManager` or reusable pool component
- `Asteroid` — HP / movement / damage / death
- `AsteroidSpawner` — spawning / difficulty input
- `StarPickup` — drift / magnet / collect
- `UpgradeManager` — XP / level-up / upgrade choices
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
