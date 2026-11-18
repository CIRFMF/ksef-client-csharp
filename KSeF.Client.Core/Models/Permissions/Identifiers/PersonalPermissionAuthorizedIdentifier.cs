namespace KSeF.Client.Core.Models.Permissions.Identifiers
{
    public class PersonalPermissionAuthorizedIdentifier
    {
        public PersonalPermissionAuthorizedIdentifierType Type { get; set; }
        public string Value { get; set; }
    }
    public enum PersonalPermissionAuthorizedIdentifierType
    {
        Nip,
        Pesel,
        Fingerprint
    }
}
