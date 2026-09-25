export type ServicePlan = {
  id: string;
  name: string;
  description: string;
  meta: string;
  price: number | null;
  badge?: string;
  workDetails?: WorkDetails;
};

export type WorkDetails = {
  overview?: string[];
  scope?: string[];
  livingRoom: string[];
  bedroom: string[];
  kitchen: string[];
  bathroom: string[];
};

export type Service = {
  id: string;
  shortName: string;
  name: string;
  description: string;
  icon: "sparkles" | "home" | "building";
  plans: ServicePlan[];
};

const roomWorkDetails: WorkDetails = {
  livingRoom: ["Quét bụi và lau sạch sàn", "Lau bàn ghế, kệ và bề mặt dễ tiếp cận", "Thu gom rác và sắp xếp đồ dùng gọn gàng", "Lau cửa ra vào và tay nắm"],
  bedroom: ["Quét bụi, hút bụi và lau sàn", "Lau bàn, tủ và các bề mặt bên ngoài", "Sắp xếp giường ngủ và đồ dùng cơ bản", "Lau gương và tay nắm cửa"],
  kitchen: ["Lau mặt bếp và khu vực chế biến", "Làm sạch bồn rửa và vòi nước", "Lau bên ngoài tủ bếp và thiết bị", "Thu gom rác, lau sàn bếp"],
  bathroom: ["Làm sạch bồn cầu và lavabo", "Lau gương, vòi nước và phụ kiện", "Chà sàn và khu vực tường dễ tiếp cận", "Thu gom rác và khử mùi cơ bản"],
};

const deepWorkDetails: WorkDetails = {
  overview: ["Làm sạch toàn bộ không gian từ trên xuống dưới", "Phân chia nhân sự theo từng khu vực", "Thu gom và bàn giao rác sau khi hoàn thành"],
  scope: ["Bề mặt bên ngoài của nội thất và thiết bị", "Sàn, tường thấp, cửa, kính trong tầm với", "Các góc khuất và khu vực tích tụ bụi lâu ngày"],
  livingRoom: ["Hút bụi sofa, thảm và khe ghế", "Lau cửa kính, khung cửa và tay nắm", "Làm sạch quạt, đèn và bề mặt trang trí", "Chà và lau kỹ toàn bộ sàn"],
  bedroom: ["Hút bụi nệm và gầm giường", "Lau bên ngoài tủ, bàn và đầu giường", "Làm sạch cửa sổ, rèm trong tầm với", "Khử bụi và lau kỹ sàn"],
  kitchen: ["Tẩy dầu mỡ mặt bếp và tường bếp", "Làm sạch bồn rửa, vòi và mặt đá", "Lau bên ngoài tủ và thiết bị nhà bếp", "Chà kỹ sàn và khe góc"],
  bathroom: ["Tẩy cặn bồn cầu, lavabo và vòi nước", "Chà sàn, tường và vách kính", "Làm sạch quạt thông gió trong tầm với", "Khử mùi và làm sạch miệng thoát nước"],
};

export const professionalInfo = {
  process: ["Khảo sát nhanh hiện trạng và khoanh vùng thi công", "Thu gom rác thô, hút bụi công nghiệp", "Làm sạch chi tiết từ trên xuống dưới", "Chà sàn, xử lý vết bẩn và vệ sinh kính", "Kiểm tra chất lượng và chụp ảnh bàn giao"],
  tools: [
    { name: "Máy hút bụi công nghiệp", description: "Hút bụi mịn và rác khô công suất cao", icon: "vacuum" },
    { name: "Máy chà sàn", description: "Làm sạch sàn diện tích lớn và vết bẩn lâu ngày", icon: "machine" },
    { name: "Bộ dụng cụ kính", description: "Cây gạt, khăn kính và dụng cụ nối dài", icon: "window" },
    { name: "Khăn microfiber", description: "Phân màu riêng cho từng khu vực", icon: "cloth" },
    { name: "Bàn chải chuyên dụng", description: "Xử lý khe góc và bề mặt khó tiếp cận", icon: "brush" },
    { name: "Hóa chất vệ sinh", description: "Dùng đúng loại theo từng bề mặt", icon: "chemical" },
  ],
};

