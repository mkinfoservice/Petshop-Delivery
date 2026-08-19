using Petshop.Api.Services.Images;

namespace Petshop.Api.Tests;

/// <summary>Dublê mínimo — os testes de isolamento de produto não tocam armazenamento de imagem.</summary>
public class FakeImageStorageProvider : IImageStorageProvider
{
    public string ProviderName => "fake";
    public Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct) => Task.FromResult("");
    public Task DeleteAsync(string url, CancellationToken ct) => Task.CompletedTask;
}
