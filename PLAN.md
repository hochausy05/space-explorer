# PLAN.md — Space Explorer

Source of truth for game logic. Keep this file compact; task details live in `TASKS.md`.

## P1 — Game Identity

- Unity 2D, landscape 16:9.
- Genre: horizontal endless space shooter + light roguelite upgrades.
- Core loop: `Shoot -> Destroy Asteroid -> Gain Score + Drop Stars -> Collect XP -> Level Up -> Choose Upgrade -> Harder Sector -> Repeat`.
- Each run starts with 10/10 Player HP. A ready Energy Shield absorbs one asteroid impact; otherwise the Player takes asteroid-type damage. Game Over occurs at 0 HP.

## P2 — Scenes

- `MainMenu`: GameLogo; Play, Instructions, Records, and Quit buttons; one right-side InfoPanel that toggles between InstructionsContent and RecordsContent. Play loads Gameplay directly and relies on the existing GameManager scene lifecycle to begin/reset a fresh run.
- `Gameplay`: Player, shooting, Asteroids, Stars, upgrades, difficulty, HUD, background; bổ sung menu Settings/Pause trong lượt chơi theo GAMEPLAY-PAUSE-MENU-01.
- `EndGame`: GAME OVER logo; Final Score, Level, and Survival Time; Play Again and Main Menu buttons. Quit is intentionally omitted from the final EndGame layout.

SCENE-01 recent-run history: after a successful `GameManager.TryFinishRun`, persist the authoritative `RunResult` fields (Final Score, survival duration, Player Level) locally as JSON in PlayerPrefs. Keep the newest 20, newest first; trim oldest records. Merely opening MainMenu never creates a record. Render records in the existing RecordsContent Scroll View by cloning the hidden `RecordRowTemplate`; each row has Legacy Text fields for match number, score, total minutes:seconds, and level, with a Vietnamese empty state. Code is implemented, but MainMenu Inspector wiring and owner Play Mode verification remain pending. The menu script controls the existing `InstructionsTitle` and `InstructionsText` separately where there is no parent `InstructionsContent` object.

SCENE-02 implementation note (DONE): `EndGamePresenter` reads the persistent `GameManager.LastResult`, formats Score/Level/Survival Time, and routes Play Again/Main Menu through `SceneLoader`. It displays placeholders if no snapshot exists and guards repeated scene transitions. The project owner confirmed all EndGame functionality and visual layout checks passed in Unity Play Mode. The final presentation is GAME OVER, Score, Level, Survival Time, Play Again, and Main Menu; Quit was intentionally removed. This is owner-reported manual verification, not an automated test or build result.

## P3 — Player Movement

- Arrow Keys, 4 directions.
- Player faces right and may move across the full visible camera area.
- Normalize diagonal input.
- Use short acceleration/deceleration; responsive, not slippery.
- Clamp the Player's existing `Collider2D.bounds` to the full visible camera area; account for the collider bounds center offset from the Rigidbody2D position. Keep the complete collider inside the camera on both axes.
- Prefer Rigidbody2D with interpolation.
- Suggested tuning: speed `6.5–8`, acceleration `18–26`, deceleration `22–32`.
- While Space is held to fire, reduce Player movement target speed to 90% of its configured base; restore immediately on release. Read the existing weapon input state and do not mutate the base movement setting. **DIFF-01 DONE:** owner-confirmed Unity Play Mode verification passed.

## P4 — Shooting & Bullet Pool

- Hold Space to auto-fire right.
- Base: 1 bullet, damage 1. Keep the serialized Gameplay interval (`0.35s`) and apply the approved 1.10 base-interval multiplier, yielding `0.385s` before Rapid Fire. Rapid Fire continues to subtract `0.04s` per level to a `0.14s` floor. **DIFF-01 DONE:** owner-confirmed Unity Play Mode verification passed.
- Multi Shot uses symmetric spread.
- Preserve the configured base interval; Rapid Fire reduces the effective interval by `0.04s` per upgrade to a minimum of `0.14s`.
- Damage Up adds 1 damage per projectile with no gameplay-level cap; reset to base damage 1 on a new run and handle damage values without integer wraparound.
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

