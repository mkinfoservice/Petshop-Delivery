using Microsoft.AspNetCore.Mvc;
using Petshop.Api.Contracts.Delivery;
using Petshop.Api.Controllers;
using Petshop.Api.Entities.Delivery;
using Petshop.Api.Services.Audit;
using Xunit;

namespace Petshop.Api.Tests;

/// <summary>
/// O teste de segurança mais importante do sistema: uma empresa nunca pode ver
/// dado de outra. vendApps é multi-tenant numa única base Postgres particionada
/// por CompanyId — sem isolamento no nível de código, não existe isolamento
/// nenhum. Em 2026-08-18 uma auditoria manual achou Deliverer/Route sem CompanyId
/// nenhum (qualquer tenant via qualquer outro) — este teste existe pra isso não
/// voltar a acontecer silenciosamente.
/// </summary>
public class MultiTenantIsolationTests
{
    private static readonly Guid CompanyA = Guid.NewGuid();
    private static readonly Guid CompanyB = Guid.NewGuid();

    [Fact]
    public async Task DeliverersList_SoRetornaEntregadoresDaPropriaEmpresa()
    {
        using var db = TestDbFactory.CreateContext();

        db.Deliverers.Add(new Deliverer { CompanyId = CompanyA, Name = "Entregador A", Phone = "11900000001", PinHash = "x", IsActive = true });
        db.Deliverers.Add(new Deliverer { CompanyId = CompanyB, Name = "Entregador B", Phone = "11900000002", PinHash = "x", IsActive = true });
        await db.SaveChangesAsync();

        var controller = new DeliverersController(db, new OperationalAuditService(db))
        {
            ControllerContext = TestDbFactory.BuildAdminContext(CompanyA),
        };

        var result = await controller.List(isActive: null, ct: default);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);

        Assert.Single(items);
    }

    [Fact]
    public async Task DeliverersGetById_NaoAcessaEntregadorDeOutraEmpresa()
    {
        using var db = TestDbFactory.CreateContext();

        var delivererB = new Deliverer { CompanyId = CompanyB, Name = "Entregador B", Phone = "11900000002", PinHash = "x", IsActive = true };
        db.Deliverers.Add(delivererB);
        await db.SaveChangesAsync();

        var controller = new DeliverersController(db, new OperationalAuditService(db))
        {
            ControllerContext = TestDbFactory.BuildAdminContext(CompanyA),
        };

        // Empresa A pedindo, por Id, um entregador que sabe existir mas pertence à empresa B.
        var result = await controller.GetById(delivererB.Id, ct: default);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeliverersResetPin_NaoAlteraPinDeEntregadorDeOutraEmpresa()
    {
        using var db = TestDbFactory.CreateContext();

        var delivererB = new Deliverer { CompanyId = CompanyB, Name = "Entregador B", Phone = "11900000002", PinHash = "hash-original", IsActive = true };
        db.Deliverers.Add(delivererB);
        await db.SaveChangesAsync();

        var controller = new DeliverersController(db, new OperationalAuditService(db))
        {
            ControllerContext = TestDbFactory.BuildAdminContext(CompanyA),
        };

        var result = await controller.ResetPin(delivererB.Id, new ResetPinRequest { Pin = "1234" }, ct: default);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("hash-original", delivererB.PinHash); // não deve ter sido tocado
    }
}
