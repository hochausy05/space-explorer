# CHANGELOG.md — Space Explorer


> Nhật ký tiến độ của dự án.
> File này chỉ dùng tiếng Việt.
> Entry mới luôn đặt ở đầu phần lịch sử.

## Quy tắc

Sau mỗi prompt có thay đổi repository, thêm một entry theo mẫu:

```md
## YYYY-MM-DD HH:mm ±HH:MM — TASK-ID: Tên ngắn

- **Trạng thái:** DONE | PARTIAL | BLOCKED
- **Phạm vi:** phần đã xử lý.
- **Thay đổi:** các thay đổi thực tế.
- **Kiểm tra:** compile/test/playtest đã chạy và kết quả.
- **Chưa kiểm tra / Rủi ro:** phần chưa thể xác minh hoặc cần kiểm tra thủ công.
- **Tiếp theo:** TASK-ID hoặc bước phù hợp tiếp theo.
```

## 2026-10-20 19:06 +07:00 — FIX-UNITY6-EDITOR-API-01: Hoàn tất thay API tìm component trong harness

- **Trạng thái:** DONE
- **Phạm vi:** Thay lời gọi API Unity 6 obsolete còn lại trong thao tác dừng UPG-01 harness.
- **Thay đổi:** `StopSelectionHarness()` dùng `Object.FindAnyObjectByType<UpgradeManager>()`, giống thao tác bắt đầu. Hàm chỉ cần một manager khả dụng để dừng kịch bản, không phụ thuộc thứ tự instance; hành vi start/stop được giữ nguyên.
- **Kiểm tra:** Rà soát toàn file, không còn `FindFirstObjectByType`; `git diff --check` trên các file thuộc task đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Chưa xác minh compile trong Unity Editor.
- **Tiếp theo:** `AST-SPEED-01`.

## 2026-10-20 18:58 +07:00 — FIX-UNITY6-EDITOR-API-01: Sửa API tìm component trong harness

- **Trạng thái:** DONE
- **Phạm vi:** Sửa lời gọi obsolete được báo tại dòng 21 của `UpgradeSelectionTestHarness`.
- **Thay đổi:** Đổi `Object.FindFirstObjectByType<UpgradeManager>()` tại lệnh bắt đầu harness thành `Object.FindAnyObjectByType<UpgradeManager>()`; thứ tự instance không phải yêu cầu của luồng này. Thêm task tooling tương ứng.
- **Kiểm tra:** Rà soát các lần dùng API trong `Assets`; `git diff --check` theo ba file thuộc task đạt. Kiểm tra toàn repo báo trailing whitespace trong `Assets/Scenes/EndGame.unity` đang có thay đổi ngoài phạm vi; không sửa scene. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Harness còn một lần `FindFirstObjectByType<UpgradeManager>()` tại `StopSelectionHarness` (dòng 81), có thể tiếp tục phát CS0618; giữ nguyên ngoài phạm vi sửa tối thiểu này.
- **Tiếp theo:** Xem xét riêng lần gọi còn lại nếu cần loại bỏ toàn bộ cảnh báo CS0618 tương tự.

## 2026-10-20 16:47 +07:00 — AST-SPEED-01: Thêm biến thể Speed Asteroid

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm biến thể Speed đầu tiên và đường spawn thủ công để kiểm tra; chưa bật hệ thống spawn Special ngẫu nhiên.
- **Thay đổi:** Asteroid có kind riêng cho Speed, dùng pool tùy chọn, chạy thẳng với tốc độ Small thường nhân DIFF-01 và hệ số ngẫu nhiên 1.5–2.0; HP lấy stage Small hiện tại. Va Player dùng nhánh hiện có với mặc định 5 HP/-25 Score, giữ Shield/protection. Bị đạn phá hủy thì thưởng mặc định +40 Score và gọi StarPool 8 lần. Có mục Context Menu để spawn thử trong Play Mode. Cập nhật task và đặc tả P5.
- **Kiểm tra:** Kiểm tra code path, giá trị prefab Small/Large, cấu hình trigger/collider và pool/reward; chủ dự án trước đó xác nhận đã quan sát HP Asteroid thường tăng theo thời gian, không xác nhận cap/reset/pause. `git diff --check` theo file thuộc task đạt. Không chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần tạo prefab `Asteroid_Speed`, gán vào `AsteroidPool`, và xác minh chuyển động, HP, hit Player/Shield, penalty, reward và tái sử dụng pool. StarPool có giới hạn dung lượng; mỗi lần phá hủy cần đủ 8 slot trống để đủ số Star.
- **Tiếp theo:** Hoàn tất setup Inspector và kiểm tra `AST-SPEED-01`; Homing, Erratic, Splitter và spawn Special tự động chưa triển khai.

## 2026-10-20 16:25 +07:00 — AST-EVOL-01: Tiến hóa HP Asteroid theo thời gian

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm tiến hóa HP Small/Large theo thời gian Gameplay chủ động; chưa triển khai bốn biến thể Asteroid đặc biệt.
- **Thay đổi:** Tái sử dụng đồng hồ active-time của `AsteroidSpawner`/DIFF-01. Mỗi 60 giây tăng một stage; khi spawn, Small nhận HP 1–6 và Large nhận HP 3–13 theo bảng duyệt, giữ nguyên HP đã gán cho Asteroid đang tồn tại. Đồng hồ reset theo `RunStarted`; prefab giữ HP baseline 1/3. Ghi yêu cầu vào P5 và thêm task `AST-EVOL-01`.
- **Kiểm tra:** Rà soát luồng spawn, reset run, trạng thái pause và pool reuse; kiểm tra whitespace theo các file thuộc task đạt. Kiểm tra toàn repo báo trailing whitespace trong scene EndGame đang được sửa ngoài phạm vi; không chỉnh scene. Chưa chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Chủ dự án xác nhận đã quan sát HP Asteroid thường tăng theo thời gian; cap, reset, pause và việc giữ nguyên HP của Asteroid cũ vẫn chưa xác minh. Không cần thay đổi Inspector hoặc prefab.
- **Tiếp theo:** Kiểm tra `AST-EVOL-01` trong Unity Play Mode; các quy tắc của bốn Special asteroid variants cần được xác định trước khi triển khai giai đoạn sau.

## 2026-10-20 14:48 +07:00 — SCENE-02: Loại tham chiếu Quit không còn dùng

- **Trạng thái:** DONE
- **Phạm vi:** Đồng bộ `EndGamePresenter` với thiết kế EndGame hiện tại chỉ có Play Again và Main Menu.
- **Thay đổi:** Bỏ trường `_quitButton`, đăng ký/hủy listener Quit và hàm xử lý Quit. Kiểm tra cấu hình vẫn yêu cầu ba nhãn kết quả cùng hai nút điều hướng còn lại; luồng RunResult và tải scene không đổi.
- **Kiểm tra:** Scene EndGame đang lưu có đủ ba nhãn và hai nút, trường `_quitButton` rỗng; xác nhận đây là nguyên nhân khiến `ReportMissingReferencesOnce()` báo lỗi. `git diff --check` đã chạy; không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Không cần đổi Inspector reference cho presenter; cần mở Unity để xác nhận Console không còn cảnh báo sau khi script biên dịch lại.
- **Tiếp theo:** `UI-01` — hoàn thiện Gameplay HUD.

## 2026-10-20 14:44 +07:00 — SCENE-02: Chốt EndGame

- **Trạng thái:** DONE
- **Phạm vi:** Ghi nhận hoàn tất EndGame theo xác nhận kiểm thử thủ công của chủ dự án; chỉ cập nhật tài liệu.
- **Thay đổi:** Đánh dấu SCENE-02 DONE trong TASKS và PLAN. Layout cuối gồm logo GAME OVER, Score, Level, Survival Time, nút Play Again và Main Menu. Nút Quit được chủ dự án chủ động loại bỏ.
- **Kiểm tra:** Chủ dự án xác nhận toàn bộ chức năng và bố cục đã đạt trong Unity Play Mode. Không chạy automated test, build hoặc Unity trong lượt tài liệu này. `git diff --check` đã chạy.
- **Chưa kiểm tra / Rủi ro:** UI-01 vẫn PARTIAL vì HUD Gameplay chưa hoàn tất.
- **Tiếp theo:** `UI-01` — hoàn thiện HUD Gameplay gồm Score, Level, XP progress, Sector và Player HP.

## 2026-10-19 21:13 +07:00 — SCENE-01: Chốt xác minh MainMenu và kỷ lục

- **Trạng thái:** DONE
- **Phạm vi:** Chốt SCENE-01 theo xác nhận Play Mode của chủ dự án; không sửa code hoặc asset.
- **Thay đổi:** Đánh dấu SCENE-01 DONE trong TASKS. Chủ dự án xác nhận MainMenu, điều hướng và lịch sử run gần đây hoạt động đúng trong Unity Play Mode.
- **Kiểm tra:** Xác nhận thủ công do chủ dự án báo cáo; không chạy Unity, automated test hoặc build trong lượt này. `git diff --check` đã chạy.
- **Chưa kiểm tra / Rủi ro:** Nút Quit chưa được kiểm tra trong standalone build; cần xác minh ở final QA.
- **Tiếp theo:** `UI-01` — hoàn thiện HUD Gameplay gồm Score, Level, XP progress, Sector và Player HP; Sector cùng bố cục/độ đọc còn dang dở.

## 2026-10-19 20:34 +07:00 — SCENE-01: Hoàn thiện controller menu và danh sách kỷ lục

- **Trạng thái:** PARTIAL
- **Phạm vi:** Hoàn thiện code điều khiển MainMenu và trình bày lịch sử 20 run; không chỉnh scene YAML.
- **Thay đổi:** `MainMenuController` điều khiển Play, Instructions, Records, Quit và CloseButton; InfoPanel được ẩn lúc khởi chạy, hai nội dung không hiển thị đồng thời. `RecentRunRecordRow` hiển thị bốn cột Legacy Text và tự ẩn Text mẫu trên root; controller clone template khi cần, giữ thứ tự mới nhất trước và tái sử dụng các row UI khi mở lại. Dữ liệu tiếp tục được lưu JSON trong PlayerPrefs từ `RunResult` sau `TryFinishRun` thành công, tối đa 20 bản ghi.
- **Kiểm tra:** Đọc wiring đã lưu trong MainMenu: có InfoPanel, InstructionsTitle/Text, RecordsContent, Scroll View, Content layout và một template Legacy Text; Play/Records/Quit chưa có callback Inspector, Instructions có `SetActive(true)`, Close có `SetActive(false)`. Rà soát code và lifecycle lưu dữ liệu; chưa chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Chưa gắn controller. Trong scene đang lưu chưa thấy RecordsHeader hoặc empty-state Text và RecordRowTemplate hiện chỉ có một Text mẫu; cần thêm bốn Text con riêng cho các cột, gắn `RecentRunRecordRow`, tạo empty-state Text dưới Content, rồi nối toàn bộ references. Xóa callback cũ ở InstructionsButton và CloseButton; lưu InfoPanel inactive. Cần kiểm tra điều hướng, danh sách/scroll 20 dòng, empty state, lưu qua lần chạy lại, thứ tự mới nhất và standalone Quit trong Unity.
- **Tiếp theo:** Hoàn tất Inspector wiring trong MainMenu và chạy checklist Play Mode để chốt SCENE-01.

## 2026-10-19 18:08 +07:00 — SCENE-01: Menu và lịch sử run gần đây

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm hành vi Play/Instructions/Records/Quit và lưu 20 run hoàn tất; không tạo scene mới.
- **Thay đổi:** Thêm `MainMenuController` để tải Gameplay qua `SceneLoader`, bật/tắt duy nhất một nội dung InfoPanel, hiển thị trạng thái rỗng hoặc danh sách Records và xử lý Quit an toàn trong Editor. Thêm `RecentRunHistory`, lưu JSON trong PlayerPrefs tại điểm `GameManager.TryFinishRun()` thành công; dùng trực tiếp `RunResult`, mới nhất trước, giữ tối đa 20. Không sửa scene/prefab YAML.
- **Kiểm tra:** Rà soát lifecycle GameManager, RunResult và SceneLoader; xác nhận scene MainMenu đã có Canvas, bốn Button và InfoPanel. `git diff --check` cho code/tài liệu thuộc task đạt; kiểm tra toàn diff chỉ báo trailing whitespace trong `Assets/Scenes/MainMenu.unity` đã dirty trước task và được giữ nguyên. Chưa chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Scene đang lưu chưa có `InstructionsContent`, `RecordsContent` hoặc Scroll View; component mới chưa gắn/wire. Cần hoàn tất Inspector setup và kiểm tra UI, điều hướng, lưu qua lần khởi động lại, giới hạn 20 record và chống ghi trùng trong Play Mode. SCENE-01 giữ PARTIAL.
- **Tiếp theo:** Trong Unity gắn `MainMenuController` lên Canvas, nối Button/InfoPanel references, tạo hai content và Records Scroll View/Text; gắn `RecentRunHistory` không cần component (được gọi bởi GameManager), lưu scene rồi chạy checklist.

## 2026-10-19 16:57 +07:00 — DIFF-01: Chốt cân bằng độ khó

