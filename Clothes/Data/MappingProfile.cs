using AutoMapper;
using Clothes.Models.DTOs;
using Clothes.Models.DTOs.CartDTOs;
using Clothes.Models.DTOs.RegistrationDTOs;
using Clothes.Models.Entities;

namespace Clothes.Data;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapping for User Registration
        CreateMap<UserForRegistrationDto, User>()
            .ForMember(user => user.UserName, opt => 
                opt.MapFrom(u => u.Email));

        // Mapping from Cart to CartDTO
        CreateMap<Cart, CartDTO>()
            .ForMember(dto => dto.Id, opt => opt.MapFrom(cart => cart.Id))
            .ForMember(dto => dto.Items, opt => opt.MapFrom(cart => cart.Items));

        // Mapping from CartItem to CartItemDTO
        CreateMap<CartItem, CartItemDTO>()
            .ForMember(dto => dto.ClothingType, opt => opt.MapFrom(ci => ci.Clothes.Type.ToString()))
            .ForMember(dto => dto.TotalPrice, opt => opt.MapFrom(ci => ci.Quantity * ci.Clothes.Price))
            .ForMember(dto => dto.ImageUrl, opt => opt.MapFrom(ci => ci.Clothes.ImageUrl))
            .ForMember(dto => dto.Size, opt => opt.MapFrom(ci => ci.Clothes.Size))
            .ForMember(dto => dto.Color, opt => opt.MapFrom(ci => ci.Clothes.Color))
            .ForMember(dto => dto.Material, opt => opt.MapFrom(ci => ci.Clothes.Material))
            .ForMember(dto => dto.Gender, opt => opt.MapFrom(ci => ci.Clothes.Gender))
            .ForMember(dto => dto.Description, opt => opt.MapFrom(ci => ci.Clothes.Description));
        
        // Mapping from AddToCartDto to CartItem
        CreateMap<AddToCartDto, CartItem>()
            .ForMember(ci => ci.ClothesId, opt => opt.MapFrom(dto => dto.ProductId))
            .ForMember(ci => ci.Quantity, opt => opt.MapFrom(dto => dto.Quantity));
        
        CreateMap<AddClothesDto, Models.Entities.Clothes>()
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.DateAdded, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<UpdateClothesDto, Models.Entities.Clothes>()
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}