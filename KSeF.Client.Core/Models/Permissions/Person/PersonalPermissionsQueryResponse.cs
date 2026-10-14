using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Person
{
    /// <summary>
    /// Odpowiedź na zapytanie o listę własnych uprawnień (POST /permissions/query/personal/grants).
    /// </summary>
    public class PersonalPermissionsQueryResponse : PagedPermissionsResponse<PersonalPermission>
    {
        /// <summary>
        /// Lista uprawnień właścicielskich uwierzytelnionego podmiotu.
        /// </summary>
        public List<PersonPermissionType> OwnerPermissions { get; set; }
    }
}
