using System.Net;
using FlemanApi.Exceptions;
using FlemanApi.Models;

namespace FlemanApi.Service.Categories;

public interface IVehicleCategoryRulesFactory
{
    IVehicleCategoryRules Create(VehicleCategory category);
}

// CH02 - parameterized factory method: the only place that knows which
// concrete rules class belongs to which category. Adding a category means
// adding one enum member, one rules class and one arm here.
public class VehicleCategoryRulesFactory : IVehicleCategoryRulesFactory
{
    public IVehicleCategoryRules Create(VehicleCategory category) => category switch
    {
        VehicleCategory.STANDARD => new StandardCategoryRules(),
        VehicleCategory.ELECTRIC => new ElectricCategoryRules(),
        VehicleCategory.COMMERCIAL => new CommercialCategoryRules(),
        _ => throw new ApiException($"Unsupported vehicle category: {category}", HttpStatusCode.BadRequest),
    };
}
