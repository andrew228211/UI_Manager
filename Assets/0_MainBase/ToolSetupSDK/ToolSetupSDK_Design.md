# Tool Setup SDK — Tài liệu thiết kế

> Trạng thái: **Thiết kế v4 — có bản khung demo, đã sửa các lỗi luồng ở mục 17** (một phần giai đoạn 1, 3, 4; xem mục 17)
> Tên package: **ToolSDK** — Namespace: `Base.ToolSDK` (lõi) và `Base.ToolSDK.<TênDịchVụ>` (từng dịch vụ)
> Vị trí hiện tại: `Assets/0_MainBase/ToolSetupSDK/` (giai đoạn phát triển, sau này chuyển thành UPM package)
> Môi trường Unity: `6000.3.5f2`, URP 17.3, Addressables 2.8.1, UniTask, VContainer

### Thay đổi so với v3

Rà soát lại luồng logic sau khi có bản khung code, sửa các chỗ mâu thuẫn trong tài liệu:

- **Sửa thứ tự luồng chính (mục 4):** phải *mở rộng phụ thuộc* trước, rồi mới kiểm tra tương thích trên tập đã mở rộng. Bản v3 kiểm tra trước khi Planner tự thêm `Requires`, nên module được tự thêm không được kiểm tra xung đột/nền tảng.
- **Bỏ trùng lặp `Parent`/`Children` với `Requires`** (mục 5.2): quan hệ cha-con chỉ là `Requires` + `Group`.
- **Thêm luật sở hữu tài nguyên dùng chung khi gỡ** (mục 9.3): không gỡ package/define mà module còn lại vẫn cần (ví dụ External Dependency Manager dùng chung giữa Firebase và Ads).
- **Thêm luật vòng phụ thuộc** và xử lý **package gián tiếp** (mục 9.3).
- **Làm rõ mặc định cặp SDK Ads** không mâu thuẫn với nguyên tắc "mặc định được cùng tồn tại" (mục 5.2, 6.2).
- **Làm rõ Warning cần xác nhận ở đâu** (mục 9.2), **Hủy tiến trình làm gì** (mục 10.4), **trạng thái cài dở** (mục 11).
- **Bước đổi Application ID** khi chuyển môi trường chỉ chạy khi bật tính năng hậu tố (mục 7.4), khớp với phạm vi ở mục 7.1.
- **Thống nhất tên:** `Group` (không dùng `Category`), menu `Tools/ToolSDK/Setup SDK`, tên phương thức theo code hiện tại (mục 5.3, 10.2).
- **Thêm giai đoạn U** (điểm dừng UPM) vào lộ trình (mục 14) để khớp với `ToolSetupSDK_UPM_Checkpoint.md`.
- **Thêm mục 17:** hiện trạng bản khung và các vấn đề logic tìm thấy trong code, **đã sửa trong cùng đợt** (17.2).

### Thay đổi v3 (so với v2)

- **Có thể cài nhiều SDK cùng loại** (nhiều SDK Ads; Firebase chia thành nhiều dịch vụ con). Thêm SDK mới phải linh hoạt, chỉ cần thêm một module.
- Chốt tên package **ToolSDK** và quy ước namespace `Base.ToolSDK.<TênDịchVụ>`.
- Môi trường Dev: chỉ dựng khung profile; các tính năng nâng cao (cài song song, đổi icon…) **để sau khi cần**.

### Lịch sử

- v3: nhiều SDK cùng loại, chốt tên ToolSDK, khung môi trường Dev.
- v2: chốt Ads + Firebase, Android + iOS, hệ thống 2 môi trường, đóng gói UPM.
- v1: kiến trúc tổng thể và logic tương thích.

---

## 1. Mục tiêu

Một **Editor Tool** cài đặt và cấu hình SDK cho game mobile, đảm bảo các SDK **tương thích với nhau** và với nền tảng, có thể **đổi môi trường Dev/Release chỉ bằng một thao tác**, và dùng lại được cho nhiều project.

Tool phải làm được:

1. Cài, cập nhật, gỡ SDK Ads và Firebase từ một cửa sổ.
2. Chặn hoặc cảnh báo khi lựa chọn gây xung đột (giữa SDK, với Unity, hoặc với nền tảng).
3. Tự cấu hình các phần SDK bắt buộc: define symbol, Player Settings, file cấu hình, Android manifest, iOS Info.plist.
4. Quản lý **2 môi trường** với bộ cấu hình riêng và chuyển đổi nhanh.
5. Chạy lại an toàn (idempotent), tiếp tục được sau khi Unity compile lại script.
6. Cài vào project mới chỉ bằng cách thêm một package.

Ngoài phạm vi giai đoạn đầu: code gameplay gọi SDK, CI build, quản lý dashboard của từng SDK, các loại SDK khác (attribution, IAP, social…). Kiến trúc vẫn để mở để thêm sau.

---

## 2. Nguyên tắc thiết kế

| Nguyên tắc | Ý nghĩa |
|---|---|
| **Code-first** | Mỗi SDK là một class C#, không dùng ScriptableObject để mô tả SDK. |
| **Tương thích là dữ liệu hạng nhất** | Luật tương thích khai báo riêng, kiểm tra bởi một bộ riêng. |
| **Tool tách khỏi dữ liệu project** | Package chỉ chứa logic. Cấu hình (ID, key, môi trường) nằm trong project dùng nó. |
| **Môi trường là một lớp riêng** | Dev/Release chỉ khác nhau ở cấu hình, không đổi danh sách SDK. |
| **Từng bước, tiếp tục được** | Thao tác là chuỗi bước nhỏ, trạng thái sống qua domain reload. |
| **Idempotent, gỡ sạch** | Chạy lại nhiều lần cho cùng kết quả, mỗi bước cài có bước gỡ. |
| **Plan trước, làm sau** | Luôn cho xem kế hoạch trước khi sửa project. |

---

## 3. Đóng gói thành UPM package

### 3.1. Cấu trúc package

```
ToolSDK/                            (tên hiển thị: ToolSDK)
  package.json                      (name: com.base.toolsdk — xem mục 3.5)
  README.md
  CHANGELOG.md
  Documentation~/                   (tài liệu này chuyển vào đây)
  Editor/
    Base.ToolSDK.Editor.asmdef      (Editor-only: lõi)
    Core/ Compatibility/ Execution/ Environments/ Config/ Window/
                                    (Planner nằm trong Execution/ như bản khung hiện tại)
    Modules/
      Firebase/
        Base.ToolSDK.Firebase.Editor.asmdef
        FirebaseCoreModule.cs, FirebaseAnalyticsModule.cs, FirebaseCrashlyticsModule.cs ...
      Ads/
        Base.ToolSDK.Ads.Editor.asmdef
        LevelPlayModule.cs, AdMobModule.cs, ...   (mỗi SDK Ads một module)
  Runtime/                          (tùy chọn, giai đoạn sau)
    Base.ToolSDK.Runtime.asmdef
    Firebase/  Base.ToolSDK.Firebase.asmdef
    Ads/       Base.ToolSDK.Ads.asmdef
  Samples~/                         (tùy chọn)
```

