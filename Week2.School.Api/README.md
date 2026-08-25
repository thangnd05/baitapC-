# Tuần 2 — School API (ASP.NET Core 10 · PostgreSQL · EF Core · Swagger)

CRUD cho ba tài nguyên **programme / course / student**, tách rõ Controller → Service → EF Core.
Chạy trực tiếp bằng `dotnet run`, không cần Docker cho API.

> **Khác với tài liệu gốc:** tài liệu dùng **MySQL + Pomelo + .NET 8**.
> Bài này dùng **PostgreSQL + Npgsql + .NET 10** cho đồng bộ với tuần 1.
> Chi tiết những chỗ phải đổi nằm ở mục 8.

---

## 1. Kiến trúc

```
HTTP request
   │
   ▼
Controller ──► Service (nghiệp vụ) ──► SchoolDbContext (EF Core) ──► PostgreSQL
   │              │
   │              └── trả ServiceResult { Success | NotFound | Conflict }
   └── dịch ServiceResult sang mã HTTP, trả Response DTO
```

| Tầng | Trách nhiệm | Không được làm |
|---|---|---|
| `Controllers/` | Nhận request, dịch kết quả sang 200/201/204/400/404/409 | Không inject `SchoolDbContext`, không chứa nghiệp vụ |
| `Services/` | Chuẩn hoá dữ liệu, kiểm tra trùng, kiểm tra FK, map sang DTO | Không tham chiếu HTTP/`ActionResult` |
| `Data/`, `Models/` | Schema và truy cập dữ liệu | Không lộ entity ra ngoài API |
| `Contracts/` | DTO Request (vào) và Response (ra) | Không dùng entity làm request/response |

Lỗi nghiệp vụ đi qua `ServiceResult`, **không** ném exception. `Infrastructure/ApiExceptionHandler.cs`
chỉ bắt lỗi ngoài dự kiến và trả `ProblemDetails` 500 — không map `InvalidOperationException`
thành 409, vì EF Core ném đúng loại exception đó cho nhiều lỗi nội bộ và sẽ che mất bug thật.

## 2. Yêu cầu môi trường

| Thành phần | Phiên bản dùng trong bài |
|---|---|
| .NET SDK | 10.0.x (`dotnet --version`) |
| PostgreSQL | 14+, lắng nghe **`localhost:5932`** |
| dotnet-ef | 10.0.11 (local tool ở `.config/dotnet-tools.json` tại gốc repo) |

## 3. Tạo database riêng cho tuần 2

Tuần 2 **không** dùng chung database với tuần 1. Chạy bằng tài khoản quản trị PostgreSQL:

```sql
CREATE USER week2_app WITH PASSWORD 'week2_secret';
CREATE DATABASE week2_school OWNER week2_app;
```

Nếu đang dùng container Postgres của tuần 1:

```bash
docker exec week1-postgres psql -U week1_app -d postgres \
  -c "CREATE USER week2_app WITH PASSWORD 'week2_secret';"
docker exec week1-postgres psql -U week1_app -d postgres \
  -c "CREATE DATABASE week2_school OWNER week2_app;"
```

## 4. Cấu hình bằng biến môi trường

Secret không nằm trong `appsettings.json`:

```bash
cp .env.example .env
```

```dotenv
DB_HOST=localhost
DB_PORT=5932
DB_NAME=week2_school
DB_USER=week2_app
DB_PASSWORD=week2_secret
```

Thứ tự ưu tiên (`ResolveConnectionString` trong `Program.cs`):

1. `ConnectionStrings__SchoolDb` — một biến chứa nguyên chuỗi kết nối
2. Ghép từ `DB_HOST` / `DB_PORT` / `DB_NAME` / `DB_USER` / `DB_PASSWORD`

Thiếu biến bắt buộc → app dừng ngay và nói rõ tên biến, thay vì lỗi 500 lúc chạy.

## 5. Restore, migration và chạy

```bash
cd baitap
dotnet tool restore

cd Week2.School.Api
cp .env.example .env
dotnet restore
dotnet ef database update      # tạo programme, course, student + __EFMigrationsHistory
dotnet ef migrations list      # InitialSchoolSchema

dotnet run                     # http://localhost:5081
```

Swagger UI: **http://localhost:5081/swagger** (mở `/` cũng tự chuyển sang `/swagger`).
Port `5081` cố định trong `Properties/launchSettings.json` để không đụng tuần 1 (`5080`).

## 6. Mô hình dữ liệu

```
programme 1 ────< N student          course (danh mục độc lập ở tuần 2)
```

| Bảng | Khóa chính | Ràng buộc đáng chú ý |
|---|---|---|
| `programme` | `programme_id` (bigint identity) | unique `ix_programme_code` |
| `course` | `course_id` (bigint identity) | unique `ix_course_code` |
| `student` | `student_id` (bigint identity) | unique `ix_student_code`, `ix_student_email`, FK `programme_id` |

FK `student.programme_id` dùng `DeleteBehavior.Restrict`: xóa programme còn student bị chặn
ở cả tầng service (trả 409 kèm thông điệp) lẫn tầng database.

### Quy tắc nghiệp vụ

* `programme_code`, `course_code`, `student_code`, `email` là duy nhất.
* Mỗi student phải trỏ tới một programme đang tồn tại.
* `duration_years` trong 1–8; `credits` trong 1–10.
* `status` thuộc `ACTIVE`, `SUSPENDED`, `GRADUATED`.
* Không xóa programme khi còn student thuộc programme đó.

