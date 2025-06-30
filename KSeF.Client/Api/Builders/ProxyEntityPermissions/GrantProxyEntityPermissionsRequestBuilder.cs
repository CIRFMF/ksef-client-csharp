using KSeF.Client.Core.Models.Permissions.ProxyEntity;
using StandardPermissionType = KSeF.Client.Core.Models.Permissions.ProxyEntity.StandardPermissionType;

namespace KSeF.Client.Api.Builders.ProxyEntityPermissions;

public static class GrantProxyEntityPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(SubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermission(StandardPermissionType permission);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        GrantPermissionsProxyEntityRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private SubjectIdentifier _subject;
        private StandardPermissionType _permission;
        private string _description;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(SubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithPermission(StandardPermissionType permission)
        {

            _permission = permission;
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantPermissionsProxyEntityRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("WithSubject(...) must be called first.");

            return new GrantPermissionsProxyEntityRequest
            {
                SubjectIdentifier = _subject,
                Permission = _permission,
                Description = _description,
            };
        }
    }
}
