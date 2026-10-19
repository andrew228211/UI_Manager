# ToolSDK — Điểm dừng UPM (Checkpoint)

> Tài liệu đi kèm `ToolSetupSDK_Design.md` (giai đoạn **U** trong lộ trình, mục 14: sau giai đoạn 6, trước giai đoạn 7).
> Mục đích: ghi rõ **khi nào** việc triển khai phải dừng để hướng dẫn thiết lập UPM, **điều kiện** cần đạt trước đó, và **những việc không được tự làm**.

---

## 1. Quy tắc dừng

Việc triển khai tool dừng lại khi sắp phải làm **bất kỳ** việc nào sau đây:

- Tạo file `package.json` cho ToolSDK.
- Di chuyển thư mục tool từ `Assets/` sang `Packages/` hoặc sang repo riêng.
- Tạo repo, nhánh, tag phiên bản hoặc đẩy code lên git để làm nguồn package.
- Sửa `Packages/manifest.json` của project (thêm ToolSDK làm dependency) hoặc của project khác.
- Cấu hình scoped registry.
- Thêm `Documentation~`, `Samples~`, `CHANGELOG.md`, `LICENSE` theo chuẩn package.

Khi gặp một trong các việc trên: **dừng, báo người dùng, hướng dẫn từng bước, chờ xác nhận.** Không tự làm tiếp.

Lý do: các việc này đụng tới git, repo và project khác của người dùng. Chúng khó hoàn tác và phụ thuộc vào lựa chọn của người dùng (tên công ty, nơi lưu repo, cách phân phối).

---

## 2. Điều kiện để được vào giai đoạn U

Chỉ coi là "đến đoạn UPM" khi **tất cả** mục dưới đây đạt. Nếu chưa đạt, quay lại sửa trước, chưa chuyển sang UPM.

### 2.1. Về code và cấu trúc

- [ ] Giai đoạn 1–6 trong `ToolSetupSDK_Design.md` đã xong (khung, môi trường, tương thích, planner/runner, module Ads + Firebase, validator).
- [ ] Toàn bộ tool nằm trong **một thư mục gốc duy nhất** `Assets/0_MainBase/ToolSetupSDK/`, không có file của tool nằm rải ở nơi khác trong `Assets/`.
- [ ] Cấu trúc thư mục đã **giống package** (`Editor/`, `Runtime/` nếu có, `Documentation~/`…) như mục 3.1 của tài liệu thiết kế.
- [ ] Mỗi assembly có `.asmdef` riêng, đặt tên đúng quy ước: `Base.ToolSDK.Editor`, `Base.ToolSDK.Ads.Editor`, và asmdef Firebase theo quyết định ở câu hỏi mở 3 của tài liệu thiết kế (`Base.ToolSDK.Firebase.Editor` hoặc `Base.ToolSDK.FirebaseService.Editor`).
- [ ] Thư mục `Editor/Modules/Demo/` đã được xóa hoặc chuyển vào `Samples~`.
- [ ] Assembly Editor chỉ chạy trong Editor (`includePlatforms: Editor`).
- [ ] Không có tham chiếu từ tool tới code của `MainBase` (`CoreScript`) trong phần lõi. Phần tích hợp `MainBase` (nếu có) nằm tách riêng.
- [ ] Không còn đường dẫn cứng kiểu `Assets/0_MainBase/...` trong code; mọi đường dẫn qua `ProjectPaths`.
- [ ] Không có dữ liệu riêng của project nằm trong thư mục tool (ID, key, profile môi trường, state). Tất cả nằm ở thư mục dữ liệu của project (mục 8 tài liệu thiết kế).

### 2.2. Về chất lượng

- [ ] Project compile sạch, không lỗi, không cảnh báo mới do tool gây ra.
- [ ] Unit test của `CompatibilityEngine` và các luật chạy đạt.
- [ ] Đã thử end-to-end trong project này: cài và gỡ Firebase, cài và gỡ ít nhất một SDK Ads, chuyển Dev ↔ Release.
- [ ] Đã thử cả hai nền tảng Android và iOS ở mức kiểm tra plan và cấu hình (build thật nếu có điều kiện).
- [ ] Chạy lại plan lần thứ hai không đổi gì (idempotent).
- [ ] Các vấn đề ở mục 17.2 của tài liệu thiết kế vẫn ở trạng thái đã xử lý (không bị tái phát khi thêm module Ads/Firebase thật).
- [ ] Đã thử gỡ một module có package/define dùng chung với module khác: module còn lại không bị ảnh hưởng.
- [ ] Đã thử hủy giữa chừng và tắt/mở lại Editor giữa chừng: Window hiện đúng trạng thái "cài dở" và chạy tiếp hoặc gỡ được.

### 2.3. Về trạng thái git

- [ ] Toàn bộ thay đổi của tool đã được **commit** (working tree sạch) để có điểm quay lại.
- [ ] File `.meta` của mọi file và thư mục trong tool đều có và đã commit. Thiếu `.meta` sẽ làm mất liên kết khi di chuyển hoặc đóng gói.

### 2.4. Về thông tin cần người dùng quyết định

Các mục này người dùng cần trả lời **khi được hướng dẫn** (không tự chọn thay):

- [ ] Tiền tố tên package UPM (mặc định đề xuất `com.base.toolsdk`; hoặc dùng tên công ty/studio).
- [ ] Nơi lưu package: thư mục `Packages/` của chính project này (embedded), repo git riêng, hay cả hai theo thứ tự.
- [ ] Cách phân phối cho project khác: Git URL hay scoped registry.
- [ ] Nếu dùng Git URL: địa chỉ repo, quyền truy cập (public/private), cách đặt tag phiên bản.
- [ ] Phiên bản Unity tối thiểu cần ghi vào `package.json` (project hiện tại là `6000.3.5f2`).
- [ ] Có kèm `Samples~` (module mẫu, tích hợp `MainBase`) hay không.

---

## 3. Việc Claude được làm và không được làm ở điểm dừng

| Được làm | Không được tự làm |
|---|---|
| Rà lại danh sách điều kiện ở mục 2 và báo mục nào chưa đạt. | Tạo `package.json` hoặc di chuyển thư mục sang `Packages/`. |
| Giải thích từng bước thiết lập UPM bằng lời, kèm ví dụ nội dung file. | Tạo repo, tag, đẩy code lên git. |
| Soạn sẵn nội dung mẫu (`package.json`, `.gitignore`, đường dẫn Git URL) **để người dùng xem**, chưa ghi vào project. | Sửa `Packages/manifest.json` hoặc thêm scoped registry. |
| Kiểm tra giúp sau khi người dùng làm xong từng bước. | Chạm vào project khác của người dùng. |

---

## 4. Các bước sẽ được hướng dẫn tại điểm dừng

Đây là dàn ý. Chi tiết (lệnh, ảnh chụp, nội dung file) sẽ được đưa ra **tại thời điểm dừng** và điều chỉnh theo câu trả lời ở mục 2.4.

1. **Rà soát cuối:** kiểm tra các điều kiện mục 2, commit sạch.
2. **Chốt tên:** `name` (chữ thường, dạng `com.<tiền-tố>.toolsdk`), `displayName` = `ToolSDK`, `version` ban đầu (đề xuất `0.1.0`), `unity` tối thiểu.
3. **Tạo `package.json`** với các trường: `name`, `version`, `displayName`, `description`, `unity`, `keywords`, `author`, `dependencies`.
4. **Chú ý `dependencies`:** UPM chỉ giải quyết dependency từ **registry**. Package lấy từ Git (như UniTask, VContainer trong project này) **không thể** khai báo trong `dependencies` của package; project dùng tool phải tự thêm chúng vào `manifest.json`. Vì vậy phần lõi của ToolSDK nên tránh phụ thuộc vào các package kiểu Git, hoặc ghi rõ yêu cầu trong README.
5. **Chuyển thư mục:** di chuyển vào dạng package (ví dụ `Packages/ToolSDK/`) **giữ nguyên file `.meta`** để GUID không đổi; kiểm tra project vẫn compile.
6. **Thêm thành phần chuẩn:** `README.md`, `CHANGELOG.md`, `LICENSE`, `Documentation~/`, `Samples~/` (các thư mục có dấu `~` không bị Unity import).
7. **Kiểm tra dữ liệu project:** xác nhận tool không ghi gì vào bên trong thư mục package.
8. **Đưa lên nguồn phân phối:** tạo repo và tag phiên bản (nếu dùng Git URL), hoặc cấu hình scoped registry.
9. **Cài vào project thứ hai** bằng Package Manager (Add package from git URL, hoặc sửa `manifest.json`).
10. **Kiểm tra trên project thứ hai:** menu `Tools/ToolSDK` xuất hiện, Window mở được, chạy được một plan cài đặt, chuyển môi trường.
11. **Quy trình phát hành về sau:** cập nhật `CHANGELOG`, tăng `version`, đặt tag mới, cập nhật phiên bản trong project dùng tool.

---

## 5. Lỗi thường gặp cần kiểm tra ở điểm dừng

| Lỗi | Dấu hiệu | Hướng xử lý |
|---|---|---|
| Thiếu hoặc mất `.meta` | Mất tham chiếu giữa các file sau khi di chuyển | Commit đủ `.meta`; di chuyển bằng Unity hoặc git, không xóa tay. |
| Asmdef tham chiếu sai | Lỗi compile "type or namespace not found" | Kiểm tra tên asmdef theo quy ước, tham chiếu theo tên. |
| Code Editor lọt vào build | Build Player lỗi | Asmdef Editor chỉ bật nền tảng Editor. |
| Tool ghi vào thư mục package | Lỗi ghi file khi package ở chế độ chỉ đọc | Chuyển mọi dữ liệu sang thư mục dữ liệu của project. |
| Dependency kiểu Git trong `package.json` | Package Manager báo không giải quyết được | Bỏ khỏi `dependencies`, hướng dẫn thêm vào `manifest.json` của project. |
| Namespace trùng SDK bên thứ ba | Lỗi compile khi gọi SDK | Theo quy ước mục 3.5 và quyết định câu hỏi mở 3 của tài liệu thiết kế (`global::Firebase` hoặc đổi tên `FirebaseService`). |
| Tool gỡ nhầm package dùng chung | Module còn lại báo thiếu package sau khi gỡ module khác | Kiểm tra luật tài nguyên dùng chung (mục 9.3 tài liệu thiết kế). |
| Đường dẫn Git URL sai | "Cannot find package" | Kiểm tra `?path=` và `#tag`; repo private cần xác thực. |

---

## 6. Sau điểm dừng

Khi người dùng xác nhận đã hoàn tất mục 4 và thử thành công trên project thứ hai, tiếp tục:

- Giai đoạn **7** trong lộ trình: wrapper Runtime tùy chọn, test trên project thứ hai, tài liệu sử dụng, phát hành phiên bản đầu.
- Cập nhật `ToolSetupSDK_Design.md`: đánh dấu giai đoạn U hoàn thành, ghi lại các quyết định đã chốt ở mục 2.4.

---

## 7. Nhật ký quyết định tại điểm dừng

_(Điền khi đến giai đoạn U)_

| Mục | Quyết định | Ngày |
|---|---|---|
| Tiền tố package | | |
| Nơi lưu package | | |
| Cách phân phối | | |
| Phiên bản Unity tối thiểu | | |
| Có kèm Samples~ | | |
