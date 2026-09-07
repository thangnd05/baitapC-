# Tuần 1 + 2 + 3 — School API có JWT & RBAC (ASP.NET Core 10 · PostgreSQL · EF Core · Swagger)

Một project duy nhất, một database duy nhất (`week1_rbac`), gồm ba lớp chồng lên nhau:

| Tuần | Đóng góp vào project này |
|---|---|
| 1 | Danh tính & vai trò: `users` / `roles` / `permissions` + 2 bảng nối, hash password bằng PBKDF2 |
| 2 | Nghiệp vụ trường học: `programme` / `course` / `student`, CRUD qua DTO + Service |
| 3 | **Bảo vệ**: JWT bearer, role-based `[Authorize]`, owner policy chống BOLA, CORS allowlist, rate limit, security headers |

> Tuần 2 đã được **gộp vào project này** thay vì đứng riêng. Lý do: owner policy cần đọc
> `users.student_id` trỏ sang bảng `student` — hai bảng đó phải nằm cùng một database thì
> mới có khóa ngoại. Thư mục `Week2.School.Api/` giữ nguyên làm bản nộp tuần 2.

---

## 1. Kiến trúc — năm cửa kiểm soát

```
HTTP request  (Authorization: Bearer <JWT>)
   │
   ├─ 1. RateLimiter      → quá 5 lần login/phút?          → 429
   ├─ 2. Authentication   → token hợp lệ? còn hạn?          → 401
   ├─ 3. Authorization    → đúng vai trò?                   → 403
   ├─ 4. Owner policy     → đúng chủ sở hữu bản ghi?        → 403
   ▼
Controller ──► Service ──► AppDbContext (EF Core) ──► PostgreSQL
   │              │
   │              └── ServiceResult { Success | NotFound | Conflict | Unauthorized | Forbidden }
   └── 5. dịch ServiceResult sang mã HTTP, trả Response DTO
```

Tầng 3 trả lời *"bạn thuộc nhóm nào"*. Tầng 4 trả lời *"bạn được làm gì trên **bản ghi này**"*.
Thiếu tầng 4 là lỗ hổng phổ biến nhất trong các API thực tế (BOLA).

| Tầng | Trách nhiệm | Không được làm |
|---|---|---|
| `Controllers/` | Nhận request, dịch kết quả sang mã HTTP | Không truy vấn database, không chứa nghiệp vụ |
| `Authorization/` | Quy tắc quyền: vai trò + chủ sở hữu | Không rải rác trong controller |
| `Services/` | Nghiệp vụ, hash password, phát token | Không tham chiếu HTTP/`ActionResult` |
| `Data/`, `Models/` | Schema và truy cập dữ liệu | Không lộ ra ngoài API |
| `Contracts/` | DTO Request (vào) và Response (ra) | Không dùng entity làm request/response |

## 2. Yêu cầu môi trường

| Thành phần | Phiên bản dùng trong bài |
|---|---|
| .NET SDK | 10.0.x (`dotnet --version`) |
| PostgreSQL | 14+ (bài này chạy trên 18), lắng nghe **`localhost:5932`** |
| dotnet-ef | 10.0.11 (local tool trong `.config/dotnet-tools.json`) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.11 |

> Tài liệu gốc của trường dùng **MySQL + .NET 8 + JwtBearer 8.0.20**. Bài này dùng
> **PostgreSQL + .NET 10 + JwtBearer 10.0.11** cho đồng bộ với tuần 1 và 2.
> Toàn bộ khái niệm (JWT, policy, CORS, rate limit) giống hệt; chỉ khác provider và số phiên bản.

### Kiểm tra trước khi bắt đầu

**Windows — PowerShell**

```powershell
cd $HOME\source\repos\baitap\Week1.Rbac.Api
dotnet --list-sdks
git status
Test-NetConnection -ComputerName localhost -Port 5932
```

**macOS / Linux — Terminal**

```bash
cd ~/source/repos/baitap/Week1.Rbac.Api
dotnet --list-sdks
git status
nc -vz localhost 5932
```

## 3. Cấu hình secret

Khóa ký JWT và mật khẩu database **không bao giờ** nằm trong `appsettings.json`.
Có hai cách nạp, chọn một:

### Cách A — `.env` (đang dùng)

