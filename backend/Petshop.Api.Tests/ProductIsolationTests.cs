using Microsoft.AspNetCore.Mvc;
using Petshop.Api.Controllers;
using Petshop.Api.Entities.Audit;
using Petshop.Api.Models;
using Xunit;

namespace Petshop.Api.Tests;

/// <summary>
/// Cobre o segundo vazamento cross-tenant achado na auditoria de 2026-08-18:
/// GetPriceHistory/GetChangeLogs buscavam por Id de produto sem checar CompanyId,
/// vazando custo/margem/histórico de alteração de produto de outra empresa.
/// </summary>
public class ProductIsolationTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    [Fact]
    public async Task GetPriceHistory_NaoVazaHistoricoDeProdutoDeOutraEmpresa()
    {
        using var db = TestDbFactory.CreateContext();

        var productB = new Product
        {
            CompanyId = CompanyB,
            Name = "Produto da Empresa B",
            Slug = "produto-empresa-b",
            CategoryId = Guid.NewGuid(),
        };
        db.Products.Add(productB);
        db.ProductPriceHistories.Add(new ProductPriceHistory
        {
            ProductId = productB.Id,
            PriceCents = 9999,
            CostCents = 5000,
            MarginPercent = 50,
        });
        await db.SaveChangesAsync();

        var controller = new AdminProductsController(db, new FakeImageStorageProvider())
        {
            ControllerContext = TestDbFactory.BuildAdminContext(CompanyA),
        };

        var result = await controller.GetPriceHistory(productB.Id, ct: default);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetChangeLogs_NaoVazaHistoricoDeProdutoDeOutraEmpresa()
    {
        using var db = TestDbFactory.CreateContext();

        var productB = new Product
        {
            CompanyId = CompanyB,
            Name = "Produto da Empresa B",
            Slug = "produto-empresa-b-2",
            CategoryId = Guid.NewGuid(),
        };
        db.Products.Add(productB);
        db.ProductChangeLogs.Add(new ProductChangeLog
        {
            ProductId = productB.Id,
            FieldName = "PriceCents",
            OldValue = "1000",
            NewValue = "2000",
        });
        await db.SaveChangesAsync();

        var controller = new AdminProductsController(db, new FakeImageStorageProvider())
        {
            ControllerContext = TestDbFactory.BuildAdminContext(CompanyA),
        };

        var result = await controller.GetChangeLogs(productB.Id, ct: default);

        Assert.IsType<NotFoundResult>(result);
    }
}