`POLISH-EXPLOSION-01` reuses the existing `AsteroidExplosion.prefab` for one visual at the captured world position on the shared genuine-destruction callback. All seven asteroid kinds, including Splitter Fragments, share a prewarmed, expandable `PrefabPool`; off-screen release and cleanup do not invoke the effect. The animation restarts at its first state frame on each lease and is returned when its non-looping Animator state completes. Animator normal time follows pause. Per-kind scale multipliers are Inspector-tunable; suggested starting values for Small/Large/Speed/Homing/Erratic/Splitter/Fragment are `0.8/1.1/1.0/1.2/1.2/1.3/0.6` and should be visually tuned against the existing sprite PPU. The visual has no physics components and does not change rewards or SFX. **DONE:** the project owner confirmed the shared effect works correctly in Unity Play Mode; this is manual owner verification, not an automated test or build result.

If off-screen left: return to pool with no penalty.

DIFF-01 approved starting balance and progression: apply an initial `0.8` speed multiplier and `0.75` spawn-frequency multiplier to the existing configured values (the current Gameplay interval `1.8s` therefore starts at `2.4s`; current Small speed `3–4` starts at `2.4–3.2`, and Large speed `2–2.5` starts at `1.6–2.0`). Every 30 seconds of active Playing time, add `0.05` to the speed multiplier and `0.08` to the spawn-frequency multiplier relative to this balanced baseline; cap those progression multipliers at `1.6` and `2.0`. The effective spawn interval is the configured interval divided by `0.75 * frequencyProgression`; it bottoms at `0.9s` with the current `1.8s` setting. Pause time does not count, new runs reset progression, and speed scaling applies only to asteroids spawned afterward. Keep asteroid-to-asteroid physical interactions enabled as approved by the owner. **DIFF-01 DONE:** the project owner confirmed all ten Unity Play Mode acceptance checks. This is manual owner verification, not an automated test or build result. HP damage defaults remain Small `1` and Large `3`, Inspector-configurable and owner-approved.

Approved Asteroid Evolution Phase 1 (`AST-EVOL-01`) is independent of Player Level and DIFF-01's 30-second speed/frequency steps. Reuse `AsteroidSpawner`'s active Playing elapsed clock; every 60 seconds advances the HP stage, and a new run resets it to stage 0. At spawn, assign Small maximum HP `1,2,3,4,5,6` and Large maximum HP `3,5,7,9,11,13` for stages 0–5 (stage 5 begins at 5:00 and remains capped). Pause time does not count. Each asteroid keeps the HP assigned at its spawn, so existing asteroids never gain HP when a stage changes. Prefab baseline HP remains Small `1` and Large `3`; collision damage and Score rules are unchanged. The owner reported observing ordinary Asteroid HP increase over time in Play Mode; this does not verify caps, reset, pause, or existing-asteroid stability.

Tiến trình HP của Special Asteroid dùng cùng đồng hồ Gameplay đang chạy của DIFF-01/AST-EVOL-01, không phụ thuộc Level Player. HP riêng của từng loại được chọn tại thời điểm spawn theo bảng dưới đây; trước phút 1, nếu spawn thử thủ công, dùng HP khởi đầu. Ở phút 5 trở đi HP giữ nguyên tại mức tối đa. Không cập nhật HP của Asteroid đang hoạt động khi chuyển giai đoạn.

| Thời gian Gameplay đang chạy | Speed | Homing | Erratic | Splitter |
| --- | ---: | ---: | ---: | ---: |
| 0:00–0:59 (mốc khởi đầu cho spawn thử) | 3 | 6 | 6 | 5 |
| 1:00–1:59 | 3 | 6 | 6 | 5 |
| 2:00–2:59 | 4 | 8 | 8 | 6 |
| 3:00–3:59 | 5 | 9 | 9 | 8 |
| 4:00–4:59 | 5 | 11 | 11 | 9 |
| 5:00 trở đi | 6 | 12 | 12 | 10 |

Giới hạn HP lần lượt là Speed 6, Homing 12, Erratic 12 và Splitter 10. Splitter Fragment luôn có 1 HP và không tiến hóa. Quy tắc này không thay đổi HP Small/Large, xác suất spawn Special (0/6/10/14/18/20%), DIFF-01 30-second speed/frequency steps hoặc bất kỳ damage/reward nào. Thời gian lấy từ `AsteroidSpawner` và dừng khi gameplay pause; run mới đặt đồng hồ về 0.

