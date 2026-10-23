using Microsoft.Extensions.Localization;
using System.Reflection;

namespace SyncWaves.Auth.Application.Resources;

public class AuthResources { }

public interface IResourceLocalizer
{
    string Localize(string key);
}

public class ResourceLocalizer : IResourceLocalizer
{
    private readonly IStringLocalizer _localizer;

    public ResourceLocalizer(IStringLocalizerFactory localizerFactory)
    {
        var type = typeof(AuthResources);
        var assemblyName = new AssemblyName(type.Assembly.FullName!);
        _localizer = localizerFactory.Create("AuthResources", assemblyName.Name!);
    }

    public string Localize(string key)
    {
        return _localizer[key];
    }
}