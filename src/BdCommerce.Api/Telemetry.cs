using System.Diagnostics;

namespace BdCommerce.Api;

/// <summary>
/// Named ActivitySource for explicit spans emitted by this API. Automatic
/// ASP.NET Core request spans come from OpenTelemetry.Instrumentation.AspNetCore;
/// these named spans add operation-level detail for the local trace pipeline.
/// </summary>
public static class BdCommerceTelemetry
{
    public const string SourceName = "BdCommerce.Api";

    public static readonly ActivitySource Source = new(SourceName, "1.0.0");
}
