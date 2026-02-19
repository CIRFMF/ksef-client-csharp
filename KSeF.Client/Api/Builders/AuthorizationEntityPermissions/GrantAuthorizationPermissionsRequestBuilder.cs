using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.AuthorizationEntityPermissions;

/// <summary>
/// Buduje żądanie nadania uprawnień autoryzacyjnych w KSeF.
/// </summary>
public static class GrantAuthorizationPermissionsRequestBuilder
{
    /// <summary>
    /// Rozpoczyna budowę żądania nadania uprawnień.
    /// </summary>
    /// <returns>
    /// Interfejs umożliwiający ustawienie identyfikatora podmiotu, któremu nadawane są uprawnienia.
    /// </returns>
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    /// <summary>
    /// Etap budowy żądania, w którym określany jest podmiot, któremu nadawane są uprawnienia.
    /// </summary>
    public interface ISubjectStep
    {
        /// <summary>
        /// Ustawia identyfikator podmiotu, któremu mają zostać nadane uprawnienia.
        /// </summary>
        /// <param name="subject">
        /// Identyfikator podmiotu (np. NIP, dane osoby). Nie może być null
        /// i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>Interfejs pozwalający wybrać rodzaj nadawanego uprawnienia.</returns>
        IPermissionsStep WithSubject(AuthorizationSubjectIdentifier subject);
    }

    /// <summary>
    /// Etap budowy żądania, w którym wybierany jest typ uprawnienia.
    /// </summary>
    public interface IPermissionsStep
    {
        /// <summary>
        /// Ustawia typ uprawnienia, które ma zostać nadane.
        /// </summary>
        /// <param name="permission">Typ uprawnienia autoryzacyjnego.</param>
        /// <returns>
        /// Interfejs pozwalający opcjonalnie ustawić opis i szczegóły podmiotu
        /// lub od razu zbudować żądanie.
        /// </returns>
        IOptionalStep WithPermission(AuthorizationPermissionType permission);
    }

    /// <summary>
    /// Etap budowy żądania, w którym można dodać opis uprawnienia oraz szczegóły podmiotu.
    /// </summary>
    public interface IOptionalStep
    {
        /// <summary>
        /// Ustawia opis nadawanego uprawnienia.
        /// </summary>
        /// <param name="description">
        /// Opis uprawnienia. Nie może być pusty; długość musi mieścić się
        /// w zakresie określonym przez <see cref="ValidValues.PermissionDescriptionMinLength"/>
        /// i <see cref="ValidValues.PermissionDescriptionMaxLength"/>.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IOptionalStep WithDescription(string description);

        /// <summary>
        /// Ustawia dodatkowe dane podmiotu, któremu nadawane są uprawnienia.
        /// </summary>
        /// <param name="subjectDetails">
        /// Szczegóły podmiotu (np. dane kontaktowe). Nie mogą być null.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IOptionalStep WithSubjectDetails(PermissionsAuthorizationSubjectDetails subjectDetails);

        /// <summary>
        /// Tworzy finalne żądanie nadania uprawnień autoryzacyjnych.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="GrantPermissionsAuthorizationRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        GrantPermissionsAuthorizationRequest Build();
    }


    /// <inheritdoc />
    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private AuthorizationSubjectIdentifier _subject;
        private AuthorizationPermissionType _permission;
        private string _description;
        private PermissionsAuthorizationSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        /// <inheritdoc />
        public IPermissionsStep WithSubject(AuthorizationSubjectIdentifier subject)
        {
            Guard.ThrowIfNull(subject);
            TypeValueValidator.Validate(subject);
            _subject = subject;
            return this;
        }

        /// <inheritdoc />
        public IOptionalStep WithPermission(AuthorizationPermissionType permission)
        {
            _permission = permission;
            return this;
        }

        /// <inheritdoc />
        public IOptionalStep WithDescription(string description)
        {
            Guard.ThrowIfNull(description);
            if (description.Length < ValidValues.PermissionDescriptionMinLength)
            {
                throw new ArgumentException($"Opis uprawnienia za krótki, minimalna długość: {ValidValues.PermissionDescriptionMinLength} znaków.", nameof(description));
            }
            if (description.Length > ValidValues.PermissionDescriptionMaxLength)
            {
                throw new ArgumentException($"Opis uprawnienia za długi, maksymalna długość: {ValidValues.PermissionDescriptionMaxLength} znaków.", nameof(description));
            }
            _description = description;
            return this;
        }

        /// <inheritdoc />
        public IOptionalStep WithSubjectDetails(PermissionsAuthorizationSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        /// <inheritdoc />
        public GrantPermissionsAuthorizationRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }

            return new GrantPermissionsAuthorizationRequest
            {
                SubjectIdentifier = _subject,
                Permission = _permission,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}