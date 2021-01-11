using AutoMapper;
using SpookVooper.Api.Entities;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Controllers.API.Profiles
{
    public class GroupProfile : Profile
    {
        public GroupProfile()
        {
            CreateMap<SpookVooper.Web.Entities.Groups.Group, GroupSnapshot>();
        }
    }
}
