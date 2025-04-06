// Required for JsonElement simulation if needed

namespace OpenPrismNode.Web.Tests.RegistrarValidation;

using System.Collections.Generic;
using OpenPrismNode.Core.Commands.Registrar;
using OpenPrismNode.Web.Models;
using OpenPrismNode.Web.Validators;
using Xunit;

public class RegistrarCreateRequestValidatorTests
{
    // Helper method to create a basic valid secret
    private static RegistrarSecret CreateValidSecret(int methodCount = 1)
    {
        var methods = new List<RegistrarVerificationMethodPrivateData>();
        for (int i = 1; i <= methodCount; i++)
        {
            methods.Add(new RegistrarVerificationMethodPrivateData
            {
                Id = $"key-{i}",
                Type = "JsonWebKey2020",
                Purpose = new List<string> { "authentication" },
                Curve = "secp256k1"
                // PrivateKeyJwk would typically be here in a real scenario
            });
        }
        return new RegistrarSecret { VerificationMethod = methods };
    }

    // Helper method to create a basic valid DID document
    private static RegistrarDidDocument CreateValidDidDocument()
    {
        // Using Dictionary directly to simulate JSON structure before potential parsing
        var doc = new RegistrarDidDocument
        {
            { "@context", new List<string> { "https://www.w3.org/ns/did/v1" } },
            {
                "service", new List<object> // Use List<object> to simulate deserialized JSON array
                {
                    new Dictionary<string, object> // Use Dictionary to simulate service object
                    {
                        { "id", "service-1" },
                        { "type", "TestService" },
                        { "serviceEndpoint", "http://example.com/service" }
                    }
                }
            }
        };
        // Manually ensure the strongly-typed property gets populated if logic relies on it
        // In real usage, deserialization + the custom converter handles this.
        // For testing direct object creation, we might need to help it.
        // doc.Service = doc.ParseServices(); // Let the internal logic parse it
        return doc;
    }

