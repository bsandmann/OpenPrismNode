// For PrismParameters

// Required for JsonElement simulation

namespace OpenPrismNode.Web.Tests.RegistrarValidation;

using System.Collections.Generic;
using System.Linq;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Commands.Registrar;
using OpenPrismNode.Web.Models;
using OpenPrismNode.Web.Validators;
using Xunit;

public class RegistrarUpdateRequestValidatorTests
{
    // --- Helper Methods ---

    private static RegistrarSecret CreateUpdateSecret(params string[] keyIds)
    {
        var methods = keyIds.Select(id => new RegistrarVerificationMethodPrivateData
        {
            Id = id,
            Type = "JsonWebKey2020",
            Purpose = new List<string> { "authentication" }, // Purpose needed for secret validation
            Curve = "secp256k1" // Curve needed for secret validation
            // PrivateKeyJwk would be here
        }).ToList();
        return new RegistrarSecret { VerificationMethod = methods };
    }

    // Creates a DID Document fragment for 'addToDidDocument' containing specified verification method IDs
    private static RegistrarDidDocument CreateAddDocWithVerificationMethods(params string[] keyIdsWithDid)
    {
        var vmList = keyIdsWithDid.Select(id =>
            new Dictionary<string, object> { { "id", id } } as object // Cast inner dictionary to object for the list
        ).ToList();

        return new RegistrarDidDocument
        {
            { "verificationMethod", vmList }
            // Can add other valid properties like @context or service here if needed
        };
    }
    
    // Creates a DID Document fragment for 'setDidDocument'
    private static RegistrarDidDocument CreateSetDoc(bool includeContext = true, bool includeService = true)
    {
        var doc = new RegistrarDidDocument();
        if (includeContext)
        {
            doc.Add("@context", new List<string> { "https://www.w3.org/ns/did/v1" });
        }
        if (includeService)
        {
             doc.Add("service", new List<object> 
                {
                    new Dictionary<string, object> 
                    {
                        { "id", "service-set" },
                        { "type", "SetService" },
                        { "serviceEndpoint", "http://example.com/set" }
                    }
                });
        }
        return doc;
    }

    // Creates a DID Document fragment for 'removeFromDidDocument'
    private static RegistrarDidDocument CreateRemoveDoc(string keyIdWithDid)
    {
         return new RegistrarDidDocument
        {
            { 
                "verificationMethod", new List<object> // Must be a list containing one item
                {
                    new Dictionary<string, object> { { "id", keyIdWithDid } } // Item must have ONLY 'id'
                } 
            }
        };
    }

    // --- Test Methods ---

    #region Valid Scenarios

