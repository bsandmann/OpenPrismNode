namespace OpenPrismNode.Core.Commands.Registrar
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Placeholder for DID Document structure.
    /// A real implementation would likely use a library that models DID Core specs.
    /// Using Dictionary for flexibility for now.
    /// </summary>
    [JsonConverter(typeof(RegistrarDidDocumentConverter))] // Allows receiving/sending raw JSON object
    public class RegistrarDidDocument : Dictionary<string, object>
    {
        private const string ContextKey = "@context";
        private const string ServiceKey = "service";
        private const string IdKey = "id";

        /// <summary>
        /// Gets or sets the context array for the DID Document.
        /// </summary>
        [JsonIgnore]
        public List<string>? Context
        {
            get => ParseContext();
            set => SetContext(value);
        }

        /// <summary>
        /// Gets or sets the services for the DID Document as strongly-typed RegistrarService objects.
        /// </summary>
        [JsonIgnore]
        public List<RegistrarService>? Service // Keep original name for backward compatibility
        {
            get => ParseServices();
            set => SetServices(value);
        }
        
        /// <summary>
        /// Alias for Service property with more meaningful name.
        /// </summary>
        [JsonIgnore]
        public List<RegistrarService>? Services
        {
            get => Service;
            set => Service = value;
        }

        /// <summary>
        /// Helper property for ID, not part of JSON
        /// </summary>
        [JsonIgnore]
        public string? Id => ContainsKey(IdKey) ? this[IdKey]?.ToString() : null;

        /// <summary>
        /// Gets a value from the dictionary with type conversion.
        /// </summary>
        private T? GetValueOrDefault<T>(string key) where T : class
        {
            if (!ContainsKey(key))
                return null;
                
            return this[key] as T;
        }

        /// <summary>
        /// Sets a value in the dictionary or removes it if null.
        /// </summary>
        private void SetOrRemoveValue<T>(string key, T? value) where T : class
        {
            if (value != null)
                this[key] = value;
            else if (ContainsKey(key))
                this.Remove(key);
        }

        /// <summary>
        /// Parses the services from various possible formats into strongly-typed RegistrarService objects.
        /// </summary>
        private List<RegistrarService>? ParseServices()
        {
            if (!ContainsKey(ServiceKey))
                return null;

            var services = new List<RegistrarService>();
            
            if (TryParseServiceFromDictionaryList(services) ||
                TryParseServiceFromObjectList(services) ||
                TryParseServiceFromJsonElement(services))
            {
                return services.Count > 0 ? services : null;
            }
            
            return null;
        }

        /// <summary>
        /// Tries to parse services from a list of dictionaries.
        /// </summary>
        private bool TryParseServiceFromDictionaryList(List<RegistrarService> services)
        {
            if (this[ServiceKey] is List<Dictionary<string, object>> dictList)
            {
                foreach (var dict in dictList)
                {
                    var service = RegistrarService.FromDictionary(dict);
                    if (service != null)
                        services.Add(service);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to parse services from a list of objects.
        /// </summary>
        private bool TryParseServiceFromObjectList(List<RegistrarService> services)
        {
            if (this[ServiceKey] is List<object> objList)
            {
                foreach (var obj in objList)
                {
                    if (obj is Dictionary<string, object> dict)
                    {
                        var service = RegistrarService.FromDictionary(dict);
                        if (service != null)
                            services.Add(service);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to parse services from a JsonElement.
        /// </summary>
        private bool TryParseServiceFromJsonElement(List<RegistrarService> services)
        {
            if (this[ServiceKey] is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var service = ParseServiceFromJsonElement(element);
                        if (service != null)
                            services.Add(service);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parses a single service from a JsonElement.
        /// </summary>
        private static RegistrarService? ParseServiceFromJsonElement(JsonElement element)
        {
            string? id = null, type = null, endpoint = null;
            
            if (element.TryGetProperty("id", out var idElement) && 
                idElement.ValueKind == JsonValueKind.String)
                id = idElement.GetString();
                
            if (element.TryGetProperty("type", out var typeElement) && 
                typeElement.ValueKind == JsonValueKind.String)
                type = typeElement.GetString();
                
            if (element.TryGetProperty("serviceEndpoint", out var endpointElement) && 
                endpointElement.ValueKind == JsonValueKind.String)
                endpoint = endpointElement.GetString();
                
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(endpoint))
            {
                return new RegistrarService
                {
                    Id = id,
                    Type = type,
                    ServiceEndpoint = endpoint
                };
            }
            
            return null;
        }

        /// <summary>
        /// Sets the services in the document.
        /// </summary>
        private void SetServices(List<RegistrarService>? value)
        {
            if (value == null || value.Count == 0)
            {
                if (ContainsKey(ServiceKey))
                    this.Remove(ServiceKey);
                return;
            }
            
            var serviceList = value.Select(s => s.ToDictionary()).ToList<object>();
            this[ServiceKey] = serviceList;
        }

        /// <summary>
        /// Gets the services as a list of dictionaries.
        /// </summary>
        private List<Dictionary<string, object>>? GetServiceDictionaries()
        {
            if (!ContainsKey(ServiceKey))
                return null;
                
            if (this[ServiceKey] is List<Dictionary<string, object>> dictList)
                return dictList;
            
            return Service?.Select(s => s.ToDictionary()).ToList();
        }
        
        /// <summary>
        /// Parses the context array from various possible formats into a list of strings.
        /// </summary>
        private List<string>? ParseContext()
        {
            if (!ContainsKey(ContextKey))
                return null;
                
            // Direct cast if already a List<string>
            if (this[ContextKey] is List<string> stringList)
                return stringList;
                
            var contexts = new List<string>();
            
            // Handle JsonElement (most common when deserializing JSON)
            if (this[ContextKey] is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        string? contextStr = element.GetString();
                        if (!string.IsNullOrEmpty(contextStr))
                            contexts.Add(contextStr);
                    }
                }
            }
            // Handle List<object> (another possible deserialization result)
            else if (this[ContextKey] is List<object> objList)
            {
                foreach (var obj in objList)
                {
                    string? context = obj?.ToString();
                    if (!string.IsNullOrEmpty(context))
                        contexts.Add(context);
                }
            }
            // Handle single string value
            else if (this[ContextKey] is string contextStr && !string.IsNullOrEmpty(contextStr))
            {
                contexts.Add(contextStr);
            }
            
            return contexts.Count > 0 ? contexts : null;
        }
        
        /// <summary>
        /// Sets the context array in the document.
        /// </summary>
        private void SetContext(List<string>? value)
        {
            if (value == null || value.Count == 0)
            {
                if (ContainsKey(ContextKey))
                    this.Remove(ContextKey);
                return;
            }
            
            this[ContextKey] = value;
        }
    }

    /// <summary>
    /// Handles serialization/deserialization of the essentially raw JSON object
    /// for RegistrarDidDocument.
    /// </summary>
    public class RegistrarDidDocumentConverter : JsonConverter<RegistrarDidDocument>
    {
        public override RegistrarDidDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
            if (dictionary == null) return null;

            var didDoc = new RegistrarDidDocument();
            foreach (var kvp in dictionary)
            {
                didDoc.Add(kvp.Key, kvp.Value);
            }
            return didDoc;
        }

        public override void Write(Utf8JsonWriter writer, RegistrarDidDocument value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (Dictionary<string, object>)value, options);
        }
    }
}