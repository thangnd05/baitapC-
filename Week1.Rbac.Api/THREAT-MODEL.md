# Threat model v1 — School API (JWT + RBAC)

Phạm vi: phần danh tính và phân quyền của `Week1.Rbac.Api` (users/roles/permissions +
programme/course/student trên cùng một database `week1_rbac`).

Cách xếp mức rủi ro: **likelihood × impact**, không phải cảm tính. Một lỗi dễ khai thác và
làm rò dữ liệu cá nhân luôn được xếp Cao, kể cả khi chưa từng xảy ra trong lab.

## Trust boundary

```
[Client: Swagger / Postman / SPA]
        │  HTTP + Authorization: Bearer <JWT>
        ▼
┌─────────────────────────────────────────────────────────┐
│  ĐƯỜNG BIÊN TIN CẬY — mọi thứ bên trên là KHÔNG tin được │
├─────────────────────────────────────────────────────────┤
│ 1. Rate limiter    → chặn dò mật khẩu (429)             │
│ 2. Authentication  → token hợp lệ? (401)                │
│ 3. Authorization   → đúng vai trò? (403)                │
│ 4. Owner policy    → đúng chủ sở hữu bản ghi? (403)     │
│ 5. Service layer   → hợp lệ nghiệp vụ? (400/404/409)    │
└─────────────────────────────────────────────────────────┘
        ▼
   PostgreSQL week1_rbac
```

Ba chỗ **buộc** phải kiểm tra: (2) danh tính, (3) vai trò, (4) chủ sở hữu bản ghi.
Bỏ (4) là lỗ hổng phổ biến nhất trong các API thực tế.

## Bảng threat