- **Trạng thái:** DONE
- **Phạm vi:** Chốt DIFF-01 theo xác nhận Play Mode của chủ dự án; không sửa gameplay trong lượt tài liệu này.
- **Thay đổi:** Đánh dấu DIFF-01, WEAPON-PACING-01 và FIRE-MOVE-PENALTY-01 hoàn tất theo cùng phạm vi đã gộp. Giữ nguyên thông số đã duyệt: speed Asteroid ban đầu `0.80`, spawn frequency `0.75`, mỗi 30 giây chơi tăng `0.05`/`0.08`, cap `1.60`/`2.00`, movement khi bắn `0.90`, firing interval multiplier `1.10`, Damage Up `+1` không giới hạn cấp gameplay. Đặt `UI-01` làm task tiếp theo.
- **Kiểm tra:** Chủ dự án xác nhận cả 10 acceptance checks của DIFF-01 đạt trong Unity Play Mode. Đây là xác nhận thủ công; không tuyên bố automated test hoặc build. `git diff --check` đạt.
- **Chưa kiểm tra / Rủi ro:** Không chạy Unity hoặc automated test trong lượt tài liệu này. UI-01 vẫn PARTIAL; phần hiển thị Sector và bố cục/độ đọc còn thiếu.
- **Tiếp theo:** `UI-01` — hoàn thiện HUD Gameplay gồm Score, Level, XP, Sector và HP. Cần quyết định quy tắc đánh số Sector trước khi triển khai binding Sector.

## 2026-10-19 16:38 +07:00 — DIFF-01: Cân bằng độ khó và Damage Up không giới hạn

- **Trạng thái:** PARTIAL
- **Phạm vi:** Áp dụng cân bằng spawn/di chuyển Asteroid, tăng độ khó theo thời gian chơi, giảm tốc độ Player khi bắn, điều chỉnh firing interval và bỏ cap gameplay của Damage Up.
- **Thay đổi:** Giữ nguyên giá trị serialize; Asteroid mới dùng hệ số tốc độ ban đầu `0.8`, interval spawn `1.8s` được quy đổi theo frequency `0.75` thành `2.4s`. Mỗi 30 giây Playing cộng hệ số tương đối baseline `+0.05` speed/`+0.08` frequency, giới hạn `1.6`/`2.0`; pause không tăng timer và run mới reset. Player di chuyển ở 90% khi giữ Space. Base interval `0.35s` nhân `1.10` thành `0.385s`, vẫn giữ Rapid Fire `-0.04s` và floor `0.14s`. Damage Up tăng +1 không giới hạn gameplay, dùng `BigInteger` qua Bullet, pool và Asteroid để tránh wraparound; HUD hiện có tiếp tục đọc sát thương thực tế. Không sửa scene/prefab hoặc collision giữa Asteroid.
- **Kiểm tra:** Rà soát các call site damage, upgrade eligibility, spawn lifecycle, pause và serialized values; tìm kiếm xác nhận không còn cap Damage Up cũ. `git diff --check` đạt. Không chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần compile và Play Mode trong Unity; kiểm tra tuning, reset/pause, Rapid Fire, Multi Shot, Drone damage kế thừa và Damage Up tăng/hiển thị. DIFF-01 vẫn PARTIAL.
- **Tiếp theo:** Gán `Player > WeaponController > Base Fire Interval Multiplier = 1.10` và `Player > PlayerController > Movement Multiplier While Firing = 0.90` nếu giá trị mặc định chưa hiện; chạy checklist DIFF-01 trong Play Mode.

## 2026-10-19 15:48 +07:00 — SHIELD-ANIM-01: Chốt xác minh hoạt ảnh Shield

- **Trạng thái:** DONE
- **Phạm vi:** Chốt SHIELD-ANIM-01 theo xác nhận kiểm tra Play Mode của chủ dự án.
- **Thay đổi:** Đánh dấu SHIELD-ANIM-01 hoàn tất trong TASKS và PLAN. Chủ dự án xác nhận hoạt ảnh ShieldVisual đã được kiểm tra thành công trong Unity Play Mode.
- **Kiểm tra:** Xác nhận thủ công do chủ dự án báo cáo; không chạy automated test, build hoặc Unity trong lượt tài liệu này. `git diff --check` đạt.
- **Chưa kiểm tra / Rủi ro:** Không có kết quả automated test hoặc build mới.
- **Tiếp theo:** `DIFF-01` — tăng Sector và điều chỉnh độ khó theo roadmap (`Depends: AST-02`; `Reads: P5,P8`).

## 2026-10-19 15:40 +07:00 — SHIELD-ANIM-01: Làm mượt hoạt ảnh ShieldVisual

- **Trạng thái:** PARTIAL
- **Phạm vi:** Làm bóng hoạt ảnh SpriteRenderer hiện có; ghi nhận HIT-PROTECTION-01 đã được chủ dự án xác minh thủ công.
- **Thay đổi:** `PlayerAsteroidCollisionHandler` điều khiển màu/alpha theo thời gian bảo vệ đã scale: fade-in 0–0.15s, pulse nhẹ 0.15–0.65s, hai nhịp blink mượt 0.65–0.90s, rồi fade-out tới 1.0s. Khôi phục nguyên tint khi ẩn; không thêm asset, Animator, clip hoặc collider. Đánh dấu HIT-PROTECTION-01 và SHIELD-FX-01 DONE theo xác nhận Play Mode của chủ dự án; animation polish theo dõi riêng.
- **Kiểm tra:** Rà soát handler, PlayerHealth, EnergyShieldController, GameManager Score API, HUD và scene hiện tại. Scene có một collision handler, protection duration 1 giây, child `ShieldVisual`; tham chiếu serialized SpriteRenderer đang rỗng và được fallback bằng tên child. `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Scene đang lưu fade-in/fade-out `0.08`/`0.18`; đặt lại thành `0.15`/`0.10` trong Inspector. Cần chủ dự án kiểm tra nhịp fade/pulse/blink mới trong Play Mode, đảm bảo pause đóng băng animation và tint được khôi phục; SHIELD-ANIM-01 vẫn PARTIAL.
- **Tiếp theo:** Xác minh SHIELD-ANIM-01 trong Unity Play Mode.

## 2026-10-19 14:57 +07:00 — HIT-PROTECTION-01: Dùng PNG ShieldVisual cho hiệu ứng bảo vệ

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thay hiệu ứng LineRenderer tạm bằng SpriteRenderer PNG `Player/ShieldVisual` hiện có; giữ nguyên luồng Shield, HP và Score.
- **Thay đổi:** Handler nhận tham chiếu `Shield Visual`, tự tìm child đúng tên làm fallback và tắt renderer khi khởi tạo, kể cả khi Inspector đang bật. Khi Shield hấp thụ hit, sprite hiện trong cửa sổ bảo vệ và alpha fade theo scaled time; hit mất HP không hiện sprite. Xóa trường, hình học và cảnh báo cũ liên quan LineRenderer. Không sửa scene hoặc asset.
- **Kiểm tra:** Đọc wiring hiện có trong Gameplay: `ShieldVisual` là child Player, có SpriteRenderer đang bật; handler đang lưu protection duration `10`. Rà soát source; `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Trong Inspector gán SpriteRenderer của `Player/ShieldVisual` vào `Shield Visual`; đặt `Post Hit Protection Duration` thành `1.0` (scene hiện lưu `10`); xóa collision handler trùng còn lại. Kiểm tra fade, pause, ẩn khi hết hạn/new run/run end và các nhánh Shield/HP trong Play Mode. Task vẫn PARTIAL.
- **Tiếp theo:** Lưu wiring trong Unity và hoàn tất checklist Play Mode của HIT-PROTECTION-01.

## 2026-10-19 14:56 +07:00 — HIT-PROTECTION-01: Gộp Shield bubble vào bảo vệ sau va chạm

- **Trạng thái:** PARTIAL
- **Phạm vi:** Mở rộng bảo vệ sau hit để bao gồm hit được Shield hấp thụ và hiệu ứng bubble; không thêm collision handler mới hoặc ảnh hưởng gameplay qua VFX.
- **Thay đổi:** Shield READY vẫn bị trừ Score một lần, hấp thụ hit và vào cooldown; đồng thời bắt đầu cửa sổ bảo vệ 1 giây. Va chạm tiếp theo trong cửa sổ không trừ Score/HP, không dùng Shield và không gia hạn timer. Hit không Shield gây sát thương như cũ; nếu sống sót thì bắt đầu cùng cửa sổ nhưng không hiện bubble. Thêm hexagon cyan bán trong suốt với glow nhẹ bằng hai `LineRenderer`, fade-in/out và tự ẩn khi hết hạn, new run hoặc run kết thúc. Gộp phạm vi SHIELD-FX-01 vào HIT-PROTECTION-01.
- **Kiểm tra:** Rà soát luồng Score → Shield → HP, scaled-time pause và lifecycle reset/finish; chưa chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần trong Inspector tạo hai child renderer dưới Player, gán Glow/Outline/Material vào `PlayerAsteroidCollisionHandler`, chỉnh bán kính/width/sorting để không che tàu. Cần xóa một collision handler trùng còn lưu trong Gameplay scene. HIT-PROTECTION-01 vẫn PARTIAL.
- **Tiếp theo:** Hoàn tất wiring VFX và kiểm tra protection, Shield, pause và reset trong Unity Play Mode.

## 2026-10-19 14:35 +07:00 — HIT-PROTECTION-01: Chặn collision handler trùng trên Player

- **Trạng thái:** PARTIAL
- **Phạm vi:** Sửa nguyên nhân Shield READY không chặn sát thương; giữ nguyên mức Shield và cooldown.
- **Thay đổi:** Scene Gameplay hiện có hai `PlayerAsteroidCollisionHandler` cùng gắn trên một Player, trong khi HUD và cả hai handler đều đọc cùng `EnergyShieldController`. Callback thứ nhất tiêu thụ Shield; callback thứ hai có bộ khử trùng riêng nên thấy Shield đang cooldown và trừ HP (đồng thời có thể trừ Score lần nữa). Thêm `[DisallowMultipleComponent]` và runtime guard để chỉ handler đầu tiên đang bật xử lý; giữ kiểm tra trạng thái qua `TryAbsorbAsteroidCollision()` dùng chung với HUD `State`. Không có damage path HP thứ hai ngoài handler này. Không sửa scene/prefab YAML.
- **Kiểm tra:** Rà soát reference scene: `BuildStatusHUD._player` trỏ cùng GameObject Player; có đúng một `EnergyShieldController` và hai collision handler. Tìm toàn bộ `ApplyDamage` để xác nhận handler là nơi gọi. Chưa chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Trong Inspector của Gameplay > Player, xóa một trong hai `PlayerAsteroidCollisionHandler` (giữ một bản có `_progression` gán), xác nhận chỉ còn một `EnergyShieldController`, và giữ `BuildStatusHUD > Player` trỏ vào cùng Player. Chạy Play Mode để kiểm tra Shield READY chặn HP, trừ Score đúng một lần, cooldown 12/9/6 giây, hit cooldown áp dụng HP/protection và protection không tiêu thụ Shield. `HIT-PROTECTION-01` vẫn PARTIAL.
- **Tiếp theo:** Làm sạch component trùng trong Inspector và xác minh Shield cùng protection trong Unity Play Mode.

## 2026-10-19 14:21 +07:00 — HIT-PROTECTION-01: Thêm thời gian bảo vệ sau sát thương

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm cửa sổ bảo vệ sau hit vào collision handler hiện có; không thêm VFX hoặc handler mới.
- **Thay đổi:** Khi Asteroid thực sự làm giảm HP và Player sống sót, bắt đầu thời lượng cấu hình được, mặc định 1 giây. Trong thời gian này, va chạm Asteroid bỏ qua cả phạt Score, sát thương HP và tiêu thụ Shield. Hit được Shield hấp thụ vẫn bị trừ Score nhưng không bắt đầu bảo vệ. Dùng `Time.timeAsDouble` theo scaled time nên bộ đếm dừng khi pause; reset trạng thái ở đầu run mới.
- **Kiểm tra:** Rà soát thứ tự xử lý va chạm, kiểm tra sống sót sau sát thương và tương tác với GameManager pause; `git diff --check` trên các file thuộc task đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Trong Inspector của Player, xác nhận `Post Hit Protection Duration` là `1.0`. Cần Play Mode kiểm tra thời điểm bắt đầu/kết thúc, penalty và Shield trong thời gian bảo vệ, pause khi chọn upgrade, Game Over ở 0 HP và reset run.
- **Tiếp theo:** Xác minh `HIT-PROTECTION-01` trong Unity Play Mode.

## 2026-10-19 13:44 +07:00 — COLLISION-HP-01: Chốt kiểm tra va chạm và Score HUD

- **Trạng thái:** DONE
- **Phạm vi:** Ghi nhận xác nhận Play Mode của chủ dự án cho COLLISION-HP-01 và phần Score của UI-01.
- **Thay đổi:** Đánh dấu COLLISION-HP-01 DONE trong TASKS và PLAN. Chủ dự án xác nhận các kiểm tra Unity Play Mode cho va chạm/Score và Score HUD đã đạt. UI-01 vẫn PARTIAL vì các yêu cầu HUD còn lại chưa hoàn thành; đặt `HIT-PROTECTION-01` làm task tiếp theo.
- **Kiểm tra:** Xác nhận thủ công do chủ dự án báo cáo; không chạy automated test, build hoặc Unity trong lượt tài liệu này. `git diff --check` được chạy.
- **Chưa kiểm tra / Rủi ro:** Không có kết quả automated test mới. UI-01 còn Sector và công việc bố cục/độ đọc; HIT-PROTECTION-01 chưa triển khai.
- **Tiếp theo:** `HIT-PROTECTION-01` — post-hit protection window.

