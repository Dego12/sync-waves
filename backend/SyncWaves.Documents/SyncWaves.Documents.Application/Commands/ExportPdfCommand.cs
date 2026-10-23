using MediatR;
using SyncWaves.Documents.Application.DTOs;

namespace SyncWaves.Documents.Application.Commands;

public class ExportPdfCommand : IRequest<PdfDTO>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
