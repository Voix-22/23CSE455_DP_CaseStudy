using FlemanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlemanApi.Data;

// Mirrors com.fleman.config.DataSeeder — India-based fixture data weighted
// toward Maharashtra: 12 states, 38 cities, 17 city hubs + 13 airport
// kiosks, 6 car types (INR), an 18-car hand-picked fleet topped up so every
// one of the 30 hubs has at least MinCarsPerHubAndType AVAILABLE cars per
// category, 3 add-ons, one sample customer + one sample CONFIRMED booking.
// No entity relationships are used anywhere — every cross-table reference
// is a plain id, matching the entities' own design.
public static class DataSeeder
{
    private const int MinCarsPerHubAndType = 10;

    public static async Task SeedAsync(FlemanDbContext context)
    {
        if (await context.States.AnyAsync()) return;

        var stateIds = await SeedStatesAsync(context);
        var cityIds = await SeedCitiesAsync(context, stateIds);
        var hubIds = await SeedHubsAsync(context, cityIds, stateIds);
        await SeedAirportsAndKioskHubsAsync(context, cityIds, stateIds, hubIds);
        var carTypeIds = await SeedCarTypesAsync(context);
        var handPickedCounts = await SeedCarsAsync(context, carTypeIds, hubIds);
        await TopUpFleetToMinimumAsync(context, carTypeIds, hubIds, handPickedCounts);
        var addonIds = await SeedAddonsAsync(context);
        await SeedCustomerAndSampleBookingAsync(context, cityIds, stateIds, hubIds, carTypeIds, addonIds);
    }

    private static async Task<Dictionary<int, long>> SeedStatesAsync(FlemanDbContext context)
    {
        string[] names =
        {
            "Maharashtra", "Karnataka", "Delhi", "Gujarat", "Tamil Nadu", "Madhya Pradesh",
            "Rajasthan", "West Bengal", "Telangana", "Uttar Pradesh", "Punjab", "Kerala",
        };
        var ids = new Dictionary<int, long>();
        for (var i = 0; i < names.Length; i++)
        {
            var s = new State { StateName = names[i] };
            context.States.Add(s);
            await context.SaveChangesAsync();
            ids[i + 1] = s.StateId;
        }
        return ids;
    }

    private static async Task<Dictionary<int, long>> SeedCitiesAsync(FlemanDbContext context, Dictionary<int, long> stateIds)
    {
        (int id, string name, int stateIdx)[] rows =
        {
            (1, "Mumbai", 1), (2, "Pune", 1), (3, "Nagpur", 1), (4, "Nashik", 1),
            (5, "Thane", 1), (6, "Navi Mumbai", 1), (7, "Chhatrapati Sambhajinagar", 1), (8, "Kolhapur", 1),
            (9, "Bengaluru", 2), (10, "Mysuru", 2), (11, "Hubballi", 2),
            (12, "New Delhi", 3), (13, "Dwarka", 3), (14, "Rohini", 3),
            (15, "Ahmedabad", 4), (16, "Surat", 4), (17, "Vadodara", 4),
            (18, "Chennai", 5), (19, "Coimbatore", 5), (20, "Madurai", 5),
            (21, "Bhopal", 6), (22, "Indore", 6), (23, "Jabalpur", 6),
            (24, "Jaipur", 7), (25, "Udaipur", 7), (26, "Jodhpur", 7),
            (27, "Kolkata", 8), (28, "Howrah", 8), (29, "Siliguri", 8),
            (30, "Hyderabad", 9), (31, "Warangal", 9),
            (32, "Lucknow", 10), (33, "Noida", 10), (34, "Kanpur", 10),
            (35, "Ludhiana", 11), (36, "Amritsar", 11),
            (37, "Kochi", 12), (38, "Thiruvananthapuram", 12),
        };
        var ids = new Dictionary<int, long>();
        foreach (var r in rows)
        {
            var c = new City { CityName = r.name, StateId = stateIds[r.stateIdx] };
            context.Cities.Add(c);
            await context.SaveChangesAsync();
            ids[r.id] = c.CityId;
        }
        return ids;
    }

