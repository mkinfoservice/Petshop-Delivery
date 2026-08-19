using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using System.Security.Claims;

namespace Petshop.Api.Tests;

/// <summary>
/// Helpers para testar controllers diretamente contra um AppDbContext InMemory,
/// sem precisar de servidor HTTP nem de um Postgres real.
///
/// Escopo deliberado: só cobre queries LINQ simples. Qualquer código sob teste que
/// use ExecuteSqlRawAsync/ExecuteSqlAsync (SQL Postgres bruto) não funciona aqui —
/// nesse caso o teste precisa de um Postgres real (Testcontainers ou instância local).
/// </summary>
public static class TestDbFactory
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Monta um ControllerContext com um JWT fake carregando a claim companyId.</summary>
    public static ControllerContext BuildAdminContext(Guid companyId, string role = "admin")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("role", role),
            new("companyId", companyId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }
}