```bash
cp .env.example .env
```

```dotenv
DB_HOST=localhost
DB_PORT=5932
DB_NAME=week1_rbac
DB_USER=week1_app
DB_PASSWORD=week1_secret

Jwt__Issuer=SchoolApi
Jwt__Audience=SchoolApiClients
Jwt__SigningKey=<chuỗi ngẫu nhiên >= 32 ký tự>
Jwt__AccessTokenMinutes=30
Jwt__RefreshTokenDays=14

Seed__Password=Lab#2026_ChangeMe
Cors__AllowedOrigins=http://localhost:5173,http://localhost:3000
```

Dấu `__` (hai gạch dưới) trong biến môi trường tương đương dấu `:` trong configuration —
`Jwt__SigningKey` chính là `Jwt:SigningKey`.

### Cách B — User Secrets

```bash
dotnet user-secrets set "Jwt:Issuer"             "SchoolApi"
dotnet user-secrets set "Jwt:Audience"           "SchoolApiClients"
dotnet user-secrets set "Jwt:SigningKey"         "$(openssl rand -base64 48)"
dotnet user-secrets set "Jwt:AccessTokenMinutes" "30"
dotnet user-secrets set "Jwt:RefreshTokenDays"   "14"
dotnet user-secrets set "Seed:Password"          "Lab#2026_ChangeMe"
dotnet user-secrets list
```

### Danh sách key phải có

| Key | Bắt buộc | Ghi chú |
|---|---|---|
| `DB_HOST` `DB_PORT` `DB_NAME` `DB_USER` `DB_PASSWORD` | ✅ | Hoặc thay bằng `ConnectionStrings__Default` |
| `Jwt:SigningKey` | ✅ | **Tối thiểu 32 byte.** App **từ chối khởi động** nếu ngắn hơn, kèm thông báo số byte hiện tại |
| `Jwt:Issuer` `Jwt:Audience` | ⬜ | Mặc định `SchoolApi` / `SchoolApiClients`. Phải khớp giữa lúc phát và lúc validate |
| `Jwt:AccessTokenMinutes` | ⬜ | Mặc định 30. Access token **không thu hồi được** nên phải sống ngắn |
| `Jwt:RefreshTokenDays` | ⬜ | Mặc định 14. Refresh token sống dài nhưng dùng một lần và thu hồi được |
| `Seed:Password` | ✅ (Development) | Mật khẩu 3 tài khoản mẫu. Thiếu → app dừng với thông báo rõ |
| `Cors:AllowedOrigins` | ⬜ | Ngăn cách bằng dấu phẩy. Mặc định `localhost:5173,localhost:3000` |

Sinh khóa riêng, **không** dùng chung với bạn cùng nhóm, **không** đưa khóa vào tài liệu nộp:

```bash
openssl rand -base64 48
```

## 4. Chạy

```bash
cd baitap
dotnet tool restore

cd Week1.Rbac.Api
cp .env.example .env          # rồi điền Jwt__SigningKey
dotnet restore
dotnet ef database update     # 10 bảng + __EFMigrationsHistory
dotnet ef migrations list     # InitialRbac · SeedOrganizerRole · AddSchoolDomainAndStudentOwnership · AddRefreshToken

dotnet run                    # http://localhost:8080
```

Mọi lệnh `dotnet ef` và `dotnet run` phải chạy **trong thư mục `Week1.Rbac.Api`**,
vì `.env` nằm ở đó và `DotNetEnv` tìm ngược lên từ thư mục hiện tại.

Swagger UI: **http://localhost:8080/swagger** — có nút **Authorize** để dán Bearer token.

Nếu chưa có database:

```bash
docker run -d --name week1-postgres \
  -e POSTGRES_USER=week1_app \
  -e POSTGRES_PASSWORD=week1_secret \
  -e POSTGRES_DB=week1_rbac \
  -p 5932:5432 postgres:latest
```

## 5. Ba tài khoản mẫu

`Data/IdentitySeeder.cs` chạy một lần lúc khởi động, **chỉ ở môi trường Development**.
Nó tạo 3 vai trò, 3 tài khoản, và một ít dữ liệu trường học để có thể chứng minh owner policy.

