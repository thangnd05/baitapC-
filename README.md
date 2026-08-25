# Tuần 1 — RBAC Backend (ASP.NET Core 10 · PostgreSQL · EF Core · Swagger)

Web API quản lý **user / role / permission** với hai bảng nối many-to-many.
Chạy trực tiếp bằng `dotnet run`.

> **Cảnh báo an toàn:** tuần 1 cố ý để mọi endpoint ở trạng thái *anonymous*
> (chưa có đăng nhập, JWT, `[Authorize]`, kiểm tra permission). **Không deploy production.**

---

## 1. Kiến trúc

```
HTTP request
   │
   ▼
Controller ──► Service (nghiệp vụ) ──► AppDbContext (EF Core) ──► PostgreSQL
   │              │
   │              └── trả ServiceResult { Success | NotFound | Conflict }
   └── dịch ServiceResult sang mã HTTP, trả Response DTO
```

| Tầng | Trách nhiệm | Không được làm |
|---|---|---|
| `Controllers/` | Nhận request, dịch kết quả sang 200/201/204/400/404/409 | Không truy vấn database, không chứa nghiệp vụ |
| `Services/` | Chuẩn hoá dữ liệu, kiểm tra trùng, hash password, gán/gỡ quan hệ, map sang DTO | Không tham chiếu HTTP/`ActionResult` |
| `Data/`, `Models/` | Schema và truy cập dữ liệu | Không lộ ra ngoài API |
| `Contracts/` | DTO Request (vào) và Response (ra) | Không dùng entity làm request/response |

Controller không còn tham chiếu `AppDbContext` — chỉ phụ thuộc `IUserService`,
`IRoleService`, `IPermissionService` (đăng ký `AddScoped` trong `Program.cs`).
`ServiceStatus` được dịch sang mã HTTP tại một chỗ duy nhất: `Controllers/ApiControllerBase.cs`.

## 2. Yêu cầu môi trường

| Thành phần | Phiên bản dùng trong bài |
|---|---|
| .NET SDK | 10.0.x (`dotnet --version`) |
| PostgreSQL | 14+ (bài này chạy trên 18), lắng nghe **`localhost:5932`** |
| dotnet-ef | 10.0.10 (local tool trong `.config/dotnet-tools.json`) |

## 3. Cấu hình database bằng biến môi trường

Secret **không** nằm trong `appsettings.json`. Connection string được ghép từ `.env`:

```bash
cp .env.example .env    # rồi sửa giá trị nếu cần
```

```dotenv
DB_HOST=localhost
DB_PORT=5932
DB_NAME=week1_rbac
DB_USER=week1_app
DB_PASSWORD=week1_secret
```

Thứ tự ưu tiên khi khởi động (`ResolveConnectionString` trong `Program.cs`):

1. `ConnectionStrings__Default` — một biến chứa nguyên chuỗi kết nối
2. Ghép từ `DB_HOST` / `DB_PORT` / `DB_NAME` / `DB_USER` / `DB_PASSWORD`

Thiếu biến bắt buộc → app dừng ngay với thông báo tên biến đang thiếu, thay vì lỗi 500 lúc chạy.
`.env` đã nằm trong `.gitignore`; chỉ commit `.env.example`.

Vì `.env` được nạp trước `CreateBuilder`, `dotnet ef` cũng dùng đúng cấu hình này —
không cần `DesignTimeDbContextFactory` riêng.

## 4. Tạo database

**Cách A — Docker (đang dùng, port 5932):**

```bash
docker run -d --name week1-postgres \
  -e POSTGRES_USER=week1_app \
  -e POSTGRES_PASSWORD=week1_secret \
  -e POSTGRES_DB=week1_rbac \
  -p 5932:5432 postgres:latest
```

**Cách B — PostgreSQL cài sẵn trên máy:** chạy bằng tài khoản quản trị (pgAdmin → Query Tool),
rồi sửa `DB_PORT` trong `.env` cho khớp port thật của server.

```sql
CREATE USER week1_app WITH PASSWORD 'week1_secret';
CREATE DATABASE week1_rbac OWNER week1_app;
```

Kiểm tra kết nối trước khi code:

```bash
psql -h localhost -p 5932 -U week1_app -d week1_rbac -c "select current_user, current_database()"
```

## 5. Restore, migration và chạy

```bash
cd week1-rbac
dotnet tool restore                       # cài dotnet-ef 10.0.10
dotnet restore Week1.Rbac.Api/Week1.Rbac.Api.csproj

cd Week1.Rbac.Api
dotnet ef database update                 # tạo 5 bảng + __EFMigrationsHistory
dotnet ef migrations list

dotnet run                                # http://localhost:5080
```

Swagger UI: **http://localhost:5080/swagger** (mở `/` cũng tự chuyển sang `/swagger`).
Swagger đọc cả XML comment của controller nên mỗi endpoint có mô tả tiếng Việt và
danh sách mã HTTP khai báo qua `[ProducesResponseType]`.

## 6. Mô hình dữ liệu

```
users ──< user_roles >── roles ──< role_permissions >── permissions
```

