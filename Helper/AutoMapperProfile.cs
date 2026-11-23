using AutoMapper;
using Du_An_Ecomerce.Data;
using Du_An_Ecomerce.ViewModel;

namespace Du_An_Ecomerce.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<RegisterViewModel, KhachHang>()
                .ForMember(dest => dest.MaKh, opt => opt.MapFrom(src => src.MaKH))
                .ForMember(dest => dest.MatKhau, opt => opt.MapFrom(src => src.MatKhau))
                .ForMember(dest => dest.HoTen, opt => opt.MapFrom(src => src.HoTen))
                .ForMember(dest => dest.GioiTinh, opt => opt.MapFrom(src => src.GioiTinh))
                .ForMember(dest => dest.NgaySinh, opt => opt.MapFrom(src => src.NgaySinh))
                .ForMember(dest => dest.DiaChi, opt => opt.MapFrom(src => src.DiaChi))
                .ForMember(dest => dest.DienThoai, opt => opt.MapFrom(src => src.DienThoai))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Hinh, opt => opt.MapFrom(src => src.Hinh));
        }
    }
}