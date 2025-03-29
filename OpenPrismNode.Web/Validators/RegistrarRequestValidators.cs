namespace OpenPrismNode.Web.Validators;

using Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class RegistrarRequestValidators
{
    ///<summary>
    /// Validates the create request according to the DID specification requirements
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <returns>An error message if validation fails, otherwise null</returns>
    public static string? ValidateCreateRequest(RegistrarCreateRequestModel request)
    {
        // Allowed values
        string[] allowedPurposes = new[] { "authentication", "assertionMethod", "keyAgreement", "capabilityInvocation", "capabilityDelegation" };
        string[] allowedCurves = new[] { "secp256k1", "Ed25519", "X25519" };

        // Check if verification methods exist
        if (request.Secret?.VerificationMethod == null || request.Secret.VerificationMethod.Count == 0)
        {
            return "At least one verification method must be provided";
        }

        // Validation for verification methods
        var verificationMethodIds = new HashSet<string>();
        foreach (var method in request.Secret.VerificationMethod)
        {
            // Check required properties
            if (string.IsNullOrEmpty(method.Id))
            {
                return "Each verification method must have an 'id' property";
            }

            // Check for duplicate id
            if (!verificationMethodIds.Add(method.Id))
            {
                return $"Duplicate verification method id found: {method.Id}";
            }

            // Type validation
            if (string.IsNullOrEmpty(method.Type))
            {
                return $"Verification method '{method.Id}' must have a 'type' property";
            }

            if (method.Type != "JsonWebKey2020")
            {
                return $"Verification method '{method.Id}' must have type 'JsonWebKey2020'";
            }

            // Purpose validation
            if (method.Purpose == null || method.Purpose.Count == 0)
            {
                return $"Verification method '{method.Id}' must have at least one purpose";
            }

            // Check for duplicate purposes
            var purposeSet = new HashSet<string>();
            foreach (var purpose in method.Purpose)
            {
                if (!purposeSet.Add(purpose))
                {
                    return $"Verification method '{method.Id}' has duplicate purpose: {purpose}";
                }

                // Check allowed purpose values
                if (!allowedPurposes.Contains(purpose))
                {
                    return $"Invalid purpose '{purpose}' in verification method '{method.Id}'. Allowed values: {string.Join(", ", allowedPurposes)}";
                }
            }

            if (method.Purpose.Count != 1)
            {
               return "A verification method must have exactly one purpose for the current implementation";
            }

            if (method.Id.StartsWith("master"))
            {
               return "The key-Id is not allowed to start with 'master' to avoid confusion with the master key";
            }

            // Curve validation
            if (string.IsNullOrEmpty(method.Curve))
            {
                return $"Verification method '{method.Id}' must have a 'curve' property";
            }

            // Check allowed curve values
            if (!allowedCurves.Contains(method.Curve))
            {
                return $"Invalid curve '{method.Curve}' in verification method '{method.Id}'. Allowed values: {string.Join(", ", allowedCurves)}";
            }
        }

        // Validation for DID Document
        if (request.DidDocument != null)
        {
            // Check if only allowed properties exist
            foreach (var key in request.DidDocument.Keys)
            {
                if (key != "@context" && key != "service")
                {
                    return $"Invalid property '{key}' in DID document. Only '@context' and 'service' are allowed";
                }
            }
            
            // Check if @context is provided (now required)
            if (!request.DidDocument.ContainsKey("@context"))
            {
                return "DID document must have an '@context' property";
            }

            // Check @context if provided
            if (request.DidDocument.TryGetValue("@context", out var contextObj) && contextObj != null)
            {
                // Handle System.Text.Json.JsonElement (most common in .NET Core)
                if (contextObj is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind != JsonValueKind.Array)
                    {
                        return "DID document '@context' must be an array";
                    }

                    var enumerable = jsonElement.EnumerateArray();
                    if (!enumerable.Any())
                    {
                        return "DID document '@context' must be a non-empty array";
                    }

                    // Check for duplicate contexts
                    var contextSet = new HashSet<string>();
                    foreach (var ctx in enumerable)
                    {
                        if (ctx.ValueKind != JsonValueKind.String)
                        {
                            return "DID document '@context' array must only contain string values";
                        }

                        string contextStr = ctx.GetString() ?? "";
                        if (string.IsNullOrWhiteSpace(contextStr))
                        {
                            return "DID document '@context' must not contain null or empty values";
                        }

                        if (!contextSet.Add(contextStr))
                        {
                            return $"Duplicate '@context' value found: {contextStr}";
                        }
                    }
                }
                else
                {
                    // Fallback for other collection types
                    IEnumerable<object>? enumerable = null;
                    
                    // Try to cast to different types of collections
                    if (contextObj is IEnumerable<object> objEnum)
                    {
                        enumerable = objEnum;
                    }
                    else if (contextObj is IEnumerable<string> strEnum)
                    {
                        enumerable = strEnum.Cast<object>();
                    }
                    else
                    {
                        return "DID document '@context' must be an array";
                    }

                    var contexts = enumerable.ToList();
                    if (contexts.Count == 0)
                    {
                        return "DID document '@context' must be a non-empty array";
                    }

                    // Check for duplicate contexts
                    var contextSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var ctx in contexts)
                    {
                        string? contextStr = ctx?.ToString();
                        if (string.IsNullOrWhiteSpace(contextStr))
                        {
                            return "DID document '@context' must not contain null or empty values";
                        }

                        if (!contextSet.Add(contextStr))
                        {
                            return $"Duplicate '@context' value found: {contextStr}";
                        }
                    }
                }
            }

            // Check services using the new RegistrarService class
            if (request.DidDocument.TryGetValue("service", out _))
            {
                // Use the strongly-typed Services property (can use either Service or Services property)
                var services = request.DidDocument.Services;
                
                // Ensure 'service' is an array
                if (services == null && request.DidDocument.ContainsKey("service"))
                {
                    return "DID document 'service' must be a valid array of service objects";
                }
                
                if (services != null && services.Count > 0)
                {
                    // Check for duplicate service IDs
                    var serviceIds = new HashSet<string>();
                    foreach (var service in services)
                    {
                        if (string.IsNullOrEmpty(service.Id))
                        {
                            return "Each service must have a non-empty 'id' property";
                        }
                        
                        if (!serviceIds.Add(service.Id))
                        {
                            return $"Duplicate service id found: {service.Id}";
                        }
                        
                        if (string.IsNullOrEmpty(service.Type))
                        {
                            return $"Service '{service.Id}' must have a non-empty 'type' property";
                        }
                        
                        if (string.IsNullOrEmpty(service.ServiceEndpoint))
                        {
                            return $"Service '{service.Id}' must have a non-empty 'serviceEndpoint' property";
                        }
                    }
                }
            }
        }

        return null;
    }
}