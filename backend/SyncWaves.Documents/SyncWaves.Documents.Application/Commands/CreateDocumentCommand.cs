using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using SyncWaves.Documents.Application.DTOs;

namespace SyncWaves.Documents.Application.Commands;

public class CreateDocumentCommand : IRequest<DocumentDTO>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}
