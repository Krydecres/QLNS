using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLNS.Api.Infrastructure
{
    /// <summary>
    /// Serialize DateTime as "yyyy-MM-dd" string to prevent UTC-to-local timezone shift on the Angular frontend.
    /// Without this, a date like 2026-06-10T00:00:00 (UTC) becomes 2026-06-09 when displayed in UTC+7 browsers.
    /// </summary>
    public class DateOnlyJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParse(str, out var dt))
                return dt;
            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Write as "yyyy-MM-dd" — no time, no timezone info
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }
    }

    public class NullableDateOnlyJsonConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str)) return null;
            if (DateTime.TryParse(str, out var dt))
                return dt;
            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
            else
                writer.WriteNullValue();
        }
    }
}
