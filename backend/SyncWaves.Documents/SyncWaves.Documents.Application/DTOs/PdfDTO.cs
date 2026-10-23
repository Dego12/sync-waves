namespace SyncWaves.Documents.Application.DTOs;

public class PdfDTO
{
    public byte[]? PdfBytes { get; set; }
    public string FileName { get; set; } = string.Empty;
}