Mỗi dịch vụ có asmdef riêng để thêm hoặc bỏ một dịch vụ không ảnh hưởng dịch vụ khác.

### 3.2. Quy tắc để dùng lại được

- **Không tham chiếu tới code của `MainBase`** trong phần lõi. Phần tích hợp `ServiceLocator`/`EventBus` đặt trong module tùy chọn hoặc Sample, để project khác không có `MainBase` vẫn dùng được.
- **Không ghi dữ liệu vào thư mục package** (package có thể chỉ đọc). Toàn bộ cấu hình project ghi vào project (xem mục 8).
- Mọi đường dẫn đi qua một lớp `ProjectPaths`, không ghi cứng `Assets/0_MainBase/...`.
- `package.json` khai báo phiên bản Unity tối thiểu và các `dependencies` bắt buộc của chính tool.
- Phiên bản tool theo semantic versioning. Danh sách phiên bản SDK mục tiêu thuộc về tool nên đổi phiên bản SDK = phát hành phiên bản tool mới.

### 3.3. Cách phân phối

- Git URL (`https://...git?path=...#<tag>`) — đơn giản, phù hợp nhóm nội bộ.
- Hoặc scoped registry riêng nếu cần quản lý phiên bản chặt hơn.

### 3.4. Giai đoạn phát triển (xem tiếp mục 3.5 về quy ước đặt tên)

Phát triển trong `Assets/0_MainBase/ToolSetupSDK/` cho tiện, nhưng **giữ cấu trúc thư mục giống package ngay từ đầu** để chuyển sang `Packages/` hoặc repo riêng chỉ là việc di chuyển thư mục và thêm `package.json`.

### 3.5. Quy ước đặt tên

| Hạng mục | Quy ước |
|---|---|
| Tên hiển thị | `ToolSDK` |
| Tên package UPM | `com.base.toolsdk` (UPM yêu cầu chữ thường; có thể đổi prefix nếu có tên công ty) |
| Namespace lõi | `Base.ToolSDK` |
| Namespace theo dịch vụ | `Base.ToolSDK.<TênDịchVụ>`, ví dụ `Base.ToolSDK.Firebase`, `Base.ToolSDK.Ads` |
| Namespace Editor | `Base.ToolSDK.Editor` (lõi), `Base.ToolSDK.<TênDịchVụ>.Editor` |
| Tên asmdef | trùng namespace: `Base.ToolSDK.Firebase`, `Base.ToolSDK.Firebase.Editor`… |
| Menu | `Tools/ToolSDK/...` |

**Lưu ý quan trọng về namespace `Base.ToolSDK.Firebase`:** SDK của Google cũng dùng namespace `Firebase` (ví dụ `Firebase.Analytics`). Bên trong `namespace Base.ToolSDK.Firebase`, tên `Firebase` sẽ bị hiểu là namespace của chúng ta thay vì của Google, làm code gọi SDK không compile. Quy ước để tránh lỗi này:

- Trong code của dịch vụ Firebase luôn dùng `global::Firebase.Analytics...` hoặc khai báo alias, ví dụ `using FirebaseSdk = global::Firebase;`.
- Hoặc đặt tên thư mục/namespace dịch vụ là `Base.ToolSDK.FirebaseService` thay cho `Firebase`. Cần chốt trước khi viết code (xem câu hỏi mở).

Cùng một vấn đề có thể xảy ra với tên dịch vụ khác nếu trùng tên namespace của SDK bên thứ ba, nên luật này áp dụng cho mọi dịch vụ.

Vấn đề tương tự với namespace lõi `Base.ToolSDK.Editor`: bên trong namespace này, tên `Editor` bị hiểu là namespace của chúng ta chứ không phải class `UnityEditor.Editor`. Khi viết custom inspector trong lõi phải dùng `UnityEditor.Editor` đầy đủ.

---

## 4. Kiến trúc tổng thể

```
Người dùng chọn SDK (ý định Cài / Gỡ) + môi trường trên Window
        │
        ▼
Planner.Expand(selection)
        │  tự thêm Requires còn thiếu, sắp theo phụ thuộc, phát hiện vòng
        ▼
CompatibilityEngine.Evaluate(tập đã mở rộng, projectState, environment)
        │  ──► có Error?  ──► hiện lỗi, khóa nút, dừng
        ▼
Planner.BuildSteps(...)  ──► SetupPlan (xem trước, kèm Warning)
        │
        ▼  (người dùng xác nhận, Warning được liệt kê trong hộp xác nhận)
Kiểm tra lại ngay trước khi chạy (trạng thái project có thể đã đổi)
        │
        ▼
SetupRunner.Run(plan)  ──► từng Step ──► lưu SetupState
        │
        ▼
Validator kiểm tra sau cài  ──►  báo kết quả lên Window
```

**Vì sao Expand phải đứng trước Evaluate:** module được tự thêm qua `Requires` cũng phải qua luật xung đột và nền tảng. Nếu kiểm tra trước khi mở rộng, một module cha không hỗ trợ build target hoặc xung đột với SDK đã cài sẽ lọt vào plan mà không bị chặn.

Các thành phần:

| Thành phần | Vai trò |
|---|---|
| `ISdkModule` / `SdkModuleBase` | Hợp đồng của một SDK. |
| `SdkRegistry` | Tự tìm module bằng `TypeCache`. |
| `CompatibilityEngine` + `ICompatibilityRule` | Kiểm tra tương thích. |
| `EnvironmentManager` | Quản lý và áp dụng profile Dev/Release. |
| `Planner` | Sắp xếp phụ thuộc, tạo kế hoạch. |
| `SetupRunner` + `ISetupStep` | Chạy từng bước, sống qua domain reload. |
| `ProjectConfigStore` | Đọc/ghi cấu hình trong project. |
| `SdkSetupWindow` | Giao diện UI Toolkit. |

---

## 5. Hợp đồng của một SDK Module

### 5.1. Metadata

- `Id` ổn định (ví dụ `firebase`, `ads-mediation`), `DisplayName`, `Group` (nhóm hiển thị, xem 5.2).
- `Version`: phiên bản mục tiêu.
- `SupportedPlatforms`: Android, iOS.
- `Source`: `UpmRegistry`, `UpmGit`, `UnityPackage`.