    [Fact]
    public void ValidateUpdateRequest_Valid_SetAddRemove_ReturnsNull()
    {
        // Arrange: Based on user example
        var did = "did:prism:a0e3f1ac977123350eaaf0b703a0262c480eab4ef18398142c8e01a26eaa7fb9";
        var request = new RegistrarUpdateRequestModel
        {
            Secret = CreateUpdateSecret("key-u1", "key-u2"), // 2 secrets
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument, PrismParameters.AddToDidDocument, PrismParameters.RemoveFromDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                CreateSetDoc(), // Doc for setDidDocument
                CreateAddDocWithVerificationMethods(did + "#key-u1", did + "#key-u2"), // Doc for addToDidDocument (Matches secrets)
                CreateRemoveDoc(did + "#key-1") // Doc for removeFromDidDocument (key-1 presumed pre-existing)
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateUpdateRequest_Valid_SetOnly_ReturnsNull()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            Secret = null, // No secret needed for just 'set'
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument },
            DidDocument = new List<RegistrarDidDocument> { CreateSetDoc() }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_Valid_AddOnly_MatchingSecret_ReturnsNull()
    {
        // Arrange
         var did = "did:prism:someIdentifier";
        var request = new RegistrarUpdateRequestModel
        {
            Secret = CreateUpdateSecret("key-add1"), // 1 secret
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument }, // 1 add op
            DidDocument = new List<RegistrarDidDocument> 
            { 
                CreateAddDocWithVerificationMethods(did + "#key-add1") // Doc has matching VM
            } 
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_Valid_RemoveOnly_ReturnsNull()
    {
        // Arrange
        var did = "did:prism:someIdentifier";
        var request = new RegistrarUpdateRequestModel
        {
            Secret = null, // No secret needed for remove
            DidDocumentOperation = new List<string> { PrismParameters.RemoveFromDidDocument },
            DidDocument = new List<RegistrarDidDocument> 
            { 
                CreateRemoveDoc(did + "#key-to-remove") 
            } 
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Invalid Scenarios - Operations and Counts

    [Fact]
    public void ValidateUpdateRequest_InvalidOperationValue_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { "invalidOperation" },
            DidDocument = new List<RegistrarDidDocument> { CreateSetDoc() } // Need matching count doc
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Invalid didDocumentOperation 'invalidOperation'", result);
    }

    [Fact]
    public void ValidateUpdateRequest_TooManySetDidDocument_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument, PrismParameters.SetDidDocument },
            DidDocument = new List<RegistrarDidDocument> { CreateSetDoc(), CreateSetDoc() }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("only one instance of a setDidDocument operation allowed", result);
    }

    [Fact]
    public void ValidateUpdateRequest_MismatchedOperationAndDocumentCounts_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument, PrismParameters.AddToDidDocument }, // 2 ops
            DidDocument = new List<RegistrarDidDocument> { CreateSetDoc() } // 1 doc
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("must match the number of providided didDocuments", result);
    }

    #endregion

    #region Invalid Scenarios - Secret and VerificationMethod Linkage

     [Fact]
    public void ValidateUpdateRequest_InvalidSecret_ReturnsError()
    {
        // Arrange: Use an invalid secret (e.g., missing curve)
        var invalidSecret = CreateUpdateSecret("key-1");
        invalidSecret.VerificationMethod![0].Curve = null; // Make it invalid

        var request = new RegistrarUpdateRequestModel
        {
            Secret = invalidSecret,
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument }, // Needs secret validation
            DidDocument = new List<RegistrarDidDocument> { CreateAddDocWithVerificationMethods("did:ex:123#key-1") }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("must have a 'curve' property", result); // Error from ValidateSecretsAndVerificationMethods
    }


    [Fact]
    public void ValidateUpdateRequest_AddToDidDocumentExceedsSecretMethods_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            Secret = CreateUpdateSecret("key-1"), // 1 secret method
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument, PrismParameters.AddToDidDocument }, // 2 add ops
            DidDocument = new List<RegistrarDidDocument> // 2 docs needed to match op count
            {
                CreateAddDocWithVerificationMethods("did:ex:123#key-1"),
                CreateAddDocWithVerificationMethods("did:ex:123#key-other") // Content doesn't matter for this specific check
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        // Error message seems slightly off in original code: references 'setDidDocument' count? Should be 'addToDidDocument'
        // Assert.Contains("The number of 'setDidDocument' operations (2) must match", result); // Based on original code's message text
         Assert.Contains("must match the number of verification methods", result); // More accurate assertion based on intent
         Assert.Contains("'addToDidDocument'", result); // Check the correct operation name is mentioned
    }

    [Fact]
    public void ValidateUpdateRequest_InsufficientDocsWithVerificationMethodForAdd_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            Secret = CreateUpdateSecret("key-1", "key-2"), // 2 secrets
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument, PrismParameters.AddToDidDocument }, // 2 add ops
            DidDocument = new List<RegistrarDidDocument>
            {
                CreateAddDocWithVerificationMethods("did:ex:123#key-1"), // Doc 1 has VM
                CreateSetDoc() // Doc 2 *does not* have VM key (using SetDoc helper for simplicity)
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("At least 2 DID document entries must contain a 'verificationMethod' property", result);
        Assert.Contains("but only 1 do", result);
    }

    [Fact]
    public void ValidateUpdateRequest_SecretMethodIdMissing_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateUpdateSecret("key-1");
        invalidSecret.VerificationMethod![0].Id = ""; // Empty ID

        var request = new RegistrarUpdateRequestModel
        {
            Secret = invalidSecret,
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument },
             DidDocument = new List<RegistrarDidDocument> { CreateAddDocWithVerificationMethods("did:ex:123#key-1") } // Doc content less relevant here
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        // This error comes from ValidateSecretsAndVerificationMethods called first
        Assert.Contains("Each verification method must have a non-null/non-empty 'id'", result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_SecretMethodIdContainsDid_ReturnsError()
    {
        // Arrange
        var invalidSecret = CreateUpdateSecret("did:ex:123#key-1"); // Has 'did:' prefix after # strip
        // The validator strips the DID part, but then checks if 'did:' remains, which it shouldn't.
        // Let's test the raw ID containing 'did:' before stripping
        invalidSecret.VerificationMethod![0].Id = "did:ex:123"; 


        var request = new RegistrarUpdateRequestModel
        {
            Secret = invalidSecret,
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument },
            DidDocument = new List<RegistrarDidDocument> { CreateAddDocWithVerificationMethods("did:ex:123#something") }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
         // Error from ValidateSecretsAndVerificationMethods
        Assert.Contains("Verification method id must not contain 'did:'", result);
    }


    [Fact]
    public void ValidateUpdateRequest_SecretMethodIdNotFoundInDocuments_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            Secret = CreateUpdateSecret("secret-key-id"), // Secret has this ID
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                // The document *lacks* the verification method with 'secret-key-id'
                CreateAddDocWithVerificationMethods("did:ex:123#different-key-id")
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("No DID document contains a verificationMethod with the id 'secret-key-id'", result);
    }

    [Fact]
    public void ValidateUpdateRequest_DocumentMethodHasExtraFieldsWhenIdMatchesSecret_ReturnsError()
    {
        // Arrange
         var did = "did:prism:a0e3f1ac9771";
         var secretId = "key-match";
        var request = new RegistrarUpdateRequestModel
        {
            Secret = CreateUpdateSecret(secretId),
            DidDocumentOperation = new List<string> { PrismParameters.AddToDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                // This doc's VM matches the secret ID but has an extra field
                new RegistrarDidDocument
                {
                    {
                        "verificationMethod", new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                { "id", did + "#" + secretId },
                                { "type", "SomeType" } // <-- Disallowed extra field
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains($"verificationMethod with id '{secretId}' that also appears", result);
        Assert.Contains("but it contains additional properties beyond 'id'", result);
    }

    #endregion

    #region Invalid Scenarios - Operation-Specific Document Structures

    // --- RemoveFromDidDocument Structure Tests ---

    [Fact]
    public void ValidateUpdateRequest_RemoveOp_InvalidDocument_WrongTopLevelKey_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.RemoveFromDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                new RegistrarDidDocument { { "@context", "value" } } // Should only have 'verificationMethod'
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("must have exactly one key 'verificationMethod'", result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_RemoveOp_InvalidDocument_TooManyVMs_ReturnsError()
    {
        // Arrange
         var did = "did:prism:a0e3f1";
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.RemoveFromDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                new RegistrarDidDocument
                {
                    {
                        "verificationMethod", new List<object>
                        {
                            new Dictionary<string, object> { { "id", did + "#key1" } },
                            new Dictionary<string, object> { { "id", did + "#key2" } } // <-- Too many VMs
                        }
                    }
                }
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("'verificationMethod' must be an array with exactly one item", result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_RemoveOp_InvalidDocument_VMHasExtraKey_ReturnsError()
    {
        // Arrange
         var did = "did:prism:a0e3f1";
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.RemoveFromDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                 new RegistrarDidDocument
                {
                    {
                        "verificationMethod", new List<object> 
                        {
                            new Dictionary<string, object> 
                            { 
                                { "id", did + "#key1" },
                                { "type", "SomeType"} // <-- VM object has extra key
                            } 
                        }
                    }
                }
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("single 'verificationMethod' object must have exactly one property 'id'", result);
    }

    // --- SetDidDocument Structure Tests ---

    [Fact]
    public void ValidateUpdateRequest_SetOp_DocumentHasVerificationMethod_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument },
            DidDocument = new List<RegistrarDidDocument>
            {
                // Invalid: Set operation doc cannot contain verificationMethod
                CreateAddDocWithVerificationMethods("did:ex:123#somekey") 
            }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("cannot have a 'verificationMethod' property", result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_SetOp_DocumentHasExtraTopLevelKey_ReturnsError()
    {
        // Arrange
        var doc = CreateSetDoc(true, true); // Starts valid with context and service
        doc.Add("invalidKey", "someValue"); // Add disallowed key

        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument },
            DidDocument = new List<RegistrarDidDocument> { doc }
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("only '@context' and 'service' are allowed", result);
    }

     [Fact]
    public void ValidateUpdateRequest_SetOp_DocumentMissingContextAndService_ReturnsError()
    {
        // Arrange
        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument },
            DidDocument = new List<RegistrarDidDocument> 
            { 
                new RegistrarDidDocument() // Empty doc, missing both required keys
            } 
        };

        // Act
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("must have at least one of '@context' or 'service'", result);
    }
    
    [Fact]
    public void ValidateUpdateRequest_SetOp_CallsValidateDidDocument_InvalidContext_ReturnsError()
    {
        // Arrange
        var doc = new RegistrarDidDocument { { "@context", "not-an-array"} }; // Invalid context format

        var request = new RegistrarUpdateRequestModel
        {
            DidDocumentOperation = new List<string> { PrismParameters.SetDidDocument },
            DidDocument = new List<RegistrarDidDocument> { doc }
        };

        // Act
        // This triggers the ValidateDidDocument call within the SetDidDocument case
        var result = RegistrarRequestValidators.ValidateUpdateRequest(request); 

        // Assert
        Assert.NotNull(result);
         // Error comes from the nested ValidateDidDocument call
        Assert.Contains("DID document '@context' must be an array", result);
    }

    #endregion
}