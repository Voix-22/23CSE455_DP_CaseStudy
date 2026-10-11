using AutoMapper;
using FlemanApi.Models;
using FlemanApi.DTO;

namespace FlemanApi.AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<State, StateDTO>().ReverseMap();
        CreateMap<City, CityDTO>().ReverseMap();
        CreateMap<Hub, HubDTO>().ReverseMap();
        CreateMap<Airport, AirportDTO>().ReverseMap();
        CreateMap<Addon, AddonDTO>().ReverseMap();

        CreateMap<CarType, CarTypeDTO>().ReverseMap();
        CreateMap<Car, CarDTO>()
            .ForMember(d => d.FuelType, o => o.MapFrom(s => s.FuelType != null ? s.FuelType.ToString() : null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status != null ? s.Status.ToString() : null))
            .ReverseMap()
            .ForMember(d => d.FuelType, o => o.MapFrom(s => s.FuelType != null ? Enum.Parse<FuelType>(s.FuelType) : (FuelType?)null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status != null ? Enum.Parse<CarStatus>(s.Status) : (CarStatus?)null));

        CreateMap<Customer, CustomerDTO>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status != null ? s.Status.ToString() : null));

        CreateMap<InvoiceHeader, InvoiceResponseDTO>()
            .ForMember(d => d.PaymentType, o => o.MapFrom(s => s.PaymentType != null ? s.PaymentType.ToString() : null))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus != null ? s.PaymentStatus.ToString() : null));

        CreateMap<BookingDetail, BookingDetailDTO>();
    }
}
