using SyncWaves.RTC.Domain.Entities;
using SyncWaves.RTC.Infrastructure.Data.Models;

namespace SyncWaves.RTC.Infrastructure.Data.Extensions;

public static class DocumentExtensions
{
    extension(DocumentStateModel documentState)
    {
        public DocumentStateEntity AsEntity() =>
            new DocumentStateEntity
            {
                DocumentId = documentState.DocumentId,
                Content = documentState.Content,
                CurrentVersion = documentState.CurrentVersion,
                LastUpdated = documentState.LastUpdated
            };
    }

    extension(DocumentVersionModel documentVersion)
    {
        public DocumentVersionEntity AsEntity() =>
            new DocumentVersionEntity
            {
                Id = documentVersion.Id,
                DocumentId = documentVersion.Id,
                VersionNumber = documentVersion.VersionNumber,
                Content = documentVersion.Content,
                CreatedAt = documentVersion.CreatedAt
            };
    }

    extension(DocumentStateEntity documentState)
    {
        public DocumentStateModel AsModel() =>
            new DocumentStateModel
            {
                DocumentId = documentState.DocumentId,
                Content = documentState.Content,
                CurrentVersion = documentState.CurrentVersion,
                LastUpdated = documentState.LastUpdated
            };
    }

    extension(DocumentVersionEntity documentVersion)
    {
        public DocumentVersionModel AsModel() =>
            new DocumentVersionModel
            {
                Id = documentVersion.Id,
                DocumentId = documentVersion.DocumentId,
                Content = documentVersion.Content,
                VersionNumber = documentVersion.VersionNumber,
                CreatedAt = documentVersion.CreatedAt
            };
    }
}
