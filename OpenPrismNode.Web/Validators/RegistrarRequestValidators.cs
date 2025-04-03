using System.Text.Json;

namespace OpenPrismNode.Web.Validators;

using Core;
using Core.Commands.Registrar;
using Models;

public static class RegistrarRequestValidators
{
    /// <summary>
    /// Validates the create request according to the DID specification requirements
    /// by calling specific validation methods for secrets and the DID document.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>An error message if validation fails, otherwise null.</returns>
    public static string? ValidateCreateRequest(RegistrarCreateRequestModel request)
    {
        // Validate Secrets and Verification Methods first
        if (request.Secret is not null)
        {
            string? secretValidationError = ValidateSecretsAndVerificationMethods(request.Secret);
            if (secretValidationError != null)
            {
                return secretValidationError;
            }
        }

        if (request.DidDocument is not null)
        {
            // Validate DID Document next
            string? didDocumentError = ValidateDidDocument(request.DidDocument, true);
            if (didDocumentError != null)
            {
                return didDocumentError;
            }
        }

        // All validations passed
        return null;
    }

    /// <summary>
    /// Validates the create request according to the DID specification requirements
    /// by calling specific validation methods for secrets and the DID document.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>An error message if validation fails, otherwise null.</returns>
    public static string? ValidateUpdateRequest(RegistrarUpdateRequestModel request)
    {
        // Validate Secrets and Verification Methods first
        if (request.Secret is not null)
        {
            string? secretValidationError = ValidateSecretsAndVerificationMethods(request.Secret);
            if (secretValidationError != null)
            {
                return secretValidationError;
            }
        }


        // (a) The only allowed didDocumentOperation values are: setDidDocument, addToDidDocument, removeFromDidDocument
        string[] allowedOperations = { PrismParameters.SetDidDocument, PrismParameters.AddToDidDocument, PrismParameters.RemoveFromDidDocument };
        if (request.DidDocumentOperation is not null)
        {
            foreach (var op in request.DidDocumentOperation)
            {
                if (!allowedOperations.Contains(op))
                {
                    return $"Invalid didDocumentOperation '{op}'. Allowed values: {string.Join(", ", allowedOperations)}";
                }
            }
        }

        // We'll often need counts of things. Let's define them here:
        var verificationMethodCount = request.Secret?.VerificationMethod?.Count ?? 0; // M
        var setDidDocumentCount = request.DidDocumentOperation?.Count(op => op == PrismParameters.SetDidDocument) ?? 0; // S
        var addToDidDocumentCount = request.DidDocumentOperation?.Count(op => op == PrismParameters.AddToDidDocument) ?? 0; // S
        var totalOpsCount = request.DidDocumentOperation?.Count ?? 0;
        var totalDidDocsCount = request.DidDocument?.Count ?? 0;

        if (setDidDocumentCount > 1)
        {
           return "There is only one instance of a setDidDocument operation allowed.";
        }

        if (totalOpsCount != totalDidDocsCount)
        {
            return $"The number of  operations ({totalOpsCount}) " +
                   $"must match the number of providided didDocuments ({totalDidDocsCount}).";
        }

        // (b) The number of addToDidDocumentCount operations must be small or identical to the number of VerificationMethod in the secrets.
        if (addToDidDocumentCount > verificationMethodCount)
        {
            return $"The number of 'setDidDocument' operations ({addToDidDocumentCount}) " +
                   $"must match the number of verification methods in the secret ({verificationMethodCount}).";
        }

        // (c) The number of DID documents *that have a 'verificationMethod'* must be
        //     at least the number of 'addToDidDocumentCount' operations.
        int docsWithVerificationMethodCount = 0;
        if (request.DidDocument is not null)
        {
            docsWithVerificationMethodCount = request.DidDocument
                .Count(d => d.ContainsKey("verificationMethod"));
        }

        if (docsWithVerificationMethodCount < addToDidDocumentCount)
        {
            return $"At least {addToDidDocumentCount} DID document entries must contain a 'verificationMethod' property, " +
                   $"but only {docsWithVerificationMethodCount} do.";
        }

        // (d) For each verification method in the secret, there must be a non-null Id,
        //     and the DID documents must contain a matching verificationMethod with the same Id.
        var secretMethodIds = new List<string>();
        if (request.Secret?.VerificationMethod != null)
        {
            foreach (var method in request.Secret.VerificationMethod)
            {
                if (string.IsNullOrEmpty(method.Id))
                {
                    return "Each verification method in the secret must have a non-null/non-empty 'id'.";
                }

                secretMethodIds.Add(method.Id!);
            }
        }

        // Collect all verificationMethod IDs present in *all* DID documents
        // so we can cross-check that each secret's ID appears there.
        var didDocVerificationIds = new HashSet<string>(StringComparer.Ordinal);
        if (request.DidDocument is not null)
        {
            foreach (var doc in request.DidDocument)
            {
                if (doc.TryGetValue("verificationMethod", out var vmObj) && vmObj is not null)
                {
                    // Typically "verificationMethod" might be a list/array of objects
                    // We'll attempt to parse out each method's "id" property.
                    if (vmObj is JsonElement vmJson && vmJson.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in vmJson.EnumerateArray())
                        {
                            if (item.TryGetProperty("id", out var idProperty) &&
                                idProperty.ValueKind == JsonValueKind.String &&
                                !string.IsNullOrEmpty(idProperty.GetString()))
                            {
                                var idStrParts = idProperty.GetString()!.Split('#')[idProperty.GetString()!.Split('#').Length - 1];
                                didDocVerificationIds.Add(idStrParts);
                            }
                        }
                    }
                    else if (vmObj is IEnumerable<object> vmList)
                    {
                        // e.g. List<object> or List<Dictionary<string,object>>
                        foreach (var item in vmList)
                        {
                            if (item is Dictionary<string, object> dict && dict.TryGetValue("id", out var maybeId))
                            {
                                var idStr = maybeId?.ToString();
                                if (!string.IsNullOrEmpty(idStr))
                                {
                                    var idStrParts = idStr.Split('#')[idStr.Split('#').Length - 1];
                                    didDocVerificationIds.Add(idStrParts);
                                }
                            }
                        }
                    }
                    // If there's some other format, we skip or parse similarly.
                }
            }
        }

