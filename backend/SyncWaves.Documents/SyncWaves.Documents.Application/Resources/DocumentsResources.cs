using Microsoft.Extensions.Localization;
using System.Reflection;

namespace SyncWaves.Documents.Application.Resources;

public class DocumentsResources { }
public interface IResourceLocalizer
{
    string Localize(string key);
}

public class ResourceLocalizer : IResourceLocalizer
{
    private readonly IStringLocalizer _localizer;

    public ResourceLocalizer(IStringLocalizerFactory localizerFactory)
    {
        var type = typeof(DocumentsResources);
        var assemblyName = new AssemblyName(type.Assembly.FullName!);
        _localizer = localizerFactory.Create("DocumentsResources", assemblyName.Name!);
    }

    public string Localize(string key)
    {
        return _localizer[key];
    }
}