| Bảng | Khóa chính | Ràng buộc đáng chú ý |
|---|---|---|
| `users` | `id` (uuid) | unique `ix_users_email` |
| `roles` | `id` (uuid) | unique `ix_roles_name` |
| `permissions` | `id` (uuid) | unique `ix_permissions_code` |
| `user_roles` | **(user_id, role_id)** | FK cascade về `users` và `roles` |
| `role_permissions` | **(role_id, permission_id)** | FK cascade về `roles` và `permissions` |

Khóa chính kép ngăn gán trùng; cascade chỉ xóa **bản ghi nối**, không xóa lây role/permission.
User **không** lưu chuỗi permission trực tiếp — quyền luôn đi qua role.

## 7. Danh sách endpoint

| Nhóm | Method + route | Kết quả |
|---|---|---|
| Users | `GET /api/users` · `GET /api/users/{id}` | Danh sách/chi tiết user kèm role |
| Users | `POST /api/users` · `PUT /api/users/{id}` · `DELETE /api/users/{id}` | Tạo/sửa/xóa; không trả `PasswordHash` |
| Roles | `GET/POST /api/roles` · `GET/PUT/DELETE /api/roles/{id}` | CRUD role + đọc permissions |
| Permissions | `GET/POST /api/permissions` · `GET/PUT/DELETE /api/permissions/{id}` | CRUD permission |
| Role ↔ Permission | `PUT`/`DELETE` `/api/roles/{roleId}/permissions/{permissionId}` | Gán/gỡ permission khỏi role |
| User ↔ Role | `PUT`/`DELETE` `/api/users/{userId}/roles/{roleId}` | Gán/gỡ role khỏi user |
| Mở rộng | `GET /api/users/{id}/permissions` | Hợp quyền từ mọi role (chưa dùng làm guard) |

Mã HTTP: `200` đọc/sửa · `201` tạo · `204` gán/gỡ/xóa · `400` validation ·
`404` không tìm thấy · `409` trùng email / role name / permission code.

## 8. Chuỗi kiểm thử trên Swagger (làm đúng thứ tự)

1. `POST /api/permissions` → `{ "code": "user.read", "description": "Xem người dùng" }` → **201**
2. `POST /api/permissions` → `{ "code": "user.create", "description": "Tạo người dùng" }` → **201**
3. `POST /api/roles` → `{ "name": "admin", "description": "Quản trị hệ thống" }` → **201**
4. `PUT /api/roles/{roleId}/permissions/{permissionId}` cho từng permission → **204**
5. `POST /api/users` → `{ "email": "admin@example.edu.vn", "displayName": "Campus Admin", "password": "P@ssword123" }` → **201**
6. `PUT /api/users/{userId}/roles/{roleId}` → **204**
7. Đọc lại:

```jsonc
// GET /api/users/{id}
{ "email": "admin@example.edu.vn", "displayName": "Campus Admin", "isActive": true, "roles": ["admin"] }

// GET /api/roles/{id}
{ "name": "admin", "permissions": ["user.create", "user.read"] }
```

## 9. Bảo mật password ở tuần 1

* Password được hash bằng `PasswordHasher<User>` (PBKDF2, định dạng ASP.NET Identity v3)
  ngay trong `UserService.CreateAsync` — plain-text không rời khỏi tầng service.
* Cột `users.password_hash` có dữ liệu trong database, nhưng **không có DTO nào chứa nó** —
  `UserResponse` cố ý không khai báo trường password, nên không thể lộ qua JSON.

## 10. Lỗi thường gặp

| Triệu chứng | Cách xử lý |
|---|---|
| `Thiếu biến môi trường 'DB_...'` | Chưa có `.env` — chạy `cp .env.example .env` |
| `password authentication failed` | Sai user/password trong `.env` |
| `database does not exist` | Tạo lại `week1_rbac` với `OWNER week1_app` |
| `Connection refused` port 5932 | Container chưa chạy: `docker start week1-postgres` |
| `dotnet ef not found` | Đứng ở thư mục có `.config/` rồi `dotnet tool restore` |
| `relation ... does not exist` | Chưa chạy `dotnet ef database update` |
| `409 Conflict` | Email/role name/permission code bị trùng |
| `validation metadata ... will be ignored` | Record DTO đang dùng `[property: Required]` — phải bỏ tiền tố `property:` |

## 11. Cấu trúc thư mục

```
week1-rbac/
├── .config/dotnet-tools.json
├── .env                     # secret, KHÔNG commit
├── .env.example             # mẫu, có commit
├── README.md
└── Week1.Rbac.Api/
    ├── Contracts/
    │   ├── Requests/        # CreateUserRequest, UpdateRoleRequest, ...
    │   └── Responses/       # UserResponse, RoleResponse, PermissionResponse
    ├── Controllers/         # mỏng: ApiControllerBase + Users/Roles/Permissions
    ├── Services/            # nghiệp vụ + ServiceResult
    ├── Data/AppDbContext.cs
    ├── Migrations/          # InitialRbac
    ├── Models/              # User, Role, Permission, UserRole, RolePermission
    ├── Program.cs           # nạp .env, DI, Swagger
    ├── appsettings.json
    └── appsettings.Development.json
```

## 12. Tuần 2

Bổ sung đăng nhập, xác thực password hash, phát JWT và chuyển permission thành
authorization policy. Database, service và CRUD của tuần 1 được giữ nguyên.
