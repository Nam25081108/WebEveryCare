# Quy ước cấu trúc EveryCare

## Nguyên tắc

- `apps/` chỉ chứa các ứng dụng có thể chạy hoặc triển khai độc lập.
- `src/app/` chỉ chịu trách nhiệm định tuyến, layout và ghép các feature.
- `src/features/` nhóm mã theo nghiệp vụ như xác thực, đặt lịch, danh mục và dashboard.
- `src/components/` chỉ chứa thành phần UI hoặc layout dùng bởi nhiều feature trong cùng ứng dụng.
- `src/lib/` chứa cấu hình và tiện ích tổng quát; không đặt component hoặc nghiệp vụ lớn ở đây.
- `public/` chỉ chứa tài nguyên được phép phục vụ công khai.
- `database/` chứa script vận hành database; EF Core migrations nằm cùng project API.
- `archive/` không được import vào ứng dụng đang chạy.

## Quy tắc phụ thuộc frontend

```text
app -> features -> components/lib
```

Một feature có thể sử dụng component dùng chung, nhưng component dùng chung không được phụ thuộc ngược vào một page. Khi một module chỉ được dùng bởi một feature, hãy đặt nó trong feature đó thay vì đưa vào thư mục dùng chung.

## Quy tắc backend

```text
Controllers -> Services -> Domain
                     -> Infrastructure
```

- `Controllers` nhận request và trả response.
- `Contracts` định nghĩa request/response contract.
- `Services` chứa quy trình nghiệp vụ.
- `Domain` chứa entity, enum và quy tắc cốt lõi.
- `Infrastructure` chứa EF Core, migrations và tích hợp kỹ thuật.

Không đưa secret, file upload, log, cache build hoặc dữ liệu production vào Git.