Các biến thể Special đã duyệt và quy tắc va chạm/phần thưởng với Player:

- **Speed:** đi thẳng sang trái, không trôi dọc hoặc xoay; tốc độ bằng `1.5x–2.0x` tốc độ Small thường sau DIFF-01 đúng một lần; HP theo bảng Special độc lập (tối đa 6); va Player `5 HP`/`-25 Score`; bị đạn phá hủy thưởng `+40 Score`/đúng 8 Star.
- **Homing:** tốc độ Small thường sau DIFF-01; hướng về Player hiện tại tối đa 3 giây với tốc độ quay không quá 75 độ/giây, sau đó tiếp tục theo hướng cuối; thiếu target thì tiếp tục an toàn; HP theo bảng Special độc lập (tối đa 12); va Player `5 HP`/`-25 Score`; phá hủy thưởng `+50 Score`/9 Star.
- **Erratic:** tốc độ Small thường sau DIFF-01; chuyển động sóng/zigzag mượt có thể chỉnh, mặc định sóng mượt (biên độ `0.7` world unit, `1.2` chu kỳ/giây); HP theo bảng Special độc lập (tối đa 12); va Player `5 HP`/`-25 Score`; phá hủy thưởng `+45 Score`/8 Star.
- **Splitter:** tốc độ Large thường sau DIFF-01; HP theo bảng Special độc lập (tối đa 10); va Player `5 HP`/`-25 Score`; bị đạn phá hủy thưởng `+60 Score`/10 Star và tạo đúng hai fragment không thể split. Fragment bay sang trái lệch lên/xuống `25°`, tốc độ `1.4x` Large thường, luôn có 1 HP và va Player gây `1 HP`/`-5 Score`; không thưởng Score/Star. Va Player, ra khỏi màn hình, dọn pool, reset run hoặc chuyển scene không kích hoạt split.

Mọi loại dùng chung vòng đời chuyển động/sát thương của `Asteroid`, `AsteroidPool`, đường damage của Bullet và Player collision handler. HP tối đa được truyền khi spawn từ thời gian Gameplay đang chạy; `Asteroid.BeginRun` khởi tạo HP hiện tại bằng HP tối đa đó, còn các lần lấy/trả pool reset trạng thái trước lần gán kế tiếp. Tính thưởng Score/Star/phá hủy idempotent trên instance được tái sử dụng. Special pool và Star pool có thể mở rộng khi cần rồi tái sử dụng high-water allocation; không có giới hạn số Special đang hoạt động hoặc cooldown gameplay. Tự động spawn Special dùng xác suất trong bảng tại mỗi nhịp spawn DIFF-01; các lần roll Special thành công phân phối Speed 35%, Erratic 30%, Homing 20%, Splitter 15%. Xác suất Small/Large không đổi khi roll không phải Special. Tự động spawn vẫn opt-in cho đến khi gán đủ bốn Special prefab, Splitter Fragment prefab, StarPool, camera và Player target. Có context-menu Play Mode spawn để thử từng loại độc lập. **Xác minh chủ dự án:** bốn loại Special, Splitter Fragments và automatic time-based spawning được xác nhận hoạt động trong Unity Play Mode. Task tích hợp code/prefab/spawn đã hoàn tất; `AST-SPECIAL-QA-01` vẫn PARTIAL để kiểm tra đầy đủ HP/cap, pause/reset, reward counts, pool reuse/stress và xác suất từng mốc. Với AST-EVOL-01, chủ dự án mới xác nhận HP Asteroid thường tăng theo thời gian; cap, pause, reset và độ ổn định HP sau spawn vẫn chưa được xác minh.

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
- **Damage Up (UPG-02; unlimited rule implemented under DIFF-01)** — each successful selection adds 1 damage per projectile, starting at base damage 1, with no gameplay-level level cap. Damage resets on a new run; runtime values use arbitrary-precision integers to avoid overflow/wraparound. Current damage remains displayed by the existing BuildStatusHUD. The project owner confirmed DIFF-01 in Unity Play Mode; no automated test/build is implied.
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

