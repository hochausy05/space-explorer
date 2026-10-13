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
