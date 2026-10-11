/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Piranha.Cache;

/// <summary>
/// A serialization binder that restricts type resolution to Piranha assemblies only.
/// This prevents deserialization gadget attacks (RCE via cache poisoning) by refusing
/// to instantiate any type whose assembly name does not start with "Piranha".
/// Used together with TypeNameHandling.Auto so that $type tokens are only written
/// when the concrete type differs from the declared type (i.e. for polymorphic fields).
/// </summary>
internal sealed class PiranhaTypesBinder : ISerializationBinder
{
    /// <inheritdoc />
    public Type BindToType(string assemblyName, string typeName)
    {
        // Strip version/culture/token — the simple name is the first segment.
        var simpleAssemblyName = assemblyName?.Split(',')[0].Trim() ?? string.Empty;

        if (!simpleAssemblyName.StartsWith("Piranha", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonSerializationException(
                $"Refusing to deserialize type '{typeName}' from assembly '{assemblyName}': " +
                "only types from Piranha assemblies are permitted in the distributed cache.");
        }

        return Type.GetType($"{typeName}, {assemblyName}", throwOnError: true);
    }

    /// <inheritdoc />
    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = serializedType.Assembly.GetName().Name;
        typeName = serializedType.FullName;
    }
}