| Email | Vai trò | `student_id` | Ghi chú |
|---|---|---|---|
| `admin@hnmu.edu.vn` | `Admin` | — | Toàn quyền, gồm xóa và quản lý vai trò |
| `staff@hnmu.edu.vn` | `Staff` | — | Đọc tất cả, sửa hồ sơ, không xóa được |
| `sv001@hnmu.edu.vn` | `Student` | **1** | Chỉ đọc/sửa được hồ sơ `SV001` |

Mật khẩu cả ba lấy từ `Seed:Password` — mặc định `Lab#2026_ChangeMe`.
**Chỉ dùng trên máy lab, không tái sử dụng ở nơi khác.**

Dữ liệu trường học được seed kèm: programme `SE`, course `INF067`/`INF012`,
và ba hồ sơ `SV001` / `SV002` / `SV003`. Cần ít nhất hai hồ sơ khác chủ sở hữu thì mới
kiểm thử được BOLA.

> Tài khoản `admin@example.edu.vn` từ tuần 1 vẫn còn trong database với vai trò `admin`
> (chữ thường). So sánh vai trò là **case-sensitive**, nên `admin` **không** khớp
> `RequireRole(AppRoles.Admin)`. Đây là dữ liệu cũ, vô hại — muốn dùng thì gán thêm
> vai trò `Admin` qua `PUT /api/users/{userId}/roles/{roleId}`.

