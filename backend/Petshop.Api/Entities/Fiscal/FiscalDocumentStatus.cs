namespace Petshop.Api.Entities.Fiscal;

public enum FiscalDocumentStatus
{
    Pending,     // aguardando processamento
    Authorized,  // autorizado pela SEFAZ
    Rejected,    // rejeitado pela SEFAZ
    Cancelled,   // cancelado pelo emissor
    Contingency, // em contingência (offline ou permanente)
    Expired      // ultrapassou o prazo legal de regularização em contingência (48h) — requer ação manual
}
