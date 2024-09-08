using Deaddit.Core.Exceptions;
using Deaddit.Core.Extensions;
using Deaddit.Core.Json.Attributes;
using Deaddit.Core.Json.Exceptions;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;
using Deaddit.Core.Utils.Extensions;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Web;

namespace Deaddit.Core.Json
{
    public static class JsonDeserializer
    {
        public static T Deserialize<T>(string json)
        {
            JsonNode jsonNode = JsonNode.Parse(json)!;

            return (T)Deserialize(jsonNode, typeof(T))!;
        }

        private static object? Deserialize(JsonNode? property, Type targetType)
        {
            // Case for IDictionary<X, Y>
            if (targetType.IsAssignableTo(typeof(IDictionary)))
            {
                // Get the types for key and value
                Type keyType = targetType.GenericTypeArguments[0];
                Type valueType = targetType.GenericTypeArguments[1];

                IDictionary targetDictionary = (IDictionary)Activator.CreateInstance(targetType)!;

                if (property is JsonObject jo)
                {
                    foreach (KeyValuePair<string, JsonNode?> kvp in jo)
                    {
                        object? key = Deserialize(JsonValue.Create(kvp.Key), keyType);
                        object? value = Deserialize(kvp.Value, valueType);
                        targetDictionary.Add(key, value);
                    }
                }

                return targetDictionary;
            }
            else if (targetType.IsAssignableTo(typeof(IList)))
            {
                Type collectionType = targetType.GenericTypeArguments[0];

                IList targetList = (IList)Activator.CreateInstance(targetType)!;

                if (property is JsonArray ja)
                {
                    foreach (JsonNode? j in ja)
                    {
                        object? item = Deserialize(j, collectionType);
                        targetList.Add(item);
                    }
                }

                return targetList;
            }
            else if (targetType == typeof(string))
            {
                return HttpUtility.HtmlDecode(property?.ToString());
            }
            else if (targetType.IsEnum)
            {
                return ParseEnum(targetType, property?.ToString());
            }
            else if (targetType == typeof(long))
            {
                return ParseLong(property?.ToString());
            }
            else if (targetType == typeof(DynamicColor))
            {
                return ParseColor(property?.ToString());
            }
            else if (targetType == typeof(int))
            {
                return ParseInt(property?.ToString());
            }
            else if (targetType == typeof(bool))
            {
                return ParseBool(property?.ToString());
            }
            else if (targetType == typeof(DateTime))
            {
                return ParseDateTime(property?.ToString());
            }
            else if (targetType == typeof(OptionalDateTime))
            {
                return ParseOptionalDateTime(property?.ToString());
            }
            else if (targetType == typeof(double))
            {
                return ParseDouble(property.ToString());
            }
            else if (targetType == typeof(decimal))
            {
                return ParseDecimal(property.ToString());
            }
            else if (targetType == typeof(ApiThing))
            {
                return ParseRedditThing(property);
            }
            else if (targetType == typeof(ApiThingCollection))
            {
                return ParseRedditThingCollection(property);
            }
            else if (Nullable.GetUnderlyingType(targetType) is Type nullableType)
            {
                if (property is null)
                {
                    return null;
                }
                else
                {
                    return Deserialize(property, nullableType);
                }
            }
            //Must be last to prevent catching other types
            else if (targetType.IsClass)
            {
                if (property is JsonValue jv && !string.IsNullOrWhiteSpace(jv.ToString()))
                {
                    throw new DeserializationException("Json Value found where object is expected");
                }

                return DeserializeObject(property, targetType);
            }

            throw new Exception("Unhandled Property Type");
        }

        private static object? DeserializeObject(JsonNode? node, Type type)
        {
            if (node is null)
            {
                return null;
            }

            object toReturn = Activator.CreateInstance(type)!;

            if (node is JsonObject jsonObject)
            {
                HashSet<string> availableProperties = jsonObject.Select(k => k.Key).ToHashSet();

                foreach (PropertyInfo pi in type.GetProperties())
                {
                    List<string> names = [];

                    if (pi.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute jpn)
                    {
                        names.Add(jpn.Name);
                    }
                    else if (pi.GetCustomAttribute<JsonPropertyNamesAttribute>() is JsonPropertyNamesAttribute jpns)
                    {
                        names.AddRange(jpns.Names);
                    }
                    else
                    {
                        names.Add(pi.Name);
                    }

                    JsonNode? property = null;

                    foreach (string propertyName in names)
                    {
                        if (jsonObject.ContainsKey(propertyName))
                        {
                            availableProperties.Remove(propertyName);
                            property = jsonObject[propertyName];
                            break;
                        }
                    }

                    try
                    {
                        if (pi.PropertyType == typeof(object) && property is not null)
                        {
                            //Create a real type if the data is available
                            throw new MisconfiguredPropertyException(pi, "Can not deserialize type object");
                        }

                        object? propertyValue = Deserialize(property, pi.PropertyType);

                        if (propertyValue is null && pi.IsRequired())
                        {
                            throw new DeserializationException($"Required property {pi.Name} was not found");
                        }

                        pi.SetValue(toReturn, propertyValue);
                    }
                    catch (DeserializationException ex)
                    {
                        Debug.WriteLine($"Error deserializing property: {pi.Name}");
                        Debug.WriteLine(ex.Message);
                    }
                }

                if (availableProperties.Count > 0)
                {
                    Debug.WriteLine($"[{type}] Unmapped properties found on object at path '{jsonObject.GetPath()}'; {string.Join(",", availableProperties)}");
                }
            }