## 2026-10-19 13:29 +07:00 — UI-01: Thêm Score vào HUD Gameplay

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm binding chỉ hiển thị Score cho `ScoreText` có sẵn; giữ nguyên HUD Level/XP/HP và logic gameplay.
- **Thay đổi:** `ProgressionHUD` nhận Legacy UI Text qua Inspector, đọc Score hiện tại từ `GameManager`, đăng ký/hủy đăng ký `ScoreChanged` an toàn và hiển thị `SCORE: 0` khi run mới bắt đầu. Không tạo UI object hoặc sửa scene/prefab.
- **Kiểm tra:** Rà soát API Score và vòng đời đăng ký event; `git diff --check` trên các file thuộc task đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần gán `ScoreText` hiện có vào trường `Score Label` của `ProgressionHUD`, sau đó kiểm tra +10/+30 khi phá Asteroid, -5/-15 khi va chạm, clamp 0 và reset về 0 trong Play Mode. UI-01 còn thiếu Sector và hoàn thiện bố cục/độ đọc.
- **Tiếp theo:** Gán Score Text và xác minh cập nhật Score trong Unity Play Mode; giữ `COLLISION-HP-01` PARTIAL đến khi kiểm thử va chạm hoàn tất.

## 2026-10-19 13:09 +07:00 — COLLISION-HP-01: Áp dụng phạt Score theo va chạm

- **Trạng thái:** PARTIAL
- **Phạm vi:** Ghi nhận xác minh thủ công PLAYER-HP-01 và thêm phạt Score cho va chạm Player-Asteroid; không triển khai thời gian bất tử sau hit.
- **Thay đổi:** Chủ dự án xác nhận HP, sát thương Asteroid, thanh HP dùng lại, XPText và gameplay hiện có hoạt động trong Unity Play Mode; đánh dấu PLAYER-HP-01 DONE. Va chạm Small/Large áp dụng phạt cấu hình mặc định 5/15 kể cả khi Shield hấp thụ; GameManager chặn Score âm và phát `ScoreChanged`. Giữ sát thương HP 1/3, ưu tiên Shield, và chỉ kết thúc run khi HP về 0.
- **Kiểm tra:** Rà soát luồng va chạm, khử callback trùng cùng collider/cùng physics step và API Score; `git diff --check` trên các file thuộc task đạt. Không chạy Unity compile hoặc Play Mode trong lượt này.
- **Chưa kiểm tra / Rủi ro:** Cần kiểm tra thủ công penalty Small/Large, Shield vẫn trừ Score, clamp về 0, va chạm không trừ lặp, HP/Game Over, RunResult cuối, reset run và không phát sinh lỗi Console. Các trường `Small Asteroid Score Penalty`/`Large Asteroid Score Penalty` trên Player handler mặc định 5/15; HP damage giữ 1/3. `HIT-PROTECTION-01` chưa được triển khai.
- **Tiếp theo:** Kiểm tra `COLLISION-HP-01` trong Unity Play Mode.

## 2026-10-19 12:21 +07:00 — PLAYER-HP-01: Giữ XP text và dùng riêng thanh bar cho HP

- **Trạng thái:** PARTIAL
- **Phạm vi:** Sửa binding HUD theo yêu cầu: XPText tiếp tục hiển thị XP/threshold, LevelText tiếp tục hiển thị Level; chỉ thanh fill cũ biểu diễn HP.
- **Thay đổi:** Khôi phục hiển thị `XPIntoCurrentLevel / XPRequiredForNextLevel` từ `StarXPCollector`; fill cập nhật theo `PlayerHealth.CurrentHP / MaxHP` khi HP đổi và khi HUD khởi tạo. Giữ tham chiếu fill serialize cũ bằng `FormerlySerializedAs`; bổ sung alias `MaxHP` tương thích với `MaximumHP`; không tạo HPText/thanh mới, không sửa logic tiến trình hoặc scene/prefab.
- **Kiểm tra:** Rà soát API và binding; `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần trong Canvas Gameplay đổi tên `XPBarBackground` thành `HPBarBackground`, `XPBarFill` thành `HPBarFill` và giữ `XPText` nguyên tên. Trong `ProgressionHUD`, gán Player `StarXPCollector` và `PlayerHealth`, LevelText, XPText, và HPBarFill; xác nhận binding cũ được giữ sau Unity recompile. Chạy Play Mode để kiểm tra HP fill, XP text, Level và reset run.
- **Tiếp theo:** Xác minh HUD HP/XP trong Unity Play Mode để hoàn tất `PLAYER-HP-01`.

## 2026-10-19 12:10 +07:00 — PLAYER-HP-01: Dùng HUD XP hiện có để hiển thị HP

- **Trạng thái:** PARTIAL
- **Phạm vi:** Chuyển phần trình bày thanh/text XP thành HP, giữ nguyên hệ thống XP và Level.
- **Thay đổi:** `ProgressionHUD` tiếp tục hiển thị Level từ `StarXPCollector`, đồng thời đọc `PlayerHealth` để hiện `current / maximum HP` và fill thanh theo tỷ lệ. Tham chiếu Text/Image cũ được bảo toàn bằng `FormerlySerializedAs`; component tự tìm `PlayerHealth` trên cùng Player với collector nếu chưa gán. Thiếu nguồn hoặc UI được xử lý an toàn bằng log cấu hình một lần. Không tạo thanh mới, không sửa scene/prefab và không đổi logic XP.
- **Kiểm tra:** Rà soát nguồn dữ liệu, sự kiện `HealthChanged`, reset run và tương thích tham chiếu serialize; `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần trong Canvas Gameplay đổi tên `XPBarBackground` thành `HPBarBackground`, `XPBarFill` thành `HPBarFill`, `XPText` thành `HPText`; trong `ProgressionHUD` gán PlayerHealth, HP Text và HP Bar Fill, xác nhận Level Text/StarXPCollector còn đúng. Sau đó kiểm tra hiển thị 10/10, cập nhật sau sát thương, reset run và XP/level-up vẫn hoạt động.
- **Tiếp theo:** Hoàn tất wiring và xác minh Play Mode cho `PLAYER-HP-01`.

## 2026-10-19 11:56 +07:00 — PLAYER-HP-01: Thêm HP Player và sát thương Asteroid

- **Trạng thái:** PARTIAL
- **Phạm vi:** Chốt FIX-PLAYER-BOUNDS-01 theo xác nhận Play Mode của chủ dự án và triển khai mô hình HP Player cùng sát thương theo loại Asteroid.
- **Thay đổi:** Đánh dấu FIX-PLAYER-BOUNDS-01 DONE sau khi chủ dự án xác nhận cả năm kiểm tra di chuyển đạt. Thêm `PlayerHealth` với tối đa 10 HP, reset theo `GameManager.RunStarted`, giới hạn HP trong khoảng 0–10 và phát event `HealthChanged`. `Asteroid` công khai loại Small/Large; collision handler giữ ưu tiên hấp thụ của Shield, nếu không thì trừ mặc định cấu hình được 1 HP với Small hoặc 3 HP với Large và chỉ kết thúc run khi HP về 0. Không thêm phạt Score, bất tử sau hit hoặc VFX.
- **Kiểm tra:** Rà soát luồng run, Shield, loại Asteroid và kết quả Game Over; `git diff --check` đạt. Play Mode của bounds là xác nhận thủ công từ chủ dự án; không chạy Unity compile hoặc kiểm tra Play Mode cho HP.
- **Chưa kiểm tra / Rủi ro:** Chưa gắn `PlayerHealth` vào Player trong Gameplay và chưa xác minh runtime HP/UI. Cần thêm component vào Player hiện có; các trường `Small Asteroid Damage` và `Large Asteroid Damage` trên `PlayerAsteroidCollisionHandler` giữ mặc định 1 và 3 hoặc chỉnh trong Inspector. Không sửa scene/prefab.
- **Tiếp theo:** Hoàn tất wiring và checklist Play Mode cho `PLAYER-HP-01`; sau đó mới tiếp tục `COLLISION-HP-01`.

## 2026-10-19 11:41 +07:00 — FIX-PLAYER-BOUNDS-01: Mở rộng giới hạn di chuyển toàn màn hình

