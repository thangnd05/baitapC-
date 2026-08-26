# 30INF067 — Lập trình Web nâng cao · Bài thực hành

| Tuần | Thư mục | Nội dung | Database | Swagger |
|---|---|---|---|---|
| 1 + 2 + 3 | [`Week1.Rbac.Api/`](Week1.Rbac.Api/) | **School API có JWT & RBAC** — RBAC tuần 1 + nghiệp vụ trường học tuần 2 + bảo vệ tuần 3 | `week1_rbac` | http://localhost:8080/swagger |
| 2 (bản nộp gốc) | [`Week2.School.Api/`](Week2.School.Api/) | School API độc lập, chưa có auth — giữ nguyên làm minh chứng tuần 2 | `week2_school` | http://localhost:5081/swagger |

Cả hai chạy trên **ASP.NET Core 10 · PostgreSQL · EF Core · Swagger**, khởi động bằng `dotnet run`.

## Vì sao tuần 2 được gộp vào tuần 1

Tuần 3 yêu cầu chống **BOLA**: một sinh viên đã đăng nhập không được sửa hồ sơ của người khác.
Để làm được, `users.student_id` phải có **khóa ngoại** trỏ sang bảng `student` — nghĩa là bảng
danh tính (tuần 1) và bảng nghiệp vụ (tuần 2) buộc phải nằm **cùng một database**.

Vì database `week1_rbac` đã chạy sẵn và đã có lịch sử migration, hướng gộp là: mang domain
School của tuần 2 vào project tuần 1 bằng một migration cộng dồn
(`AddSchoolDomainAndStudentOwnership`) — chỉ **thêm** bảng và cột, không đụng dữ liệu cũ.

`Week2.School.Api/` được giữ nguyên, không sửa, để vẫn còn bản nộp tuần 2 đúng như lúc chấm.

> Tài liệu gốc của trường dùng MySQL + .NET 8. Repo này dùng PostgreSQL + .NET 10 cho đồng bộ
> giữa các tuần — bảng đối chiếu từng chỗ khác nằm trong README của mỗi tuần.

## Chạy nhanh

```bash
dotnet tool restore            # dotnet-ef 10.0.11, chạy một lần ở gốc repo

cd Week1.Rbac.Api
cp .env.example .env           # rồi điền Jwt__SigningKey (>= 32 ký tự)
dotnet ef database update
dotnet run                     # http://localhost:8080/swagger
```

Đăng nhập lấy token:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@hnmu.edu.vn","password":"Lab#2026_ChangeMe"}'
```

Ba tài khoản mẫu: `admin@` (Admin) · `staff@` (Staff) · `sv001@` (Student, sở hữu hồ sơ SV001),
đều thuộc `@hnmu.edu.vn`, mật khẩu lấy từ `Seed__Password`.

Mọi lệnh `dotnet ef` và `dotnet run` phải chạy **trong thư mục project**, vì `.env` nằm ở đó
và `DotNetEnv` tìm ngược lên từ thư mục hiện tại.

Chi tiết: [README chính](Week1.Rbac.Api/README.md) · [Threat model](Week1.Rbac.Api/THREAT-MODEL.md) ·
[README tuần 2 gốc](Week2.School.Api/README.md).

## Quy ước chung

* **Secret không vào Git.** `.env` nằm trong `.gitignore`; chỉ commit `.env.example`.
  Khóa ký JWT và mật khẩu DB chỉ ở `.env` hoặc User Secrets.
* **Mặc định là từ chối.** Controller gắn `[Authorize]` ở mức class rồi mới mở ra từng
  endpoint công khai, thay vì để mở rồi cố nhớ khóa từng cái.
* **Controller mỏng.** Controller không inject `DbContext`, chỉ gọi Service.
* **Quy tắc quyền nằm trong policy/handler**, không rải rác trong controller.
* **Entity không phải DTO.** Request/Response luôn đi qua `Contracts/`.
* **Lỗi nghiệp vụ không dùng exception.** Service trả `ServiceResult`, `ApiControllerBase`
  dịch sang 200/201/204/401/403/404/409 tại một chỗ duy nhất.
* **Mỗi project có file `.http`** chứa sẵn chuỗi kiểm thử — kể cả **kiểm thử âm**
  chứng minh hệ thống biết từ chối.

## Phạm vi được cấp quyền

Toàn bộ thao tác kiểm thử bảo mật trong repo này chỉ thực hiện trên **localhost** và trên
chính project do sinh viên tạo. Không quét, dò hoặc khai thác bất kỳ hệ thống thật nào
khi chưa có ủy quyền hợp pháp.
