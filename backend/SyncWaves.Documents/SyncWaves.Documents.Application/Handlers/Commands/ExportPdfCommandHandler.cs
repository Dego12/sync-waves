using MediatR;
using SyncWaves.Documents.Application.Commands;
using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Domain.Abstractions;

namespace SyncWaves.Documents.Application.Handlers.Commands;

public class ExportPdfCommandHandler : IRequestHandler<ExportPdfCommand, PdfDTO>
{
    private readonly IDocumentPdfGenerator _documentPdfGenerator;

    public ExportPdfCommandHandler(IDocumentPdfGenerator documentPdfGenerator)
    {
        _documentPdfGenerator = documentPdfGenerator;
    }

    public async Task<PdfDTO> Handle(ExportPdfCommand command, CancellationToken cancellationToken)
    {
        var pdfBytes = await _documentPdfGenerator.GenerateAsync(command.Title, command.Content, cancellationToken);

        var safeTitle = string.IsNullOrEmpty(command.Title) ? "document" : string.Concat(command.Title.Split(Path.GetInvalidFileNameChars()));

        var fileName = $"{safeTitle}.pdf";

        return new PdfDTO()
        {
            PdfBytes = pdfBytes,
            FileName = fileName,
        };
    }
}