- **Trạng thái:** PARTIAL
- **Phạm vi:** Bỏ chặn Player ở nửa trái, giữ toàn bộ Collider2D trong camera orthographic.
- **Thay đổi:** Sửa `PlayerController` để giới hạn X dùng mép trái/phải đầy đủ của camera; tiếp tục tính offset giữa tâm collider và Rigidbody2D trên cả hai trục. Nếu collider lớn hơn vùng nhìn theo một trục, giới hạn trục đó về tâm camera để không tạo khoảng clamp đảo chiều. Giữ nguyên tốc độ, gia tốc, giảm tốc, input và Rigidbody2D.
- **Kiểm tra:** Rà soát công thức bounds và diff; `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần kiểm tra thủ công bốn hướng/góc camera, đi từ mép trái tới mép phải không dừng giữa màn hình, collider luôn trong khung, di chuyển chéo, cảm giác gia tốc/giảm tốc và bắn khi đang di chuyển; chưa xác minh lỗi Console.
- **Tiếp theo:** Chạy checklist Play Mode của `FIX-PLAYER-BOUNDS-01`; không cần thay đổi Inspector.

## 2026-10-18 20:55 +07:00 — ROADMAP-REVISION-01: Đồng bộ kế hoạch gameplay đã duyệt

- **Trạng thái:** DONE
- **Phạm vi:** Cập nhật hướng phát triển và ranh giới tính năng theo chỉ đạo; chỉ sửa tài liệu, không triển khai gameplay.
- **Thay đổi:** Player được định hướng di chuyển toàn màn hình; kế hoạch bổ sung HP 10/10, sát thương theo loại Asteroid, phạt Score mỗi va chạm thật, bảo vệ sau hit và hiệu ứng Shield. Giá trị Small 1/Large 3 HP, thời gian bảo vệ khoảng 1 giây, hiệu ứng khoảng 0,8 giây, mức phạt Score và cân bằng bắn/spawn đều được ghi là đề xuất cần duyệt/Play Mode tune. Giữ Score khi phá Asteroid và Star chỉ cấp XP; ghi rõ khác biệt với yêu cầu bài tập. Chia việc còn lại thành 5 giai đoạn và cập nhật các bất biến liên quan trong AGENTS; sửa README để khớp luồng HP.
- **Kiểm tra:** Rà soát ID, phụ thuộc, trạng thái và NEXT trong TASKS; xác nhận các task DONE hiện có được giữ nguyên. Kiểm tra diff tài liệu và `git diff --check`. Không chạy Unity, compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Các task gameplay mới vẫn chưa triển khai; giá trị đề xuất và cân bằng cần chủ dự án duyệt, sau đó kiểm tra trong Unity.
- **Tiếp theo:** `FIX-PLAYER-BOUNDS-01` — hoàn tất giới hạn Collider2D cho toàn vùng camera và xác minh riêng mép màn hình/tầm bắn.

## 2026-10-18 18:48 +07:00 — Gỡ nội dung Core Hub chưa được phê duyệt

- **Trạng thái:** DONE
- **Phạm vi:** Sửa tài liệu để khôi phục đúng luồng Main Menu → Play → Gameplay; không có chọn Core trước run.
- **Thay đổi:** Gỡ mục Core Hub khỏi TASKS và PLAN, sửa entry đồng bộ trước đó để không mô tả tính năng này là đã duyệt, khôi phục NEXT về `FIX-PLAYER-BOUNDS-01` theo giá trị có trong TASKS trước thay đổi HUB.
- **Kiểm tra:** `git diff --check` trên TASKS.md, PLAN.md và CHANGELOG.md đạt; xác nhận không còn HUB-01 hoặc mô tả Core Selection Hub trong lộ trình hiện hành. Không chạy Unity.
- **Chưa kiểm tra / Rủi ro:** Không có thay đổi runtime; các trạng thái hoàn tất khác được giữ nguyên.
- **Tiếp theo:** Tiếp tục `FIX-PLAYER-BOUNDS-01` theo lộ trình đã có.

## 2026-10-18 18:40 +07:00 — Đồng bộ tài liệu hoàn tất gameplay và UI

- **Trạng thái:** DONE
- **Phạm vi:** Đồng bộ trạng thái UPG-05, UPG-03-VISUAL-01, UI-BUILD-01 và UPG-UI-01 theo xác nhận Play Mode của chủ dự án.
- **Thay đổi:** Ghi nhận Star Magnet cấp 3; sprite Drone riêng, fallback và đội hình đã chỉnh; BuildStatusPanel dạng text trong Canvas Gameplay; panel nâng cấp với sáu icon, tên và mô tả động. UI-BUILD-01 và UPG-UI-01 được đánh dấu DONE. Không thêm task icon cho Build HUD.
- **Kiểm tra:** Chủ dự án xác nhận đã kiểm tra thủ công các tính năng trên trong Unity Play Mode. Đây không phải automated test; không chạy Unity compile, build hoặc Play Mode trong lượt đồng bộ tài liệu này.
- **Chưa kiểm tra / Rủi ro:** Các task còn lại giữ nguyên trạng thái tại thời điểm đồng bộ.
- **Tiếp theo:** Tiếp tục theo task kế tiếp đã có trong roadmap.

## 2026-10-18 18:19 +07:00 — UPG-UI-01: Hiển thị icon, tên và mô tả nâng cấp

- **Trạng thái:** PARTIAL
- **Phạm vi:** Liên kết dữ liệu trình bày của các lựa chọn ngẫu nhiên với ba thẻ UI nâng cấp đang có.
- **Thay đổi:** Thêm trường Description và Sprite Icon vào `UpgradeDefinition`; `UpgradeSelectionPanel` hiển thị tên, mô tả, icon theo đúng definition được chọn, xóa nội dung cũ và ẩn icon thiếu. Giữ nguyên interface mock, lựa chọn ngẫu nhiên, giới hạn/candidate, callback Inspector và luồng pause. Thêm task UPG-UI-01 và ghi chú P7.
- **Kiểm tra:** Rà soát lựa chọn qua index/token và cách panel quản lý runtime listeners; `git diff --check` trên script/tài liệu task đạt. Kiểm tra scene Gameplay riêng báo trailing whitespace trên scene đang có thay đổi trước task; không sửa scene. Không chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Chưa gán ba mảng Description Text/Icon Image trong Inspector, chưa nhập mô tả và sáu icon vào các asset nâng cấp, chưa kiểm tra trong Unity; task giữ PARTIAL.
- **Tiếp theo:** Trong Gameplay, gán ba DescriptionText và ba UpgradeIcon cho `UpgradeSelectionPanel`, giữ nguyên ba name Text; trong sáu asset `MultiShot`, `RapidFire`, `DamageUp`, `DroneWingUpgrade`, `EnergyShieldUpgrade`, `StarMagnet`, nhập mô tả theo bảng task và gán icon PNG tương ứng. Lưu scene/assets rồi chạy checklist Play Mode.

## 2026-10-18 15:20 +07:00 — UI-BUILD-01: HUD trạng thái build trong Gameplay

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm panel chỉ hiển thị số Drone, trạng thái Shield, sát thương mỗi viên đạn, chu kỳ bắn hiệu lực và số đạn mỗi loạt của Player.
- **Thay đổi:** Thêm `BuildStatusHUD` đọc API hiện có; thêm event thông báo thay đổi chỉ số từ `WeaponController`, `DroneWingController` và `EnergyShieldController`. Cập nhật P13 và thêm task UI-BUILD-01. Ghi nhận UPG-03-VISUAL-01 DONE theo xác nhận kiểm tra Unity của chủ dự án.
- **Kiểm tra:** Rà soát source và wiring hiện có; `git diff --check` trong phạm vi các file liên quan đạt, kiểm tra whitespace của `BuildStatusHUD.cs` đạt. Kiểm tra toàn repo báo trailing whitespace sẵn có trong Gameplay.unity (dòng 1671); không sửa scene. Không chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Chưa tạo panel con dưới Canvas hiện có, gán năm Legacy UI Text, bố trí giao diện hoặc xác minh cập nhật trong Play Mode; UI-BUILD-01 giữ PARTIAL.
- **Tiếp theo:** Trong Canvas Gameplay hiện có, tạo panel gọn ở góc dưới trái với năm hàng icon tùy chọn, nhãn và Legacy UI Text; gắn `BuildStatusHUD`, gán Player GameObject và đủ năm Text references, lưu scene, rồi kiểm tra các thay đổi nâng cấp, cooldown Shield, pause và reset run.

## 2026-10-18 14:52 +07:00 — UPG-03-VISUAL-01: Sprite Drone chỉnh được

- **Trạng thái:** PARTIAL
- **Phạm vi:** Cho phép chọn sprite riêng cho Drone Wing, không đổi logic bay/bắn.
- **Thay đổi:** Thêm trường Inspector `Drone Sprite`; cả hai Drone dùng sprite được gán, nếu để trống thì dùng sprite Player như trước. Giữ nguyên scale, vị trí, tint và sorting; tắt flip để giữ hướng phải của sprite.
- **Kiểm tra:** Rà soát đường khởi tạo Drone và fallback; `git diff --check` cho file code/tài liệu đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Chưa xác minh hai Drone nhận sprite riêng hoặc fallback trong Unity.
- **Tiếp theo:** Gán sprite hướng phải vào `Player > Drone Wing Controller > Drone Sprite`, chạy thử hai Drone; để trống trường này để kiểm tra fallback.

## 2026-10-18 14:44 +07:00 — UPG-05: Chốt Star Magnet

- **Trạng thái:** DONE
- **Phạm vi:** Chốt UPG-05 theo xác nhận kiểm thử thủ công của chủ dự án trong Unity Play Mode.
- **Thay đổi:** Chủ dự án xác nhận `StarMagnetController` đã gắn lên Player, asset `StarMagnetUpgradeDefinition` đã tạo và đăng ký; bán kính ba cấp 3/4/5 world units hoạt động cùng Star pickup, XP, pooling và lựa chọn nâng cấp ngẫu nhiên.
- **Kiểm tra:** Chủ dự án xác nhận chạy Play Mode thành công. Đây là xác minh thủ công do người dùng báo cáo; không ghi nhận automated test hoặc build.
- **Chưa kiểm tra / Rủi ro:** Không có kiểm tra tự động mới trong lượt cập nhật tài liệu này.
- **Tiếp theo:** Tiếp tục theo task kế tiếp đã có trong roadmap.

## 2026-10-18 14:25 +07:00 — UPG-05: Triển khai Star Magnet

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm lực hút Star theo cấp vào luồng nâng cấp hiện có; không sửa scene hoặc prefab.
- **Thay đổi:** Thêm `StarMagnetController` với bán kính 3/4/5 world units, giới hạn cấp 3, tốc độ chỉnh được, reset khi bắt đầu run mới và dừng hút khi run kết thúc. `StarPickup` đăng ký các Star đang dùng trong pool, chuyển động có gia tốc về Player khi trong bán kính, giới hạn bước để tránh vượt mục tiêu và trở về drift khi ra ngoài; logic thu XP/thu hồi pool giữ nguyên. Thêm `StarMagnetUpgradeDefinition` với menu tạo asset, dùng eligibility/cap của framework hiện có.
- **Kiểm tra:** Rà soát API nâng cấp, vòng đời pool, collider trigger và run/pause state; chưa chạy Unity compile, automated test hoặc Play Mode. Unity Editor đang mở nên không chạy batch mode.
- **Chưa kiểm tra / Rủi ro:** Chưa gắn `StarMagnetController` lên Player, chưa tạo/đăng ký asset production và chưa kiểm tra runtime; UPG-05 tiếp tục PARTIAL.
- **Tiếp theo:** Trong Unity thêm `StarMagnetController` vào Player; tạo asset qua `Assets > Create > Space Explorer > Upgrades > Star Magnet`, đặt Id `star_magnet`, Display Name `Star Magnet`, thêm asset vào `UpgradeManager > Production Upgrade Registry` mà giữ nguyên entries hiện có; lưu Gameplay và chạy checklist Play Mode.

## 2026-10-17 20:33 +07:00 — Visual assets và chỉnh sửa Player

- **Trạng thái:** PARTIAL — `FIX-PLAYER-BOUNDS-01` vẫn chờ xác minh chi tiết.
- **Phạm vi:** Ghi nhận tích hợp hình ảnh và các chỉnh sửa Player đã được chủ dự án xác nhận.
- **Thay đổi:** Chủ dự án xác nhận background sci-fi xanh navy đậm; fighter xanh xám có ánh sáng cyan hướng sang phải; đạn năng lượng xanh cyan; Star vàng năm cánh; Asteroid Small và Large tối màu có điểm nhấn cyan đã được nhập, gán sprite và tinh chỉnh trong Unity. Sprite Player được thay mới; kích thước và PolygonCollider2D được chỉnh theo hình tàu, bật `Is Trigger`; tiếp xúc Asteroid không còn đẩy vật lý ngoài ý muốn và hành vi va chạm hoạt động. `PlayerController` dùng bounds Collider2D cùng độ lệch tâm để giới hạn di chuyển, giữ giới hạn nửa trái.
- **Kiểm tra:** Chủ dự án xác nhận gameplay sau cập nhật hoạt động thành công; đây là xác nhận thủ công, không phải kiểm tra tự động. Không tuyên bố đã chạy Unity compile hoặc Play Mode trong lượt này.
- **Chưa kiểm tra / Rủi ro:** Chưa có xác nhận riêng cho từng biên camera hoặc khả năng bắn trúng mục tiêu sát mép trên/dưới; `FIX-PLAYER-BOUNDS-01` tiếp tục PARTIAL cho đến khi các trường hợp đó được kiểm tra.
- **Tiếp theo:** Xác minh riêng giới hạn collider ở các biên và tầm bắn tại sát mép dọc trong Unity.

## 2026-10-17 19:37 +07:00 — FIX-PLAYER-BOUNDS-01: Giới hạn di chuyển theo collider

- **Trạng thái:** PARTIAL
- **Phạm vi:** Chuyển tính giới hạn Player từ bounds của renderer sang collider hiện có.
- **Thay đổi:** `PlayerController` cache Collider2D, dùng bounds thế giới của collider và bù độ lệch tâm collider so với Rigidbody2D khi tính biên camera; giữ nguyên giới hạn ngang ở nửa trái và các thông số/chuyển động vật lý. Cập nhật P3 và thêm task follow-up.
- **Kiểm tra:** Rà source, xác nhận Gameplay có PolygonCollider2D trên Player và muzzle chỉ lệch theo trục X; `git diff --check` cho code/tài liệu đạt. Không sửa scene/prefab. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần kiểm tra Play Mode tại bốn biên. Collider-based clamp giữ muzzle ở trong camera nhưng không đưa muzzle lên sát mép dọc: WeaponController bắn ngang tại Y của Player, nên mục tiêu sát mép trên/dưới có thể ngoài tầm đường đạn; cần quan sát với kích thước collider thực tế.
- **Tiếp theo:** Xác minh `FIX-PLAYER-BOUNDS-01` trong Unity: giữ Player ở biên trên/dưới/trái/nửa phải và kiểm tra collider không vượt khung; thử bắn mục tiêu gần hai mép dọc.

## 2026-10-17 16:10 +07:00 — SCENE-02: Thêm presenter kết quả EndGame

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm controller hiển thị kết quả run và điều hướng EndGame; chưa chỉnh scene.
- **Thay đổi:** Thêm `EndGamePresenter` dùng `GameManager.LastResult`/`RunResult` để hiển thị Score, Level, Survival Time dạng `HH:MM:SS` không quay vòng giờ; xử lý thiếu snapshot bằng nhãn dự phòng. Play Again và Main Menu dùng `SceneLoader`; Quit gọi `Application.Quit()` trong build. Listener được quản lý theo vòng đời component và khóa chuyển scene lặp.
- **Kiểm tra:** Rà soát code, API kết quả/scene loader và cấu trúc EndGame; `git diff --check` scoped cho TASKS/PLAN/CHANGELOG đạt, kiểm tra trailing whitespace của script/meta không có lỗi. Kiểm tra toàn working tree báo trailing whitespace trong `Gameplay.unity` đã có thay đổi trước task này; không sửa scene. Không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** EndGame scene chưa có Canvas/UI hoặc wiring. Cần gắn presenter, nối nhãn/nút và kiểm thử trong Editor; Quit cần kiểm tra standalone.
- **Tiếp theo:** Trong Unity tạo/gán ba Text legacy và ba Button, để trống Inspector `On Click()`, lưu scene; chạy checklist Play Mode và standalone ở báo cáo.

## 2026-10-17 16:03 +07:00 — SCORE-01: Chốt xác minh và chuẩn bị SCENE-02

- **Trạng thái:** DONE
- **Phạm vi:** Chốt SCORE-01 theo xác nhận kiểm thử thủ công của chủ dự án; chuyển NEXT sang SCENE-02.
- **Thay đổi:** Đánh dấu SCORE-01 hoàn tất trong TASKS và PLAN. Phần hiển thị kết quả EndGame tiếp tục thuộc SCENE-02, không gộp vào SCORE-01. Làm rõ SCENE-02: hiển thị Score cuối, Level đạt được, Survival Time và cung cấp Play Again, Main Menu, Quit.
- **Kiểm tra:** Chủ dự án xác nhận các kiểm tra Unity cho phần SCORE-01 đã hoàn tất thành công. Đây là xác nhận thủ công; không tuyên bố automated test hoặc build.
- **Chưa kiểm tra / Rủi ro:** SCENE-02 chưa triển khai; chưa chạy kiểm tra Unity trong lượt tài liệu này.
- **Tiếp theo:** `SCENE-02` — dựng EndGame presentation/navigation; phụ thuộc `GAME-01` (đã DONE), theo `P2,P8`.

## 2026-10-17 16:01 +07:00 — SCORE-01: Rà soát điểm, thời gian và snapshot kết quả

- **Trạng thái:** PARTIAL
- **Phạm vi:** Rà soát và ghi nhận luồng Score/run-result hiện có; không sửa gameplay code hoặc scene.
- **Thay đổi:** Xác nhận trong source rằng Asteroid Small/Large cộng lần lượt 10/30 khi bị phá hủy; bắt đầu run reset Score và xóa `LastResult`; GAME-01 chụp Score/Level/thời gian vào `RunResult` bất biến và GameManager giữ snapshot qua scene load. Cập nhật TASKS và P8. Scene EndGame hiện không có object hoặc presenter hiển thị kết quả.
- **Kiểm tra:** Đọc các script liên quan và kiểm tra cấu trúc `EndGame.unity`; không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Chưa xác minh runtime score/reset/time/result; chưa có EndGame result display. Giữ PARTIAL cho tới khi kiểm tra Unity và hoàn tất presenter trong phạm vi scene EndGame.
- **Tiếp theo:** Kiểm tra Small +10/Large +30 đúng một lần, reset khi bắt đầu run mới, thời gian run mới, dữ liệu snapshot và giá trị hiển thị ở EndGame; phối hợp phần hiển thị với `SCENE-02`.

## 2026-10-17 15:52 +07:00 — GAME-01, UPG-04: Chốt xác minh Unity

- **Trạng thái:** DONE
- **Phạm vi:** Chốt GAME-01 và UPG-04 theo xác nhận kiểm thử thủ công của chủ dự án.
- **Thay đổi:** Cập nhật TASKS và PLAN: nhánh va chạm Player–Asteroid dùng Energy Shield sẵn sàng để hấp thụ một hit; khi Shield không hấp thụ, luồng kết thúc run ghi Score/Level/thời gian vào `RunResult`, kết thúc qua `GameManager` và chuyển sang `EndGame` qua `SceneLoader`. Ghi nhận Shield cooldown và tích hợp đã được xác minh; không thay đổi code/gameplay.
- **Kiểm tra:** Chủ dự án xác nhận Unity setup và Play Mode cho GAME-01, UPG-04 hoàn tất thành công. Không tuyên bố automated test hoặc build đã chạy.
- **Chưa kiểm tra / Rủi ro:** Không có kiểm tra tự động mới trong lượt cập nhật tài liệu này.
- **Tiếp theo:** `SCORE-01` — hoàn thiện Score, survival time, reset run và snapshot kết quả; phụ thuộc `COMBAT-02`.

## 2026-10-17 15:40 +07:00 — GAME-01: Sửa định danh va chạm theo EntityId

- **Trạng thái:** PARTIAL
- **Phạm vi:** Sửa tương thích Unity cho guard chống xử lý trùng collider trong `PlayerAsteroidCollisionHandler`.
- **Thay đổi:** Đổi trường định danh sang `EntityId`, lấy giá trị bằng `Collider2D.GetEntityId()`, so sánh bằng `EntityId.Equals()` và reset thành `EntityId.None`. Giữ nguyên kiểm tra cùng physics step và nhánh Shield/Game Over; GAME-01 và UPG-04 vẫn PARTIAL.
- **Kiểm tra:** Xác nhận API `EntityId.None`, `GetEntityId()` và `Equals(EntityId)` trong metadata API Unity 6000.5.9f1; rà soát diff và `git diff --check`. Không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Cần Unity compile/runtime verification sau khi Editor import thay đổi.
- **Tiếp theo:** Xác minh compile và hoàn tất wiring/Play Mode cho GAME-01 và UPG-04.

## 2026-10-17 15:26 +07:00 — GAME-01: Nối va chạm Player và Game Over

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm nhánh va chạm Player–Asteroid dùng Shield hiện có hoặc kết thúc run; không sửa scene/prefab.
- **Thay đổi:** Thêm `PlayerAsteroidCollisionHandler` cho trigger Player. Va chạm Asteroid chỉ được xử lý một lần trong cùng physics step; Shield sẵn sàng gọi API `TryAbsorbAsteroidCollision()`, còn hit không được hấp thụ sẽ khóa movement/fire, chụp Score/Level/thời gian run, gọi `GameManager.TryFinishRun()` rồi chuyển `EndGame` qua `SceneLoader`. Cập nhật trạng thái GAME-01/UPG-04 và mô tả P9.
- **Kiểm tra:** Rà soát API Shield, run lifecycle, result capture và luồng scene loader; xác nhận Gameplay/EndGame có trong Build Settings. `git diff --check` cho tài liệu đạt, kiểm tra trailing whitespace source đạt. Không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Chưa gắn handler/`EnergyShieldController` vào Player trong Gameplay, chưa tạo/đăng ký asset Energy Shield, và chưa xác minh hành vi runtime.
- **Tiếp theo:** Trong Unity gắn hai component lên Player, tạo/đăng ký Energy Shield asset, lưu Gameplay, rồi kiểm tra hấp thụ, cooldown, Game Over, dữ liệu `RunResult` và chuyển EndGame.

## 2026-10-17 15:21 +07:00 — UPG-04: Triển khai lõi Energy Shield

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm trạng thái Shield theo cấp, cooldown và định nghĩa nâng cấp; không thay đổi scene, prefab hoặc hệ thống nâng cấp khác.
- **Thay đổi:** Thêm `EnergyShieldController` với cấp tối đa 3, cooldown Inspector mặc định 12/9/6 giây, API hấp thụ một va chạm khi sẵn sàng, và reset cấp/cooldown khi bắt đầu run mới. Thêm `EnergyShieldUpgradeDefinition` cùng menu tạo asset. Nâng cấp trong cooldown chỉ đổi cấp, không sửa bộ đếm hiện tại.
- **Kiểm tra:** Rà soát source, references và cấu hình collider: Player dùng trigger, Asteroid dùng collider thường. Chưa chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Chưa có Player-Asteroid collision/death handler trong code; API Shield chưa được gọi khi va chạm và nhánh Shield hết sẵn sàng chưa nối với Game Over. UPG-04 giữ PARTIAL cho tới khi GAME-01 cung cấp luồng này, asset được đăng ký và kiểm thử Unity hoàn tất.
- **Tiếp theo:** Gắn `EnergyShieldController` lên Player, tạo asset `EnergyShieldUpgradeDefinition` (`Id: energy_shield`, `Display Name: Energy Shield`) và thêm vào `UpgradeManager` registry; sau đó tích hợp collision API với GAME-01 và chạy checklist Play Mode.

## 2026-10-17 15:14 +07:00 — UPG-02, UPG-RANDOM-01, UPG-03: Chốt xác minh nâng cấp

- **Trạng thái:** DONE
- **Phạm vi:** Chốt trạng thái tài liệu cho ba task nâng cấp đã được người dùng xác nhận kiểm thử thủ công thành công trong Unity Play Mode.
- **Thay đổi:** Đánh dấu UPG-02, UPG-RANDOM-01 và UPG-03 là DONE trong `TASKS.md`; ghi nhận trạng thái xác minh trong `PLAN.md`; chuyển `NEXT` sang UPG-04 — Energy Shield. Giữ nguyên mô tả và ranh giới UPG-04/UPG-05.
- **Kiểm tra:** Người dùng xác nhận kiểm thử Unity Play Mode thành công cho chọn ngẫu nhiên, Drone Wing và ba nâng cấp Multi Shot/Rapid Fire/Damage Up. Không chạy automated test hoặc build mới.
- **Chưa kiểm tra / Rủi ro:** Không ghi nhận xác minh chi tiết nào ngoài kết quả thành công được người dùng báo cáo.
- **Tiếp theo:** `UPG-04` — Energy Shield.

## 2026-10-17 15:02 +07:00 — UPG-RANDOM-01: Chọn ngẫu nhiên nâng cấp

- **Trạng thái:** PARTIAL
- **Phạm vi:** Cập nhật `UpgradeManager` để chọn ngẫu nhiên các nâng cấp đủ điều kiện; tách yêu cầu này khỏi phạm vi UPG-01 ban đầu.
- **Thay đổi:** Lọc toàn bộ registry trước khi chọn, dùng Fisher–Yates cho mẫu đều không hoàn lại, và khi trùng bộ ID với lượt hiển thị trước thì chọn đều từ các tổ hợp còn lại. Lượt có tối đa ba lựa chọn vẫn hiển thị tất cả. Queue tiếp tục tính eligibility mới cho từng cấp; trạng thái lựa chọn production và Editor mock được theo dõi riêng, xóa khi bắt đầu/kết thúc run.
- **Kiểm tra:** Rà soát source và luồng queue; `git diff --check -- PLAN.md TASKS.md CHANGELOG.md` đạt. `git diff --check` toàn repo báo trailing whitespace trong `Gameplay.unity` đã có thay đổi trước task; không sửa scene. Không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Cần Unity xác minh phân bố mẫu, tránh lặp bộ lựa chọn, lượt queue liên tiếp và reset theo vòng đời run. Registry hiện có thể chỉ có ba upgrade eligible nên cần ít nhất bốn lựa chọn hợp lệ để quan sát các bộ khác nhau.
- **Tiếp theo:** Xác minh UPG-RANDOM-01 trong Unity Play Mode; sau đó tiếp tục wiring và kiểm tra UPG-03.

## 2026-10-17 14:46 +07:00 — UPG-03: Triển khai Drone Wing

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm Drone Wing giới hạn hai drone, không triển khai Energy Shield hoặc Star Magnet.
- **Thay đổi:** Thêm `DroneWingController`, `DroneController` và `DroneUpgradeDefinition`. Drone dùng sprite Player làm hình tạm, bám theo bằng vị trí con có offset chỉnh được, bắn một viên sang phải qua BulletPool dùng chung theo base fire interval, và đọc sát thương hiện tại để chỉ kế thừa Damage Up. Multi Shot/Rapid Fire không được dùng. Số drone và trạng thái hoạt động reset khi bắt đầu run mới; tối đa hai đối tượng con được giữ lại để tái sử dụng trong run sau. `WeaponController` cung cấp tham chiếu BulletPool dùng chung.
- **Kiểm tra:** Rà soát source và lifecycle; scoped `git diff --check` cho code/tài liệu đạt. `git diff --check` toàn repo báo trailing whitespace trong `Gameplay.unity` đã có thay đổi trước task; không sửa scene. Không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Chưa gắn `DroneWingController` vào Player hoặc đăng ký asset production trong scene; chưa xác minh compile hay gameplay thực tế.
- **Tiếp theo:** Trong Unity thêm `DroneWingController` lên Player; tạo asset qua `Assets > Create > Space Explorer > Upgrades > Drone Wing`, đặt Id `drone_wing`, Display Name `Drone Wing`, rồi thêm vào `UpgradeManager > Production Upgrade Registry` mà vẫn giữ các asset UPG-02 hiện có. Lưu scene và chạy checklist Play Mode trong báo cáo.

## 2026-10-17 14:42 +07:00 — UPG-02: Xác nhận hoàn tất nâng cấp vũ khí

- **Trạng thái:** DONE
- **Phạm vi:** Chốt UPG-02 theo xác nhận kiểm tra thủ công của người dùng; không bắt đầu UPG-03.
- **Thay đổi:** Đánh dấu UPG-02 hoàn tất và chuyển `NEXT` sang UPG-03.
- **Kiểm tra:** Người dùng xác nhận đã kiểm tra thủ công trong Unity và cả ba nâng cấp vũ khí hoạt động đúng. Codex không chạy Unity hoặc kiểm tra tự động mới.
- **Chưa kiểm tra / Rủi ro:** Không có thêm edge case nào được báo cáo hoặc ghi nhận trong lần xác nhận này.
- **Tiếp theo:** `UPG-03`.

## 2026-10-17 12:58 +07:00 — UPG-02: Thêm ba nâng cấp vũ khí

- **Trạng thái:** PARTIAL
- **Phạm vi:** Triển khai effect và điều kiện hợp lệ cho Multi Shot, Rapid Fire và Damage Up; không triển khai UPG-03 đến UPG-05.
- **Thay đổi:** `WeaponController` theo dõi cấp nâng cấp theo run, bắn volley đối xứng tối đa 5 viên, giảm khoảng bắn 0,04 giây đến tối thiểu 0,14 giây mà giữ nguyên interval Inspector, và tăng sát thương mỗi viên từ 1 đến tối đa 5. `BulletPool` chuyển hướng và sát thương cho từng viên; `Bullet` reset sát thương khi tái sử dụng. Thêm `WeaponUpgradeDefinition` với ba loại effect, eligibility và menu tạo asset.
- **Kiểm tra:** Rà soát mã và luồng pool bằng source inspection. `git diff --check` toàn bộ diff phát hiện trailing whitespace ở scene Gameplay đã có thay đổi trước task; không chỉnh scene này. Không chạy Unity compile, Play Mode hoặc automated test.
- **Chưa kiểm tra / Rủi ro:** Chưa tạo ba `WeaponUpgradeDefinition` asset hoặc đăng ký chúng trong `UpgradeManager` của Gameplay; chưa xác minh thực tế effect, eligibility/cap, reset run và bắn đạn trong Unity.
- **Tiếp theo:** Trong Unity tạo ba asset tại `Assets > Create > Space Explorer > Upgrades > Weapon Upgrade`; đặt lần lượt Id/Display Name/Type là `multi_shot`/`Multi Shot`/`MultiShot`, `rapid_fire`/`Rapid Fire`/`RapidFire`, `damage_up`/`Damage Up`/`DamageUp`. Kéo cả ba vào `UpgradeManager > Production Upgrade Registry > Registered Upgrades`, lưu scene rồi kiểm tra Play Mode.

## 2026-10-17 12:53 +07:00 — UPG-01: Xác nhận hoàn tất framework chọn nâng cấp

- **Trạng thái:** DONE
- **Phạm vi:** Chốt UPG-01 theo kết quả kiểm thử Play Mode do người dùng xác nhận; không triển khai effect thuộc UPG-02–UPG-05.
- **Thay đổi:** Đánh dấu UPG-01 hoàn tất và chuyển `NEXT` sang `UPG-02`. Rà scene Gameplay xác nhận `UpgradeManager` nối `StarXPCollector`/panel và panel có ba cặp Button/Text; registry production hiện không đăng ký nâng cấp giả.
- **Kiểm tra:** Người dùng xác nhận trong Unity Play Mode: ba mock upgrade hợp lệ được chọn tuần tự, panel đóng và gameplay tiếp tục; mock trùng và chưa triển khai bị lọc; yêu cầu không có lựa chọn được xử lý an toàn; pause token riêng giữ game tạm dừng đến khi được nhả; kết thúc run khi panel mở đóng panel và không resume; bắt đầu run mới hoạt động. Codex không chạy Unity compile, Play Mode hay automated test.
- **Chưa kiểm tra / Rủi ro:** Không kiểm thử hoặc triển khai hiệu ứng nâng cấp cụ thể; phần đó thuộc UPG-02–UPG-05.
- **Tiếp theo:** `UPG-02`.

## 2026-10-16 19:24 +07:00 — UPG-01: Bind GameManager singleton an toàn

- **Trạng thái:** PARTIAL
- **Phạm vi:** Xác minh và sửa cách `UpgradeManager` tham chiếu GameManager persistent; không thay đổi run-state contract hoặc wiring scene.
- **Thay đổi:** Bỏ tham chiếu GameManager serialized có thể trỏ tới scene duplicate đã bị hủy. `UpgradeManager` bind/unbind instance authoritative theo runtime, thử lại nếu singleton chưa khởi tạo, đồng bộ trạng thái Finished khi bind và bảo đảm event subscription không trùng. Theo dõi `RunStarted` trong lifetime component để reset queue khi component tạm disable.
- **Kiểm tra:** Rà tĩnh vòng đời `Awake`/`OnEnable`/`Update`/`OnDisable`/`OnDestroy`, xác nhận `GameManager.Instance` là nguồn binding duy nhất và các event được gỡ đúng lúc. Chưa chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần Unity xác minh load Gameplay lần đầu, reload Gameplay với GameManager persistent, và disable/re-enable UpgradeManager trong lúc run cùng qua một lần bắt đầu run mới. UPG-01 vẫn PARTIAL.
- **Tiếp theo:** Wire Gameplay UI rồi xác minh các trường hợp binding và flow UPG-01 trong Unity Editor/Play Mode.

## 2026-10-16 19:10 +07:00 — UPG-01: Cứng hóa vòng đời và hàng đợi

- **Trạng thái:** PARTIAL
- **Phạm vi:** Gia cố lifecycle, xử lý lỗi cấu hình UI và cô lập yêu cầu test của UPG-01 trước khi nối scene.
- **Thay đổi:** Giữ yêu cầu level-up ở đầu queue cho tới khi được chọn; giữ queue khi component bị disable, vô hiệu callback cũ và nhả đúng pause token. Panel lỗi cấu hình được retry có backoff, không giữ pause trong lúc chờ; yêu cầu không có lựa chọn hợp lệ vẫn được tiêu thụ an toàn. Harness Editor từ chối khi còn yêu cầu gameplay/queue đang xử lý và gắn nhãn riêng yêu cầu mock. Panel chỉ gỡ listener runtime của chính nó; tài liệu yêu cầu Button không có `On Click()` Inspector handler xung đột.
- **Kiểm tra:** `git diff --check` đạt; kiểm tra trailing whitespace và rà tĩnh callback/listener, queue, pause token, retry backoff và Editor-only mock request. Không tìm thấy project `.sln`/`.csproj` để compile độc lập; không chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Scene Gameplay chưa được nối; hành vi runtime và sửa cấu hình panel cần xác minh trong Unity Editor. UPG-01 vẫn PARTIAL.
- **Tiếp theo:** Nối UI trong Gameplay và xác minh UPG-01 bằng Unity Editor/Play Mode.

## 2026-10-16 18:38 +07:00 — UPG-01: Thêm khung chọn nâng cấp

- **Trạng thái:** PARTIAL
- **Phạm vi:** Triển khai framework chọn nâng cấp theo level-up; không thêm hiệu ứng cụ thể UPG-02–UPG-05.
- **Thay đổi:** Thêm contract `IUpgradeOption`/`UpgradeDefinition`, `UpgradeManager` xử lý queue tuần tự và lọc lựa chọn, panel UI dùng Unity UI hiện có, pause request token do `GameManager` quản lý, và Editor-only harness với mock một lần. Chặn `WeaponController` bắn khi run không Playing. Không sửa scene/prefab và không đăng ký lựa chọn giả vào production.
- **Kiểm tra:** `git diff --check` đạt; rà tĩnh xác nhận `GameManager` là nơi duy nhất ghi `Time.timeScale`, lựa chọn rỗng không mở panel/giữ pause và Editor harness chỉ hoạt động trong Editor. Không chạy Unity compile, automated test hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Gameplay scene chưa được nối. Sau khi Unity import script và sinh `.meta`, thêm panel dưới Canvas với nền, tối đa 3 Button/Legacy Text; gán Button/Text arrays; thêm `UpgradeManager` và nối Player `StarXPCollector`, `GameManager`, `UpgradeSelectionPanel`. Để registry production trống cho tới khi có effect thật. Chạy menu `Tools/Space Explorer/UPG-01` để kiểm tra mock, queue, lọc, pause token và Game Over; sau đó kiểm tra wiring/no-choice/new-run trong Play Mode.
- **Tiếp theo:** Hoàn tất scene wiring và xác minh UPG-01; chưa bắt đầu UPG-02.

## 2026-10-16 17:54 +07:00 — UPG-01: Chốt phụ thuộc và chiến lược kiểm thử

- **Trạng thái:** DONE
- **Phạm vi:** Cập nhật tài liệu và task tracking trước khi triển khai UPG-01; không triển khai gameplay.
- **Thay đổi:** Bổ sung phụ thuộc `PROG-01`, quy tắc test mock chỉ trong harness/test assembly, cách xử lý an toàn khi không có lựa chọn hợp lệ; xác nhận `GM-INTEGRATION-01` DONE theo xác minh Unity thủ công do chủ dự án báo cáo và chuyển `NEXT` sang `UPG-01`. UPG-01 vẫn chưa hoàn tất.
- **Kiểm tra:** Rà tĩnh GameManager, AsteroidSpawner và wiring trong Gameplay; người dùng xác nhận runtime đã kiểm tra thủ công trong Unity. Codex không chạy Unity, Play Mode hay automated test.
- **Chưa kiểm tra / Rủi ro:** Chưa triển khai hoặc kiểm thử UPG-01.
- **Tiếp theo:** `UPG-01`.

## 2026-10-16 17:31 +07:00 — PROG-01: Xác nhận hoàn tất tiến trình XP

- **Trạng thái:** DONE
- **Phạm vi:** Chốt tài liệu cho ngưỡng XP, lên cấp, XP dư và reset tiến trình khi bắt đầu run mới.
- **Thay đổi:** Đánh dấu `PROG-01` hoàn tất trong `TASKS.md` theo xác nhận của người dùng.
- **Kiểm tra:** Người dùng xác nhận đã kiểm tra thủ công trong Unity: trạng thái ban đầu, các ngưỡng XP, lên cấp, giữ XP dư, nhiều lần lên cấp từ một lần cộng XP, sự kiện lên cấp và reset khi bắt đầu run mới đều đạt.
- **Chưa kiểm tra / Rủi ro:** Codex không chạy Unity, Play Mode hoặc kiểm tra tự động trong lượt này.
- **Tiếp theo:** `UPG-01`.

## 2026-10-13 20:07 +07:00 — UI-01: Đối chiếu HUD Level/XP đã tích hợp

- **Trạng thái:** PARTIAL
- **Phạm vi:** Đối chiếu xác nhận thủ công của chủ dự án với scene Gameplay đã lưu và tiêu chí UI-01.
- **Thay đổi:** Xác nhận scene có Canvas, `ProgressionHUD`, LevelText, XPText, XP bar và các serialized references trỏ tới `StarXPCollector` cùng đúng UI components. Không sửa script hoặc scene. Chủ dự án báo HUD đã tích hợp và game đã được kiểm tra thành công; đây là xác nhận của chủ dự án, không phải Play Mode do Codex chạy.
- **Kiểm tra:** Rà tĩnh wiring/fileID và loại component trong scene. `git diff --check` không đạt do trailing whitespace ở các dòng `m_Name: ` trong diff Gameplay đã tồn tại trước lượt này; không sửa scene. Không chạy automated test, Unity compile hoặc Play Mode. Không có kiểm tra hay triển khai HP bar.
- **Chưa kiểm tra / Rủi ro:** Scene lưu các phần tử Level/XP quanh tâm màn hình (tọa độ neo gần (24,-24), (24,-62), (24,-100)); text xám đậm. Chưa xác nhận độ dễ đọc và tiêu chí không che các vùng gameplay bằng bố cục hiện tại.
- **Tiếp theo:** Chỉnh vị trí/độ tương phản trong Unity Editor rồi xác minh lại bố cục; sau đó mới chốt UI-01.

## 2026-10-13 20:24 +07:00 — UI-01: Tích hợp Level/XP HUD vào Gameplay

- **Trạng thái:** PARTIAL
- **Phạm vi:** Kiểm tra khả năng tích hợp `ProgressionHUD` vào scene Gameplay và giữ task chưa hoàn tất khi thiếu wiring scene.
- **Thay đổi:** Chưa sửa scene hoặc script; `ProgressionHUD` hiện có nhưng Gameplay chưa có Canvas, Level/XP text, progress bar hay serialized reference tới component.
- **Kiểm tra:** Đọc scene và tìm các component UI; xác nhận Unity Editor đang chạy với project này. Không chạy compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Không thể điều khiển cửa sổ Unity Editor trong phiên này; chạy batch-mode song song trên cùng project đang mở không an toàn. Theo quy tắc Unity của repository, không tự dựng scene YAML/fileID/reference. Cần tạo Canvas, Level Text, XP Text, fill Image; gắn `ProgressionHUD`, nối Player `StarXPCollector` và ba UI references trong Inspector, rồi lưu Gameplay.
- **Tiếp theo:** Hoàn tất scene wiring và kiểm tra cập nhật/reset trong Unity Play Mode.

## 2026-10-13 20:16 +07:00 — UI-01: HUD Level và XP

- **Trạng thái:** PARTIAL
- **Phạm vi:** Bổ sung HUD gameplay hiển thị Level và tiến độ XP; chưa thêm Score, Sector hoặc HUD khác.
- **Thay đổi:** Thêm `ProgressionHUD` đọc trực tiếp `StarXPCollector`, hiển thị Level, XP hiện tại/yêu cầu và fill bar. Collector phát `ProgressChanged` sau khi nhận XP hoặc reset run để HUD cập nhật; không tính toán trùng progression. Dùng lại `UI-01` trong roadmap.
- **Kiểm tra:** `git diff --check` và rà tĩnh tham chiếu API; không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Gameplay chưa có Canvas/UI object; cần tạo Canvas và các Text/Image trong scene, gắn `ProgressionHUD`, gán `StarXPCollector` cùng ba UI reference, rồi kiểm tra bố cục, cập nhật XP/Level và reset trong Play Mode.
- **Tiếp theo:** Tích hợp HUD vào Gameplay và xác minh thủ công.

## 2026-10-13 20:04 +07:00 — PROG-01: Tiến trình XP và cấp độ

- **Trạng thái:** PARTIAL
- **Phạm vi:** Bổ sung tiến trình cấp độ từ Star XP, giữ XP tách biệt khỏi Score; chưa thêm chọn nâng cấp hoặc độ khó.
- **Thay đổi:** `StarXPCollector` theo dõi Level, tổng XP, XP trong cấp hiện tại và XP cần cho cấp kế tiếp. Mảng ngưỡng Inspector mặc định là 8/12/16/20/24 XP; giá trị cuối được lặp lại sau phần tử cuối (mặc định là 24 XP từ Level 4). XP dư được giữ và mỗi cấp tăng phát một sự kiện. `CurrentXP` tiếp tục là alias tổng XP. Star prefab vẫn cấp 1 XP. `GameManager.TryBeginRun` phát `RunStarted`, dùng để reset tiến trình đúng lúc bắt đầu run.
- **Kiểm tra:** `git diff --check` đạt; rà tĩnh các tham chiếu XP hiện có và luồng reset. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần xác minh trong Unity các ngưỡng, XP dư/nhiều cấp tăng từ một lần cộng XP, sự kiện phát đúng một lần mỗi cấp và reset khi bắt đầu run mới.
- **Tiếp theo:** Xác minh thủ công `PROG-01` trong Unity; sau đó tiếp tục `UPG-01` theo roadmap.

## 2026-10-13 19:51 +07:00 — SCORE-DEBUG-01: Gỡ overlay chẩn đoán tạm

- **Trạng thái:** DONE
- **Phạm vi:** Gỡ helper Editor Play Mode chỉ dùng để quan sát Score và XP.
- **Thay đổi:** Xóa script overlay cùng metadata Unity của script và thư mục riêng. Không sửa logic Score hoặc XP, scene hay prefab.
- **Kiểm tra:** `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Các kiểm tra gameplay của `SCORE-01` vẫn cần xác minh riêng; overlay không còn trong project.
- **Tiếp theo:** Tiếp tục xác minh thủ công `SCORE-01` bằng cách quan sát phù hợp trong Unity.

