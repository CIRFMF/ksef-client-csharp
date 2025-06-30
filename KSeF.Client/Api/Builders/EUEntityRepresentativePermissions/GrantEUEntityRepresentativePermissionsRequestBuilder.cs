using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;
using StandardPermissionType = KSeF.Client.Core.Models.Permissions.EUEntityRepresentative.StandardPermissionType;

namespace KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;

public static class GrantEUEntityRepresentativePermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(SubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermissions(params StandardPermissionType[] permissions);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        GrantPermissionsEUEntitRepresentativeRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private SubjectIdentifier _subject;
        private ICollection<StandardPermissionType> _permissions;
        private string _description;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(SubjectIdentifier subject)
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

        public GrantPermissionsEUEntitRepresentativeRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("WithSubject(...) must be called first.");
            if (_permissions is null)
                throw new InvalidOperationException("WithPermissions(...) must be called after subject.");

            return new GrantPermissionsEUEntitRepresentativeRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
            };
        }
    }
}