    private static async Task<Dictionary<int, long>> SeedHubsAsync(
        FlemanDbContext context, Dictionary<int, long> cityIds, Dictionary<int, long> stateIds)
    {
        (int id, string name, string address, int cityIdx, int stateIdx, string pincode, string contact, string email)[] rows =
        {
            (1, "WanderCar — Mumbai Hub", "Plot 14, Andheri-Kurla Road", 1, 1, "400059", "+91 22 4001 5566", "mumbai.hub@wandercar.example"),
            (2, "WanderCar — Pune Hub", "221, FC Road, Shivajinagar", 2, 1, "411005", "+91 20 4002 5566", "pune.hub@wandercar.example"),
            (3, "WanderCar — Nagpur Hub", "45, Wardha Road", 3, 1, "440012", "+91 712 400 5566", "nagpur.hub@wandercar.example"),
            (4, "WanderCar — Nashik Hub", "12, College Road", 4, 1, "422005", "+91 253 400 5566", "nashik.hub@wandercar.example"),
            (5, "WanderCar — Thane Hub", "8, Ghodbunder Road", 5, 1, "400607", "+91 22 4003 5566", "thane.hub@wandercar.example"),
            (6, "WanderCar — Bengaluru Hub", "77, Outer Ring Road", 9, 2, "560103", "+91 80 4004 5566", "bengaluru.hub@wandercar.example"),
            (7, "WanderCar — New Delhi Hub", "19, Connaught Place", 12, 3, "110001", "+91 11 4005 5566", "delhi.hub@wandercar.example"),
            (8, "WanderCar — Ahmedabad Hub", "5, SG Highway", 15, 4, "380015", "+91 79 4006 5566", "ahmedabad.hub@wandercar.example"),
            (9, "WanderCar — Chennai Hub", "31, Anna Salai", 18, 5, "600002", "+91 44 4007 5566", "chennai.hub@wandercar.example"),
            (10, "WanderCar — Bhopal Hub", "26, MP Nagar Zone II", 21, 6, "462011", "+91 755 400 5566", "bhopal.hub@wandercar.example"),
            (11, "WanderCar — Indore Hub", "9, Vijay Nagar Square", 22, 6, "452010", "+91 731 400 5566", "indore.hub@wandercar.example"),
            (12, "WanderCar — Jaipur Hub", "3, Malviya Nagar", 24, 7, "302017", "+91 141 400 5566", "jaipur.hub@wandercar.example"),
            (13, "WanderCar — Kolkata Hub", "58, Park Street", 27, 8, "700016", "+91 33 4008 5566", "kolkata.hub@wandercar.example"),
            (14, "WanderCar — Hyderabad Hub", "22, Banjara Hills Road", 30, 9, "500034", "+91 40 4009 5566", "hyderabad.hub@wandercar.example"),
            (15, "WanderCar — Lucknow Hub", "17, Hazratganj", 32, 10, "226001", "+91 522 400 5566", "lucknow.hub@wandercar.example"),
            (16, "WanderCar — Ludhiana Hub", "40, Ferozepur Road", 35, 11, "141002", "+91 161 400 5566", "ludhiana.hub@wandercar.example"),
            (17, "WanderCar — Kochi Hub", "6, Marine Drive", 37, 12, "682031", "+91 484 400 5566", "kochi.hub@wandercar.example"),
        };
        var ids = new Dictionary<int, long>();
        foreach (var r in rows)
        {
            ids[r.id] = await SaveHubAsync(context, r.name, r.address, cityIds[r.cityIdx], stateIds[r.stateIdx], r.pincode, r.contact, r.email);
        }
        return ids;
    }