### 5.2. Quan hệ

- `Requires`: SDK/package bắt buộc, kèm khoảng phiên bản.
- `Conflicts`: không được cài cùng. **Chỉ khai báo khi thực sự xung đột**; mặc định hai module khác nhau được phép cùng tồn tại. Ngoại lệ duy nhất là các cặp module cùng `Provides` một khả năng chồng lấn (ví dụ `ad-mediation`): cặp chưa có trong ma trận tương thích mặc định là `Warning` (mục 6.2, 9.9).
- `Provides`: khả năng cung cấp (`ad-mediation`, `ad-network`, `analytics`, `crash-reporting`…). Luật viết theo khả năng, không theo tên SDK.
- **Dịch vụ con** (ví dụ Firebase Analytics là con của Firebase Core) được biểu diễn bằng `Requires` tới module cha + cùng `Group`. Không có trường `Parent`/`Children` riêng để tránh hai nguồn sự thật. Cài con thì tự kéo cha vào plan; gỡ cha khi con còn tồn tại thì bị chặn.
- Đồ thị `Requires` **phải không có vòng**. Có vòng → Error (mục 9.3).
- `Group`: nhóm hiển thị trên Window (Ads, Firebase…).

### 5.2b. Thêm SDK mới (tính mở rộng)

Mục tiêu: **thêm một SDK không phải sửa code lõi**.

1. Tạo class kế thừa `SdkModuleBase`, đặt trong thư mục dịch vụ tương ứng (namespace `Base.ToolSDK.<TênDịchVụ>`).
2. Khai báo metadata, quan hệ, `PackageIds`, `Defines`. Chỉ override `BuildInstallSteps()` / `BuildRemoveSteps()` khi cần bước riêng.
3. `SdkRegistry` tự phát hiện module bằng `TypeCache`, nên Window và bộ kiểm tra tương thích nhận ngay.
4. Nếu SDK có xung đột đặc thù, thêm luật hoặc dòng vào ma trận tương thích; nếu không thì không cần làm gì thêm.
5. Module có thể nằm **ngoài package ToolSDK** (ví dụ trong package của project khác) miễn là có tham chiếu tới `Base.ToolSDK.Editor`. `SdkRegistry` quét toàn bộ assembly Editor nên module bên ngoài cũng được nhận.
6. Cung cấp một **module mẫu** (`Samples~/NewModuleTemplate`) và checklist để người khác tự thêm SDK.

Checklist khi thêm module: metadata đầy đủ; bước cài và bước gỡ đối xứng; `GetInstallState()` phản ánh đúng kết quả của các bước cài (chưa cài / cài dở / đã cài); `Validate` kiểm tra được trạng thái sau cài; khai báo `Provides`; khai báo đúng package/define dùng chung với module khác (để không bị gỡ nhầm, mục 9.3); có test cho luật mới nếu có.

### 5.3. Hành vi

| Phương thức | Trạng thái | Ý nghĩa |
|---|---|---|
| `BuildInstallSteps()` / `BuildRemoveSteps()` | Đã có | Danh sách bước cài / gỡ. Giai đoạn 2 sẽ nhận thêm tham số context (môi trường, build target). |
| `GetInstallState()` | Đã có | Trả về `NotInstalled` / `Partial` (cài dở: có một phần, hoặc package sai phiên bản) / `Installed`. `IsInstalled()` còn lại là extension method (= `Installed`). Module không có package/define phải tự override. Còn thiếu: báo phiên bản cụ thể và "cần cập nhật" riêng. |
| `Validate(context)` | Chưa có | Kiểm tra sau cài. |
| `GetSettings()` | Chưa có | Các trường người dùng cần nhập, **mỗi trường khai báo có khác nhau theo môi trường hay không** (xem mục 7). |

---

## 6. Hai SDK ban đầu

> Cần xác minh các chi tiết dưới đây với tài liệu chính thức của đúng phiên bản SDK khi triển khai. Đây là hướng thiết kế, không phải đặc tả cuối.

### 6.1. Firebase

- Gồm nhiều dịch vụ (Analytics, Crashlytics, Remote Config, Messaging…). Mỗi dịch vụ là **một module con riêng** (`Requires` = Firebase Core), người dùng tick chọn từng dịch vụ cần dùng. Thêm dịch vụ Firebase mới sau này = thêm một module con, không sửa module khác.
- `FirebaseCoreModule` lo phần chung: Firebase App, External Dependency Manager, file cấu hình theo môi trường. Các module con chỉ thêm package riêng của dịch vụ đó.
- Phụ thuộc cơ chế giải quyết thư viện native (External Dependency Manager). Đảm bảo chỉ có **một** bản, dùng chung với module Ads nếu Ads cũng cần. Nên tách EDM thành **module riêng** mà cả Firebase Core và Ads cùng `Requires`, để luật gỡ (mục 9.3) tự bảo vệ nó: gỡ Firebase không làm mất EDM khi Ads còn cần.
- Cần file cấu hình **riêng cho từng nền tảng**: Android và iOS. Mỗi môi trường có cặp file riêng (xem mục 7).
- Crashlytics cần bước cấu hình bổ sung khi build (symbol upload, Gradle) — đưa vào `CustomStep` và kiểm tra trong `Validate`.
- Cần kiểm tra: minSdk Android, phiên bản iOS tối thiểu, bật các dịch vụ Google Play/CocoaPods theo yêu cầu của phiên bản Firebase được chọn.

### 6.2. Ads

- **Có thể cài nhiều SDK Ads** (ví dụ LevelPlay, AppLovin MAX, AdMob…). Mỗi SDK là một module riêng trong nhóm `Ads`, không giới hạn số lượng ở mức thiết kế.
- Vì nhiều SDK Ads thường **chồng lấn** (cùng quản lý mạng quảng cáo, cùng thư viện Google, cùng bước khởi tạo), tool **không** cấm theo nguyên tắc chung. Thay vào đó:
  - Từng cặp SDK Ads được đánh giá qua ma trận tương thích: `Compatible`, `Warning` (cùng tồn tại được nhưng cần cấu hình cẩn thận), hoặc `Conflict`.
  - Cặp chưa có trong ma trận → mặc định `Warning` kèm gợi ý "chưa kiểm chứng". Luật này chỉ áp dụng cho các module cùng `Provides` khả năng chồng lấn (như `ad-mediation`), không áp dụng cho mọi cặp module khác nhau.
  - Window hiển thị rõ các cặp đang cùng được chọn và lý do.
