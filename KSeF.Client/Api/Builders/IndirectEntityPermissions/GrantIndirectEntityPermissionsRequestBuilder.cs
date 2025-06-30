using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using StandardPermissionType = KSeF.Client.Core.Models.Permissions.IndirectEntity.StandardPermissionType;

namespace KSeF.Client.Api.Builders.IndirectEntityPermissions;

public static class GrantIndirectEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IContextStep WithSubject(SubjectIdentifier subject);
    }
    public interface IContextStep
    {
        IPermissionsStep WithContext(TargetIdentifier context);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermissions(params StandardPermissionType[] permissions);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        GrantPermissionsIndirectEntityRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IContextStep,
        IPermissionsStep,
        IOptionalStep
    {
        private SubjectIdentifier _subject;
        private ICollection<StandardPermissionType> _permissions;
        private string _description;
        private TargetIdentifier _context;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IContextStep WithSubject(SubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithPermissions(params StandardPermissionType[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                throw new ArgumentException("At least one permission.", nameof(permissions));

            _permissions = permissions;
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantPermissionsIndirectEntityRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("WithSubject(...) must be called first.");
            if (_context is null)
                throw new InvalidOperationException("WithSubject(...) must be called");
            if (_permissions is null)
                throw new InvalidOperationException("WithPermissions(...) must be called after subject.");

            return new GrantPermissionsIndirectEntityRequest
            {
                SubjectIdentifier = _subject,
                TargetIdentifier = _context,
                Permissions = _permissions,
                Description = _description,
            };
        }

        public IPermissionsStep WithContext(TargetIdentifier context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            return this;
        }        
    }
}