## 2026-10-13 19:40 +07:00 — SCORE-DEBUG-01: Overlay kiểm tra Score và XP

- **Trạng thái:** PARTIAL
- **Phạm vi:** Hiển thị Score và XP hiện tại để hỗ trợ xác minh SCORE-01 trong Game view của Unity Editor.
- **Thay đổi:** Thêm overlay tạm chỉ biên dịch trong Editor, tự tạo khi Play Mode bắt đầu và chỉ hiện ở scene Gameplay. Overlay đọc `GameManager.CurrentScore` và `StarXPCollector.CurrentXP`; không thay đổi dữ liệu điểm/XP hoặc scene, prefab.
- **Kiểm tra:** `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần xác minh overlay hiển thị trong Game view khi vào Gameplay, cập nhật đúng điểm/XP và không xuất hiện trong bản build.
- **Tiếp theo:** Dùng overlay để kiểm tra SCORE-01 thủ công; sau đó xóa helper tạm.

## 2026-10-13 19:31 +07:00 — SCORE-01: Theo dõi điểm

- **Trạng thái:** PARTIAL
- **Phạm vi:** Cộng điểm khi phá Asteroid, tách biệt với XP và reset điểm khi bắt đầu run.
- **Thay đổi:** Small cộng 10 điểm, Large cộng 30 điểm theo P5. `GameManager.CurrentScore` cung cấp điểm hiện tại; `TryAddScore` chỉ nhận điểm dương khi run đang Playing và điểm được reset trong `TryBeginRun`. Star XP không thay đổi. Chưa tạo snapshot `RunResult`; API hiện tại yêu cầu thêm Level và Survival Time, ngoài phạm vi task này.
- **Kiểm tra:** `git diff --check` đạt. Không chạy Unity compile vì Unity Editor đang mở; không chạy Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần kiểm tra trong Play Mode điểm tăng đúng một lần khi Small/Large bị phá hủy, không tăng khi Asteroid rời màn hình, XP không làm đổi điểm, và điểm được reset ở run mới. Chưa xác minh biên dịch trong Unity.
- **Tiếp theo:** Xác minh Unity cho `GM-INTEGRATION-01`; sau đó hoàn tất kiểm tra `SCORE-01`.

## 2026-10-13 19:13 +07:00 — GM-INTEGRATION-01: Nối GameManager với Gameplay

- **Trạng thái:** PARTIAL
- **Phạm vi:** Nối run state hiện có với vòng đời Gameplay và AsteroidSpawner; không thêm game-over hoặc tính năng mới.
- **Thay đổi:** Thêm đúng một GameManager vào scene Gameplay. GameManager bắt đầu run khi Gameplay tải, reset trạng thái Finished trước lượt mới; AsteroidSpawner yêu cầu GameManager và chỉ spawn khi trạng thái Playing. Không tìm thấy xử lý player-death/run-finish hiện có để nối.
- **Kiểm tra:** Kiểm tra tĩnh xác nhận một GameManager trong scene, component/script/SceneRoots liên kết nhất quán và không có fileID YAML trùng; `git diff --check` đạt. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần xác minh scene mở được, chỉ có một GameManager tồn tại, trạng thái chuyển Ready/Finished sang Playing khi vào Gameplay và spawner dừng khi không Playing.
- **Tiếp theo:** Hoàn tất xác minh Unity cho `GM-INTEGRATION-01`; `SCORE-01` vẫn là task roadmap kế tiếp.

## 2026-10-13 18:52 +07:00 — FIX-BULLET-COLLIDER-01: Lưu collider Bullet

- **Trạng thái:** DONE
- **Phạm vi:** Sửa cấu hình collider của Bullet và kiểm tra khởi tạo.
- **Thay đổi:** Thêm một `CircleCollider2D` bán kính `0.5`, bật trigger trong prefab; `Bullet.Awake()` dùng collider đã lưu và báo lỗi, vô hiệu hóa component nếu thiếu collider hoặc collider không phải trigger. Không đổi xử lý bắn, sát thương hay pool.
- **Kiểm tra:** Đã rà cấu trúc YAML, diff và kiểm tra `git diff --check`. Không chạy Unity compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần xác nhận prefab hiển thị đúng trong Inspector và thử hit/miss trong Play Mode.
- **Tiếp theo:** `SCORE-01`; chưa triển khai.

## 2026-10-13 11:38 +07:00 — STAR-01: Chốt xác minh Star pickup

- **Trạng thái:** DONE
- **Phạm vi:** Chốt STAR-01 theo kết quả Play Mode do người dùng xác nhận.
- **Thay đổi:** Đánh dấu STAR-01 hoàn tất; task tiếp theo theo roadmap là `SCORE-01`.
- **Kiểm tra:** Người dùng xác nhận Small thả 1 Star, Large thả 2–3 Stars; nhặt Star tăng XP mà không đổi Score; Star được thu hồi khi nhặt hoặc rời màn hình; pool tái sử dụng đúng; không có lỗi Console mới. Không chạy kiểm tra tự động mới.
- **Chưa kiểm tra / Rủi ro:** Không có hạng mục Play Mode STAR-01 nào khác được báo cáo là chưa xác minh.
- **Tiếp theo:** `SCORE-01`; chưa triển khai.

## 2026-10-13 11:29 +07:00 — STAR-01: Star pickup và XP

- **Trạng thái:** PARTIAL
- **Phạm vi:** Triển khai Star drop từ Asteroid, pool, drift, pickup nhận XP và thu hồi ngoài màn hình; chưa làm Score hoặc nâng cấp.
- **Thay đổi:** `Asteroid` báo pool một lần khi HP về 0; Small thả 1 Star, Large thả ngẫu nhiên 2–3. `StarPool` dùng `PrefabPool<StarPickup>` và prewarm 64; `StarPickup` reset vận tốc/trạng thái khi tái sử dụng, trôi sang trái, cộng XP vào `StarXPCollector` của Player rồi về pool, hoặc tự thu hồi khi qua mép trái. Tạo prefab Star tạm bằng sprite placeholder có sẵn, trigger collider và Rigidbody2D. Scene `Gameplay` đã gắn StarPool với prefab/camera, nối AsteroidPool tới StarPool, đồng thời thêm trigger collider và `StarXPCollector` lên Player.
- **Kiểm tra:** Unity Editor đã biên dịch mã sau khi sửa hai tham chiếu `Random` mơ hồ; editor setup đã lưu prefab và scene. Rà YAML xác nhận tham chiếu prefab/pool/camera, collider trigger và pool prewarm 64. `git diff --check` hoàn tất không lỗi whitespace. Không chạy Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần Play Mode xác nhận Small thả 1 Star, Large thả 2–3, chạm Player tăng XP mà không ảnh hưởng Score, Star được thu hồi sau thu thập/ra khỏi mép trái và pool tái sử dụng không còn vận tốc/trạng thái cũ. Cần kiểm tra Console trong lượt chạy.
- **Tiếp theo:** Hoàn tất xác minh thủ công `STAR-01`; chưa bắt đầu task sau.

## 2026-10-13 11:18 +07:00 — COMBAT-02: Chốt xác minh Play Mode

- **Trạng thái:** DONE
- **Phạm vi:** Hoàn tất ghi nhận COMBAT-02 dựa trên kết quả Play Mode do người dùng xác nhận.
- **Thay đổi:** Xác nhận trạng thái DONE của task trong tài liệu; không thay đổi code.
- **Kiểm tra:** Người dùng xác nhận Large không bật ngược khi trúng đạn; mỗi viên đạn gây sát thương một lần; Large bị tiêu diệt sau 3 hit; đạn được pool tái sử dụng; bắn trượt vẫn hoạt động bình thường; Console không có lỗi mới. Không chạy automated test mới.
- **Chưa kiểm tra / Rủi ro:** Không ghi nhận hạng mục COMBAT-02 nào còn chờ xác minh.
- **Tiếp theo:** `STAR-01`; chưa triển khai.

## 2026-10-13 11:11 +07:00 — COMBAT-02: Xác nhận kiểm tra Play Mode

- **Trạng thái:** DONE
- **Phạm vi:** Chốt trạng thái tài liệu `COMBAT-02` theo xác nhận kiểm tra thủ công của người dùng.
- **Thay đổi:** Đánh dấu task hoàn tất và chuyển task kế tiếp thành `STAR-01` theo roadmap.
- **Kiểm tra:** Người dùng xác nhận trong Unity Play Mode: Asteroid Large không bật ngược khi trúng đạn, sát thương hoạt động đúng, Large bị tiêu diệt sau 3 hit, đạn được pool tái sử dụng và Console không có lỗi mới. Không chạy automated test mới.
- **Chưa kiểm tra / Rủi ro:** Không còn mục xác minh COMBAT-02 được báo cáo.
- **Tiếp theo:** `STAR-01`; chưa triển khai.

## 2026-10-13 11:06 +07:00 — COMBAT-02: Ngăn Asteroid bật khi trúng đạn

- **Trạng thái:** PARTIAL
- **Phạm vi:** Chuyển hit detection của Bullet sang trigger để không tạo xung lực vật lý lên Asteroid.
- **Thay đổi:** Xác nhận nguyên nhân là Bullet và Asteroid đều dùng Rigidbody2D động cùng collider không phải trigger. Bullet nay bật `isTrigger` cho CircleCollider2D và xử lý `OnTriggerEnter2D`; va chạm chỉ gây damage cho component Asteroid, đánh dấu đạn đã dùng trước khi gọi `TakeDamage()`, rồi trả đạn qua pool hiện tại. Không sửa Asteroid, prefab hay thiết lập physics toàn cục.
- **Kiểm tra:** Đã rà mã xử lý hit và YAML prefab Bullet/Asteroid Large; `git diff --check` hoàn tất không có lỗi whitespace. Không chạy compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần xác minh trong Unity Play Mode rằng Asteroid Large không bị đẩy, nhận damage một lần, Bullet được thu hồi/tái sử dụng và va chạm vật thể khác không gây damage.
- **Tiếp theo:** Xác minh thủ công `COMBAT-02`; chưa bắt đầu `STAR-01`.

## 2026-10-13 10:59 +07:00 — COMBAT-02: Va chạm đạn và Asteroid

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm xử lý Bullet trúng Asteroid theo damage cơ bản đã tài liệu hóa; chưa mở rộng sang task khác.
- **Thay đổi:** `Bullet` bảo đảm có CircleCollider2D khi pool tạo instance, bật va chạm liên tục, chỉ nhận Asteroid làm mục tiêu gây damage và tiêu thụ/hoàn trả đạn sau hit hợp lệ đầu tiên. Damage cấu hình mặc định là 1; `Asteroid.TakeDamage()` tiếp tục xử lý máu và vòng đời pool hiện có. Không sửa Asteroid, pool, prefab hoặc scene.
- **Kiểm tra:** Đã rà cấu hình prefab: Bullet có Rigidbody2D nhưng thiếu collider; Asteroid Small/Large có CircleCollider2D thường và Rigidbody2D động. Đã rà tĩnh luồng callback, guard chống hit lặp và đường trả Bullet qua pool. Không chạy compile hay Play Mode.
- **Chưa kiểm tra / Rủi ro:** Unity Editor đang mở; compile và kiểm tra thực tế va chạm/thu hồi chưa được xác minh. Cần vào Play Mode, bắn trúng Small/Large để xác nhận trừ đúng 1 HP, đạn chỉ bị tiêu thụ một lần và được pool tái sử dụng; bắn trượt/va vật thể khác không gây damage.
- **Tiếp theo:** Hoàn tất xác minh `COMBAT-02`; task sau đó theo roadmap là `STAR-01`.

## 2026-10-13 10:53 +07:00 — AST-02: Xác nhận tích hợp Gameplay

- **Trạng thái:** DONE
- **Phạm vi:** Chốt `AST-02` sau khi xác nhận cấu hình đã lưu trong scene `Gameplay`.
- **Thay đổi:** Đánh dấu `AST-02` hoàn tất; task kế tiếp theo thứ tự hiện có là `COMBAT-02`.
- **Kiểm tra:** Rà soát YAML xác nhận `AsteroidSpawner` gắn trên GameObject `AsteroidPool`; tham chiếu `_pool` trỏ tới component `AsteroidPool`, `_camera` trỏ tới `Main Camera`, và hai prefab Small/Large được pool gán hợp lệ. Thông số đã lưu: interval 1,8 giây, Large 20%, vertical inset 0,8, offset mép phải 0,5. Không chạy compile hoặc Play Mode trong lượt này.
- **Chưa kiểm tra / Rủi ro:** Chưa xác minh hành vi runtime bằng Play Mode.
- **Tiếp theo:** `COMBAT-02`; chưa triển khai.

## 2026-10-13 10:50 +07:00 — AST-02: Kiểm tra tích hợp Gameplay

- **Trạng thái:** PARTIAL
- **Phạm vi:** Rà soát yêu cầu chốt `AST-02`; chưa thể xác nhận tích hợp trong scene đang lưu.
- **Thay đổi:** Cập nhật trạng thái AST-02 và giữ nguyên task kế tiếp. Không sửa code hoặc scene.
- **Kiểm tra:** Đọc `Gameplay.unity`: có `AsteroidPool`, hai prefab Small/Large và camera được gán; không có component tham chiếu GUID của `AsteroidSpawner`. Không chạy compile hoặc Play Mode.
- **Chưa kiểm tra / Rủi ro:** Cần lưu `AsteroidSpawner` trên GameObject trong `Gameplay` trước khi đánh dấu DONE. Không thể ghi nhận tích hợp chưa có trong scene workspace.
- **Tiếp theo:** Hoàn tất lưu tích hợp scene và xác minh `AST-02`.

## 2026-10-13 10:38 +07:00 — AST-02: Thêm bộ sinh Asteroid

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm component sinh Asteroid tự động; chưa tích hợp component vào scene `Gameplay`.
- **Thay đổi:** Thêm `AsteroidSpawner`, gọi `AsteroidPool.TrySpawn` theo khoảng 1,8 giây; chọn Large với xác suất Inspector chỉnh được 20% (tài liệu không quy định tỷ lệ), lấy Y ngẫu nhiên trong vùng camera có chừa mép và đặt ngoài cạnh phải. Giới hạn số lượng dựa trên pool hiện có (24 Small, 8 Large). Khi có `GameManager`, chỉ sinh ở trạng thái Playing; scene chưa có GameManager được xem là đang chạy trực tiếp.
- **Kiểm tra:** Rà soát tĩnh xác nhận dùng pool, khoảng spawn theo P8, bounds camera, giới hạn pool và điều kiện run-state. Unity batch mode thoát mã 1 trước bước import/compile với thông báo access token; compile chưa xác minh. Play Mode: NOT RUN.
- **Chưa kiểm tra / Rủi ro:** Script chưa có `.meta` do Unity chưa import; cần thêm `AsteroidSpawner` vào GameObject `AsteroidPool` trong `Gameplay`, gán pool/camera rồi kiểm tra compile, nhịp sinh, phân bố loại, giới hạn pool và trạng thái Ready/Paused/Finished trong Play Mode.
- **Tiếp theo:** Hoàn tất tích hợp và xác minh `AST-02`; chưa bắt đầu task sau.

## 2026-10-12 19:03 +07:00 — AST-01: Hoàn tất xác minh thủ công

- **Trạng thái:** DONE
- **Phạm vi:** Chốt `AST-01` theo xác nhận Play Mode thủ công của người dùng.
- **Thay đổi:** Đánh dấu `AST-01` hoàn tất, chuyển task kế tiếp sang `AST-02`; gỡ `AsteroidDebugSpawner` tạm và metadata của nó. Không có tham chiếu helper trong scene/prefab production.
- **Kiểm tra:** Người dùng xác nhận Small và Large spawn bằng phím thử nghiệm, di chuyển sang trái, xoay, được thu hồi khi ra khỏi màn hình và được pool tái sử dụng; Unity chạy không có lỗi. Không chạy kiểm tra tự động mới.
- **Chưa kiểm tra / Rủi ro:** Không chạy kiểm tra tự động mới; Play Mode được xác nhận thủ công bởi người dùng.
- **Tiếp theo:** `AST-02`; chưa triển khai trong lượt này.

## 2026-10-12 18:55 +07:00 — AST-01: Thêm công cụ kiểm tra Asteroid

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm script debug riêng để kiểm tra thủ công Asteroid và pool trong Play Mode; không triển khai AST-02.
- **Thay đổi:** Thêm `AsteroidDebugSpawner`: nhấn T/Y gọi pool lấy Small/Large ở ngoài cạnh phải camera Orthographic, với Y ngẫu nhiên trong vùng nhìn thấy và Z theo đối tượng pool. Kiểm tra tham chiếu/cấu hình và giới hạn log lặp. Không đổi Asteroid, Combat, prefab hoặc scene production.
- **Kiểm tra:** Rà soát tĩnh xác nhận dùng `Keyboard.current`, gọi `AsteroidPool.TrySpawn`, lấy tọa độ từ camera và không gọi `Instantiate`/`Destroy`. Unity compile và Play Mode: NOT RUN; Editor đang mở.
- **Chưa kiểm tra / Rủi ro:** Unity chưa sinh `.meta` cho script mới. Cần làm mới/import script, thêm `AsteroidDebugSpawner` tạm thời vào một GameObject kiểm tra trong `Gameplay`, gán `AsteroidPool` và camera, rồi kiểm tra T/Y, vị trí spawn và việc trả Asteroid về pool trong Play Mode. Task vẫn PARTIAL đến khi người dùng xác minh.
- **Tiếp theo:** Hoàn tất xác minh thủ công `AST-01`; chưa bắt đầu `AST-02`.

## 2026-10-12 17:09 +07:00 — AST-01: Nền tảng Asteroid và pool

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm component Asteroid và pool dùng lại `PrefabPool<T>`; chưa tích hợp prefab vào `Gameplay`.
- **Thay đổi:** Thêm `Asteroid` với máu, vận tốc trái, drift dọc và tốc độ xoay có thể cấu hình; `AsteroidPool` hỗ trợ pool riêng Small/Large, prewarm, giới hạn dung lượng và tái sử dụng. Reset trạng thái, máu và vận tốc khi lấy/trả; thu hồi khi Asteroid vượt biên trái camera.
- **Kiểm tra:** Rà soát tĩnh cấu trúc vòng đời, reset và giới hạn pool. Unity compile và Play Mode: NOT RUN; không chạy batch mode vì Editor của dự án đang mở.
- **Chưa kiểm tra / Rủi ro:** Còn thiếu prefab Small/Large và liên kết AsteroidPool trong scene `Gameplay`; Unity chưa tạo các tệp `.meta`. Cần mở hoặc làm mới dự án trong Editor, tạo hai prefab với `Rigidbody2D`, `CircleCollider2D`, `SpriteRenderer` và component `Asteroid`; thêm `AsteroidPool` vào Gameplay, gán prefab và camera. Sau đó kiểm tra Compile và Play Mode, gồm sát thương, reset và thu hồi.
- **Tiếp theo:** Hoàn tất tích hợp prefab và xác minh `AST-01`; chưa bắt đầu `AST-02`.

## 2026-10-12 17:04 +07:00 — COMBAT-01: Xác nhận bắn và tái sử dụng đạn

- **Trạng thái:** DONE
- **Phạm vi:** Đóng `COMBAT-01` theo xác nhận kiểm tra thủ công của người dùng trong Unity Play Mode.
- **Thay đổi:** Đánh dấu `COMBAT-01` hoàn tất; task kế tiếp theo roadmap là `AST-01`.
- **Kiểm tra:** Người dùng xác nhận giữ Space 2–3 giây bắn liên tục sang phải; nhả phím dừng tạo đạn mới; đạn xuất hiện và bay đúng hướng; đạn ngoài màn hình được thu hồi và hoạt động bình thường khi bắn lại; Player di chuyển đủ bốn hướng trong khi bắn; Console không có lỗi mới.
- **Chưa kiểm tra / Rủi ro:** Không chạy kiểm tra tự động mới trong lượt này; kết quả trên là xác nhận Play Mode thủ công của người dùng.
- **Tiếp theo:** `AST-01` — mô hình, máu, chuyển động, xoay, pooling và thu hồi Asteroid ngoài màn hình.

Yêu cầu:

- Dùng thời gian thực tế nếu môi trường cung cấp được.
- Không bịa kết quả kiểm tra.
- Không ghi suy luận nội bộ.
- Không sửa hoặc xóa entry cũ nếu người dùng không yêu cầu.
- Mặc định AI chỉ cần đọc entry mới nhất; chỉ đọc sâu hơn khi cần truy vết lỗi hoặc quyết định cũ.
- Mỗi entry nên ngắn để có thể gửi riêng cho ChatGPT sau mỗi prompt.

---

<!-- ENTRY MỚI ĐẶT NGAY DƯỚI DÒNG NÀY -->

## 2026-10-12 16:56 +07:00 — COMBAT-01: Bắn và tái sử dụng đạn

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm bắn tự động bằng Space, pool đạn dùng lại `PrefabPool<T>` hiện có và thu hồi theo biên camera/thời gian sống.
- **Thay đổi:** Thêm `WeaponController`, `BulletPool`, `Bullet`; bổ sung binding Space cho action `Player/Attack`; tạo prefab `Bullet` từ sprite tam giác placeholder Unity; tích hợp pool và weapon vào `Gameplay`. Fire interval 0,35 giây, tốc độ đạn 12, lifetime 5 giây, pool tối đa 64 và prewarm 12. Dùng `Rigidbody2D.linearVelocity` cho Unity 6.
- **Kiểm tra:** Unity Editor biên dịch assembly thành công; không có lỗi/cảnh báo mới sau khi cập nhật API vận tốc. Play Mode mở được, Player hiển thị, pool root có 12 Bullet clone prewarm; Console không có lỗi/cảnh báo. Phím Space gửi dưới dạng nhấn ngắn qua tự động hóa UI chưa làm hiện projectile.
- **Chưa kiểm tra / Rủi ro:** Chưa xác nhận giữ Space bắn lặp, nhả Space dừng bắn, đạn đi ra ngoài camera được thu hồi và tái sử dụng, hoặc chuyển động Player còn hoạt động sau tích hợp. Cần kiểm tra thủ công trong Play Mode bằng bàn phím thật.
- **Tiếp theo:** Hoàn tất xác minh Play Mode cho `COMBAT-01`.

## 2026-10-12 16:45 +07:00 — PLAYER-01: Xác nhận Play Mode

- **Trạng thái:** DONE
- **Phạm vi:** Đóng `PLAYER-01` dựa trên kết quả kiểm tra thủ công do người dùng xác nhận trong Unity Play Mode.
- **Thay đổi:** Đánh dấu task hoàn tất trong `TASKS.md`; cập nhật task kế tiếp thành `COMBAT-01`.
- **Kiểm tra:** Người dùng xác nhận Player di chuyển đủ 4 hướng; di chuyển chéo không nhanh hơn; nhả phím và đổi hướng có gia tốc/giảm tốc hợp lý; Player nằm trong biên camera và nửa trái màn hình.
- **Chưa kiểm tra / Rủi ro:** Không chạy kiểm tra tự động mới trong lượt này.
- **Tiếp theo:** `COMBAT-01` (chưa triển khai).

## 2026-10-12 16:40 +07:00 — PLAYER-01: Tích hợp Player vào Gameplay

- **Trạng thái:** PARTIAL
- **Phạm vi:** Tích hợp đối tượng Player vào scene `Gameplay`; giữ nguyên phần triển khai điều khiển hiện có.
- **Thay đổi:** Tạo Player dùng sprite tam giác placeholder tích hợp của Unity tại `(-4, 0, 0)`; gắn `Rigidbody2D` và `PlayerController`, giữ speed 7, acceleration 22, deceleration 27. Gán Input Actions, Main Camera và Sprite Renderer; lưu gravity 0, interpolation Interpolate, khóa xoay.
- **Kiểm tra:** Scene mở trong Unity Editor và vào Play Mode; Player hiển thị, Console có 0 lỗi và 0 cảnh báo. Kiểm tra compile assembly thành công đã được ghi ở entry trước; lần này không chạy kiểm tra compile độc lập.
- **Chưa kiểm tra / Rủi ro:** Phím mũi tên gửi qua tự động hóa UI không tạo chuyển động quan sát được; chưa xác nhận 4 hướng, chuẩn hóa chéo, tăng/giảm tốc hoặc giới hạn biên. Cần kiểm tra thủ công trong Play Mode; chưa kết luận lỗi do điều khiển hay do cách gửi input.
- **Tiếp theo:** Hoàn tất xác minh Play Mode cho `PLAYER-01`.

## 2026-10-12 10:26 +07:00 — PLAYER-01: Di chuyển người chơi

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm điều khiển 4 hướng dùng Input System, chuyển động Rigidbody2D có gia tốc và giới hạn trong vùng camera.
- **Thay đổi:** Thêm `PlayerController` với tốc độ 7, gia tốc 22, giảm tốc 27; chuẩn hóa input chéo, nội suy Rigidbody2D, tắt trọng lực/đóng băng xoay và kẹp biên theo extents của Renderer. Không sửa scene hay tạo art.
- **Kiểm tra:** Unity Editor biên dịch assembly thành công; cấu hình `Player/Move` hiện có binding phím mũi tên. Người dùng xác nhận ARCH-01 không còn lỗi hoặc vấn đề; ghi nhận đóng task này.
- **Chưa kiểm tra / Rủi ro:** Chưa chạy Play Mode; `Gameplay` chưa có Player object để kiểm tra và cần gắn `PlayerController`, gán InputActionAsset/camera/renderer trong Editor. Batch scene setup với project đang mở không chạy được; bản cô lập dừng ở bước refresh asset database.
- **Tiếp theo:** Hoàn tất scene integration và kiểm tra Play Mode cho PLAYER-01.

## 2026-10-12 10:03 +07:00 — ARCH-01: Nền tảng runtime

- **Trạng thái:** PARTIAL
- **Phạm vi:** Thêm quản lý trạng thái run, dữ liệu kết quả, tải scene và prefab pool dùng chung.
- **Thay đổi:** `GameManager` với chuyển trạng thái có kiểm soát; `RunResult`; `SceneLoader` kiểm tra tên và Build Settings; `PrefabPool<T>` có prewarm, giới hạn tổng số instance, lease/release an toàn và hook reset.
- **Kiểm tra:** Rà soát tĩnh xác nhận không trùng class và API tra build index có trong Unity 6.5. Người dùng đã xác nhận ba scene mở được ở Editor cho SETUP-01. Không có kiểm tra tự động bổ sung được xác nhận.
- **Chưa kiểm tra / Rủi ro:** Compile/batch validation mã mới chưa chạy; Editor đang mở và log hiện tại chưa xác nhận đã import các script này. Cần xác minh compile và pool trong Editor.
- **Tiếp theo:** Hoàn tất xác minh ARCH-01.

## 2026-10-12 — SETUP-01: Xác minh hoàn tất

- **Trạng thái:** DONE
- **Kiểm tra:** Người dùng xác nhận `MainMenu`, `Gameplay`, `EndGame` mở được trong Unity Editor, không có lỗi tải scene. Batch mode trước đó hoàn tất tạo scene và biên dịch; kiểm tra tĩnh YAML, GUID, build settings và độ phân giải đều khớp.
- **Tiếp theo:** ARCH-01.

## 2026-10-12 09:53 +07:00 — SETUP-01: Khởi tạo nền Unity

- **Trạng thái:** PARTIAL
- **Phạm vi:** Tạo cấu trúc thư mục, ba scene, cấu hình landscape 16:9 ở 1280×720 và đăng ký scene trong build settings.
- **Thay đổi:** Thêm `Assets/Art`, `Audio`, `Prefabs`, `Scenes`, `Scripts` (các thư mục con theo kế hoạch), `Tests`; tạo `MainMenu`, `Gameplay`, `EndGame`; giữ thiết lập URP/input hiện có.
- **Kiểm tra:** Unity 6.5 batch mode chạy thành công, biên dịch script và tạo scene; kiểm tra tĩnh YAML, GUID, danh sách build settings và độ phân giải đều khớp.
- **Chưa kiểm tra / Rủi ro:** Chưa xác minh mở lần lượt ba scene trong Editor. Lượt batch xác minh scene bị chặn do Editor đang mở cùng project; cần mở thủ công để hoàn tất.
- **Tiếp theo:** Hoàn tất xác minh SETUP-01 trong Unity; sau đó ARCH-01.

## Khởi tạo tài liệu dự án

- **Trạng thái:** DONE
- **Phạm vi:** Khởi tạo bộ tài liệu điều hướng cho Codex.
- **Thay đổi:** Tạo AGENTS.md, PLAN.md, TASKS.md, CHANGELOG.md và README.md.
- **Kiểm tra:** Đã kiểm tra cấu trúc tài liệu và liên kết section/task.
- **Chưa kiểm tra / Rủi ro:** Chưa bắt đầu implementation trong Unity.
- **Tiếp theo:** SETUP-01.
