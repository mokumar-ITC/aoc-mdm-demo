using AutoMapper;
using AOC_MDM.Core.Entities;
using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.AutoMapperProfile
{

    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
          
           // CreateMap<ETLBatchDTO, ETLBatch>().ReverseMap();


            CreateMap<MDMSpendAreaDTO, MDMSpendArea>().ReverseMap();

            CreateMap<MDMTeamDTO, MDMTeam>().ReverseMap();

            CreateMap<MDMAOCLevel1DTO, MDMAOCLevel1>().ReverseMap();

            CreateMap<MDMAOCLevel2DTO, MDMAOCLevel2>().ReverseMap();

            CreateMap<MDMAOCLevel3DTO, MDMAOCLevel3>().ReverseMap();


            CreateMap<MDMViewVendorRefAttrsDTO, MDMViewVendorRefAttrs>().ReverseMap();
            


            CreateMap<MDMVendorRefAttriDTO, MDMVendorRefAttributes>().ReverseMap();

            CreateMap<MDMVendorRefAttributesAuditDTO, MDMVendorRefAttributesAudit>().ReverseMap();


            CreateMap<MDMItemRefAttriDTO, MDMItemRefAttributes>().ReverseMap();

            CreateMap<MDMItemRefAttributesAuditDTO, MDMItemRefAttributesAudit>().ReverseMap();

            CreateMap<MDMViewItemRefAttrsDTO, MDMViewItemRefAttrs>().ReverseMap();

            CreateMap<MDMItemSupplierRefAttriDTO, MDMItemSupplierRefAttributes>().ReverseMap();

            CreateMap<MDMItemSupplierRefAttributesAuditDTO, MDMItemSupplierRefAttributesAudit>().ReverseMap();

            CreateMap<MDMViewItemSupplierRefAttrsDTO, MDMViewItemSupplierRefAttrs>().ReverseMap();

            CreateMap<UserRoleDTO, UserRole>().ReverseMap();

            CreateMap<MDMViewAOCLevel1DTO, MDMViewAOCLevel1>().ReverseMap();

            CreateMap<MDMViewAOCLevel2DTO, MDMViewAOCLevel2>().ReverseMap();

            CreateMap<MDMViewAOCLevel3DTO, MDMViewAOCLevel3>().ReverseMap();
        }
    }
}