Sector increases over run time (Sector presentation remains separate UI work). DIFF-01 implements approved gradual asteroid difficulty scaling based on active Playing time: balanced speed progression is `1.0 + 0.05 * floor(activeSeconds / 30)`, capped at `1.6`; balanced spawn-frequency progression is `1.0 + 0.08 * floor(activeSeconds / 30)`, capped at `2.0`. Starting values apply `0.8` speed and `0.75` spawn-frequency multipliers to existing configured asteroid values. Pause/menu/finished time is excluded; each new run resets progression. Only newly spawned asteroids receive the current speed multiplier. Retain asteroid-to-asteroid physical collisions.

Difficulty may scale:

- spawn interval
- Asteroid HP
- speed
- Large Asteroid probability

The effective spawn interval is the existing configured base interval divided by `0.75 * spawnFrequencyProgression`; with the current `1.8s` serialized interval, it starts at `2.4s` and bottoms at `0.9s` when the relative progression reaches `2.0`. The initial Small/Large speed ranges are reduced by 20% from their existing prefab ranges. **DIFF-01 is DONE** by owner-confirmed Unity Play Mode verification of all ten acceptance checks.

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
- A configurable post-hit protection window defaults to `1.0s`. It starts after a ready Shield absorbs a hit or after an unshielded collision reduces HP and the Player survives. During protection, further asteroid contacts cause no Score penalty, HP damage, or Shield consumption and do not restart the timer. Use scaled time so the timer pauses during upgrade selection; reset it at new-run start. Repeated callbacks or continued overlap from one impact must not count as new impacts.
- A Shield-absorbed hit displays the existing transparent PNG `ShieldVisual` around the Player for the same protection window, with a short fade-in and fade-out. Unshielded nonlethal hits show no visual. Hide it when the run ends, a new run starts, or protection expires; it has no collider and does not affect gameplay.
- At 0 HP -> finish the run through the existing GameManager/result/EndGame flow, capturing Score, Level, and Survival Time.
- Thoát chủ động từ Settings Gameplay quay về MainMenu mà không kết thúc run, ghi lịch sử run hoặc phát Game Over SFX. Xóa pause request tạm thời và đưa GameManager về Ready để lần vào Gameplay tiếp theo bắt đầu run mới.
- Shield cooldown levels and upgrade-during-cooldown behavior are defined in P7.

Implementation note (GAME-01 — DONE for its original scope): `PlayerAsteroidCollisionHandler` is integrated with the Player's existing trigger collider, ignores non-Asteroid contacts, consumes one ready Shield charge, and otherwise disables movement/fire, captures score/level/survival time, finishes the run via `GameManager`, then loads `EndGame` through `SceneLoader`. The project owner confirmed successful Unity setup and Play Mode verification of that original one-hit flow; this is manual verification, not an automated test or build result. The revised 10 HP behavior will extend this integration through the planned Stage 1 health and collision tasks.

COLLISION-HP-01 implementation note (DONE): `PlayerAsteroidCollisionHandler` applies the configured 5/15 Score penalty once after its existing same-collider/same-physics-step guard and before trying Shield absorption. A ready Shield therefore prevents HP loss but not the penalty. Unshielded impacts use the existing configured 1/3 HP damage and only finish at zero. `GameManager.TryApplyScorePenalty()` clamps Score to zero and publishes `ScoreChanged`; Score awards, reset, final snapshot, and run ownership remain in GameManager. The project owner confirmed the COLLISION-HP-01 behavior in Unity Play Mode; this is manual owner verification, not an automated test or build result. Timed post-hit protection and its Shield-only visual are implemented under HIT-PROTECTION-01, which remains pending Play Mode verification.

HIT-PROTECTION-01 implementation note (DONE — manually verified by project owner): `PlayerAsteroidCollisionHandler` compares scaled Unity time with a serialized protection end time. A ready Shield still receives the collision penalty, absorbs one hit, enters its existing cooldown, and starts protection; unshielded damage starts the same protection only if HP decreases and the Player survives. Protected contacts return before Score penalty and Shield handling. The `Time.timeAsDouble` clock follows `Time.timeScale`, so selection pauses stop the window. The existing child `ShieldVisual` SpriteRenderer is disabled on handler initialization, shown only for Shield-absorbed hits, and faded through its alpha; no additional renderer or collider is created. The owner confirmed successful Play Mode verification of protection and the PNG effect; this is owner-reported manual verification, not an automated test or build. The enhanced multi-phase animation is tracked separately under SHIELD-ANIM-01.