Đăng nhập:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@hnmu.edu.vn","password":"Lab#2026_ChangeMe"}'
```

## 6. Bảng phân quyền

| Endpoint | Ẩn danh | Student | Staff | Admin |
|---|---|---|---|---|
| `POST /api/auth/login` | ✅ công khai | ✅ | ✅ | ✅ |
| `POST /api/auth/refresh` | ✅ công khai | ✅ | ✅ | ✅ |
| `POST /api/auth/logout` | ✅ công khai | ✅ | ✅ | ✅ |
| `GET /api/auth/me` | 401 | ✅ | ✅ | ✅ |
| `GET /api/students` | 401 | **403** | ✅ | ✅ |
| `GET /api/students/{id}` | 401 | chỉ hồ sơ của mình | ✅ | ✅ |
| `PUT /api/students/{id}` | 401 | chỉ hồ sơ của mình | ✅ | ✅ |
| `POST /api/students` | 401 | **403** | ✅ | ✅ |
| `DELETE /api/students/{id}` | 401 | **403** | **403** | ✅ |
| `GET /api/programmes`, `GET /api/courses` | 401 | ✅ | ✅ | ✅ |
| `POST`/`PUT` programmes, courses | 401 | **403** | ✅ | ✅ |
| `DELETE` programmes, courses | 401 | **403** | **403** | ✅ |
| `GET /api/programmes/{id}/students` | 401 | **403** | ✅ | ✅ |
| `/api/users`, `/api/roles`, `/api/permissions` | 401 | **403** | **403** | ✅ |

Mỗi ô **403** trong bảng là một đặc tả kiểm thử: phải có một kiểm thử âm chứng minh hệ thống
trả 403, không phải 200.

### Bảng trên được khai báo ở đâu

Controller **không** viết tên vai trò. Mỗi endpoint chỉ khai báo *quyền* nó cần bằng
`[Authorize(Policy = AppPolicies.X)]`; ánh xạ quyền → vai trò nằm đúng **một chỗ** là
`AddRbacAuthorization()` trong `Authorization/AppPolicies.cs`:

| Policy | Vai trò được cấp | Dùng ở |
|---|---|---|
| `ManageIdentity` | Admin | `UsersController`, `RolesController`, `PermissionsController` (mức class) |
| `WriteCatalog` | Staff, Admin | `POST`/`PUT` programmes, courses |
| `DeleteCatalog` | Admin | `DELETE` programmes, courses |
| `ManageStudentDirectory` | Staff, Admin | `GET`/`POST /api/students`, `GET /api/programmes/{id}/students` |
| `DeleteStudent` | Admin | `DELETE /api/students/{id}` |
| `CanReadStudent` / `CanEditStudent` | *resource-based* — xem [§8](#8-cách-chống-bola) | `GET`/`PUT /api/students/{id}` |

Mở quyền xóa course cho Staff = sửa **một dòng** `DeleteCatalog` ở file đó. Nếu rải
`[Authorize(Roles = "...")]` khắp controller thì phải đi sửa từng nơi và rất dễ sót một endpoint —
mà endpoint bị sót chính là lỗ hổng.

### Hợp đồng mã lỗi

| Status | Khi nào trả | Thông điệp cho client |
|---|---|---|
| `200` / `201` / `204` | Được phép và thực hiện xong | Dữ liệu hoặc không body |
| `400` | Sai validation | Chi tiết trường lỗi |
| `401` | Thiếu token, token sai định dạng hoặc đã hết hạn | Chưa xác thực; không nói rõ vì sao |
| `403` | Đã xác thực nhưng thiếu vai trò hoặc không phải chủ sở hữu | Không đủ quyền cho tài nguyên này |
| `404` | Bản ghi không tồn tại | Không tìm thấy |
| `409` | Trùng mã/email | Nói rõ giá trị bị trùng |
| `429` | Vượt giới hạn số lần gọi | Thử lại sau, kèm header `Retry-After` |

## 7. Vòng đời token — access + refresh

Login trả về **cặp** token, hai thứ có tính chất ngược nhau:

| | Access token | Refresh token |
|---|---|---|
| Là gì | JWT có chữ ký, mang claim `role` / `student_id` | Chuỗi ngẫu nhiên 32 byte, **không** mang thông tin gì |
| Sống bao lâu | 30 phút (`Jwt:AccessTokenMinutes`) | 14 ngày (`Jwt:RefreshTokenDays`) |
| Gửi kèm ở đâu | Header `Authorization: Bearer ...` mọi request | Chỉ trong body của `POST /api/auth/refresh` |
| Server có lưu không | **Không** — xác thực bằng chữ ký, stateless | **Có** — bảng `refresh_token`, nhưng chỉ lưu **SHA-256** |
| Thu hồi được không | **Không** — đây là lý do nó phải sống ngắn | **Có** — set `revoked_at` |
| Dùng được mấy lần | Nhiều lần đến khi hết hạn | **Đúng một lần** (rotation) |

### Vì sao database chỉ lưu băm

`refresh_token.token_hash` chứa SHA-256 của token, không phải token thật. Nếu database bị đọc
trộm, kẻ tấn công cầm được hash cũng không gọi được `/api/auth/refresh`. Ở đây SHA-256 trần là
đủ — token có 256 bit entropy nên không thể dò ngược như mật khẩu, và không cần PBKDF2 (refresh
chạy mỗi 30 phút, phải nhanh).

### Rotation và phát hiện replay

```
login          →  A1 + R1                      (R1 active)
refresh(R1)    →  A2 + R2                      (R1 revoked='rotated', replaced_by=R2)
refresh(R1)    →  401  ← R1 ĐÃ BỊ THU HỒI MÀ VẪN CÓ NGƯỜI DÙNG
                       ⇒ hủy TOÀN BỘ token của user (R2 cũng chết, ='replay_detected')