export const services: Service[] = [
  {
    id: "room",
    shortName: "Dọn dẹp nhà cửa",
    name: "Dọn dẹp nhà cửa",
    description: "Dọn nhanh theo giờ cho căn hộ và nhà ở đang sử dụng.",
    icon: "sparkles",
    plans: [
      { id: "r1", name: "Gói 1 giờ", description: "Tối đa 30m² hoặc 1 phòng", meta: "1 người • 1 giờ", price: 120000, workDetails: roomWorkDetails },
      { id: "r2", name: "Gói 2 giờ", description: "Tối đa 55m² hoặc 2 phòng", meta: "1 người • 2 giờ", price: 210000, badge: "Phổ biến", workDetails: roomWorkDetails },
      { id: "r3", name: "Gói 3 giờ", description: "Tối đa 85m² hoặc 3 phòng", meta: "1 người • 3 giờ", price: 300000, workDetails: roomWorkDetails },
      { id: "r4", name: "Gói 4 giờ", description: "Tối đa 105m² hoặc 4 phòng", meta: "1 người • 4 giờ", price: 380000, workDetails: roomWorkDetails },
    ],
  },
  {
    id: "deep",
    shortName: "Tổng vệ sinh",
    name: "Tổng vệ sinh",
    description: "Làm sạch toàn diện với đội ngũ phù hợp quy mô công việc.",
    icon: "home",
    plans: [
      { id: "d1", name: "Căn hộ 60m²", description: "Tổng vệ sinh quy mô nhỏ", meta: "2 người • 3 giờ", price: 690000, workDetails: deepWorkDetails },
      { id: "d2", name: "Nhà 80m²", description: "Làm sạch kỹ từng khu vực", meta: "2 người • 4 giờ", price: 890000, badge: "Được chọn nhiều", workDetails: deepWorkDetails },
      { id: "d3", name: "Nhà 100m²", description: "Đội ngũ 3 người chuyên nghiệp", meta: "3 người • 3 giờ", price: 1090000, workDetails: deepWorkDetails },
      { id: "d4", name: "Nhà 150m²", description: "Phù hợp nhà phố nhiều phòng", meta: "3 người • 4 giờ", price: 1490000, workDetails: deepWorkDetails },
      { id: "d5", name: "Nhà 200m²", description: "Tổng vệ sinh quy mô lớn", meta: "4 người • 4 giờ", price: 1990000, workDetails: deepWorkDetails },
      { id: "d6", name: "Công trình 400m²", description: "Một ngày làm việc toàn diện", meta: "4 người • 8 giờ", price: 3690000, workDetails: deepWorkDetails },
    ],
  },
  {
    id: "office",
    shortName: "Văn phòng",
    name: "Dọn dẹp văn phòng định kỳ",
    description: "Linh hoạt theo buổi, theo ngày hoặc lịch cố định hằng tháng.",
    icon: "building",
    plans: [
      { id: "o100", name: "Tối đa 100m²", description: "Vệ sinh văn phòng", meta: "1 người • 2 giờ", price: 260000 },
    ],
  },
  {
    id: "hospitality",
    shortName: "Buồng phòng",
    name: "Dọn dẹp buồng phòng",
    description: "Dọn phòng cho khách sạn, homestay, căn hộ dịch vụ và villa.",
    icon: "building",
    plans: [
      { id: "hospitality-custom", name: "Chọn loại phòng", description: "Tính theo số lượng phòng", meta: "Giá theo cấu hình", price: 0 },
    ],
  },
  {
    id: "professional",
    shortName: "Chuyên nghiệp",
    name: "Vệ sinh chuyên nghiệp",
    description: "Dành cho nhà lâu năm, sau xây dựng và văn phòng.",
    icon: "building",
    plans: [
      { id: "p1", name: "Nhà lâu năm dưới 60m²", description: "Làm sạch sâu toàn bộ nhà ở", meta: "Đã gồm dụng cụ & hóa chất", price: 2100000 },
      { id: "p2", name: "Nhà lâu năm 60–80m²", description: "Quy trình vệ sinh công nghiệp", meta: "Đã gồm dụng cụ & hóa chất", price: 2700000, badge: "Phổ biến" },
      { id: "p3", name: "Nhà lâu năm 81–100m²", description: "Đội ngũ và thiết bị chuyên dụng", meta: "Đã gồm dụng cụ & hóa chất", price: 3300000 },
      { id: "p4", name: "Diện tích 101–500m²", description: "Khảo sát theo hiện trạng công trình", meta: "Nhận báo giá riêng", price: null },
    ],
  },
];

export const formatCurrency = (amount: number | null) =>
  amount === null ? "Liên hệ" : new Intl.NumberFormat("vi-VN").format(amount) + "đ";