The prior standalone SHIELD-FX-01 scope is folded into HIT-PROTECTION-01 and should be verified as part of that task, not implemented a second time.

## P10 — Background

- Fixed gameplay camera.
- World/background scrolls right-to-left.
- Use reusable background tiles instead of a huge map.
- Recycle tiles after they leave the left edge.
- Optional slower far-star layer for parallax.

`WORLD-PARALLAX-01` selects the approved two-layer starfield option: preserve the current static space background exactly, and add procedural `FarStars`/`NearStars` using one in-memory soft-circle sprite shared by all SpriteRenderers. Defaults are 45 far stars at `0.2` world units/s and 20 near stars at `0.6` world units/s; expose count, size, opacity, color, speed, sorting and camera fields in Inspector. Both layers use scaled `Time.deltaTime`, recycle positions after crossing the left edge, and map existing normalized positions to changed orthographic camera bounds to keep coverage when the viewport aspect changes. Use the current `Default` sorting layer, between static background order `-100` and gameplay renderers at `0`, with HUD Canvas in Screen Space Overlay. No background texture scrolling, physics, persistent objects or external assets. **DONE:** the owner confirmed successful Unity Play Mode testing with the existing circular stars; no four-pointed star changes are requested. This is manual owner verification, not an automated test or build result.

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

`POLISH-EXPLOSION-01` reuses the project's existing asteroid explosion prefab in a dedicated shared effect pool. It may grow beyond prewarm during bursts, then reuses the high-water allocation.

`WORLD-PARALLAX-01` creates its configured decorative stars once per Gameplay scene, shares one small runtime sprite/texture, reuses the same objects by repositioning, and destroys its runtime sprite/texture when the scene object is destroyed.

`FIX-POOL-DISPOSE-01` hardens the shared `PrefabPool<T>.Dispose()` lifecycle with Unity-aware destroyed-object checks and idempotent cleanup for scene unload/shutdown. **DONE:** the project owner confirmed the MissingReferenceException is resolved in Unity. This is owner-reported verification; no automated test or build is claimed here.

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

Gameplay HUD (UI-01) approved scope:

- Score display
- Level display
- Numeric Star XP text only; no XP progress bar
- Player HP displayed through the existing HP bar; do not add a duplicate
- Preserve the approved overall layout and the existing BuildStatusHUD implementation
- Sector is explicitly excluded from UI-01 and has no display requirement. This does not change DIFF-01 or gameplay mechanics.

UI-01 is DONE. The project owner approved the current Gameplay HUD and confirmed BuildStatusHUD was tested successfully in Unity Play Mode. This is owner-reported manual verification; no new automated test or build result is implied.

Level-up choices use a separate selection UI with one selectable control per offered upgrade. Keep the panel readable and avoid obscuring essential gameplay context.

On an Energy Shield impact, show the existing transparent PNG `ShieldVisual` around the Player, fading in and out within the same `1.0s` post-hit protection window. This is visual-only, adds no collider or collision behavior, and was manually verified by the project owner under HIT-PROTECTION-01. The requested multi-phase fade/pulse/blink refinement is tracked under SHIELD-ANIM-01. Optional brief Player hit feedback belongs in POLISH-01 and must not duplicate the Shield impact effect.

SHIELD-ANIM-01 specifies the existing SpriteRenderer animation only: fade in during `0–0.15s`, pulse subtly during `0.15–0.65s`, blink smoothly twice during `0.65–0.90s`, then fade out completely by `1.0s`. Use scaled protection time, preserve/restore the original sprite tint, and do not add assets, animation clips, Animator components, or gameplay effects. The project owner confirmed successful Unity Play Mode verification; this is manual owner verification, not an automated test or build result.

`POLISH-EXPLOSION-01` is a separate shared asteroid-death visual: reuse only the existing eight-frame `AsteroidExplosion` prefab, with type-specific scale multipliers, no physics components, and no additional SFX. Trigger it from the shared destruction callback so genuine Small/Large, Speed, Homing, Erratic, Splitter, and Fragment deaths each request one pooled effect; ordinary despawns and cleanup do not. Rewind the existing non-looping Animator state on reuse, return after its actual playback completes, and use Normal Animator time so pause freezes it. The project owner confirmed the shared effect works correctly in Unity Play Mode; this is manual verification, not automated testing.

Optional:

- Shield status
- Survival Time
- Build status panel (UI-BUILD-01): display Drone count, Shield readiness/cooldown, per-projectile damage, effective Player fire interval, and Player projectiles per volley from the existing gameplay APIs; display only and do not own or recalculate gameplay state.

UI-BUILD-01 is DONE by owner-reported Unity Play Mode verification. The panel intentionally remains text-based; no icon conversion is planned. The owner confirmed stat updates, Shield cooldown display, and new-run reset.

### GAMEPLAY-PAUSE-MENU-01 — Settings và tạm dừng

Đặt nút Settings biểu tượng bánh răng ở góc trên bên phải Gameplay. Panel gọn, ban đầu ẩn, có đúng ba nút: TIẾP TỤC, TẮT TIẾNG/BẬT TIẾNG và THOÁT. Khi mở, panel lấy pause request riêng từ `GameManager`, nên gameplay theo scaled time cùng DIFF-01/tiến hóa Asteroid dừng, còn UI Unity vẫn tương tác được. Khóa nút Settings khi đã có pause request để tránh chồng lên Upgrade Selection. Resume chỉ nhả token của menu; không tự đặt `Time.timeScale` hoặc ghi đè chủ sở hữu pause khác.

Tắt/mở tiếng qua `SfxAudioService` persistent, giữ nguyên volume trong Inspector và lưu lựa chọn mute bằng PlayerPrefs qua các scene/lần mở ứng dụng. Nhãn nút phản ánh trạng thái thực; Click tuân theo mute hiện tại và được handler runtime phát một lần. Exit chủ động bỏ run và quay về MainMenu qua `SceneLoader` mà không gọi `TryFinishRun`, lưu result/lịch sử hoặc phát Game Over SFX; xóa pause ownership tạm thời và chuyển run về Ready. Không thêm điều khiển nhạc, slider, hộp thoại xác nhận hoặc cài đặt khác.

Code nền đã có, nhưng GAMEPLAY-PAUSE-MENU-01 vẫn PARTIAL cho tới khi chủ dự án tạo và nối UI trên Canvas hiện có trong Unity, rồi xác nhận checklist Play Mode. Để trống cả bốn danh sách Button Inspector `On Click()` vì controller tự đăng ký runtime listeners.

### Sound effects (AUDIO-SFX-01)

Use only the owner's eight imported clips: Shoot, Bullet Hit, Asteroid Break, Shield Hit, XP Collect, Level Up, Click, and Game Over. A single persistent `SfxAudioService` is authoritative across scene loads. Calls originate from the existing gameplay/UI event paths: one Shoot sound after a successful player volley, Bullet Hit on a valid asteroid trigger, Asteroid Break only when a projectile destroys an asteroid (including fragments), Shield Hit only after successful absorption, XP Collect on a valid Star pickup, one Level Up sound per `StarXPCollector.LevelUp`, Click in existing menu/result/upgrade selection handlers, and Game Over only after `GameManager.TryFinishRun` succeeds. Never trigger sounds from pool release, despawn, or scene cleanup.

Playback is non-spatial (`spatialBlend = 0`), allows up to seven regular overlapping voices plus one reserved Game Over voice, and uses short per-effect repeat gates to avoid rapid-fire/hit/pickup spam. The Inspector provides master and per-effect volume multipliers. Create and wire the service in MainMenu, Gameplay, and EndGame so each scene can also be launched directly in Editor; `DontDestroyOnLoad` and singleton duplicate rejection retain the first configured service during normal navigation. The owner-supplied clips are in `Assets/Audio/SFX/`. The owner confirms all eight approved SFX are configured in Unity and work in Play Mode; AUDIO-SFX-01 is DONE. Background music is intentionally excluded.

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
- Shield protection, cooldown, and the refined multi-phase impact animation work; SHIELD-ANIM-01 was manually verified by the project owner.
- Sector difficulty increases.
- Score/Level/Time reach EndGame correctly.
- Assignment deviations are clear: destroyed Asteroids award Score rather than collision-only scoring; asteroid impacts incur a configurable penalty; Stars remain XP and do not add Score or spawn independently.
- Random asteroid movement and initial/later pacing are acceptable.
- Performance, pooling cleanup, and Unity Console are checked.
- No severe Console errors remain.
- README and CHANGELOG match the real project.
