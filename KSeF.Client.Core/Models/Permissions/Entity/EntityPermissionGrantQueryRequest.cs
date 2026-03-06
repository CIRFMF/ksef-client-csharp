using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace KSeF.Client.Core.Models.Permissions.Entity
{
    public class EntityPermissionGrantQueryRequest
    {
        public EntityPermissionGrantQueryContextIdentifier ContextIdentifier { get; set; }
    }

    public class EntityPermissionGrantQueryContextIdentifier
    {
        public EntityPermissionGrantQueryContextIdentifierType Type { get; set; }
        public string Value { get; set; }

    }

    public enum EntityPermissionGrantQueryContextIdentifierType
    {
        [EnumMember(Value = "Nip")]
        Nip,
        [EnumMember(Value = "InternalId")]
        InternalId
    }
}
