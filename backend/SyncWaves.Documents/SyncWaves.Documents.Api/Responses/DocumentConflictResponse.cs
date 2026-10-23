using Microsoft.AspNetCore.Mvc;
using SyncWaves.Documents.Application.DTOs;

namespace SyncWaves.Documents.Api.Responses;

public class DocumentConflictResponse : ActionResult
{
    public DocumentDTO? Current { get; set; }
}
