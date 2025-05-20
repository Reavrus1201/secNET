using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using secNET.Models;

namespace secNET.Models
{
    public class ChecklistSectionConverter : JsonConverter<ChecklistSection>
    {
        public override ChecklistSection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token.");
            }

            var section = new ChecklistSection
            {
                Questions = new Dictionary<string, string>(),
                CROComments = new Dictionary<string, string>(),
                ManagersActionTaken = new Dictionary<string, string>()
            };

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return section;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token.");
                }

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Questions":
                        section.Questions = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                        break;
                    case "CROComments":
                        var croComments = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                        if (croComments != null)
                        {
                            section.CROComments = croComments;
                        }
                        break;
                    case "ManagersActionTaken":
                        var managersActionTaken = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                        if (managersActionTaken != null)
                        {
                            section.ManagersActionTaken = managersActionTaken;
                        }
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON.");
        }

        public override void Write(Utf8JsonWriter writer, ChecklistSection value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Questions");
            JsonSerializer.Serialize(writer, value.Questions, options);

            writer.WritePropertyName("CROComments");
            JsonSerializer.Serialize(writer, value.CROComments, options);

            writer.WritePropertyName("ManagersActionTaken");
            JsonSerializer.Serialize(writer, value.ManagersActionTaken, options);

            writer.WriteEndObject();
        }
    }
}