        // Cross-check each secret method's ID is found in at least one DID doc's verificationMethod
        foreach (var id in secretMethodIds)
        {
            if (!didDocVerificationIds.Contains(id))
            {
                return $"No DID document contains a verificationMethod with the id '{id}', which appears in the secret.";
            }
        }

        // (e) Each DID document entry that has a verificationMethod with an Id that also appears in the secret
        //     cannot have additional fields in that verificationMethod object beyond 'id'.
        //     (But the DID doc can still have @context, service, etc. at the top level.)
        //     In other words, for each verificationMethod object in the doc,
        //     if its 'id' is in our secretMethodIds, it must not have type/controller/etc.
        if (request.DidDocument is not null)
        {
            foreach (var doc in request.DidDocument)
            {
                if (doc.TryGetValue("verificationMethod", out var vmObj) && vmObj is not null)
                {
                    // We need to examine each verificationMethod item
                    IEnumerable<Dictionary<string, object>>? vmCollection = null;

                    // Attempt to unify possible structures
                    if (vmObj is JsonElement vmJson && vmJson.ValueKind == JsonValueKind.Array)
                    {
                        vmCollection = vmJson.EnumerateArray()
                            .Select(je => je.ValueKind == JsonValueKind.Object
                                ? je.Deserialize<Dictionary<string, object>>()
                                : null)
                            .Where(d => d is not null)!
                            .ToList();
                    }
                    else if (vmObj is List<object> objList)
                    {
                        vmCollection = objList
                            .Select(o => o as Dictionary<string, object>)
                            .Where(d => d is not null)
                            .ToList();
                    }
                    else if (vmObj is List<Dictionary<string, object>> dictList)
                    {
                        vmCollection = dictList;
                    }

                    if (vmCollection != null)
                    {
                        foreach (var vmDict in vmCollection)
                        {
                            if (vmDict.TryGetValue("id", out var maybeIdObj))
                            {
                                var idStr = maybeIdObj?.ToString();
                                // If the id belongs to one of our secrets, the only allowed key is "id"
                                if (!string.IsNullOrEmpty(idStr) && secretMethodIds.Contains(idStr))
                                {
                                    // Check if there is more than just 'id' in this verificationMethod
                                    if (vmDict.Keys.Any(k => k != "id"))
                                    {
                                        return $"DID document has a verificationMethod with id '{idStr}' that also appears " +
                                               "in the secrets, but it contains additional properties beyond 'id'. " +
                                               "This is not allowed.";
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // (f) We must match each didDocumentOperation entry to the corresponding
        //     didDocument by index. For that, they must have the same count or at least
        //     we can't proceed in a consistent manner if mismatch.
        if (totalOpsCount != totalDidDocsCount)
        {
            return $"The number of didDocumentOperation entries ({totalOpsCount}) does not match " +
                   $"the number of didDocument entries ({totalDidDocsCount}). They must match in count.";
        }

        // Now we enforce operation-specific structure rules:
        // For i-th operation, we look at the i-th DID document.

        // (g) removeFromDidDocument => the matching didDocument must have
        //     *only* a "verificationMethod" key, containing exactly one object with an "id" property, no other fields.
        //
        // (h) setDidDocument => the matching didDocument cannot have "verificationMethod".
        //     It can only have "@context" or "service" keys, and must have at least one of them.
        // (No extra constraints given for addToDidDocument in the prompt.)

        for (int i = 0; i < totalOpsCount; i++)
        {
            var op = request.DidDocumentOperation![i];
            var doc = request.DidDocument![i]; // same index

            switch (op)
            {
                case PrismParameters.RemoveFromDidDocument:
                {
                    // Must have exactly 1 top-level key: "verificationMethod"
                    // That key must be an array with exactly one item that has an "id" only.

                    // Check top-level keys
                    if (doc.Keys.Count != 1 || !doc.ContainsKey("verificationMethod"))
                    {
                        return $"When didDocumentOperation is 'removeFromDidDocument' (index={i}), " +
                               $"the DID document must have exactly one key 'verificationMethod'.";
                    }

                    // Parse the "verificationMethod"
                    var vmObj = doc["verificationMethod"];
                    List<Dictionary<string, object>>? vmList = null;

                    if (vmObj is JsonElement je && je.ValueKind == JsonValueKind.Array)
                    {
                        vmList = je.EnumerateArray()
                            .Select(x => x.Deserialize<Dictionary<string, object>>())
                            .Where(x => x is not null)
                            .ToList()!;
                    }
                    else if (vmObj is List<Dictionary<string, object>> dictList)
                    {
                        vmList = dictList;
                    }
                    else if (vmObj is List<object> listObj)
                    {
                        vmList = listObj
                            .Select(o => o as Dictionary<string, object>)
                            .Where(d => d is not null)
                            .ToList();
                    }

                    if (vmList == null || vmList.Count != 1)
                    {
                        return $"When didDocumentOperation is 'removeFromDidDocument' (index={i}), " +
                               $"'verificationMethod' must be an array with exactly one item.";
                    }

                    var singleVM = vmList[0];
                    // Now that single item must have exactly one key "id"
                    if (singleVM.Keys.Count != 1 || !singleVM.ContainsKey("id"))
                    {
                        return $"When didDocumentOperation is 'removeFromDidDocument' (index={i}), " +
                               $"the single 'verificationMethod' object must have exactly one property 'id'.";
                    }
                }
                    break;

                case PrismParameters.SetDidDocument:
                {
                    // The doc cannot have "verificationMethod".
                    // It may only have "@context" or "service" as top-level keys,
                    // and must have at least one of them.

                    if (doc.ContainsKey("verificationMethod"))
                    {
                        return $"When didDocumentOperation is 'setDidDocument' (index={i}), " +
                               $"the DID document cannot have a 'verificationMethod' property.";
                    }

                    // Allowed top-level keys: "@context", "service" only
                    foreach (var key in doc.Keys)
                    {
                        if (key != "@context" && key != "service")
                        {
                            return $"When didDocumentOperation is 'setDidDocument' (index={i}), " +
                                   $"only '@context' and 'service' are allowed as top-level keys.";
                        }
                    }

                    // Must have at least one of @context or service
                    if (!doc.ContainsKey("@context") && !doc.ContainsKey("service"))
                    {
                        return $"When didDocumentOperation is 'setDidDocument' (index={i}), " +
                               $"the DID document must have at least one of '@context' or 'service'.";
                    }


                    string? didDocumentError = ValidateDidDocument(doc, false);
                    if (didDocumentError != null)
                    {
                        return didDocumentError;
                    }
                }
                    break;

                case PrismParameters.AddToDidDocument:
                    // No extra constraints specified, so do nothing
                    break;
            }
        }

        // All validations passed
        return null;
    }

    /// <summary>
    /// Validates the Secrets and Verification Methods part of the create request.
    /// </summary>
    /// <param name="request">The request containing the secrets and verification methods to validate.</param>
    /// <returns>An error message if validation fails, otherwise null.</returns>
    private static string? ValidateSecretsAndVerificationMethods(RegistrarSecret secret)
    {
        // Allowed values for verification methods
        string[] allowedPurposes = { "authentication", "assertionMethod", "keyAgreement", "capabilityInvocation", "capabilityDelegation" };
        string[] allowedCurves = { "secp256k1", "Ed25519", "X25519" };

        // Check if secret or verification methods exist
        if (secret.VerificationMethod == null || secret.VerificationMethod.Count == 0)
        {
            return "At least one verification method must be provided";
        }

        // Validation for verification methods
        var verificationMethodIds = new HashSet<string>();
        foreach (var method in secret.VerificationMethod)
        {
            // Check required properties: Id
            if (string.IsNullOrEmpty(method.Id))
            {
                return "Each verification method must have an 'id' property";
            }

            // Check for key without # and change to the last part
            if (method.Id.Contains("#"))
            {
                method.Id = method.Id.Split('#')[method.Id.Split('#').Length - 1];
            }

            // Check for key without did
            if (method.Id.Contains("did:"))
            {
                return "Verification method id must not contain 'did:'";
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

            // Check for duplicate purposes within the same method
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

            // Implementation specific: Enforce exactly one purpose
            if (method.Purpose.Count != 1)
            {
                return "A verification method must have exactly one purpose for the current implementation";
            }

            // Prevent using reserved prefix for key Id
            if (method.Id.StartsWith("master", StringComparison.OrdinalIgnoreCase)) // Added IgnoreCase for robustness
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

        // All secret/verification method checks passed
        return null;
    }

    /// <summary>
    /// Validates the DID Document part of the create request.
    /// </summary>
    /// <param name="didDocument">The request containing the DID document to validate.</param>
    /// <returns>An error message if validation fails, otherwise null.</returns>
    private static string? ValidateDidDocument(RegistrarDidDocument didDocument, bool contextRequired)
    {
        // If no DID Document is provided, there's nothing to validate here.
        if (didDocument == null)
        {
            return null;
        }

        // Check if only allowed top-level properties exist
        foreach (var key in didDocument.Keys)
        {
            if (key != "@context" && key != "service")
            {
                // Assuming 'Keys' excludes properties mapped to specific C# properties like 'Services'
                // If 'Keys' includes 'service', this check might need adjustment depending on DidDocumentModel structure.
                // Check the implementation of DidDocumentModel to be sure.
                return $"Invalid property '{key}' in DID document. Only '@context' and 'service' are allowed at the top level (besides strongly-typed properties)";
            }
        }

        // Check if @context is provided (required)
        if (contextRequired && !didDocument.ContainsKey("@context"))
        {
            return "DID document must have an '@context' property";
        }

        // Check @context value
        if (didDocument.TryGetValue("@context", out var contextObj) && contextObj != null)
        {
            IEnumerable<string>? contextStrings = null;

            // Handle System.Text.Json.JsonElement
            if (contextObj is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind != JsonValueKind.Array)
                {
                    return "DID document '@context' must be an array";
                }

                contextStrings = jsonElement.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
            }
            // Handle cases where it might already be deserialized to a list/array of strings
            else if (contextObj is IEnumerable<string> stringEnumerable)
            {
                contextStrings = stringEnumerable.ToList();
            }
            // Handle cases where it might be a list/array of objects (less ideal, but possible)
            else if (contextObj is IEnumerable<object> objectEnumerable)
            {
                contextStrings = objectEnumerable.Select(o => o?.ToString() ?? string.Empty).ToList();
            }
            else
            {
                return "DID document '@context' must be an array";
            }

            if (!contextStrings.Any())
            {
                return "DID document '@context' must be a non-empty array";
            }

            // Check for duplicate contexts and invalid values
            var contextSet = new HashSet<string>();
            foreach (var ctxStr in contextStrings)
            {
                if (string.IsNullOrWhiteSpace(ctxStr))
                {
                    return "DID document '@context' array must only contain non-empty string values";
                }

                if (!contextSet.Add(ctxStr))
                {
                    return $"Duplicate '@context' value found: {ctxStr}";
                }
            }
        }

        // Check services
        // Use the strongly-typed Services property if available and populated
        var services = didDocument.Services;

        // Check consistency: if 'service' key exists but 'Services' property is null/empty, it's likely a deserialization issue or invalid structure
        if (didDocument.ContainsKey("service") && (services == null || services.Count == 0))
        {
            // allow empty array
        }

        if (services != null && services.Count > 0)
        {
            var serviceIds = new HashSet<string>();
            foreach (var service in services)
            {
                if (service == null)
                {
                    return "DID document 'service' array must not contain null entries";
                }

                // Validate service properties
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
        // If 'service' key exists but Services property is null, it might indicate an invalid format
        else if (services == null && didDocument.ContainsKey("service"))
        {
            // Attempt to check the raw value if possible (might require DidDocumentModel changes)
            // For now, we assume if the key 'service' exists, the 'Services' property should be non-null (even if empty)
            // This indicates the input might be like: "service": null or "service": "not an array"
            return "DID document 'service' property must be a valid array of service objects if present";
        }


        // All DID document checks passed
        return null;
    }

    public static string? ValidateDeactivateRequest(RegistrarDeactivateRequestModel request)
    {
        return null;
    }
}