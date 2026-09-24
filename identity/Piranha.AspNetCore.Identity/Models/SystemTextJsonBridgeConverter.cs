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
using Newtonsoft.Json.Linq;

namespace Piranha.AspNetCore.Identity.Models;

/// <summary>
/// Newtonsoft.Json converter that hands a value over to System.Text.Json.
/// The Manager registers Newtonsoft.Json as the MVC input formatter, but the
/// Fido2NetLib WebAuthn types are only annotated for System.Text.Json
/// (base64url byte arrays, camelCase property names such as
/// <c>clientDataJSON</c>), so Newtonsoft can't bind them on its own.
/// </summary>
internal sealed class SystemTextJsonBridgeConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType) => true;

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var json = JToken.Load(reader).ToString(Formatting.None);

        return System.Text.Json.JsonSerializer.Deserialize(json, objectType);
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value, value.GetType()));
    }
}