- Mỗi SDK Ads có thể có danh sách con riêng (adapter mạng quảng cáo) với phụ thuộc và yêu cầu manifest/plist riêng.
- Ở phần Runtime (giai đoạn sau): dùng giao diện chung `IAdsProvider`, mỗi SDK là một provider; cấu hình quyết định provider nào chạy, thứ tự ưu tiên, và có cho chạy nhiều provider cùng lúc không.
- Ads thường kéo thư viện native chung với Firebase (dịch vụ Google). Đây là điểm xung đột phiên bản chính cần luật riêng (mục 9.3).
- iOS: cần mô tả quyền theo dõi (ATT) và danh sách SKAdNetwork; Android: Advertising ID và quyền liên quan. Module cung cấp bước `EditManifest` tương ứng.
- Cần luồng **consent/quyền riêng tư** trước khi khởi tạo Ads. Tool cảnh báo nếu chưa thiết lập.

### 6.3. Quan hệ giữa hai SDK

| Quan hệ | Xử lý |
|---|---|
| Ads và Firebase cùng dùng thư viện Google native | Luật phiên bản chung (mục 9.3). |
| Cả hai cùng sửa `AndroidManifest`/Gradle/`Info.plist`/Podfile | Luật tài nguyên dùng chung và hợp nhất (mục 9.4). |
| Ads cần consent trước khi khởi tạo; Firebase Analytics cũng bị ảnh hưởng bởi consent | Luật thứ tự và quyền riêng tư (mục 9.5). |
| Firebase là dịch vụ phân tích, Ads là kiếm tiền | Có thể liên kết sự kiện doanh thu quảng cáo sang Analytics (tùy chọn, giai đoạn sau). |

---

## 7. Hệ thống 2 môi trường (Dev / Release)

### 7.1. Khái niệm

Một **Environment Profile** là bộ giá trị cấu hình cho tất cả SDK trong một môi trường. Mặc định có hai profile: `Dev` và `Release`. Cấu trúc cho phép thêm profile (ví dụ `Staging`) mà không đổi kiến trúc.

**Phạm vi triển khai:** ở giai đoạn đầu chỉ dựng **khung** profile (lưu giá trị, chuyển môi trường, chặn build sai môi trường). Các tính năng của môi trường Dev như cài song song với Release (hậu tố Application ID), đổi tên hoặc icon theo môi trường sẽ **làm sau khi có nhu cầu**. Mỗi tính năng như vậy là một `Step` độc lập nên thêm sau không phải sửa kiến trúc, và mặc định đang tắt.

Vì mỗi SDK tự khai báo trường cấu hình nào khác nhau theo môi trường (`GetSettings()`), thêm SDK mới tự động có tab Dev/Release trên Window mà không cần code thêm.

### 7.2. Cái gì khác nhau giữa các môi trường

| Hạng mục | Dev | Release |
|---|---|---|
| Firebase project / file cấu hình Android + iOS | project Dev | project Release |
| Application ID / Bundle ID | có thể thêm hậu tố (ví dụ `.dev`) để cài song song | ID chính thức |
| ID quảng cáo (app ID, ad unit ID) | ID test | ID thật |
| Chế độ quảng cáo | test / log chi tiết | thật |
| Log, debug của SDK | bật | tắt |
| Crashlytics / Analytics | tùy chọn (thường tắt hoặc tách project) | bật |
| Define symbol | `SDK_ENV_DEV` | `SDK_ENV_RELEASE` |
| Tên hiển thị, icon (tùy chọn) | có dấu hiệu Dev | bình thường |

### 7.3. Nơi lưu

- Mỗi profile là một thư mục trong project, ví dụ `<ThưMụcDữLiệu>/Environments/Dev/` và `.../Release/`, chứa:
  - `profile.json`: giá trị cấu hình (ID, cờ bật/tắt).
  - Các file cấu hình do SDK yêu cầu theo nền tảng, lưu **nguyên bản** (ví dụ cặp file Firebase của Android và iOS).
- Profile đang hoạt động ghi vào một file riêng `active-environment`, **không** đưa vào bản build ngoài những gì cần thiết.

### 7.4. Chuyển môi trường (Apply Environment)

Một thao tác `Apply Environment(Dev|Release)` tạo ra một plan gồm các bước:

1. Sao chép file cấu hình SDK của profile vào đúng vị trí SDK đọc.
2. Ghi giá trị vào các nơi cần (settings của SDK, manifest, plist).
3. Đổi Application ID / Bundle ID theo profile — **chỉ khi** tính năng hậu tố ID đã bật (mặc định tắt, mục 7.1). Khi tắt, bước này không có trong plan.
4. Đổi define symbol `SDK_ENV_*`.
5. Chạy `Validate` để chắc chắn môi trường đã nhất quán.

Vì là plan nên có thể **xem trước**, **hoàn tác**, và chạy lại an toàn.

### 7.5. Bảo vệ khi build Release

- Hook `IPreprocessBuildWithReport`: nếu build bản Release mà đang ở môi trường Dev (hoặc ngược lại) thì **dừng build hoặc cảnh báo rõ ràng**.
- Cần định nghĩa "bản Release" để hook nhận biết. Đề xuất: build **không** bật `Development Build` = bản Release. Có thể thay bằng cờ riêng hoặc Build Profile (câu hỏi mở 9).
- Bắt buộc không còn ID test trong profile Release; `Validate` kiểm tra.
- Hiển thị môi trường đang hoạt động ngay trên Window và (tùy chọn) trên toolbar Editor để tránh nhầm.

### 7.6. Dữ liệu nhạy cảm

- `profile.json` chứa ID/key. Cho người dùng chọn: commit lên git (nếu chỉ là ID công khai) hoặc đưa vào `.gitignore` và dùng file `.example` để chia sẻ cấu trúc.
- Tool tự thêm các đường dẫn nhạy cảm vào `.gitignore` nếu người dùng chọn chế độ riêng tư.

---

## 8. Cấu hình lưu trong project

Vì tool là package dùng chung nên mọi dữ liệu riêng của project nằm ngoài package:

```
<ProjectRoot>/
  ProjectSettings/SdkSetup/        (hoặc thư mục dữ liệu do người dùng chọn)
    selection.json                 Danh sách SDK đã chọn và phiên bản
    Environments/
      Dev/profile.json  + file cấu hình SDK
      Release/profile.json + file cấu hình SDK
    state.json                     Trạng thái cài đặt gần nhất
```

- Đường dẫn gốc cấu hình được chọn lần đầu mở tool và lưu lại.
- Có phiên bản định dạng (`schemaVersion`) để nâng cấp cấu hình khi tool cập nhật.

---

## 9. Logic tương thích

Toàn bộ nằm trong `Compatibility/`, tách khỏi code cài đặt.

### 9.1. Đầu vào

`CompatibilityContext`: SDK chọn (cài/giữ/gỡ) **sau khi đã mở rộng `Requires`** (gồm cả danh sách module được tự thêm), hai tập riêng vì cài và gỡ là hai lần chạy: `AfterInstall` = (đang có + sắp cài), **không** trừ module sắp gỡ; `AfterRemove` = đang có − sắp gỡ, **không** tính module sắp cài; "đang có" gồm cả module cài dở, môi trường đang áp dụng, trạng thái project (package và phiên bản, define, build target, Unity version, Scripting Backend, API level, render pipeline), thông tin từ module.

### 9.2. Mức độ kết quả

| Mức | Hành vi |
|---|---|
| **Error** | Chặn thao tác cùng phạm vi (`Install` hoặc `Remove`): lỗi khi cài chỉ khóa nút Cài, lỗi khi gỡ chỉ khóa nút Gỡ. |
| **Warning** | Cho tiếp tục sau khi xác nhận. Toàn bộ Warning **phải được liệt kê trong hộp xác nhận** trước khi chạy, không chỉ hiện trên khung Compatibility. |
| **Info** | Gợi ý. |
| **AutoFix** | Tool tự sửa được, người dùng chỉ cần đồng ý. |

### 9.3. Luật phiên bản và phụ thuộc

- Thiếu `Requires` → tự thêm vào plan (Info); `Requires` trỏ tới module không tồn tại → Error.
- Đồ thị `Requires` có vòng → Error, không tạo plan.
- Gỡ một module mà module khác trong tập `AfterRemove` vẫn `Requires` nó → Error (gỡ module phụ thuộc trước hoặc gỡ cùng lúc).
- **Tài nguyên dùng chung khi gỡ:** một package hoặc define chỉ được gỡ khi **không còn module nào đang được giữ lại (`AfterRemove`) khai báo nó**. Nếu còn, bước gỡ tài nguyên đó bị bỏ khỏi plan và hiện Info. Nếu không có luật này, gỡ một module sẽ gỡ luôn package mà module khác vẫn dùng.
- **Package gián tiếp:** package chỉ có mặt vì là dependency của package khác thì không tính là "đã cài" theo module, và không được đưa vào bước gỡ (gỡ không có tác dụng và bước gỡ sẽ treo tới khi quá hạn). Nhận biết qua `PackageInfo.isDirectDependency` hoặc đọc `manifest.json`.
- Module cha được tự thêm khi cài con **không tự gỡ** khi gỡ con. Window hiện Info gợi ý gỡ cha nếu không còn module nào cần.
- Phiên bản nằm ngoài khoảng cho phép → đề nghị nâng/hạ. Lưu ý: so khớp "đã cài" phải so cả phiên bản khi module khai báo `@version`, nếu không sẽ không bao giờ phát hiện được trạng thái "cần cập nhật".
- Ads và Firebase cùng kéo một thư viện native (Android/iOS) với phiên bản khác nhau → Warning, gợi ý bộ phiên bản đã kiểm chứng.
- Chỉ một bản External Dependency Manager trong project.

### 9.4. Luật tài nguyên dùng chung

- Nhiều SDK cùng sửa `AndroidManifest`, Gradle template, `Info.plist`, Podfile, ProGuard → tool **hợp nhất** theo khối có đánh dấu (mỗi SDK sở hữu một khối), không ghi đè của nhau.
- Phát hiện trùng khóa/entry giữa các SDK → Warning.

### 9.5. Luật thứ tự và quyền riêng tư

- Consent trước, rồi mới khởi tạo Ads và các dịch vụ thu thập dữ liệu.
- iOS: thiếu mô tả quyền theo dõi hoặc danh sách SKAdNetwork cần thiết → AutoFix.
- Android: thiếu quyền/khai báo cần cho Advertising ID → AutoFix.

### 9.6. Luật nền tảng

- SDK không hỗ trợ build target đang bật → Error.
- minSdk Android, phiên bản iOS tối thiểu, Scripting Backend, kiến trúc CPU, Unity version tối thiểu → AutoFix nếu sửa được.
- Build target Android hoặc iOS chưa cài module hỗ trợ trong Unity Hub → Warning.

### 9.7. Luật môi trường

- Thiếu file cấu hình cho một nền tảng trong profile đang dùng → Error.
- Profile Release còn ID test, hoặc Dev trùng ID với Release → Error/Warning.
- Application ID của profile không khớp với ID khai báo trong file Firebase của profile đó → Error.

### 9.8. Cách viết luật

```csharp
public interface ICompatibilityRule
{
    string Id { get; }
    void Evaluate(CompatibilityContext ctx, CompatibilityReport report);
}
```

Tự tìm bằng `TypeCache`. Thêm luật = thêm một file. Luật chạy theo thứ tự `Id` (đặt tiền tố số như `1-requires`, `2-conflict`, `3-platform` để thứ tự ổn định).

### 9.9. Ma trận tương thích ban đầu

| A | B | Quan hệ | Ghi chú |
|---|---|---|---|
| Ads SDK 1 | Ads SDK 2 | **Theo từng cặp** (`Compatible` / `Warning` / `Conflict`) | Cho phép cài nhiều SDK Ads; cặp chưa kiểm chứng mặc định `Warning`. |
| Firebase (dịch vụ con) | Firebase Core | **Yêu cầu** | Cài dịch vụ con thì tự thêm Core. |
| Ads | Firebase | **Phải thống nhất** | Thư viện Google native dùng chung, kiểm tra phiên bản. |
| Ads | Consent flow | **Phụ thuộc thứ tự** | Consent trước khi khởi tạo. |
| Firebase | External Dependency Manager | **Yêu cầu** | Một bản duy nhất. |
| Firebase | Env profile | **Yêu cầu** | Mỗi profile cần đủ file Android + iOS. |
| Ads | Env profile | **Yêu cầu** | Dev dùng ID test, Release dùng ID thật. |

> Bảng sẽ được bổ sung số phiên bản cụ thể đã kiểm chứng sau khi chọn SDK Ads cụ thể.

### 9.10. Preset

Danh sách SDK + phiên bản đã kiểm chứng chạy cùng nhau, ví dụ `Ads + Firebase (Android/iOS)`. Chọn preset vẫn chạy bộ kiểm tra như bình thường.

---

## 10. Lập kế hoạch và thực thi

### 10.1. Plan (dry run)

`Planner` sắp xếp theo đồ thị phụ thuộc và tạo `SetupPlan`. Người dùng xem trước: package nào thêm, define nào, file nào bị sửa, Player Settings nào đổi, môi trường nào được áp dụng.

- Cài: module được cần đứng trước. Gỡ: module phụ thuộc gỡ trước, module được cần gỡ sau. Trong một module: cài package trước rồi thêm define; gỡ define trước rồi gỡ package (tránh code `#if` bị bật khi package đã mất).
- Cài và gỡ là **hai plan riêng**, chạy bằng hai nút riêng; không trộn trong một lần chạy.
- Ngay trước khi chạy phải tính lại plan và kiểm tra lại tương thích, vì trạng thái project có thể đã đổi từ lúc xem trước.

### 10.2. Các loại bước

| Bước | Việc làm |
|---|---|
| `AddPackageStep` / `RemovePackageStep` | Thêm, gỡ package UPM. |
| `ImportUnityPackageStep` | Import `.unitypackage`. |
| `WaitForCompileStep` | Đợi compile xong. |
| `AddDefineStep` / `RemoveDefineStep` | Scripting Define Symbols (theo từng build target). |
| `PlayerSettingsStep` | minSdk, Scripting Backend, kiến trúc, Application/Bundle ID. |
| `EditAndroidManifestStep` | Sửa manifest theo khối của từng SDK. |
| `EditIosPlistStep` / `EditPodfileStep` | Sửa `Info.plist`, Podfile (qua bước hậu build khi cần). |
| `CopyEnvFileStep` | Sao chép file cấu hình của profile vào vị trí SDK đọc. |
| `WriteSettingsStep` | Ghi giá trị cấu hình. |
| `GenerateWrapperStep` | Sinh wrapper dịch vụ (giai đoạn sau). |
| `CustomStep` | Logic riêng của từng SDK (ví dụ Crashlytics). |

Hợp đồng của một bước (theo bản khung hiện tại):

| Thành viên | Ý nghĩa |
|---|---|
| `StepData` (`kind` + `arg`) | Dữ liệu thuần, serialize được, là thứ duy nhất được lưu qua domain reload. Bước được dựng lại từ `StepData` sau reload. |
| `IsDone()` | Kết quả mong muốn đã có chưa. Gọi trước `Begin()` để bỏ qua bước đã xong (idempotent), và dùng để xác nhận kết quả sau reload. |
| `Begin()` | Bắt đầu việc (có thể bất đồng bộ, ví dụ request Package Manager). |
| `Poll()` | `Running` / `Done` / `Failed`. |
| `Undo()` | **Chưa có**, làm ở giai đoạn 6. |

Bước bất đồng bộ phải xác định được kết quả **chỉ từ trạng thái thực tế của project** (vì đối tượng request mất sau domain reload), và phải phân biệt được "đang chạy" với "đã thất bại nhưng mất request" — nếu không, lỗi sau reload chỉ lộ ra khi quá thời gian chờ.

### 10.3. Xử lý domain reload

- `SetupState` lưu bằng `SessionState` (sống qua domain reload) **và** file `Library/ToolSDK/SetupState.json` (còn lại khi tắt/mở lại Editor). Nếu Unity bị đóng giữa chừng, lần mở sau trạng thái được nạp lại là "bị gián đoạn ở bước X/N" và **không tự chạy tiếp**; người dùng kiểm tra rồi chạy lại plan (idempotent).
- Một class `[InitializeOnLoad]` kiểm tra `SetupState` mỗi lần reload và **tự chạy tiếp** các bước còn lại.
- Đánh dấu bước "đã bắt đầu" và **lưu trạng thái trước khi gọi `Begin()`**, để nếu reload xảy ra ngay trong `Begin()` thì lần chạy tiếp không gọi lại `Begin()` lần hai.
- Mỗi bước có thời gian chờ tối đa, quá hạn thì đánh dấu lỗi và dừng. Thời gian Unity compile/import **không tính** vào thời gian chờ của bước (project lớn có thể compile vài phút).

### 10.4. Lỗi và khôi phục

- Bước lỗi → dừng plan, ghi lại bước nào và lý do.
- Người dùng chọn **Thử lại**, **Bỏ qua** (nếu không bắt buộc), hoặc **Hoàn tác** bằng `Undo()`.
- Cảnh báo nếu git working tree chưa sạch trước khi chạy.
- **Hủy tiến trình** chỉ dừng runner trước bước kế tiếp; request Package Manager đang chạy dở vẫn tiếp tục trong Unity và không được hoàn tác. Sau khi hủy, project có thể ở trạng thái **cài dở**; Window phải hiện rõ trạng thái này (mục 11) và cho chạy lại plan để hoàn tất hoặc gỡ phần đã cài.

---

## 11. Giao diện Editor Window

UI Toolkit, menu `Tools/ToolSDK/Setup SDK` (theo quy ước mục 3.5).

1. **Thanh trên:** môi trường đang hoạt động (nổi bật, đổi màu theo Dev/Release), nút chuyển môi trường, chọn Preset, Unity version và build target.
2. **Danh sách SDK:** checkbox, tên, phiên bản đã cài so với mục tiêu, trạng thái (chưa cài, đã cài, **cài dở**, cần cập nhật, lỗi, xung đột). Module cài dở phải gỡ được (chạy plan gỡ cho phần đã có), không chỉ cài tiếp.
3. **Khung chi tiết:** mô tả, `Requires`/`Conflicts`, ô nhập cấu hình **theo môi trường** (tab Dev / Release, tách Android / iOS khi cần).
4. **Khung Compatibility:** Error / Warning / Info / AutoFix cập nhật theo thời gian thực.
5. **Khung Plan:** xem trước các bước.
6. **Thanh dưới:** `Apply`, `Remove`, tiến trình, log.

Quy tắc:

- `Apply` bị khóa khi còn Error phạm vi cài; `Remove` bị khóa khi còn Error phạm vi gỡ.
- **Ý định của checkbox:** dùng một checkbox chung. Chưa cài → vào kế hoạch Cài; đã cài → vào kế hoạch Gỡ; **cài dở → vào cả hai**, người dùng chọn nút Cài (hoàn tất) hoặc Gỡ (dọn phần đã cài). Để module vừa cài xong không tự chuyển sang kế hoạch Gỡ, Window **tự bỏ chọn tất cả khi một lần chạy kết thúc thành công**; nếu lỗi hoặc bị hủy thì giữ nguyên lựa chọn để thử lại.
- Kế hoạch đánh dấu bước đã có sẵn "(đã có sẵn, sẽ bỏ qua)" và liệt kê tài nguyên được giữ lại vì module khác còn dùng.
- Hộp xác nhận liệt kê các bước **và** toàn bộ Warning (mục 9.2).

---

## 12. Tích hợp với `MainBase` (tùy chọn)

Phần lõi không phụ thuộc `MainBase`. Với project có `MainBase` (đã có `ServiceLocator`, `EventBus`, assembly `CoreScript`), tool có thể bật module tùy chọn:

- Sinh wrapper `IAdsService`, `IAnalyticsService` theo mẫu `IService`.
- Wrapper đặt trong assembly riêng, dùng `#if` hoặc Version Defines để project vẫn compile khi chưa cài SDK.
- Sự kiện (xem quảng cáo, doanh thu…) đi qua `EventBus`.

---

## 13. Kiểm thử

- **Unit test** cho `CompatibilityEngine` và từng luật (context giả → kiểm tra report). Quan trọng nhất.
- **Test Planner:** thứ tự bước đúng đồ thị phụ thuộc.
- **Test idempotent:** chạy plan hai lần, lần hai không đổi gì.
- **Test gỡ:** cài rồi gỡ, project trở về trạng thái đầu (so `manifest.json`, define, Player Settings).
- **Test môi trường:** Dev → Release → Dev, mọi file và ID khớp profile; build Release bị chặn khi đang ở Dev.
- Thử trên project trống ở cả Android và iOS trước khi chạy trên project thật.
- Thử cài package vào một project thứ hai để kiểm tra khả năng tái sử dụng.

---

## 14. Lộ trình triển khai

Tiến độ thực tế: bản khung đã làm **trước một phần giai đoạn 1, 3, 4** (Window, Registry, luật Requires/Conflict/Platform, Planner, Runner, bước package/define, module demo) và **bỏ qua giai đoạn 2**. Các vấn đề luồng logic ở mục 17 đã được sửa; bước tiếp theo là quay lại giai đoạn 2 (vì `BuildInstallSteps` sẽ cần context môi trường).

| Giai đoạn | Nội dung | Kết quả |
|---|---|---|
| **0** | Chốt SDK Ads cụ thể, tên package, quy ước thư mục dữ liệu. Điền ma trận tương thích với phiên bản thật. | Danh sách phiên bản đã kiểm chứng. |
| **1** | Khung package, `ISdkModule`, `SdkRegistry`, Window khung, module mẫu. | Window liệt kê SDK, package cài được vào project khác. |
| **2** | `ProjectConfigStore`, `EnvironmentManager`, profile Dev/Release (chưa gắn SDK). | Chuyển môi trường và ghi file cấu hình. |
| **3** | `CompatibilityEngine` + luật Dependency, Conflict, Platform, Environment. | Báo lỗi trong Window. |
| **4** | `Planner`, `SetupRunner`, Step cơ bản, xử lý domain reload. | Cài/gỡ end-to-end module Firebase. |
| **5** | Module Ads + luật tài nguyên chung, thứ tự, quyền riêng tư; hợp nhất manifest/plist. | Ads + Firebase chạy cùng nhau trên Android và iOS. |
| **6** | Validator, Undo, chặn build sai môi trường, Preset. | Quy trình an toàn. |
| **U** | **Điểm dừng UPM**: dừng lại, hướng dẫn người dùng đóng gói thành package. Không tự làm. Xem `ToolSetupSDK_UPM_Checkpoint.md`. | ToolSDK cài được vào project thứ hai. |
| **7** | Wrapper theo `MainBase` (tùy chọn), test, tài liệu sử dụng, phát hành phiên bản đầu. | Bản dùng cho nhiều project. |

---

## 15. Câu hỏi còn mở

**Đã chốt:** Window dùng một checkbox chung và tự bỏ chọn sau khi chạy thành công (mục 11); tên package `ToolSDK`; namespace `Base.ToolSDK.<TênDịchVụ>`; cài được nhiều SDK Ads; Firebase chia thành nhiều dịch vụ con; tính năng nâng cao của Dev làm sau.

**Còn mở:**

1. **Danh sách SDK Ads cụ thể** cần có module ở bản đầu (LevelPlay, AppLovin MAX, AdMob…). Có thể bắt đầu bằng một SDK rồi thêm dần.
2. **Dịch vụ Firebase** làm trước (gợi ý: Core, Analytics, Crashlytics).
3. **Tên Firebase trong namespace:** dùng `Base.ToolSDK.Firebase` kèm quy ước `global::Firebase...` (mục 3.5), hay đặt `Base.ToolSDK.FirebaseService` để tránh trùng hẳn?
4. **Tiền tố UPM** cho `package.json`: `com.base.toolsdk` hay dùng tên công ty/studio?
5. Cách **phân phối**: Git URL hay scoped registry?
6. Dữ liệu cấu hình **commit lên git** hay giữ riêng tư (`.gitignore`)?
7. Nguồn lấy SDK: chỉ UPM, hay cần cả `.unitypackage` tải sẵn?
8. Có cần sinh **wrapper Runtime** (`IAdsProvider`, `IAnalyticsService`…) ngay từ đầu không?
9. **Cách nhận biết bản build Release** cho hook chặn build (mục 7.5): theo cờ `Development Build`, cờ riêng của tool, hay Build Profile?

---

## 16. Quyết định đã chốt

- Tool là **Editor Window** (UI Toolkit), mỗi SDK là một **module C#**.
- Tên package **ToolSDK**, namespace `Base.ToolSDK.<TênDịchVụ>`, mỗi dịch vụ một asmdef riêng.
- SDK ban đầu: **Ads** (cho phép nhiều SDK Ads, kiểm tra theo từng cặp) và **Firebase** (chia thành dịch vụ con); nền tảng: **Android và iOS**.
- Thêm SDK mới = thêm một module, tự được nhận bởi `SdkRegistry`; module có thể nằm ngoài package.
- **Hai môi trường** Dev/Release dựa trên profile, chuyển bằng một plan có thể xem trước và hoàn tác, có chặn build sai môi trường.
- Tool là **UPM package** dùng chung; dữ liệu cấu hình nằm trong project, không nằm trong package; phần lõi không phụ thuộc `MainBase`.
- Logic tương thích tách riêng thành **bộ luật** và **ma trận tương thích**.
- Mọi thao tác theo thứ tự **Plan → Run**, từng bước idempotent, sống qua domain reload.

---

## 17. Hiện trạng bản khung và vấn đề đã xử lý

> Rà soát ngày 2026-10-20 trên Unity `6000.3.5f2`. Trước khi sửa, project compile sạch và tool không gây lỗi hay cảnh báo nào, nhưng có 14 lỗi **luồng logic** chỉ lộ ra khi chạy đúng tình huống. Tất cả đã được sửa trong cùng đợt (17.2); sau khi sửa, project compile sạch, không lỗi và không cảnh báo mới.

### 17.1. Đã có

| Thành phần | File |
|---|---|
| `ISdkModule` (có `GetInstallState`), `SdkModuleBase`, `SdkRegistry` (TypeCache) | `Editor/Core/` |
| `DefineUtil` (Android, iOS, Standalone, có cache), `PackageUtil` (chỉ tính package trực tiếp, có cache, so phiên bản) | `Editor/Core/` |
| `CompatibilityEngine` + luật `1-requires` (gồm vòng phụ thuộc, gợi ý gỡ cha), `2-conflict`, `3-platform` | `Editor/Compatibility/` |
| `Planner` (mở rộng Requires, thứ tự cài/gỡ, giữ tài nguyên dùng chung khi gỡ), `SetupRunner` (SessionState + file `Library/`), bước package/define | `Editor/Execution/` |
| `SdkSetupWindow` (UI Toolkit, menu `Tools/ToolSDK/Setup SDK`) | `Editor/Window/` |
| Module demo: Mobile Notifications (package thật) + 5 module giả lập chỉ thêm define | `Editor/Modules/Demo/` |

Luồng chính trong code đúng thứ tự mục 4: Window gọi `Planner.ExpandInstall` trước, rồi mới dựng `CompatibilityContext` và chạy luật.

### 17.2. Các vấn đề đã sửa

| # | Mức | Vấn đề | Cách đã sửa | Vị trí |
|---|---|---|---|---|
| 1 | **Cao** | Gỡ module gỡ luôn package/define mà module khác vẫn dùng. | `Planner.BuildRemoveSteps(ordered, keep, out notes)` bỏ các bước gỡ mà module được giữ lại (`AfterRemove`) vẫn sở hữu, ghi chú vào kế hoạch. Đã thử: gỡ define dùng chung → 0 bước + ghi chú; không dùng chung → 1 bước. | `Planner` |
| 2 | **Cao** | Package gián tiếp bị coi là "đã cài"; bước gỡ treo tới khi quá hạn. | Chỉ tính package cài trực tiếp (`isDirectDependency`); `RemovePackageStep` xong khi không còn là dependency trực tiếp. | `PackageUtil`, `SetupSteps` |
| 3 | **Cao** | Module vừa cài xong vẫn tick nên tự chuyển sang kế hoạch Gỡ. | Window tự bỏ chọn khi một lần chạy kết thúc thành công; lỗi/hủy thì giữ lựa chọn. Cờ `_wasRunning` được serialize nên sống qua domain reload. | `SdkSetupWindow` |
| 4 | Trung bình | Module cài dở không gỡ được. | Thêm `SdkInstallState.Partial`. Module cài dở nằm ở cả kế hoạch Cài lẫn Gỡ; Window hiện nhãn "Cài dở". | `ISdkModule`, `SdkModuleBase`, `SdkSetupWindow` |
| 5 | Trung bình | Package lỗi đúng lúc reload chỉ lộ ra sau 5 phút. | Sau reload mất request: đợi 15 giây, nếu chưa xong thì gửi lại đúng 1 lần, lần nữa không xong thì báo lỗi rõ ràng. Request báo thành công mà project chưa phản ánh quá 20 giây cũng báo lỗi. | `PackageStep` |
| 6 | Trung bình | Thời gian compile bị tính vào thời gian chờ của bước. | Reset bộ đếm khi Unity đang compile/import và khi vừa reload. | `SetupRunner` |
| 7 | Trung bình | Warning không có trong hộp xác nhận. | Hộp xác nhận liệt kê Warning đúng phạm vi (Cài/Gỡ) trước danh sách bước. | `SdkSetupWindow` |
| 8 | Trung bình | Trạng thái runner mất khi tắt Editor. | Ghi thêm `Library/ToolSDK/SetupState.json`. Mở lại Unity: nạp trạng thái "bị gián đoạn", không tự chạy tiếp. | `SetupRunner` |
| 9 | Thấp | Vòng `Requires` không báo lỗi. | `RequiresRule` phát hiện vòng, báo Error phạm vi Cài. | `RequiresRule` |
| 10 | Thấp | `Begin()` chạy trước khi lưu `started`. | Lưu trạng thái trước rồi mới `Begin()`; lỗi trong `Begin()`/`Poll()` được bắt và dừng plan. `DefineStep.Poll` áp lại nếu chưa đạt. | `SetupRunner`, `SetupSteps` |
| 11 | Thấp | `@version` bị bỏ qua khi xét "đã cài". | `PackageUtil.IsInstalled` so cả phiên bản khi id có `@x.y.z` (không áp dụng cho URL git). Sai phiên bản → cài dở → bước thêm sẽ cập nhật. | `PackageUtil` |
| 12 | Thấp | Luật gỡ bị chặn bởi module sắp cài; luật xung đột bỏ sót module đang chờ gỡ. | Tách `AfterInstall` / `AfterRemove` trong `CompatibilityContext` (mục 9.1). Xung đột kiểm tra trên `AfterInstall` (module chờ gỡ vẫn còn lúc cài) và gợi ý "hãy gỡ nó trước". | `Compatibility`, `ConflictRule`, `RequiresRule` |
| 13 | Thấp | Window gọi lại trạng thái tốn kém mỗi 700 ms. | Cache 0,3 giây cho danh sách package và define; xóa cache khi có thay đổi (`Events.registeredPackages`, `DefineUtil.Add/Remove`). | `PackageUtil`, `DefineUtil` |
| 14 | Thấp | Gỡ con không gợi ý gỡ cha. | Info phạm vi Gỡ khi một module cha không còn ai cần. | `RequiresRule` |

### 17.3. Giới hạn còn lại

- `DefineUtil.IsDefined` yêu cầu define có ở **cả 3** target; thiếu ở một target → module ở trạng thái cài dở (đúng thiết kế).
- Module không có `PackageIds` lẫn `Defines` phải override `GetInstallState()`.
- Package cài bằng git URL chưa được nhận biết (tên không suy ra được từ id).
- Nút Hủy không hoàn tác request Package Manager đang chạy dở; project có thể ở trạng thái cài dở và Window hiện rõ điều đó.
- Chưa có `Undo()`, `Validate()`, `GetSettings()`, môi trường Dev/Release (giai đoạn 2, 6).
- Thư mục `Modules/Demo/` chỉ để thử tool, phải xóa hoặc chuyển vào `Samples~` trước giai đoạn U.
