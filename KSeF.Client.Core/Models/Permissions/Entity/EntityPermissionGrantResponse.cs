using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Permissions.Entity
{
    public class EntityPermissionGrantResponse
    {
        public List<EntityPermissionGrant> Permissions { get; set; }
        public bool HasMore { get; set; }
    }

    public class EntityPermissionGrant
    {
        public string Id { get; set; }
        public EntityPermissionGrantQueryContextIdentifier ContextIdentifier { get; set; }
        public PermissionScope PermissionScope { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public bool CanDelegate { get; set; }
    }

    public enum PermissionScope
    {
        InvoiceWrite,
        InvoiceRead
    }
}
