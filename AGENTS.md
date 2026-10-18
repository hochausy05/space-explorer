# AGENTS.md — Space Explorer Codex Guide

Scope: entire repository.

## Purpose

Keep Codex focused, token-efficient, safe with Unity assets, and aligned with the current task.

## Source priority

When instructions conflict, use this order:

1. Current user prompt.
2. `AGENTS.md`.
3. Current task in `TASKS.md`.
4. Referenced sections in `PLAN.md`.
5. Latest relevant entry in `CHANGELOG.md`.
6. `README.md`.

If a prompt intentionally changes product logic, update the affected docs in the same task.

## Context routing

For every coding task:

1. Read the requested `TASK-ID` in `TASKS.md`.
2. Read only the `PLAN.md` sections listed under that task's `Reads`.
3. Read only the latest `CHANGELOG.md` entry by default.
4. Inspect only the code, prefab, scene, or asset files directly related to the task.
5. Read `README.md` only for setup, usage, build, or documentation work.

Do not scan the full repository unless the task genuinely requires it.
Do not read all of `PLAN.md` or `CHANGELOG.md` by default.

## Task scope

- Default: one prompt = one `TASK-ID`.
- Do not implement the next task automatically.
- Prefer the smallest correct patch.
- Do not add unrelated features or refactors.
- Preserve existing user changes.

## Product invariants

Space Explorer is a 2D landscape horizontal endless space shooter with lightweight roguelite upgrades.

Do not change these unless the user explicitly asks:

- Player moves in 4 directions across the full visible camera area and remains inside camera bounds by its Collider2D.
- Player shoots to the right.
- Asteroids spawn from the right and move left.
- Destroyed asteroids award score and drop Stars.
- Stars are upgrade XP, not score.
- Level-up pauses gameplay and offers 1 of 3 valid upgrades.
- Standard upgrades: Multi Shot, Rapid Fire, Damage Up, Drone Wing, Energy Shield, Star Magnet.
- Player starts each run at 10/10 HP; asteroid damage varies by type, a ready Energy Shield prevents HP damage for one collision, and Game Over occurs at 0 HP. Small/Large damage suggestions and collision Score penalty remain configurable pending Play Mode approval.
- The existing game flow has no pre-run Core Selection Hub or defensive base.
- Difficulty increases over time by Sector.
- Main scenes: MainMenu, Gameplay, EndGame.

Do not add boss, shop, inventory, permanent progression, multiplayer, backend, or large frameworks unless requested.

## Unity safety

- Never invent Unity GUIDs, fileIDs, or serialized references.
- Avoid hand-editing complex `.unity`, `.prefab`, or `.meta` YAML unless the existing structure makes it clearly safe.
- If Editor wiring cannot be done safely, implement the code and provide short manual Inspector steps.
- Never claim Unity compile/playtest passed unless it actually ran.
- Keep Inspector-tunable gameplay values serialized.

## Performance invariants

- Bullet, Asteroid, Star, and repeated Effect runtime objects must use pooling.
- Do not Instantiate/Destroy one bullet per shot after pool warm-up.
- Recycle Bullet when hit, off-screen, or expired.
- Recycle Asteroid when destroyed or off-screen left.
- Recycle Star when collected or off-screen left.
- Recycle repeated effects when finished.
- Do not leave gameplay objects alive indefinitely outside the camera.
- Cache component references used in hot paths.
- Avoid `GameObject.Find`, scene-wide Find APIs, repeated `GetComponent`, LINQ, and avoidable allocations in `Update`, `FixedUpdate`, collision callbacks, and fire loops.
- Randomize asteroid movement parameters on spawn, not every frame.

## Player feel

Keep movement responsive with light smoothing:

- Normalize diagonal input.
- Use short acceleration and deceleration.
- Avoid long drifting inertia.
- Clamp movement to gameplay camera bounds.
- Prefer Rigidbody2D interpolation when applicable.

## Physics

Only keep gameplay collision relationships that matter:

- Player <-> Asteroid
- Player <-> Star
- Bullet <-> Asteroid

Avoid unnecessary collision pairs.

## Code style

- Use English identifiers and asset/folder names.
- Prefer `[SerializeField] private` for Inspector fields.
- Keep classes focused; avoid God Objects.
- Comment intent or non-obvious decisions, not obvious syntax.
- Keep tuning values configurable when practical.

## Verification

After changes, run the narrowest useful verification available:

1. Unity/C# compile if possible.
2. Existing automated tests if relevant.
3. Static sanity checks.
4. Short manual playtest checklist when Editor interaction is required.

If something cannot be run, say `NOT RUN` or `MANUAL PENDING`.
Do not fabricate successful checks.

## Documentation update

After any prompt that changes the repository:

- Update the current task status in `TASKS.md`.
- Prepend one new entry to `CHANGELOG.md`.
- `CHANGELOG.md` must remain Vietnamese.
- Do not rewrite old changelog history unless asked.

## Git safety

- No `git reset --hard`.
- No force push.
- Do not discard user changes.
- Do not commit or push unless requested.
- Check `git status` before and after work.

## Completion rule

A task is `DONE` only when:

- requested scope is implemented;
- relevant verification was run or clearly marked manual;
- `TASKS.md` is updated;
- `CHANGELOG.md` is updated.

Otherwise use `PARTIAL` or `BLOCKED`.

## Final response format

Keep the result concise:

```text
TASK-ID — DONE | PARTIAL | BLOCKED

Changed:
- ...

Verified:
- ...

Manual:
- ...   # only when needed

Next:
- ...
```