refresh(R2)    →  401
```

Mỗi lần refresh, token cũ bị thu hồi **ngay trong cùng giao dịch** và token mới được nối vào
`replaced_by_token_id`. Nếu một token đã thu hồi lại được dùng lần nữa, chỉ có hai khả năng:
client giữ lại bản cũ, hoặc token đã bị đánh cắp và kẻ tấn công đang dùng. Server không phân
biệt được, nên xử lý theo hướng an toàn nhất — **hủy hết phiên của tài khoản đó** và ghi log
cảnh báo, buộc người dùng đăng nhập lại.

### Ba endpoint

| Endpoint | Quyền | Kết quả |
|---|---|---|
| `POST /api/auth/login` | công khai, rate limit | 200 kèm cặp token · 401 nếu sai |
| `POST /api/auth/refresh` | công khai, rate limit | 200 kèm cặp **mới** · 401 nếu sai/hết hạn/replay |
| `POST /api/auth/logout` | công khai | **204 luôn luôn** — kể cả token không tồn tại |

`/api/auth/refresh` phải là `AllowAnonymous`: client gọi nó **đúng lúc** access token đã hết hạn,
nên không thể yêu cầu access token hợp lệ. Bảo vệ nó bằng chính giá trị bí mật của refresh token
cộng rate limit.

`/api/auth/logout` luôn trả 204 kể cả khi token không tồn tại — trả lời khác nhau sẽ cho biết
token nào có thật trong hệ thống.

> **Đánh đổi đã biết:** refresh token nằm trong response body, nghĩa là JavaScript đọc được và
> có thể bị lấy qua XSS. Web thật nên đặt nó vào cookie `HttpOnly; Secure; SameSite=Strict`.
> Bài này trả trong body để kiểm thử được bằng Postman và file `.http`.

## 8. Cách chống BOLA

Role check một mình **không** ngăn được một sinh viên đã đăng nhập gọi
`PUT /api/students/{id}` với id của người khác. Lỗ hổng đó tên là **BOLA** —
Broken Object Level Authorization, đứng số 1 trong OWASP API Security Top 10 2023.

Cơ chế trong project này:

1. `users.student_id` (bigint, FK sang `student`, `ON DELETE SET NULL`) ghi nhận
   tài khoản nào sở hữu hồ sơ nào.
2. Lúc đăng nhập, `TokenService` nhét giá trị đó vào token dưới claim `student_id`.
3. `StudentOwnerHandler` so claim đó với id tài nguyên đang bị gọi:

```csharp
if (context.User.IsInRole(AppRoles.Staff) || context.User.IsInRole(AppRoles.Admin))
{
    context.Succeed(requirement);   // Staff/Admin thao tác trên mọi hồ sơ
    return Task.CompletedTask;
}

var claim = context.User.FindFirst(TokenService.StudentIdClaim)?.Value;
if (long.TryParse(claim, out var owned) && owned == studentId)
{
    context.Succeed(requirement);   // đúng chủ sở hữu
}
// không gọi Succeed nghĩa là từ chối
```

4. Controller hỏi policy **trước khi** chạm vào service:

```csharp
var check = await authorization.AuthorizeAsync(User, id, AppPolicies.CanEditStudent);
if (!check.Succeeded) return Forbid();   // 403, không phải 401
```

Quy tắc quyền nằm gọn trong `Authorization/StudentOwnerHandler.cs`, không rải rác trong controller.

## 9. Hardening

| Biện pháp | Cấu hình | Chứng minh |
|---|---|---|
| CORS allowlist | `WithOrigins(...)` — origin cụ thể, **không** `AllowAnyOrigin` | Origin lạ → không có header `Access-Control-Allow-Origin` |
| Rate limit login | Fixed window 1 phút, 5 request, **phân hoạch theo IP** | Lần thứ 6 → `429` + `Retry-After: 60` |
| Security headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` | Xem response header bất kỳ endpoint nào |
| Thông báo lỗi mơ hồ | Cùng một message cho "email không tồn tại" và "mật khẩu sai" | Ca D1 và D2 trong file `.http` trả về giống hệt nhau |
| Chốt thuật toán ký | `ValidAlgorithms = [HmacSha256]` | Token ký bằng thuật toán khác → 401 |
| ClockSkew tường minh | 30 giây thay vì mặc định 5 phút | Token hết hạn bị từ chối gần như ngay lập tức |

Phân hoạch rate limit theo IP là bắt buộc: nếu không phân hoạch, mọi client dùng chung
một hạn mức, một người spam là cả lớp bị chặn.

**CORS không phải cơ chế bảo mật API.** Nó là chính sách của *trình duyệt* — `curl` và Postman
bỏ qua hoàn toàn. CORS rộng không tạo lỗ hổng quyền, nhưng cũng không bao giờ thay được
authorization.

## 10. Kiểm thử âm — bằng chứng bắt buộc

Kiểm thử dương chứng minh tính năng chạy. Chỉ **kiểm thử âm** chứng minh hệ thống biết từ chối.
Bốn ca dưới đây nằm ở mục B của `Week1.Rbac.Api.http`:

| Ca | Điều kiện | Kỳ vọng | Kết quả thực tế | Rủi ro nếu sai |
|---|---|---|---|---|
| B1 | Không header `Authorization` | 401 | ✅ 401 | API mở hoàn toàn |
| B2 | Token đã hết hạn | 401 | ✅ 401 | Phiên không bao giờ kết thúc |
| B3 | Student gọi `DELETE` | 403 | ✅ 403 | Leo thang đặc quyền |
| B4 | Student sửa hồ sơ người khác | 403 | ✅ 403 | BOLA, rò dữ liệu cá nhân |

