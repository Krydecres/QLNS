# Dự án Quản lý nhân sự và chấm công (QLNS)

Dự án này phục vụ cho 2 môn học, được chia thành 2 nhánh công nghệ hoàn toàn độc lập nằm trong cùng một không gian làm việc.

## Cấu trúc dự án

### Nhánh 1: Full .NET (MVC)
- **Thư mục:** `./QLNS.FullNet/`
- **Công nghệ:** ASP.NET Core MVC (Backend & Frontend HTML/Razor)
- **Kiến trúc:**
  - `QLNS.FullNet.Web`: Project giao diện MVC.
  - `QLNS.FullNet.Data`: Project chứa Models và Database Context (Entity Framework Core). Đã được reference vào project Web.

### Nhánh 2: .NET Backend + Angular Frontend
- **Thư mục:** `./QLNS.AngularStack/`
- **Công nghệ:** ASP.NET Core Web API (Backend) + Angular (Frontend)
- **Kiến trúc:**
  - `QLNS.Api`: Project Web API cung cấp dữ liệu JSON.
  - `qlns-frontend`: Ứng dụng Single Page Application (SPA) xây dựng bằng Angular.

## Hướng dẫn cho AI
- **Ngữ cảnh làm việc:** Trước khi thực hiện thay đổi, hãy xác định rõ ràng yêu cầu đang dành cho nhánh MVC (Môn 1) hay nhánh Angular (Môn 2) để thao tác trên đúng thư mục.
- **Tuân thủ quy ước:** Giữ sự tách biệt tuyệt đối giữa 2 kiến trúc, không trộn lẫn thư viện hoặc cấu hình giữa 2 môn.