Mã và email được chuẩn hoá trước khi so trùng (code → chữ hoa, email → chữ thường).
Đây **không** phải chi tiết trang trí: PostgreSQL phân biệt hoa/thường, nên nếu bỏ bước
chuẩn hoá thì `SE` và `se` sẽ cùng lọt qua unique index.

## 7. Danh sách endpoint

| Nhóm | Method + route | Kết quả |
|---|---|---|
| Programmes | `GET/POST /api/programmes` · `GET/PUT/DELETE /api/programmes/{id}` | CRUD; response kèm `studentCount` |
| Programmes | `GET /api/programmes/{id}/students` | Danh sách student của programme |
| Courses | `GET/POST /api/courses` · `GET/PUT/DELETE /api/courses/{id}` | CRUD danh mục học phần |
| Students | `GET/POST /api/students` · `GET/PUT/DELETE /api/students/{id}` | CRUD; response kèm `programmeCode` / `programmeName` |

Mã HTTP: `200` đọc/sửa · `201` tạo · `204` xóa · `400` validation ·
`404` không tìm thấy (kể cả `programmeId` không tồn tại khi tạo student) ·
`409` trùng code/email hoặc xóa programme còn student.

## 8. Chuyển từ MySQL sang PostgreSQL — những chỗ phải đổi

| Tài liệu gốc (MySQL) | Bài này (PostgreSQL) |
|---|---|
| `Pomelo.EntityFrameworkCore.MySql` | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `UseMySql(cs, ServerVersion.AutoDetect(cs))` | `UseNpgsql(cs)` |
| `HasColumnType("tinyint unsigned")` | **Bỏ hẳn** — PostgreSQL không có kiểu này |
| `byte` cho credits / duration_years | `short` (map sang `smallint`) |
| `CREATE DATABASE ... CHARACTER SET utf8mb4` | `CREATE DATABASE week2_school;` (UTF8 mặc định) |
| `CREATE USER ...@localhost` + `GRANT ALL ON db.*` | `CREATE USER ... WITH PASSWORD` + `OWNER` |
| `Server=...;User=...;` port 3306 | `Host=...;Username=...;` port 5432 |
| `SHOW TABLES` / `DESCRIBE t` / `SHOW GRANTS` | `\dt` / `\d t` / `\du` |
| Collation `utf8mb4_0900_ai_ci` không phân biệt hoa/thường | PostgreSQL **có** phân biệt — phải tự chuẩn hoá code/email |

`ServerVersion.AutoDetect` biến mất là một cải thiện: bản MySQL bắt database phải sống
ngay lúc khởi động app, còn `UseNpgsql` thì không.

Nếu đã lỡ tạo migration bằng provider MySQL thì phải **xóa thư mục `Migrations/` và tạo lại**,
không dùng chung migration giữa hai provider.

## 9. Chuỗi kiểm thử

`Week2.School.Api.http` chạy được thẳng trong VS Code hoặc Visual Studio, gồm đủ
happy path lẫn các case lỗi 400/404/409. Thứ tự bắt buộc:

1. `POST /api/programmes` → **201**, lấy `programmeId`
2. `POST /api/courses` → **201**
3. `POST /api/students` (dùng `programmeId` ở bước 1) → **201**, response có `programmeCode`
4. `GET /api/students` → **200**
5. `PUT /api/students/{id}` đổi email và status → **200**
6. `DELETE /api/programmes/{id}` khi còn student → **409**
7. Dọn dữ liệu đúng thứ tự student → programme → course, mỗi lệnh → **204**

Các case lỗi cần chứng minh: `durationYears = 0` → **400**, `programmeCode` trùng → **409**,
`programmeId` không tồn tại → **404**, `status = "DROPPED"` → **400**.

## 10. Lỗi thường gặp

| Triệu chứng | Cách xử lý |
|---|---|
| `Thiếu biến môi trường 'DB_...'` | Chưa có `.env` — chạy `cp .env.example .env` |
| `password authentication failed` | Sai user/password trong `.env` |
| `database "week2_school" does not exist` | Chạy lại SQL ở mục 3 |
| `Run "dotnet tool restore"...` dù đã restore | Manifest pin sai version hoặc có `"rollForward": false` |
| `relation "programme" does not exist` | Chưa chạy `dotnet ef database update` |
| `could not be translated` | Đang `.Where()` trên DTO đã `Select` — phải lọc trên entity **trước** khi projection |
| `409` khi xóa programme | Còn student thuộc programme — xóa student trước |

## 11. Cấu trúc thư mục

```
Week2.School.Api/
├── .env                       secret, KHÔNG commit
├── .env.example               mẫu, có commit
├── README.md
├── Contracts/Requests/        CreateProgrammeRequest, CreateStudentRequest, ...
├── Contracts/Responses/       ProgrammeResponse, CourseResponse, StudentResponse
├── Controllers/               ApiControllerBase + Programmes/Courses/Students
├── Data/SchoolDbContext.cs
├── Infrastructure/            ApiExceptionHandler (ProblemDetails cho lỗi 500)
├── Migrations/                InitialSchoolSchema
├── Models/                    Programme, Course, Student
├── Services/                  nghiệp vụ + ServiceResult
├── Program.cs                 nạp .env, DI, Swagger
├── Week2.School.Api.http      chuỗi kiểm thử
├── appsettings.json
└── appsettings.Development.json
```

## 12. Chưa có ở tuần 2

JWT, RBAC guard, frontend, Redis, và hai bảng nối `student_course` / `course_programme`.
Phần authorization sẽ dựng trên schema RBAC của tuần 1 ở `../Week1.Rbac.Api`.