### Tái hiện ca B2 (token hết hạn) một cách tất định

Không cần ngồi đợi 30 phút — phát token đã hết hạn sẵn bằng cách truyền tham số dòng lệnh
(command line có độ ưu tiên cao hơn `.env`):

```bash
dotnet run --no-launch-profile --urls http://localhost:5081 -- --Jwt:AccessTokenMinutes=-1
```

Đăng nhập trên cổng 5081 rồi dùng token đó gọi bất kỳ endpoint nào — nhận 401 kèm
`WWW-Authenticate: Bearer error="invalid_token", error_description="The token lifetime is invalid..."`.

### Chạy nhanh toàn bộ

Mở `Week1.Rbac.Api.http` bằng extension **REST Client** của VS Code, chạy lần lượt mục A
(đăng nhập, token tự chuyền sang các request sau) rồi mục B, C, D.

> Khi chụp ảnh minh chứng nộp bài: **xóa/che token thật** khỏi ảnh.

## 11. Mô hình dữ liệu

```
users ──< user_roles >── roles ──< role_permissions >── permissions
  │
  └── student_id ──► student ──► programme          course (độc lập)
```

| Bảng | Khóa chính | Ràng buộc đáng chú ý |
|---|---|---|
| `users` | `id` (uuid) | unique `ix_users_email`; **`student_id`** FK → `student`, `ON DELETE SET NULL` |
| `roles` | `id` (uuid) | unique `ix_roles_name` |
| `permissions` | `id` (uuid) | unique `ix_permissions_code` |
| `user_roles` | (user_id, role_id) | FK cascade về `users` và `roles` |
| `role_permissions` | (role_id, permission_id) | FK cascade về `roles` và `permissions` |
| `programme` | `programme_id` (bigint) | unique `ix_programme_code` |
| `course` | `course_id` (bigint) | unique `ix_course_code` |
| `student` | `student_id` (bigint) | unique `ix_student_code`, `ix_student_email`; FK → `programme` `RESTRICT` |

`ON DELETE SET NULL` trên `users.student_id`: xóa hồ sơ student thì tài khoản vẫn còn
nhưng mất quyền sở hữu — không kéo theo mất tài khoản.

## 12. Danh sách endpoint

| Nhóm | Method + route | Quyền |
|---|---|---|
| Auth | `POST /api/auth/login` | công khai, có rate limit |
| Auth | `POST /api/auth/refresh` | công khai, có rate limit — xoay vòng token |
| Auth | `POST /api/auth/logout` | công khai — thu hồi refresh token |
| Auth | `GET /api/auth/me` | mọi vai trò đã đăng nhập |
| Students | `GET /api/students` · `POST /api/students` | Staff, Admin |
| Students | `GET`/`PUT` `/api/students/{id}` | owner policy |
| Students | `DELETE /api/students/{id}` | Admin |
| Programmes | `GET /api/programmes` · `GET /api/programmes/{id}` | mọi vai trò |
| Programmes | `POST`/`PUT` · `GET /{id}/students` | Staff, Admin |
| Programmes | `DELETE /api/programmes/{id}` | Admin |
| Courses | `GET` | mọi vai trò · `POST`/`PUT` Staff+Admin · `DELETE` Admin |
| Users / Roles / Permissions | toàn bộ CRUD + gán quan hệ | Admin |

## 13. Tự giải thích trước khi nộp

**Vì sao role check một mình không chặn được BOLA?**
Vai trò là thuộc tính của *người gọi*, không phải của *bản ghi*. Một policy thuần vai trò
cho phép mọi Student đi qua, kể cả khi id trên URL thuộc về Student khác. Phải so danh tính
người gọi với chủ sở hữu bản ghi thì mới chặn được.

**Claim role trong token khác gì bảng `user_role`? Khi nào lệch nhau?**
Bảng là nguồn sự thật; claim là bản chụp tại thời điểm đăng nhập. Chúng lệch nhau ngay khi
Admin gán/gỡ vai trò cho một người đang cầm token còn hạn — token cũ vẫn mang vai trò cũ đến
khi hết hạn. Đánh đổi: đọc claim thì khỏi truy vấn DB mỗi request; rút ngắn `exp` để giảm cửa sổ lệch.

