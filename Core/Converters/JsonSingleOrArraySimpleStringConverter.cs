using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CKAN
{
    public class JsonSingleOrArraySimpleStringConverter<T> : JsonConverter
    {
        public JsonSingleOrArraySimpleStringConverter()
        {
        }

        public override bool CanConvert(Type object_type)
        {
            // We *only* want to be triggered for types that have explicitly
            // set an attribute in their class saying they can be converted.
            // By returning false here, we declare we're not interested in participating
            // in any other conversions.
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            List<string> values;
            switch (token.Type)
            {
                case JTokenType.Array:
                    values = token.ToObject<List<string>>();
                    break;
                case JTokenType.Null:
                    return null;
                default:
                    values = new List<string> {token.ToObject<string>()};
                    break;
            }

            values.ForEach(p => p.ToString());


            return values.Select(p => Activator.CreateInstance(typeof(T), p)).Cast<T>().ToArray();
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

