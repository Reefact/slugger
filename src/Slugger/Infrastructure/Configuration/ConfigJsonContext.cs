#region Usings declarations

using System.Text.Json.Serialization;

using Slugger.Application.Options;

#endregion

namespace Slugger.Infrastructure.Configuration;

/// <summary>
///     The saved defaults' JSON, generated at build time rather than reflected at run time, so that a
///     natively compiled command can read and write them.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             PropertyNameCaseInsensitive = true,
                             UseStringEnumConverter = true)]
[JsonSerializable(typeof(SluggerOptions))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext;