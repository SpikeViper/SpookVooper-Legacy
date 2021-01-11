using AutoMapper;
using SpookVooper.Api.Entities;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Controllers.API.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<SpookVooper.Web.Entities.User, UserSnapshot>();
        }
    }
}
