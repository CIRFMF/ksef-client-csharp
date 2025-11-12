using KSeF.Client.Core.Models.Permissions.Identifiers;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Person
{
    public class PersonPermissionsQueryRequest
    {
        public PersonPermissionsAuthorIdentifier AuthorIdentifier { get; set; }
        public PersonPermissionsAuthorizedIdentifier AuthorizedIdentifier { get; set; }
        public PersonPermissionsContextIdentifier ContextIdentifier { get; set; }
        public PersonPermissionsTargetIdentifier TargetIdentifier { get; set; }
        public List<PersonPermissionType> PermissionTypes { get; set; }
        public PersonPermissionState? PermissionState { get; set; }
        public PersonQueryType QueryType { get; set; }
    }
    public enum PersonQueryType
    {
        PermissionsInCurrentContext,
        PermissionsGrantedInCurrentContext
    }
}
