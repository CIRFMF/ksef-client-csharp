using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.EntityPermissions;

/// <summary>
/// Buduje żądanie nadania uprawnień wskazanemu podmiotowi w KSeF.
/// </summary>
public static class GrantEntityPermissionsRequestBuilder
{
    /// <summary>
    /// Rozpoczyna budowę żądania nadania uprawnień.
    /// </summary>
    /// <returns>
    /// Interfejs pozwalający ustawić podmiot, któremu mają zostać nadane uprawnienia.
    /// </returns>
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    /// <summary>
    /// Etap budowy żądania, w którym wybierany jest podmiot.
    /// </summary>
    public interface ISubjectStep
    {
        /// <summary>
        /// Ustawia identyfikator podmiotu, któremu mają zostać nadane uprawnienia.
        /// </summary>
        /// <param name="subject">
        /// Identyfikator podmiotu (np. NIP, dane jednostki). Nie może być null
        /// i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający określić listę nadawanych uprawnień.
        /// </returns>
        IPermissionsStep WithSubject(GrantPermissionsEntitySubjectIdentifier subject);
    }

    /// <summary>
    /// Etap budowy żądania, w którym określane są uprawnienia nadawane podmiotowi.
    /// </summary>
    public interface IPermissionsStep
    {
        /// <summary>
        /// Ustawia listę uprawnień nadawanych podmiotowi.
        /// </summary>
        /// <param name="permissions">
        /// Co najmniej jedno uprawnienie, które ma zostać nadane.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający opcjonalnie dodać opis i szczegóły podmiotu
        /// lub od razu zbudować żądanie.
        /// </returns>
        IOptionalStep WithPermissions(params EntityPermission[] permissions);
    }

    /// <summary>
    /// Etap budowy żądania, w którym można dodać opis i szczegóły podmiotu.
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
        /// Ustawia dodatkowe dane podmiotu, któremu nadawane są uprawnienia.
        /// </summary>
        /// <param name="subjectDetails">
        /// Szczegóły podmiotu (np. dane kontaktowe). Nie mogą być null.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IOptionalStep WithSubjectDetails(PermissionsEntitySubjectDetails subjectDetails);

        /// <summary>
        /// Tworzy finalne żądanie nadania uprawnień podmiotowi.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="GrantPermissionsEntityRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        GrantPermissionsEntityRequest Build();
    }

    /// <inheritdoc />
    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private GrantPermissionsEntitySubjectIdentifier _subject;
        private ICollection<EntityPermission> _permissions;
        private string _description;
        private PermissionsEntitySubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        /// <summary>
        /// Tworzy nową implementację buildera żądania nadania uprawnień.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        /// <inheritdoc />
        public IPermissionsStep WithSubject(GrantPermissionsEntitySubjectIdentifier subject)
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
        public IOptionalStep WithPermissions(params EntityPermission[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
            {
                throw new ArgumentException("Wymagane jest co najmniej jedno uprawnienie.", nameof(permissions));
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
        public IOptionalStep WithSubjectDetails(PermissionsEntitySubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        /// <inheritdoc />
        public GrantPermissionsEntityRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Najpierw należy wywołać WithSubject(...).");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Po wskazaniu podmiotu należy wywołać WithPermissions(...).");
            }

            return new GrantPermissionsEntityRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}