**Khi nào 403, khi nào 404?**
403 = "có tồn tại nhưng bạn không được đụng". 404 = "không có". Bài này chọn 403 cho tài nguyên
tồn tại nhưng không thuộc quyền, để thông báo lỗi dạy được. Đánh đổi: chính 403 xác nhận bản ghi
đó *có tồn tại* — hệ thống thật xử lý dữ liệu nhạy cảm nên cân nhắc trả 404 cho cả hai trường hợp.

**Nếu khóa ký JWT bị lộ, làm gì và theo thứ tự nào?**
Xem mục cuối `THREAT-MODEL.md`. Lưu ý: đổi khóa ký làm chết mọi **access** token đang lưu hành,
nhưng refresh token thì không - nó không ký bằng khóa đó. Muốn cắt sạch phải `update refresh_token
set revoked_at = now()` nữa.

**CORS và authorization khác nhau ở chỗ nào?**
CORS chỉ nói cho *trình duyệt* biết trang web nào được phép đọc response. Nó không kiểm tra danh
tính, không chạy với `curl`/Postman. Authorization chạy ở server cho **mọi** client. CORS rộng
không tạo lỗ hổng quyền, nhưng không bao giờ thay được authorization.

## 14. Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Mọi endpoint trả 401 dù token đúng | `UseAuthorization` đặt trước `UseAuthentication` | Đổi lại thứ tự trong `Program.cs` |
| 401 với thông điệp về signature | Khóa ký lúc phát khác lúc validate | `dotnet user-secrets list` / kiểm tra `.env`; khóa phải ≥ 32 byte |
| 401 dù vừa đăng nhập xong | Sai `Issuer`/`Audience` giữa lúc phát và lúc validate | So khớp `Jwt:Issuer`, `Jwt:Audience` ở cả hai nơi |
| App không khởi động, báo thiếu `Jwt:SigningKey` | Chưa đặt khóa | Thêm `Jwt__SigningKey` vào `.env` |
| App báo khóa chỉ dài N byte | Khóa < 32 byte | `openssl rand -base64 48` |
| Student nhận 401 thay vì 403 | Token không có claim role | Kiểm tra `Include(UserRoles).ThenInclude(Role)` trong `AuthService` |
| Endpoint trả 403 cho cả Admin | Handler không `Succeed` cho Staff/Admin | Bổ sung nhánh `IsInRole` trong `StudentOwnerHandler` |
| `Forbid()` trả 401 | Không có authentication scheme mặc định | `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` |
| Rate limit không chặn | Thiếu `app.UseRateLimiter()` hoặc thiếu attribute | Thêm middleware và `[EnableRateLimiting("login")]` |
| `CS1061: không có AddPolicy` | Thiếu `using Microsoft.AspNetCore.RateLimiting` | `using System.Threading.RateLimiting` một mình không đủ |
| `CS0234: Microsoft.OpenApi.Models không tồn tại` | Swashbuckle 10 dùng Microsoft.OpenApi **v2** | Đổi thành `using Microsoft.OpenApi`; reference dùng `new OpenApiSecuritySchemeReference("Bearer")`; `AddSecurityRequirement` nhận **lambda** |
| Browser báo lỗi CORS sau khi thêm auth | `Authorization` chưa nằm trong `WithHeaders` | Bổ sung `"Authorization"` vào allowlist |
| `Thiếu Seed:Password` | Chưa đặt biến seed | Thêm `Seed__Password` vào `.env` |
| `Connection refused` port 5932 | Container chưa chạy | `docker start week1-postgres` |
| `relation ... does not exist` | Chưa chạy migration | `dotnet ef database update` |

## 15. Cấu trúc thư mục

