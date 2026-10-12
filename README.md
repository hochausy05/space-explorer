# Space Explorer

A Unity 2D landscape endless horizontal space shooter built for a Game Programming midterm.

## Concept

The Player controls a spaceship on the left side of the screen and shoots Asteroids approaching from the right.

Destroyed Asteroids:

- award Score;
- drop Stars.

Stars are upgrade XP.

When enough Stars are collected, gameplay pauses and the Player chooses 1 of up to 3 upgrades.

Core loop:

`Shoot -> Destroy -> Collect -> Level Up -> Upgrade -> Survive -> Harder Sector -> Repeat`

The run ends when the Player collides with an Asteroid without an active Energy Shield.

---

## Controls

| Input | Action |
|---|---|
| Arrow Keys | Move spaceship |
| Space | Hold to shoot |
| Mouse | Menu and upgrade selection |

Controls may later be implemented through the Unity Input System without changing gameplay intent.

---

## Main Features

- Landscape 16:9 gameplay.
- Smooth 4-direction movement.
- Hold-to-fire shooting.
- Bullet object pooling.
- Small and Large Asteroids.
- Randomized Asteroid spawn parameters.
- Asteroid pooling and off-screen recycling.
- Star drop, collection, and pooling.
- Star XP and leveling.
- 1-of-3 upgrade selection.
- Six standard upgrades:
  - Multi Shot
  - Rapid Fire
  - Damage Up
  - Drone Wing
  - Energy Shield
  - Star Magnet
- Sector-based difficulty scaling.
- Score and Survival Time.
- MainMenu, Gameplay, EndGame scenes.
- Instructions panel.
- Recycled infinite background/parallax.

---

## Scenes

### MainMenu

- Play
- Instructions
- Quit

### Gameplay

- Player
- shooting
- Asteroids
- Stars
- leveling
- upgrades
- Sector difficulty
- HUD
- background

### EndGame

- Final Score
- Level reached
- Survival Time
- Play Again
- Main Menu
- Quit

---

## Documentation Map

| File | Purpose |
|---|---|
| `AGENTS.md` | Compact Codex rules and context routing |
| `PLAN.md` | Product and technical source of truth |
| `TASKS.md` | Roadmap, scope, dependencies, and Definition of Done |
| `CHANGELOG.md` | Vietnamese prompt-by-prompt progress history |
| `README.md` | Human-facing project overview and setup |

Codex should not read every file in full for every task.

Each task in `TASKS.md` lists the exact `PLAN.md` sections it needs under `Reads`.

---

## Recommended Codex Prompt

Use a short prompt:

```text
Implement PLAYER-01 from TASKS.md.
Stay within task scope and verify the result.
```

Replace `PLAYER-01` with the required task ID.

The repository instructions define how Codex should load the remaining context.

---

## Recommended Repository Structure

```text
Assets/
  Art/
  Audio/
  Prefabs/
    Player/
    Projectiles/
    Asteroids/
    Pickups/
    Effects/
    UI/
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

AGENTS.md
PLAN.md
TASKS.md
CHANGELOG.md
README.md
```

An existing reasonable project layout does not need to be reorganized only to match this example.

---

## Performance Rules

Frequently created runtime objects must have bounded lifecycles.

- Bullet -> recycle on hit, timeout, or off-screen.
- Asteroid -> recycle on death or off-screen left.
- Star -> recycle on collect or off-screen left.
- Repeated Effect -> recycle after completion.
- Background -> reposition/recycle.

Do not use per-shot Instantiate/Destroy after Bullet pool warm-up.

Do not allow gameplay objects to accumulate outside the camera.

---

## Unity Version

Do not hard-code the Unity version in documentation.

The project version source of truth is:

`ProjectSettings/ProjectVersion.txt`

Do not upgrade Unity unless explicitly requested.

---

## Opening the Project

1. Open the repository using the Unity Editor version in `ProjectSettings/ProjectVersion.txt`.
2. Wait for import and script compilation.
3. Check the Console.
4. Open MainMenu or Gameplay.
5. If a task could not safely wire scene/prefab references automatically, follow the manual Inspector steps from that task result.

---

## Current Development State

Use:

- `TASKS.md` -> `Current`
- `CHANGELOG.md` -> latest entry

Do not infer completion state from README.

---

## Design Authority

If documentation and code disagree:

1. current user prompt;
2. `AGENTS.md`;
3. current task in `TASKS.md`;
4. referenced sections in `PLAN.md`.

When product logic changes, update the related documentation in the same task.
