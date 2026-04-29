using LawncareApi.Controllers;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace LawncareApi.Tests;

public class TreatmentsControllerTests
{
    private const string TestUid = "test-uid";

    private static TreatmentsController CreateController(InMemoryTreatmentService? svc = null)
    {
        var controller = new TreatmentsController(
            svc ?? new InMemoryTreatmentService(),
            NullLogger<TreatmentsController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, TestUid) }))
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTreatmentsExist()
    {
        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var treatments = Assert.IsAssignableFrom<IReadOnlyList<Treatment>>(ok.Value);
        Assert.Empty(treatments);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithSingleZone()
    {
        var controller = CreateController();
        var request = new TreatmentRequest
        {
            ZoneIds = ["zone-1"],
            ZoneNames = ["Front Lawn"],
            ApplicationDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Fertilizer", AmountApplied = 3.5, AmountUnit = "oz" },
            ],
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var treatment = Assert.IsType<Treatment>(created.Value);

        Assert.NotNull(treatment.Id);
        Assert.Single(treatment.ZoneIds);
        Assert.Equal("zone-1", treatment.ZoneIds[0]);
        Assert.Single(treatment.ZoneNames);
        Assert.Equal("Front Lawn", treatment.ZoneNames[0]);
        Assert.Single(treatment.LineItems);
        Assert.Equal("Fertilizer", treatment.LineItems[0].ProductName);
        Assert.Equal(3.5, treatment.LineItems[0].AmountApplied);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithMultipleZones()
    {
        var controller = CreateController();
        var request = new TreatmentRequest
        {
            ZoneIds = ["zone-1", "zone-2", "zone-3"],
            ZoneNames = ["Front Lawn", "Back Lawn", "Side Yard"],
            ApplicationDate = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Pre-emergent", AmountApplied = 10.0, AmountUnit = "oz" },
            ],
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var treatment = Assert.IsType<Treatment>(created.Value);

        Assert.Equal(3, treatment.ZoneIds.Count);
        Assert.Equal(3, treatment.ZoneNames.Count);
        Assert.Contains("zone-2", treatment.ZoneIds);
        Assert.Contains("Back Lawn", treatment.ZoneNames);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithGdd()
    {
        var controller = CreateController();
        var request = new TreatmentRequest
        {
            ZoneIds = ["zone-1"],
            ZoneNames = ["Front Lawn"],
            ApplicationDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-2", ProductName = "Fungicide", AmountApplied = 2.0, AmountUnit = "oz" },
            ],
            Gdd = 450.5,
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var treatment = Assert.IsType<Treatment>(created.Value);

        Assert.Equal(450.5, treatment.Gdd);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithoutGdd_WhenGddNotProvided()
    {
        var controller = CreateController();
        var request = new TreatmentRequest
        {
            ZoneIds = ["zone-1"],
            ZoneNames = ["Front Lawn"],
            ApplicationDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Fertilizer", AmountApplied = 3.5, AmountUnit = "oz" },
            ],
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var treatment = Assert.IsType<Treatment>(created.Value);

        Assert.Null(treatment.Gdd);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.GetById("nonexistent", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsTreatment_AfterCreate()
    {
        var svc = new InMemoryTreatmentService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(TestUid, new TreatmentRequest
        {
            ZoneIds = ["zone-1"],
            ZoneNames = ["Back Lawn"],
            ApplicationDate = new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Herbicide", AmountApplied = 5.0, AmountUnit = "oz" },
            ],
        });

        var result = await controller.GetById(created.Id!, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var treatment = Assert.IsType<Treatment>(ok.Value);
        Assert.Equal("Herbicide", treatment.LineItems[0].ProductName);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.Update(
            "none",
            new TreatmentRequest
            {
                ZoneIds = ["zone-1"],
                ZoneNames = ["Front Lawn"],
                ApplicationDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LineItems =
                [
                    new TreatmentLineItemRequest { ProductId = "p", ProductName = "X", AmountApplied = 1.0, AmountUnit = "oz" },
                ],
            },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_UpdatesZonesAndGdd()
    {
        var svc = new InMemoryTreatmentService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(TestUid, new TreatmentRequest
        {
            ZoneIds = ["zone-1"],
            ZoneNames = ["Front Lawn"],
            ApplicationDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Fertilizer", AmountApplied = 3.5, AmountUnit = "oz" },
            ],
        });

        var updateRequest = new TreatmentRequest
        {
            ZoneIds = ["zone-1", "zone-2"],
            ZoneNames = ["Front Lawn", "Back Lawn"],
            ApplicationDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Fertilizer", AmountApplied = 4.0, AmountUnit = "oz" },
            ],
            Gdd = 300.0,
        };

        var result = await controller.Update(created.Id!, updateRequest, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var treatment = Assert.IsType<Treatment>(ok.Value);

        Assert.Equal(2, treatment.ZoneIds.Count);
        Assert.Equal(2, treatment.ZoneNames.Count);
        Assert.Equal(300.0, treatment.Gdd);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ForExistingTreatment()
    {
        var svc = new InMemoryTreatmentService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(TestUid, new TreatmentRequest
        {
            ZoneIds = ["zone-1"],
            ZoneNames = ["Side Yard"],
            ApplicationDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            LineItems =
            [
                new TreatmentLineItemRequest { ProductId = "prod-1", ProductName = "Weed killer", AmountApplied = 2.0, AmountUnit = "oz" },
            ],
        });

        var result = await controller.Delete(created.Id!, CancellationToken.None);
        Assert.IsType<NoContentResult>(result);

        var getResult = await controller.GetById(created.Id!, CancellationToken.None);
        Assert.IsType<NotFoundResult>(getResult);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.Delete("ghost", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }
}
