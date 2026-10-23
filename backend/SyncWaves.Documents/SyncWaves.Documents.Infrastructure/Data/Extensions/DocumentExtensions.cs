using SyncWaves.Documents.Domain.Entities;
using SyncWaves.Documents.Infrastructure.Data.Models;

namespace SyncWaves.Documents.Infrastructure.Data.Extensions;

public static class DocumentExtensions
{
    extension(DocumentModel document)
    {
        public DocumentEntity AsEntity() =>
            new DocumentEntity
            {
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                UserId = document.UserId,
                Version = document.Version,
                Collaborators = document.Collaborators.Select(x => x.AsEntity()).ToList(),
            };
    }

    extension(DocumentEntity document)
    {
        public DocumentModel AsModel() =>
            new DocumentModel
            {   
                Id = document.Id,
                Title = document.Title,
                Content = document.Content,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                UserId = document.UserId,
                Version = document.Version,
                Collaborators = document.Collaborators.Select(x => x.AsModel()).ToList(),
            };
    }

    extension(DocumentCollaboratorEntity collaborator)
    {
        public DocumentCollaboratorModel AsModel() =>
            new DocumentCollaboratorModel
            {
                DocumentId = collaborator.DocumentId,
                CanEdit = collaborator.CanEdit,
                Document = collaborator.Document.AsModel(),
                UserId= collaborator.UserId
            };
    }

    extension(DocumentCollaboratorModel collaborator)
    {
        public DocumentCollaboratorEntity AsEntity() =>
            new DocumentCollaboratorEntity
            {
                DocumentId = collaborator.DocumentId,
                CanEdit = collaborator.CanEdit,
                Document = collaborator.Document.AsEntity(),
                UserId = collaborator.UserId
            };
    }
}
