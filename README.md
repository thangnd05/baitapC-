# 30INF067 — Lập trình Web nâng cao · Bài thực hành

Mỗi tuần là **một project độc lập**: database riêng, `.env` riêng, migration riêng, port riêng.
Chỉ `.config/dotnet-tools.json` và `.gitignore` dùng chung ở gốc repo.

| Tuần | Thư mục | Nội dung | Database | Swagger |
|---|---|---|---|---|
| 1 | [`Week1.Rbac.Api/`](Week1.Rbac.Api/) | RBAC: user / role / permission, 2 bảng nối many-to-many | `week1_rbac` | http://localhost:5080/swagger |
| 2 | [`Week2.School.Api/`](Week2.School.Api/) | School API: programme / course / student, CRUD có DTO + Service | `week2_school` | http://localhost:5081/swagger |

Cả hai chạy trên **ASP.NET Core 10 · PostgreSQL · EF Core · Swagger**, khởi động bằng `dotnet run`.

> Tài liệu tuần 2 gốc dùng MySQL + .NET 8. Bài này dùng PostgreSQL + .NET 10 cho đồng bộ
> với tuần 1 — bảng đối chiếu từng chỗ phải đổi nằm ở mục 8 của
> [README tuần 2](Week2.School.Api/README.md).

## Chạy nhanh

```bash
dotnet tool restore            # dotnet-ef 10.0.11, chạy một lần ở gốc repo

cd Week1.Rbac.Api              # hoặc Week2.School.Api
cp .env.example .env
dotnet ef database update
dotnet run
```

Mọi lệnh `dotnet ef` và `dotnet run` phải chạy **trong thư mục project**, vì `.env` nằm ở đó
và `DotNetEnv` tìm ngược lên từ thư mục hiện tại.

Chi tiết từng tuần: [README tuần 1](Week1.Rbac.Api/README.md) ·
[README tuần 2](Week2.School.Api/README.md).

## Quy ước chung cho cả hai tuần

* **Secret không vào Git.** `.env` nằm trong `.gitignore`; chỉ commit `.env.example`.
* **Controller mỏng.** Controller không inject `DbContext`, chỉ gọi Service.
* **Entity không phải DTO.** Request/Response luôn đi qua `Contracts/`.
* **Lỗi nghiệp vụ không dùng exception.** Service trả `ServiceResult { Success | NotFound | Conflict }`,
  `ApiControllerBase` dịch sang 200/201/204/404/409 tại một chỗ duy nhất.
* **Mỗi project có file `.http`** chứa sẵn chuỗi kiểm thử, kể cả các case lỗi.

## Cảnh báo an toàn

Cả hai tuần cố ý để mọi endpoint ở trạng thái **anonymous** — chưa có đăng nhập, JWT,
`[Authorize]` hay kiểm tra permission. Đây là bài học về database và CRUD, **không deploy production**.
JWT và authorization policy sẽ được thêm ở tuần 3.