```
Week1.Rbac.Api/
├── .env                        secret, KHÔNG commit
├── .env.example                mẫu, có commit
├── README.md                   file này
├── THREAT-MODEL.md             threat model v1 (10 dòng THR-xx)
├── Week1.Rbac.Api.http         chuỗi kiểm thử: A đăng nhập · B 4 ca âm · C dương · D hardening
├── Authorization/
│   ├── AppRoles.cs             hằng số Admin/Staff/Student (chỉ dùng để seed + khai báo policy)
│   ├── AppPolicies.cs          tên policy + AddRbacAuthorization(): NƠI DUY NHẤT ánh xạ quyền → vai trò
│   └── StudentOwnerHandler.cs  requirement + handler chống BOLA
├── Contracts/
│   ├── Requests/               AuthRequests, User/Role/Permission, Student/Programme/Course
│   └── Responses/              AuthResponses (LoginResponse, MeResponse) + các DTO khác
├── Controllers/
│   ├── ApiControllerBase.cs    dịch ServiceStatus → HTTP + khai báo mã lỗi dùng chung (400/401/403/500)
│   ├── AuthController.cs       login · refresh · logout (AllowAnonymous + rate limit) · me
│   ├── StudentsController.cs   role check + owner policy
│   ├── ProgrammesController.cs · CoursesController.cs
│   └── UsersController.cs · RolesController.cs · PermissionsController.cs   policy ManageIdentity
├── Data/
│   ├── AppDbContext.cs         RBAC + School trong một context
│   └── IdentitySeeder.cs       3 vai trò, 3 tài khoản, dữ liệu student mẫu
├── Infrastructure/
│   ├── ApiExceptionHandler.cs  ProblemDetails cho lỗi 500
│   └── SecurityPolicies.cs     tên policy CORS/rate limit + middleware security headers
├── Migrations/                 InitialRbac · SeedOrganizerRole · AddSchoolDomainAndStudentOwnership · AddRefreshToken
├── Models/                     User(+StudentId), Role, Permission, UserRole, RolePermission, RefreshToken,
│                               Programme, Course, Student
├── Services/
│   ├── AuthService.cs          xác thực, phát cặp token, rotation, phát hiện replay
│   ├── PasswordService.cs      PBKDF2 qua PasswordHasher<User>
│   ├── TokenService.cs         JwtOptions + JWT (iss/aud/exp/role/student_id) + sinh refresh token
│   └── ... các service nghiệp vụ
└── Program.cs                  DI, JWT bearer, policy, CORS, rate limit, thứ tự middleware
```

## 16. Checklist tự đánh giá

- [x] `appsettings.json` không chứa khóa ký, mật khẩu hay connection string thật
- [x] Đăng nhập sai trả đúng một thông điệp chung, không phân biệt email sai hay mật khẩu sai
- [x] Token có `iss`, `aud`, `exp` và claim vai trò; `ClockSkew` đặt tường minh (30 giây)
- [x] Có refresh token: rotation một lần dùng, phát hiện replay, thu hồi được, DB chỉ lưu băm
- [x] Controller mặc định `[Authorize]`; chỉ `login` được `AllowAnonymous`
- [x] `DELETE` chỉ Admin gọi được; kiểm thử âm chứng minh Staff nhận 403
- [x] Sinh viên chỉ sửa được hồ sơ của mình; có kiểm thử âm cho BOLA
- [x] Quy tắc quyền nằm trong handler/policy, không rải rác trong controller
- [x] CORS chỉ liệt kê origin cụ thể; rate limit áp cho endpoint đăng nhập
- [x] README ghi cách chạy, tên các key secret và ba tài khoản mẫu
- [x] Threat model có 10 dòng, mỗi dòng có mitigation tương ứng

## 17. Chưa làm (phần tự học mở rộng)

* **Refresh token qua cookie HttpOnly** thay vì response body, để chống XSS đánh cắp token.
* **Dọn token hết hạn**: job định kỳ xóa dòng `refresh_token` đã quá hạn hoặc đã thu hồi lâu.
* **Thu hồi access token**: hiện access token đã phát ra không hủy được trước khi hết hạn.
* **OIDC**: thay auth nội bộ bằng Keycloak, so sánh authorization code + PKCE với luồng hiện tại.
* **Quét lỗ hổng**: OWASP ZAP baseline trên localhost, chọn một finding để khắc phục.
* **Ghi log an toàn**: thêm correlation id, ghi user id vào log — tuyệt đối không ghi token hay mật khẩu.
* **Enforce permission**: bảng `permissions` của tuần 1 hiện chưa được dùng làm guard;
  bước tiếp theo là đổi từ role-based sang permission-based policy.
