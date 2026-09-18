# EveryCare Platform

EveryCare được tổ chức thành ba frontend Next.js độc lập và một ASP.NET Core Web API dùng chung.

## Cấu trúc

- `customer-web/` — website khách hàng, chạy tại `http://localhost:3000`
- `admin-web/` — website quản trị, chạy tại `http://localhost:3001`
- `partner-web/` — website đối tác, chạy tại `http://localhost:3002`
- `backend/EveryCare.Api/` — ASP.NET Core API, chạy tại `http://localhost:5185`
- `_legacy-single-app/` — bản lưu frontend cũ trước khi tách, chỉ dùng để đối chiếu hoặc khôi phục

Ba frontend không truy cập PostgreSQL trực tiếp và không gọi nội bộ lẫn nhau. Dữ liệu nghiệp vụ được trao đổi qua `EveryCare.Api`.

## Cài đặt

```powershell
npm install
```

Thiết lập mật khẩu PostgreSQL cục bộ (thay `MAT_KHAU_POSTGRES_CUA_BAN` bằng mật khẩu thật):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=everycare;Username=postgres;Password=MAT_KHAU_POSTGRES_CUA_BAN" --project backend\EveryCare.Api\EveryCare.Api.csproj
```

Mật khẩu được lưu trong .NET User Secrets của máy, không được ghi vào Git.

## Chạy dự án

Mở bốn cửa sổ PowerShell tại thư mục gốc và chạy:

```powershell
npm run dev:api
npm run dev:customer
npm run dev:admin
npm run dev:partner
```

## Kiểm tra

```powershell
npm run typecheck
npm run build
```

Mỗi frontend có `.env.example` riêng để cấu hình URL API và URL của hai website còn lại.
