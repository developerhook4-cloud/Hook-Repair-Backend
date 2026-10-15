using System.Text.Json;
using FluentAssertions;
using HookRepair.Modules.Inventory.Application.Products;

namespace HookRepair.Tests;

public sealed class ProductContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string FrontendPayload = """
        {"name":"Pantalla iPhone 12","categoryId":null,"brandId":null,"modelId":null,
         "upc":"","imageUrl":null,"condition":"Nuevo","inStock":5,"stockWarning":2,
         "minimumReorderPoint":1,"price":350.5,"generalSalePrice":800,
         "distributorSalePrice":650,"isSerialized":false,"active":true}
        """;

    [Fact]
    public void Frontend_payload_deserializes_and_is_valid_for_create()
    {
        var request = JsonSerializer.Deserialize<ProductRequest>(FrontendPayload, Web);

        ProductRequestValidator.ValidateCreate(request).Should().BeNull();
        request!.SupplierId.Should().BeNull();
    }

    [Theory]
    [InlineData("""{"name":"  ","price":1,"generalSalePrice":1,"distributorSalePrice":1}""", "name")]
    [InlineData("""{"name":"A","generalSalePrice":1,"distributorSalePrice":1}""", "price")]
    [InlineData("""{"name":"A","price":-1,"generalSalePrice":1,"distributorSalePrice":1}""", "price")]
    [InlineData("""{"name":"A","price":1.234,"generalSalePrice":1,"distributorSalePrice":1}""", "price")]
    [InlineData("""{"name":"A","price":1,"generalSalePrice":1,"distributorSalePrice":1,"inStock":-1}""", "inStock")]
    [InlineData("""{"name":"A","price":1,"generalSalePrice":1,"distributorSalePrice":1,"isSerialized":true,"inStock":3}""", "inStock")]
    [InlineData("""{"name":"A","price":1,"generalSalePrice":1,"distributorSalePrice":1,"imageUrl":"ftp://x"}""", "imageUrl")]
    public void Create_rejects_invalid_payloads(string json, string field)
    {
        var request = JsonSerializer.Deserialize<ProductRequest>(json, Web);

        ProductRequestValidator.ValidateCreate(request).Should().Contain(field);
    }

    [Fact]
    public void Update_does_not_validate_in_stock()
    {
        var request = JsonSerializer.Deserialize<ProductRequest>(
            """{"name":"A","price":1,"generalSalePrice":1,"distributorSalePrice":1,"inStock":-50}""", Web);

        ProductRequestValidator.ValidateUpdate(request).Should().BeNull();
    }

    [Fact]
    public void Response_serializes_with_frontend_field_names()
    {
        var response = new ProductResponse(Guid.Empty, "A", null, null, null, [], null, "400000000001",
            null, null, 0, 0, 0, 1m, 2m, 3m, false, true);

        var json = JsonSerializer.Serialize(response, Web);

        foreach (var field in new[] { "\"id\"", "\"name\"", "\"categoryId\"", "\"brandId\"", "\"modelId\"",
                     "\"modelIds\"", "\"supplierId\"", "\"upc\"", "\"imageUrl\"", "\"condition\"", "\"inStock\"",
                     "\"stockWarning\"", "\"minimumReorderPoint\"", "\"price\"", "\"generalSalePrice\"",
                     "\"distributorSalePrice\"", "\"isSerialized\"", "\"active\"" })
            json.Should().Contain(field);
        json.Should().NotContain("organizationId");
    }
}
