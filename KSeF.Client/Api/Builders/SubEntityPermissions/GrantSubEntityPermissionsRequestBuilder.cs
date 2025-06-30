using KSeF.Client.Core.Models.Permissions.SubUnit;

namespace KSeF.Client.Api.Builders.SubUnitPermissions;

public static class GrantSubUnitPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IContextStep WithSubject(SubjectIdentifier subject);
    }

    public interface IContextStep
    {
        IOptionalStep WithContext(ContextIdentifier context);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        GrantPermissionsSubUnitRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IContextStep,
        IOptionalStep
    {
        private SubjectIdentifier _subject;
        private ContextIdentifier _context;
        private string _description;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IContextStep WithSubject(SubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithContext(ContextIdentifier context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantPermissionsSubUnitRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("WithSubject(...) must be called first.");
            if (_context is null)
                throw new InvalidOperationException("WithContext(...) must be called after subject.");

            return new GrantPermissionsSubUnitRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
            };
        }
    }
}
