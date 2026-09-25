export type UtilityService={slug:string;name:string;description:string;icon:string;available?:boolean;bookingService?:"room"|"deep"|"professional"};
export type ServiceCategory={id:string;name:string;shortName:string;eyebrow:string;description:string;accent:string;soft:string;services:UtilityService[]};

export const serviceCategories:ServiceCategory[]=[
 {id:"cleaning",name:"Vệ sinh & dọn dẹp",shortName:"Dọn dẹp",eyebrow:"Không gian sạch sẽ",description:"Từ một căn phòng nhỏ đến tổng vệ sinh và công trình chuyên nghiệp.",accent:"#18785f",soft:"#e7f6ef",services:[
  {slug:"ve-sinh-phong-le",name:"Dọn dẹp nhà cửa",description:"Dọn theo giờ, phù hợp phòng trọ, căn hộ và nhà ở.",icon:"sparkles",available:true,bookingService:"room"},
  {slug:"tong-ve-sinh",name:"Tổng vệ sinh",description:"Đội nhóm làm sạch toàn diện theo diện tích.",icon:"home",available:true,bookingService:"deep"},
  {slug:"ve-sinh-chuyen-nghiep",name:"Vệ sinh chuyên nghiệp",description:"Nhà lâu năm, sau xây dựng và công trình chuyên sâu.",icon:"building",available:true,bookingService:"professional"}
 ]},
 {id:"business",name:"Dịch vụ cho doanh nghiệp",shortName:"Doanh nghiệp",eyebrow:"Vận hành chỉn chu",description:"Giải pháp nhân sự vệ sinh linh hoạt cho văn phòng và cơ sở lưu trú.",accent:"#3564c8",soft:"#eaf0ff",services:[
  {slug:"don-dep-van-phong-dinh-ky",name:"Dọn dẹp văn phòng định kỳ",description:"Nhân sự theo ca, theo ngày hoặc lịch cố định.",icon:"briefcase"},
  {slug:"don-dep-buong-phong",name:"Dọn dẹp buồng phòng",description:"Cho căn hộ cho thuê, homestay và khách sạn.",icon:"bed"},
  {slug:"ve-sinh-van-phong-chuyen-sau",name:"Vệ sinh văn phòng chuyên sâu",description:"Làm sạch định kỳ quy mô lớn và khu vực chuyên biệt.",icon:"building"}
 ]},
 {id:"care",name:"Chăm sóc & hỗ trợ",shortName:"Chăm sóc",eyebrow:"An tâm cho gia đình",description:"Kết nối người hỗ trợ phù hợp cho trẻ nhỏ, người cao tuổi và người bệnh.",accent:"#d45872",soft:"#fff0f3",services:[
  {slug:"trong-tre",name:"Trông trẻ",description:"Hỗ trợ chăm sóc trẻ theo giờ tại nhà.",icon:"baby"},
  {slug:"cham-soc-nguoi-cao-tuoi",name:"Chăm sóc người cao tuổi",description:"Bầu bạn và hỗ trợ sinh hoạt hằng ngày.",icon:"heart"},
  {slug:"cham-soc-nguoi-benh",name:"Chăm sóc người bệnh",description:"Hỗ trợ tại nhà hoặc cơ sở y tế theo lịch.",icon:"stethoscope"}
 ]},
 {id:"beauty",name:"Làm đẹp tại nhà",shortName:"Làm đẹp",eyebrow:"Rạng rỡ đúng lúc",description:"Chuyên viên làm đẹp đến tận nơi theo lịch bạn chọn.",accent:"#a653b7",soft:"#f8ecfb",services:[
  {slug:"trang-diem-tai-nha",name:"Trang điểm",description:"Trang điểm sự kiện, dự tiệc và phong cách cá nhân.",icon:"wand"}
 ]},
 {id:"appliances",name:"Bảo dưỡng điện máy",shortName:"Điện máy",eyebrow:"Thiết bị bền hơn",description:"Vệ sinh và tháo lắp thiết bị gia dụng với kỹ thuật viên phù hợp.",accent:"#e17932",soft:"#fff2e8",services:[
  {slug:"ve-sinh-may-lanh",name:"Vệ sinh máy lạnh",description:"Làm sạch dàn lạnh, lưới lọc và kiểm tra vận hành.",icon:"air"},
  {slug:"ve-sinh-may-nong-lanh",name:"Vệ sinh máy nóng lạnh",description:"Vệ sinh bình nóng lạnh trong phòng tắm.",icon:"shower"},
  {slug:"ve-sinh-may-giat",name:"Vệ sinh máy giặt",description:"Làm sạch lồng giặt và cặn bẩn tích tụ.",icon:"washer"},
  {slug:"thao-lap-may-lanh",name:"Tháo lắp máy lạnh",description:"Tháo, di dời và lắp đặt lại thiết bị.",icon:"wrench"}
 ]},
 {id:"advanced",name:"Tiện ích nâng cao",shortName:"Tiện ích",eyebrow:"Nhẹ việc mỗi ngày",description:"Những việc nhỏ nhưng tốn thời gian, được hỗ trợ theo lịch của gia đình.",accent:"#16859b",soft:"#e6f7fa",services:[
  {slug:"giat-ui",name:"Giặt ủi",description:"Thu gom, giặt, sấy và giao lại theo lịch.",icon:"shirt"},
  {slug:"nau-an-gia-dinh",name:"Nấu ăn gia đình",description:"Chuẩn bị bữa ăn tại nhà theo khẩu vị.",icon:"cooking"},
  {slug:"di-cho",name:"Đi chợ",description:"Mua thực phẩm và nhu yếu phẩm theo danh sách.",icon:"basket"},
  {slug:"khu-khuan",name:"Khử khuẩn",description:"Khử khuẩn không gian nhà ở và nơi làm việc.",icon:"shield"}
 ]}
];

export const utilityServices=serviceCategories.flatMap(category=>category.services.map(service=>({...service,category})));
