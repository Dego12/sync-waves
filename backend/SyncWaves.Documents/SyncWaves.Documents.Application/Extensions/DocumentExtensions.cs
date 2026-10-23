using SyncWaves.Documents.Application.DTOs;
using SyncWaves.Documents.Domain.Entities;

namespace SyncWaves.Documents.Application.Extensions;

public static class DocumentExtensions
{
    extension(DocumentEntity document)
    {
        public DocumentDTO AsDTO() => new DocumentDTO
        {
            Id = document.Id,
            Title = document.Title,
            Content = document.Content,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            UserId = document.UserId,
            Version = document.Version
        };
    }
}
