using AutoMapper;

namespace FlemanApi.Tests.TestHelpers;

// A real AutoMapper IMapper built from the app's own MappingProfile —
// exercises the actual ForMember/enum-string conversions rather than a
// stubbed-out mock, which would just paper over mapping mistakes.
public static class TestMapperFactory
{
    public static IMapper Create()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FlemanApi.AutoMapper.MappingProfile>(),
            new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory());
        return config.CreateMapper();
    }
}
