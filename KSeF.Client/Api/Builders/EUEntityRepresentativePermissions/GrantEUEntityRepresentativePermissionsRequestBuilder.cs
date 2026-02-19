using KSeF.Client.Core.Models.Permissions.EuEntityRepresentative;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;

/// <summary>
/// Buduje żądanie nadania uprawnień przedstawicielowi podmiotu z UE w KSeF.
/// </summary>
public static class GrantEUEntityRepresentativePermissionsRequestBuilder
{
    /// <summary>
    /// Rozpoczyna budowę żądania nadania uprawnień przedstawicielowi podmiotu z UE.
    /// </summary>
    /// <returns>
    /// Interfejs pozwalający ustawić przedstawiciela, któremu nadawane są uprawnienia.
    /// </returns>
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    /// <summary>
    /// Etap budowy żądania, w którym ustawiany jest przedstawiciel podmiotu z UE.
    /// </summary>
    public interface ISubjectStep
    {
        /// <summary>
        /// Ustawia identyfikator przedstawiciela podmiotu z UE, któremu nadawane są uprawnienia.
        /// </summary>
        /// <param name="subject">
        /// Identyfikator przedstawiciela (np. dane osoby). Nie może być null
        /// i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający określić listę nadawanych uprawnień.
        /// </returns>
        IPermissionsStep WithSubject(EuEntityRepresentativeSubjectIdentifier subject);
    }

    /// <summary>
    /// Etap budowy żądania, w którym określane są uprawnienia przedstawiciela.
    /// </summary>
    public interface IPermissionsStep
    {
        /// <summary>
        /// Ustawia listę uprawnień nadawanych przedstawicielowi podmiotu z UE.
        /// </summary>
        /// <param name="permissions">
        /// Co najmniej jedno uprawnienie, które ma zostać nadane.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający opcjonalnie dodać opis i szczegóły przedstawiciela
        /// lub od razu zbudować żądanie.
        /// </returns>
        IOptionalStep WithPermissions(params EuEntityRepresentativeStandardPermissionType[] permissions);
    }

    /// <summary>
    /// Etap budowy żądania, w którym można dodać opis i dane szczegółowe przedstawiciela.
    /// </summary>
    public interface IOptionalStep
    {
        /// <summary>
        /// Ustawia opis nadawanych uprawnień.
        /// </summary>
        /// <param name="description">
        /// Opis uprawnienia. Nie może być pusty; długość musi mieścić się
        /// w zakresie <see cref="ValidValues.PermissionDescriptionMinLength"/>
        ///–<see cref="ValidValues.PermissionDescriptionMaxLength"/>.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IOptionalStep WithDescription(string description);

        /// <summary>
        /// Ustawia dodatkowe dane przedstawiciela podmiotu z UE (np. dane kontaktowe).
        /// </summary>
        /// <param name="subjectDetails">
        /// Szczegóły przedstawiciela. Nie mogą być null.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IOptionalStep WithSubjectDetails(EuEntityRepresentativeSubjectDetails subjectDetails);

        /// <summary>
        /// Tworzy finalne żądanie nadania uprawnień przedstawicielowi podmiotu z UE.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="GrantPermissionsEuEntityRepresentativeRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        GrantPermissionsEuEntityRepresentativeRequest Build();
    }

    /// <inheritdoc />
    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private EuEntityRepresentativeSubjectIdentifier _subject;
        private ICollection<EuEntityRepresentativeStandardPermissionType> _permissions;
        private string _description;
        private EuEntityRepresentativeSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        /// <summary>
        /// Tworzy nową implementację buildera żądania nadania uprawnień przedstawicielowi podmiotu z UE.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        /// <inheritdoc />
        public IPermissionsStep WithSubject(EuEntityRepresentativeSubjectIdentifier subject)
        {
            Guard.ThrowIfNull(subject);
            if (!TypeValueValidator.Validate(subject))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
            }

            _subject = subject;
            return this;
        }

        /// <inheritdoc />
        public IOptionalStep WithPermissions(params EuEntityRepresentativeStandardPermissionType[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
            {
                throw new ArgumentException("Należy podać co najmniej jedno uprawnienie.", nameof(permissions));
            }

            _permissions = permissions;
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
        public IOptionalStep WithSubjectDetails(EuEntityRepresentativeSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        /// <inheritdoc />
        public GrantPermissionsEuEntityRepresentativeRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu podmiotu.");
            }

            return new GrantPermissionsEuEntityRepresentativeRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}