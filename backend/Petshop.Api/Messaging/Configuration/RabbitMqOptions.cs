namespace Petshop.Api.Messaging.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// Quando false, MassTransit não é registrado e o sistema opera exatamente como antes.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// URL completa de conexão — alternativa mais simples aos campos individuais.
    /// O CloudAMQP fornece esta URL no painel da conta (ex: amqps://user:pass@host/vhost).
    /// Quando preenchido, tem precedência sobre Host/Port/Username/Password/VirtualHost.
    /// Env var: RabbitMq__Uri
    /// </summary>
    public string? Uri { get; set; }

    // Campos individuais — usados apenas quando Uri não está configurado
    public string Host        { get; set; } = "localhost";
    public int    Port        { get; set; } = 5672;
    public string Username    { get; set; } = "guest";
    public string Password    { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public bool   UseSsl      { get; set; } = false;

    /// <summary>
    /// Prefixo aplicado a todas as filas do vendApps.
    /// Evita colisão em instâncias compartilhadas (ex: CloudAMQP free tier).
    /// </summary>
    public string QueuePrefix { get; set; } = "vendapps";
}
