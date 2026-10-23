using HTMLQuestPDF.Extensions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SyncWaves.Documents.Domain.Abstractions;
using System.Text.RegularExpressions;

namespace SyncWaves.Documents.Infrastructure.Helpers;

public class DocumentPdfGenerator : IDocumentPdfGenerator
{
    public DocumentPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateAsync(string title, string content, CancellationToken ct)
    {
        var docTitle = string.IsNullOrWhiteSpace(title) ? "Untitled" : title;
        var html = content ?? string.Empty;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().HTML(handler =>
                    {
                        handler.SetHtml(html);

                        handler.SetTextStyleForHtmlElement(
                            "h1",
                            TextStyle.Default.FontSize(26));

                        handler.SetTextStyleForHtmlElement(
                            "h2",
                            TextStyle.Default.FontSize(19.5f));

                        handler.SetTextStyleForHtmlElement(
                            "h3",
                            TextStyle.Default.FontSize(15.21f));

                        handler.SetTextStyleForHtmlElement(
                            "p",
                            TextStyle.Default.FontSize(13));

                        handler.SetTextStyleForHtmlElement(
                            "strong",
                            TextStyle.Default.FontSize(13).Bold());

                        handler.SetTextStyleForHtmlElement(
                            "em",
                            TextStyle.Default.FontSize(13).Italic());

                        handler.SetContainerStyleForHtmlElement(
                            "ul",
                            c => c.PaddingVertical(4));

                        handler.SetContainerStyleForHtmlElement(
                            "ol",
                            c => c.PaddingVertical(4));

                        handler.SetListVerticalPadding(4);
                    });
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdfBytes);
    }
}