    private static async Task<long> SaveHubAsync(
        FlemanDbContext context, string name, string address, long cityId, long stateId, string pincode, string contactNo, string email)
    {
        var h = new Hub { HubName = name, Address = address, CityId = cityId, StateId = stateId, Pincode = pincode, ContactNo = contactNo, Email = email };
        context.Hubs.Add(h);
        await context.SaveChangesAsync();
        return h.HubId;
    }

    private static async Task SeedAirportsAndKioskHubsAsync(
        FlemanDbContext context, Dictionary<int, long> cityIds, Dictionary<int, long> stateIds, Dictionary<int, long> hubIds)
    {
        // Kiosk hub local indices start at 20 to stay clear of the 17 city-hub indices (1-17).
        (string code, string name, int cityIdx, int stateIdx, int kioskHubIdx)[] rows =
        {
            ("BOM", "Chhatrapati Shivaji Maharaj International Airport", 1, 1, 20),
            ("PNQ", "Pune Airport", 2, 1, 21),
            ("NAG", "Dr. Babasaheb Ambedkar International Airport", 3, 1, 22),
            ("ISK", "Nashik Airport", 4, 1, 23),
            ("BLR", "Kempegowda International Airport", 9, 2, 24),
            ("DEL", "Indira Gandhi International Airport", 12, 3, 25),
            ("AMD", "Sardar Vallabhbhai Patel International Airport", 15, 4, 26),
            ("MAA", "Chennai International Airport", 18, 5, 27),
            ("IDR", "Devi Ahilya Bai Holkar Airport", 22, 6, 28),
            ("JAI", "Jaipur International Airport", 24, 7, 29),
            ("CCU", "Netaji Subhas Chandra Bose International Airport", 27, 8, 30),
            ("HYD", "Rajiv Gandhi International Airport", 30, 9, 31),
            ("LKO", "Chaudhary Charan Singh International Airport", 32, 10, 32),
        };

        foreach (var r in rows)
        {
            var kioskHub = await SaveHubAsync(
                context, $"WanderCar — {r.code} Airport Kiosk", $"{r.name} . Rental Car Counter",
                cityIds[r.cityIdx], stateIds[r.stateIdx], "000000", "1800-123-5566", "airport.kiosk@wandercar.example");
            hubIds[r.kioskHubIdx] = kioskHub;

            context.Airports.Add(new Airport
            {
                AirportCode = r.code,
                AirportName = r.name,
                CityId = cityIds[r.cityIdx],
                StateId = stateIds[r.stateIdx],
                HubId = kioskHub,
            });
        }
        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<int, long>> SeedCarTypesAsync(FlemanDbContext context)
    {
        (int id, string name, double daily, double weekly, double monthly, string image)[] rows =
        {
            (1, "Economy", 15.0, 100.0, 2000.0, "/cars/economy.svg"),
            (2, "Compact", 20.0, 120.0, 3000.0, "/cars/compact.svg"),
            (3, "Sedan", 30.0, 200.0, 5000.0, "/cars/sedan.svg"),
            (4, "SUV", 40.0, 250.0, 5000.0, "/cars/suv.svg"),
            (5, "Luxury", 100.0, 500.0, 25000.0, "/cars/luxury.svg"),
            (6, "Minivan", 50.0, 300.0, 8000.0, "/cars/minivan.svg"),
        };
        var ids = new Dictionary<int, long>();
        foreach (var r in rows)
        {
            var ct = new CarType
            {
                CarTypeName = r.name,
                DailyRate = r.daily,
                WeeklyRate = r.weekly,
                MonthlyRate = r.monthly,
                RateValidFrom = new DateOnly(2026, 1, 1),
                RateValidTo = new DateOnly(2026, 12, 31),
                ImagePath = r.image,
            };
            context.CarTypes.Add(ct);
            await context.SaveChangesAsync();
            ids[r.id] = ct.CarTypeId;
        }
        return ids;
    }

    private static async Task<Dictionary<string, int>> SeedCarsAsync(
        FlemanDbContext context, Dictionary<int, long> carTypeIds, Dictionary<int, long> hubIds)
    {
        // 14 of 18 cars sit at Maharashtra hubs (1-5); one each at Bengaluru
        // and Ahmedabad, and two at the new Madhya Pradesh hubs (10, 11).
        (int carTypeIdx, int hubIdx, string vehicleNo, string brand, string model, int year, string color,
            FuelType fuel, int seats, double mileage, int odometer, int fuelLevel, bool available, CarStatus status)[] rows =
        {
            (1, 1, "MH01AB1234", "Maruti Suzuki", "Swift", 2024, "White", FuelType.PETROL, 4, 22.5, 18240, 82, true, CarStatus.AVAILABLE),
            (2, 1, "MH01AC2345", "Hyundai", "i20", 2025, "Silver", FuelType.PETROL, 5, 18.0, 9120, 50, true, CarStatus.AVAILABLE),
            (3, 1, "MH01AD3456", "Honda", "City", 2025, "Grey", FuelType.PETROL, 5, 17.8, 4210, 95, true, CarStatus.AVAILABLE),
            (4, 1, "MH01AE4567", "Mahindra", "XUV700", 2024, "Black", FuelType.DIESEL, 7, 14.5, 22890, 40, false, CarStatus.BOOKED),
            (5, 1, "MH01AF5678", "Mercedes-Benz", "E-Class", 2025, "Black", FuelType.PETROL, 5, 12.0, 3100, 100, true, CarStatus.AVAILABLE),
            (4, 2, "MH12BG6789", "Tata", "Nexon EV", 2024, "Blue", FuelType.ELECTRIC, 5, 0.0, 6100, 88, true, CarStatus.AVAILABLE),
            (1, 2, "MH12BH7890", "Maruti Suzuki", "Baleno", 2024, "Red", FuelType.PETROL, 5, 21.0, 27650, 55, true, CarStatus.AVAILABLE),
            (6, 2, "MH12BI8901", "Toyota", "Innova Crysta", 2023, "White", FuelType.DIESEL, 7, 11.5, 41800, 60, true, CarStatus.UNDER_MAINTENANCE),
            (4, 3, "MH31CJ9012", "Kia", "Seltos", 2024, "Grey", FuelType.PETROL, 5, 16.5, 12200, 70, true, CarStatus.AVAILABLE),
            (3, 3, "MH31CK0123", "Hyundai", "Verna", 2025, "White", FuelType.PETROL, 5, 18.4, 2200, 90, true, CarStatus.AVAILABLE),
            (5, 4, "MH15DL1234", "BMW", "5 Series", 2025, "Black", FuelType.PETROL, 5, 13.0, 8700, 75, true, CarStatus.AVAILABLE),
            (1, 4, "MH15DM2345", "Maruti Suzuki", "Alto", 2023, "Silver", FuelType.PETROL, 4, 24.0, 38900, 45, true, CarStatus.AVAILABLE),
            (4, 5, "MH04EN3456", "Toyota", "Fortuner", 2024, "White", FuelType.DIESEL, 7, 10.5, 17600, 60, true, CarStatus.AVAILABLE),
            (2, 5, "MH04EO4567", "Hyundai", "Grand i10 Nios", 2024, "Blue", FuelType.PETROL, 5, 20.3, 5400, 85, true, CarStatus.AVAILABLE),
            (3, 6, "KA01FP5678", "Honda", "Amaze", 2024, "Grey", FuelType.PETROL, 5, 19.2, 9800, 65, true, CarStatus.AVAILABLE),
            (1, 8, "GJ01GQ6789", "Tata", "Tiago", 2023, "Red", FuelType.PETROL, 5, 23.8, 14300, 70, true, CarStatus.AVAILABLE),
            (2, 10, "MP04HR7890", "Hyundai", "i20", 2024, "White", FuelType.PETROL, 5, 18.0, 15200, 60, true, CarStatus.AVAILABLE),
            (4, 11, "MP09HS8901", "Mahindra", "XUV700", 2024, "Grey", FuelType.DIESEL, 7, 14.5, 9800, 55, true, CarStatus.AVAILABLE),
        };

        var handPickedCounts = new Dictionary<string, int>();
        foreach (var r in rows)
        {
            context.Cars.Add(new Car
            {
                CarTypeId = carTypeIds[r.carTypeIdx],
                HubId = hubIds[r.hubIdx],
                VehicleNumber = r.vehicleNo,
                Brand = r.brand,
                Model = r.model,
                ManufactureYear = r.year,
                Color = r.color,
                FuelType = r.fuel,
                SeatingCapacity = r.seats,
                Mileage = r.mileage,
                Odometer = r.odometer,
                FuelLevel = r.fuelLevel,
                IsAvailable = r.available,
                Status = r.status,
            });
            var key = $"{r.hubIdx}-{r.carTypeIdx}";
            handPickedCounts[key] = handPickedCounts.GetValueOrDefault(key) + 1;
        }
        await context.SaveChangesAsync();
        return handPickedCounts;
    }

    private static readonly Dictionary<int, (string brand, string model, FuelType fuel, int seats, double mileage)[]> TypeVariants = new()
    {
        [1] = new[] { ("Maruti Suzuki", "Alto", FuelType.PETROL, 4, 24.0), ("Tata", "Tiago", FuelType.PETROL, 5, 23.0), ("Renault", "Kwid", FuelType.PETROL, 5, 22.0) },
        [2] = new[] { ("Hyundai", "i20", FuelType.PETROL, 5, 20.0), ("Maruti Suzuki", "Baleno", FuelType.PETROL, 5, 21.0), ("Tata", "Altroz", FuelType.PETROL, 5, 19.0) },
        [3] = new[] { ("Honda", "City", FuelType.PETROL, 5, 18.0), ("Hyundai", "Verna", FuelType.PETROL, 5, 18.4), ("Skoda", "Slavia", FuelType.PETROL, 5, 19.0) },
        [4] = new[] { ("Mahindra", "XUV700", FuelType.DIESEL, 7, 15.0), ("Kia", "Seltos", FuelType.PETROL, 5, 16.5), ("Tata", "Harrier", FuelType.DIESEL, 5, 14.6) },
        [5] = new[] { ("Mercedes-Benz", "E-Class", FuelType.PETROL, 5, 12.0), ("BMW", "5 Series", FuelType.PETROL, 5, 13.0), ("Audi", "A6", FuelType.PETROL, 5, 12.5) },
        [6] = new[] { ("Toyota", "Innova Crysta", FuelType.DIESEL, 7, 12.0), ("Maruti Suzuki", "Ertiga", FuelType.PETROL, 7, 17.0), ("Kia", "Carens", FuelType.DIESEL, 7, 16.0) },
    };

    private static readonly string[] FillerColors = { "White", "Silver", "Black", "Grey", "Red", "Blue", "Maroon", "Beige" };

    // Tops up every (hub, carType) combination to at least
    // MinCarsPerHubAndType cars total, hand-picked fleet included — most
    // importantly the 13 airport kiosk hubs, which otherwise have zero
    // cars and would show an empty vehicle-selection page.
    private static async Task TopUpFleetToMinimumAsync(
        FlemanDbContext context, Dictionary<int, long> carTypeIds, Dictionary<int, long> hubIds, Dictionary<string, int> handPickedCounts)
    {
        var vehicleNumberSeq = 19; // continues on from the hand-picked fleet's 18 cars
        foreach (var hubIndex in hubIds.Keys)
        {
            for (var typeIndex = 1; typeIndex <= 6; typeIndex++)
            {
                var existing = handPickedCounts.GetValueOrDefault($"{hubIndex}-{typeIndex}");
                var toAdd = MinCarsPerHubAndType - existing;
                if (toAdd <= 0) continue;

                var variants = TypeVariants[typeIndex];
                for (var i = 0; i < toAdd; i++)
                {
                    var variant = variants[i % variants.Length];
                    context.Cars.Add(new Car
                    {
                        CarTypeId = carTypeIds[typeIndex],
                        HubId = hubIds[hubIndex],
                        VehicleNumber = $"WCR{vehicleNumberSeq++:D4}",
                        Brand = variant.brand,
                        Model = variant.model,
                        ManufactureYear = 2023 + (i % 3),
                        Color = FillerColors[i % FillerColors.Length],
                        FuelType = variant.fuel,
                        SeatingCapacity = variant.seats,
                        Mileage = variant.mileage,
                        Odometer = 500 * i,
                        FuelLevel = 100,
                        IsAvailable = true,
                        Status = CarStatus.AVAILABLE,
                    });
                }
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<int, long>> SeedAddonsAsync(FlemanDbContext context)
    {
        // Additional Driver and Roadside Assistance Plus were removed from
        // the catalog per product decision.
        (int id, string name, double daily, string description)[] rows =
        {
            (1, "GPS Navigation", 5.0, "Turn-by-turn navigation unit, mounted and ready."),
            (2, "Child Seat", 5.0, "Rear-facing or booster, installed at pickup."),
            (5, "WiFi Hotspot", 10.0, "In-car 4G hotspot, unlimited data."),
        };
        var ids = new Dictionary<int, long>();
        foreach (var r in rows)
        {
            var a = new Addon
            {
                AddonName = r.name,
                DailyRate = r.daily,
                RateValidFrom = new DateOnly(2026, 1, 1),
                RateValidTo = new DateOnly(2026, 12, 31),
                Description = r.description,
            };
            context.Addons.Add(a);
            await context.SaveChangesAsync();
            ids[r.id] = a.AddonId;
        }
        return ids;
    }

    private static async Task SeedCustomerAndSampleBookingAsync(
        FlemanDbContext context, Dictionary<int, long> cityIds, Dictionary<int, long> stateIds,
        Dictionary<int, long> hubIds, Dictionary<int, long> carTypeIds, Dictionary<int, long> addonIds)
    {
        var customer = new Customer
        {
            FullName = "Gryffindors Team",
            Email = "Gryffindors.team@email.com",
            Phone = "9876540000",
            DateOfBirth = new DateOnly(2026, 4, 1),
            DrivingLicenseNo = "MH1220230012345",
            Address1 = "12, gulmohar road juhu, mumbai",
            Address2 = "",
            CityId = cityIds[1], // Mumbai
            StateId = stateIds[1], // Maharashtra
            Pincode = "411005",
            Status = CustomerStatus.ACTIVE,
            // Same example password as before ('123456789'), properly hashed.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456789"),
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var header = new BookingHeader
        {
            ConfirmationNo = "Team-0002",
            BookingDate = new DateTime(2026, 7, 18, 9, 30, 0),
            CustomerId = customer.CustomerId,
            CarTypeId = carTypeIds[2], // Compact — reserved, not yet assigned a specific car
            CarId = null,
            PickupHubId = hubIds[1], // Mumbai
            DropHubId = hubIds[1],
            PickupDatetime = new DateTime(2026, 7, 18, 10, 0, 0),
            ReturnDatetime = new DateTime(2026, 7, 21, 10, 0, 0),
            BookingStatus = BookingStatus.CONFIRMED,
            // 3 days x Compact daily rate (2200) = 6600, + 1 GPS Navigation x 3 days (250 x 1 x 3 = 750)
            EstimatedAmount = 7350.0,
            Remarks = "",
        };
        context.BookingHeaders.Add(header);
        await context.SaveChangesAsync();

        context.BookingDetails.Add(new BookingDetail
        {
            BookingId = header.BookingId,
            AddonId = addonIds[1], // GPS Navigation
            AddonRate = 250.0,
            Quantity = 1,
            Subtotal = 750.0,
        });
        await context.SaveChangesAsync();
    }
}