    [Fact]
    public void ValidateCreateRequest_ValidRequest_ReturnsNull()
    {
        // Arrange
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(2),
            DidDocument = CreateValidDidDocument()
            // Other properties like Method, Options are not validated by this specific method
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateCreateRequest_NullSecret_ValidDocument_ReturnsError()
    {
        // Arrange
        // Note: The validator *itself* doesn't enforce Secret != null,
        // but ValidateSecretsAndVerificationMethods does if Secret.VerificationMethod is accessed.
        // If Secret itself is null, the first check passes. Let's test where Secret *exists* but has no methods.
        var request = new RegistrarCreateRequestModel
        {
            Secret = new RegistrarSecret { VerificationMethod = new List<RegistrarVerificationMethodPrivateData>() }, // Empty list
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("At least one verification method must be provided", result);
    }

    [Fact]
    public void ValidateCreateRequest_SecretIsNull_ValidDocument_ReturnsNull()
    {
        // Arrange
        // If the secret object *itself* is null, the first validator check is skipped.
        var request = new RegistrarCreateRequestModel
        {
            Secret = null,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
         // If Secret is null, ValidateSecretsAndVerificationMethods is skipped.
         // If DidDocument is valid, ValidateDidDocument returns null.
         // Therefore, the overall result should be null according to the code structure.
        Assert.Null(result);
    }


    [Fact]
    public void ValidateCreateRequest_ValidSecret_NullDocument_ReturnsNull()
    {
        // Arrange
        // Note: While the endpoint logic might *require* a DidDocument for create,
        // the validator method itself handles a null DidDocument gracefully by skipping its validation.
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = null
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.Null(result); // Validation passes as the document validation step is skipped
    }

    [Fact]
    public void ValidateCreateRequest_InvalidSecret_MissingId_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateValidSecret();
        invalidSecret.VerificationMethod![0].Id = null; // Make it invalid
        var request = new RegistrarCreateRequestModel
        {
            Secret = invalidSecret,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Each verification method must have an 'id' property", result);
    }

    [Fact]
    public void ValidateCreateRequest_InvalidSecret_DuplicateId_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateValidSecret(2);
        invalidSecret.VerificationMethod![1].Id = invalidSecret.VerificationMethod[0].Id; // Duplicate ID
        var request = new RegistrarCreateRequestModel
        {
            Secret = invalidSecret,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains($"Duplicate verification method id found: {invalidSecret.VerificationMethod[0].Id}", result);
    }

    [Fact]
    public void ValidateCreateRequest_InvalidSecret_InvalidPurpose_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateValidSecret();
        invalidSecret.VerificationMethod![0].Purpose = new List<string> { "invalid-purpose" };
        var request = new RegistrarCreateRequestModel
        {
            Secret = invalidSecret,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid purpose 'invalid-purpose'", result);
    }

    [Fact]
    public void ValidateCreateRequest_InvalidSecret_MultiplePurposes_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateValidSecret();
        // Implementation specific check: only one purpose allowed
        invalidSecret.VerificationMethod![0].Purpose = new List<string> { "authentication", "assertionMethod" };
        var request = new RegistrarCreateRequestModel
        {
            Secret = invalidSecret,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("A verification method must have exactly one purpose", result);
    }

    [Fact]
    public void ValidateCreateRequest_InvalidSecret_InvalidCurve_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateValidSecret();
        invalidSecret.VerificationMethod![0].Curve = "invalid-curve";
        var request = new RegistrarCreateRequestModel
        {
            Secret = invalidSecret,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid curve 'invalid-curve'", result);
    }

     [Fact]
    public void ValidateCreateRequest_InvalidSecret_IdStartsWithMaster_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateValidSecret();
        invalidSecret.VerificationMethod![0].Id = "masterKey"; // Invalid prefix
        var request = new RegistrarCreateRequestModel
        {
            Secret = invalidSecret,
            DidDocument = CreateValidDidDocument()
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("The key-Id is not allowed to start with 'master'", result);
    }

    [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_MissingContext_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        invalidDocument.Remove("@context"); // Make it invalid for create
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("DID document must have an '@context' property", result);
    }

    [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_ContextNotArray_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        invalidDocument["@context"] = "not-an-array"; // Invalid context format
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("DID document '@context' must be an array", result);
    }

    [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_DuplicateContext_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        invalidDocument["@context"] = new List<string> { "https://www.w3.org/ns/did/v1", "https://www.w3.org/ns/did/v1" }; // Duplicate
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Duplicate '@context' value found", result);
    }

    [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_InvalidTopLevelKey_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        invalidDocument.Add("invalidKey", "someValue"); // Invalid top-level key
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid property 'invalidKey' in DID document", result);
    }

    [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_InvalidServiceId_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        // Modify the service data to be invalid (e.g., missing ID)
         var services = invalidDocument["service"] as List<object>;
         var serviceDict = services![0] as Dictionary<string, object>;
         serviceDict!.Remove("id");
         invalidDocument["service"] = services; // Put it back (might not be necessary if modified in place)
        // Need to potentially clear the cached strongly-typed property if it exists/was used
        // invalidDocument.Service = null; // Or force re-parsing if needed

        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        // The error comes from parsing within ValidateDidDocument -> ParseServices -> FromDictionary
        Assert.Contains("Each service must have a non-empty 'id' property", result);
    }

    [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_DuplicateServiceId_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        var services = invalidDocument["service"] as List<object>;
        services!.Add(new Dictionary<string, object> // Add a second service with the same ID
            {
                { "id", "service-1" }, // Duplicate ID
                { "type", "AnotherService" },
                { "serviceEndpoint", "http://example.com/another" }
            });
        invalidDocument["service"] = services;

        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Duplicate service id found: service-1", result);
    }

     [Fact]
    public void ValidateCreateRequest_ValidSecret_InvalidDocument_ServiceNotArray_ReturnsError()
    {
        // Arrange
        var invalidDocument = CreateValidDidDocument();
        invalidDocument["service"] = "not-an-array"; // Service should be an array
        var request = new RegistrarCreateRequestModel
        {
            Secret = CreateValidSecret(),
            DidDocument = invalidDocument
        };

        // Act
        var result = RegistrarRequestValidators.ValidateCreateRequest(request);

        // Assert
        Assert.NotNull(result);
         // This error message comes from the final check in ValidateDidDocument
        Assert.Contains("DID document 'service' property must be a valid array of service objects if present", result);
    }
}