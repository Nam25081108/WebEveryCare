# EveryCare API và PostgreSQL

Backend dùng ASP.NET Core 8, Entity Framework Core, PostgreSQL và PostGIS.

## Khởi tạo database

Tạo database bằng pgAdmin hoặc SQL:

```sql
CREATE DATABASE everycare;
```

Nếu đang có database cũ tên `sachnha`, hãy dừng backend, kết nối pgAdmin vào database `postgres`, rồi chạy file `backend/database/rename-to-everycare.sql`. Dữ liệu cũ được giữ nguyên, chỉ đổi tên database.

Đặt chuỗi kết nối trong PowerShell, không lưu mật khẩu thật vào repository:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=everycare;Username=postgres;Password=YOUR_PASSWORD"
$env:DOTNET_CLI_HOME="C:\Users\leduc\webdondepvs\.dotnet-home"
dotnet tool run dotnet-ef database update --project backend\EveryCare.Api\EveryCare.Api.csproj --startup-project backend\EveryCare.Api\EveryCare.Api.csproj
```

Migration `ExpandEveryCareServiceCatalog` bổ sung phân nhóm và trạng thái “sắp triển khai” cho danh mục tiện ích EveryCare.

Chạy API và seed dữ liệu mẫu:

```powershell
$env:Database__ApplyMigrationsOnStartup="true"
dotnet run --project backend\EveryCare.Api\EveryCare.Api.csproj --launch-profile http
```

Swagger nằm tại `http://localhost:5185/swagger`.

## Endpoint chính

- `GET /api/system/health`: kiểm tra API.
- `GET /api/system/database`: kiểm tra PostgreSQL.
- `GET /api/service-groups`: danh mục dịch vụ.
- `POST /api/bookings`: tạo đơn, tìm đối tác theo dịch vụ, vị trí và lịch rảnh.
- `GET /api/admin/bookings`: đơn hàng trong trang admin.

## Luồng đối tác

- `POST /api/partner-applications`: gửi hồ sơ, tọa độ và ảnh CCCD.
- `GET /api/admin/partner-applications`: admin xem hồ sơ.
- `POST /api/admin/partner-applications/{id}/approve`: duyệt và gửi Gmail.
- `POST /api/admin/partner-applications/{id}/reject`: từ chối hồ sơ.
- `POST /api/partner/auth/login`: đăng nhập bằng số điện thoại.
- `GET/PUT /api/partner/me/availability`: đọc và lưu lịch rảnh.
- `GET /api/partner/me/invitations`: xem lời mời được lưu khi ngoại tuyến.
- `POST /api/partner/me/invitations/{id}/respond`: nhận hoặc từ chối việc.

Ảnh CCCD được lưu trong `App_Data/partner-identities`, không nằm trong thư mục tĩnh công khai.

## Cấu hình Gmail SMTP

Sử dụng Gmail App Password và đặt biến môi trường trước khi chạy API:

```powershell
$env:Email__Smtp__Enabled="true"
$env:Email__Smtp__FromAddress="youraccount@gmail.com"
$env:Email__Smtp__Username="youraccount@gmail.com"
$env:Email__Smtp__Password="YOUR_GMAIL_APP_PASSWORD"
```

Nếu SMTP chưa được cấu hình, hồ sơ vẫn được duyệt và admin sẽ thấy lý do email chưa gửi.

## Các bảng chính

- Tài khoản: `users`, `customer_profiles`, `addresses`, `partner_sessions`.
- Đối tác: `partner_profiles`, `partner_team_members`, `partner_service_capabilities`.
- Lịch làm việc: `partner_availability_rules`, `partner_availability_overrides`.
- Dịch vụ: `service_groups`, `service_packages`, `service_work_items`, `service_process_steps`, `service_tools`, `professional_pricing_rules`.
- Đơn hàng: `bookings`, `booking_assignments`, `booking_extra_charges`, `booking_photos`.
- Thông báo: `partner_notifications`.
- Sau dịch vụ: `payments`, `reviews`, `favorite_partners`.

Vị trí dùng kiểu PostGIS `geography(point)` với chỉ mục GiST.
