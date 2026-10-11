namespace FlemanApi.Models;

// CH02 - the category a car type belongs to. Every car type that existed
// before this change is STANDARD; ELECTRIC and COMMERCIAL are new. Stored
// as its string name, like every other enum in this project.
public enum VehicleCategory { STANDARD, ELECTRIC, COMMERCIAL }
