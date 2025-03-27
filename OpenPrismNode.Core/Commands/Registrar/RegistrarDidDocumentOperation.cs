namespace OpenPrismNode.Core.Commands.Registrar
{
    /// <summary>
    /// Standard DID Document update operations
    /// </summary>
    public static class RegistrarDidDocumentOperation
    {
        public const string SetDidDocument = "setDidDocument";
        public const string AddToDidDocument = "addToDidDocument";
        public const string RemoveFromDidDocument = "removeFromDidDocument";
        public const string Deactivate = "deactivate"; // From Extensions
    }
}