# EveryCare Platform

EveryCare là monorepo gồm ba ứng dụng Next.js độc lập và một ASP.NET Core Web API dùng chung. Việc tái cấu trúc chỉ thay đổi cách tổ chức mã nguồn; giao diện và luồng nghiệp vụ hiện tại được giữ nguyên.

## Cấu trúc dự án

```text
everycare/
├── apps/
│   ├── customer-web/       # Website khách hàng (cổng 3000)
│   ├── admin-web/          # Website quản trị (cổng 3001)
│   ├── partner-web/        # Website đối tác (cổng 3002)
│   └── api/                # ASP.NET Core API (cổng 5185)
├── archive/
│   └── legacy-single-app/  # Bản frontend cũ, chỉ dùng để đối chiếu
├── database/               # Script quản trị và khởi tạo PostgreSQL
├── docs/                   # Tài liệu kiến trúc và quy ước dự án
├── package.json            # Workspace và lệnh dùng chung
└── NuGet.Config            # Cấu hình package .NET
```

Mỗi frontend sử dụng cấu trúc thống nhất:

```text
apps/<web-app>/
├── public/                 # Tài nguyên tĩnh
├── src/
│   ├── app/                # Next.js App Router và stylesheet cấp trang
│   ├── components/         # UI/layout dùng chung trong ứng dụng
│   ├── features/           # Mã được nhóm theo nghiệp vụ
│   └── lib/                # Cấu hình và tiện ích dùng chung
├── .env.example
├── next.config.ts
├── package.json
└── tsconfig.json
```

API tiếp tục được tổ chức theo các lớp `Domain`, `Contracts`, `Services`, `Infrastructure` và `Controllers` trong `apps/api/EveryCare.Api`.

## Cài đặt

```powershell
npm install
```

Thiết lập mật khẩu PostgreSQL bằng .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=everycare;Username=postgres;Password=MAT_KHAU_POSTGRES_CUA_BAN" --project apps\api\EveryCare.Api\EveryCare.Api.csproj
```

## Chạy dự án

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

Xem thêm [quy ước cấu trúc dự án](docs/architecture/project-structure.md).