| Mã | Asset | Threat | Mức rủi ro | Mitigation đã làm | Bằng chứng |
|---|---|---|---|---|---|
| THR-01 | Hồ sơ sinh viên | Sinh viên đã đăng nhập sửa hồ sơ của người khác qua `PUT /api/students/{id}` (BOLA — OWASP API Security Top 10 #1) | **Cao** | `StudentOwnerHandler` đối chiếu claim `student_id` với id tài nguyên; áp cho cả GET lẫn PUT | Ca B4 → 403; ca C1 (sửa hồ sơ của mình) → 200 |
| THR-02 | Mật khẩu tài khoản | Dò mật khẩu tự động qua `POST /api/auth/login` | Trung bình | Rate limit 5 request/phút phân hoạch theo IP + thông báo lỗi mơ hồ | Ca D3 → 429 kèm `Retry-After: 60` |
| THR-03 | Khóa ký JWT | Khóa bị commit vào Git rồi dùng để tự phát token Admin | **Cao** | Khóa chỉ nằm trong `.env` (đã gitignore) hoặc User Secrets; app từ chối khởi động nếu khóa < 32 byte | `.env` không được track; `appsettings.json` không chứa secret nào |
| THR-04 | Danh sách email trong hệ thống | Kẻ tấn công gọi login hàng loạt để phân biệt "email có thật" với "email không tồn tại" (user enumeration) | Trung bình | Một thông điệp duy nhất `"Email hoặc mật khẩu không đúng"` cho cả hai trường hợp, và cho cả tài khoản bị khóa | Ca D1 và D2 trả về response giống hệt nhau |
| THR-05 | Endpoint quản trị (`/api/users`, `/api/roles`, `/api/permissions`) | Tài khoản Student/Staff tự cấp thêm vai trò cho mình → leo thang lên Admin | **Cao** | Cả ba controller gắn `[Authorize(Policy = AppPolicies.ManageIdentity)]` ở mức class (policy này chỉ cấp cho `Admin`) | Ca C8 (Staff) → 403; ca C9 (Admin) → 200 |
| THR-06 | Phiên đăng nhập | Access token bị lộ vẫn dùng được vô thời hạn | Trung bình | `exp` 30 phút, `ValidateLifetime = true`, `ClockSkew` rút xuống 30 giây (mặc định 5 phút) | Ca B2 → 401 với `error_description` nói rõ token lifetime invalid |
| THR-07 | Toàn bộ API | Token giả mạo: đổi payload rồi ký lại bằng thuật toán khác, hoặc `"alg": "none"` | **Cao** | `ValidateIssuerSigningKey = true` + `ValidAlgorithms = [HmacSha256]` chốt cứng thuật toán | Token bị sửa chữ ký → 401 |
| THR-08 | Toàn bộ API | Token do hệ thống khác phát (cùng thư viện, khác ứng dụng) được chấp nhận | Thấp | `ValidateIssuer` + `ValidateAudience` bật, so khớp `Jwt:Issuer` / `Jwt:Audience` | Sai issuer/audience → 401 |
| THR-09 | Dữ liệu cá nhân sinh viên | Trang web độc hại đọc API bằng session của người dùng | Thấp | CORS allowlist origin cụ thể, không dùng `AllowAnyOrigin` | Ca D5 không trả `Access-Control-Allow-Origin`; ca D6 có |
| THR-10 | Response của API | MIME sniffing / clickjacking khi response được nhúng trong trang khác | Thấp | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` | Ca D4 |
| THR-11 | Refresh token | Token bị đánh cắp và dùng song song với phiên thật, kéo dài quyền truy cập vô thời hạn | **Cao** | Rotation một-lần-dùng: mỗi lần refresh thu hồi token cũ ngay. Dùng lại token đã thu hồi bị coi là replay và **hủy toàn bộ phiên** của tài khoản, kèm log cảnh báo | Ca E3 → 401; ca E4 chứng minh token mới cũng chết theo |
| THR-12 | Bảng `refresh_token` | Database bị đọc trộm, kẻ tấn công lấy token đi gọi `/api/auth/refresh` | Trung bình | Chỉ lưu **SHA-256** của token, không bao giờ lưu giá trị thật. Token có 256 bit entropy nên không dò ngược được | `select token_hash from refresh_token` chỉ ra chuỗi hex 64 ký tự |
| THR-13 | Endpoint `/api/auth/logout` và `/refresh` | Dò xem refresh token nào có thật trong hệ thống | Thấp | `logout` luôn trả 204 bất kể token có tồn tại hay không; `refresh` dùng chung một thông điệp lỗi cho mọi lý do thất bại | Ca E5 và E7 |

## Hạn chế đã biết của phiên bản 1

1. **Access token đã phát ra không thu hồi được.** Nó stateless — server không lưu gì để hủy.
   Một access token bị lộ vẫn dùng được đến khi hết hạn (tối đa 30 phút). Refresh token thì
   thu hồi được, nên cửa sổ rủi ro bị chặn ở 30 phút thay vì 14 ngày. Muốn thu hồi tức thì thì
   phải thêm denylist (Redis) và tra nó mỗi request — đánh đổi bằng một lượt I/O trên mọi request.
   Ngoài ra refresh token đang nằm trong response body: an toàn hơn thì đặt vào cookie
   `HttpOnly; Secure; SameSite=Strict`, nhưng như vậy không kiểm thử được bằng file `.http`.
2. **Claim role trong token có thể lệch với bảng `user_roles`.** Token mang bản chụp vai trò
   tại thời điểm đăng nhập. Nếu Admin gỡ vai trò của một người, token cũ của người đó vẫn còn
   vai trò đến khi hết hạn. Đây là đánh đổi có chủ đích: đọc claim thì không phải truy vấn DB
   mỗi request. Rút ngắn `exp` là cách giảm cửa sổ lệch.
3. **Chọn 403 thay vì 404 cho tài nguyên không thuộc quyền.** Trả 403 tự nó là một rò rỉ nhỏ:
   nó xác nhận bản ghi đó *có tồn tại*. Bài này chọn 403 để thông báo lỗi dạy được cho sinh viên;
   hệ thống thật xử lý dữ liệu nhạy cảm nên cân nhắc trả 404 cho cả hai trường hợp.
4. **HTTP, không HTTPS.** Lab chạy `http://localhost:8080` nên token đi qua mạng dưới dạng rõ.
   Chấp nhận được trên máy lab, tuyệt đối không chấp nhận khi triển khai thật.
5. **Bảng `permissions` chưa được enforce.** Tuần 1 dựng sẵn permission-based RBAC nhưng
   tuần 3 mới chỉ enforce ở mức role. Các code `event.*` hiện là dữ liệu chết.

## Nếu khóa ký JWT bị lộ — thứ tự xử lý

1. Sinh khóa mới và thay `Jwt__SigningKey` (mọi **access** token đang lưu hành lập tức mất hiệu lực).
2. Thu hồi luôn refresh token — chúng **không** ký bằng khóa đó nên đổi khóa không đụng tới chúng:
   `update refresh_token set revoked_at = now(), revoked_reason = 'key_compromised' where revoked_at is null;`
3. Khởi động lại ứng dụng.
4. Nếu khóa đã từng nằm trong lịch sử Git: rewrite lịch sử hoặc coi repo là đã nhiễm; đổi luôn
   mật khẩu DB vì nó ở cùng file cấu hình.
5. Rà log tìm request dùng token không do hệ thống phát (user id lạ, vai trò lạ).
6. Buộc đổi mật khẩu các tài khoản Admin.