            return toReturn;
        }

        private static bool ParseBool(string? value)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            throw new MissingConversionException();
        }

        private static DynamicColor? ParseColor(string? v)
        {
            if (string.IsNullOrWhiteSpace(v))
            {
                return null;
            }

            if (DynamicColor.TryParse(v, out DynamicColor longValue))
            {
                return longValue;
            }

            if (v == "dark")
            {
                return DynamicColor.Parse("#212121");
            }

            if (v == "light")
            {
                return DynamicColor.Parse("#D3D3D3");
            }

            if (v == "transparent")
            {
                return new DynamicColor(0, 0, 0, 0);
            }

            throw new MissingConversionException($"No color mapping for value {v}");
        }

        private static DateTime ParseDateTime(string? value)
        {
            if (DateTime.TryParse(value, out DateTime dt))
            {
                return dt;
            }

            if (decimal.TryParse(value, out decimal decimalValue) && (long)decimalValue == decimalValue)
            {
                long ms = (long)decimalValue;

                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(ms).UtcDateTime;

                return dateTime;
            }

            throw new MissingConversionException();
        }

        private static decimal ParseDecimal(string? value)
        {
            if (decimal.TryParse(value, out decimal result))
            {
                return result;
            }

            throw new MissingConversionException();
        }

        private static double ParseDouble(string? value)
        {
            if (double.TryParse(value, out double result))
            {
                return result;
            }

            throw new MissingConversionException();
        }

        private static object? ParseEnum(Type targetType, string? v)
        {
            Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);

            object? nullValue = null;

            foreach (Enum value in Enum.GetValues(targetType))
            {
                string? text = value.ToString();

                if (value.GetAttribute<EnumMemberAttribute>() is EnumMemberAttribute ema)
                {
                    text = ema.Value;
                }

                if (text is null)
                {
                    nullValue = value;
                }
                else
                {
                    values[text] = value;
                }
            }

            if (v is null)
            {
                if (nullValue is null)
                {
                    //mimic underlying dictionary request exception
                    throw new KeyNotFoundException();
                }
                else
                {
                    return nullValue;
                }
            }
            else
            {
                return values[v];
            }
        }

        private static int ParseInt(string? v)
        {
            if (int.TryParse(v, out int intValue))
            {
                return intValue;
            }

            throw new MissingConversionException();
        }

        private static long ParseLong(string? v)
        {
            if (long.TryParse(v, out long longValue))
            {
                return longValue;
            }

            throw new MissingConversionException();
        }

        private static OptionalDateTime ParseOptionalDateTime(string? value)
        {
            if (value is null)
            {
                return OptionalDateTime.Null;
            }

            if (DateTime.TryParse(value, out DateTime dt))
            {
                return dt;
            }

            if (decimal.TryParse(value, out decimal decimalValue) && (long)decimalValue == decimalValue)
            {
                long ms = (long)decimalValue;

                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(ms).UtcDateTime;

                return dateTime;
            }

            if (value == "false")
            {
                return OptionalDateTime.Null;
            }

            throw new MissingConversionException();
        }

        private static ApiThing? ParseRedditThing(JsonNode? property)
        {
            if (property is null)
            {
                return null;
            }

            JsonNode kindProperty = property["kind"];

            ThingKind kind = (ThingKind)ParseEnum(typeof(ThingKind), kindProperty?.ToString());

            JsonNode data = property["data"];

            return kind switch
            {
                ThingKind.Comment => (ApiComment)DeserializeObject(data, typeof(ApiComment)),
                ThingKind.Subreddit => (ApiSubReddit)DeserializeObject(data, typeof(ApiSubReddit)),
                ThingKind.More => (ApiMore)DeserializeObject(data, typeof(ApiMore)),
                ThingKind.Link => (ApiPost)DeserializeObject(data, typeof(ApiPost)),
                _ => throw new EnumNotImplementedException(kind),
            };
        }

        private static ApiThingCollection? ParseRedditThingCollection(JsonNode? property)
        {
            if (property is null)
            {
                return null;
            }

            if (property is JsonValue jv && jv.ToString() == string.Empty)
            {
                return null;
            }

            JsonObject dataProperty = property["data"] as JsonObject;

            JsonArray childrenProperty = dataProperty["children"] as JsonArray;

            dataProperty.Remove("children");

            ApiThingCollection collection = DeserializeObject(dataProperty, typeof(ApiThingCollection)) as ApiThingCollection;

            if (childrenProperty is not null)
            {
                foreach (JsonObject jo in childrenProperty.Cast<JsonObject>())
                {
                    ApiThing thing = ParseRedditThing(jo);
                    collection.Children.Add(thing);
                }
            }

            return collection;
        }
    }
}