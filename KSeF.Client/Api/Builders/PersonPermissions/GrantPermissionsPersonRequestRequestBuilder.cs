using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.PersonPermissions;

/// <summary>
/// Buduje żądanie nadania uprawnień osobie fizycznej w KSeF.
/// </summary>
public static class GrantPersonPermissionsRequestBuilder
{
    /// <summary>
    /// Rozpoczyna budowę żądania nadania uprawnień osobie.
    /// </summary>
    /// <returns>
    /// Interfejs pozwalający ustawić osobę, której nadawane są uprawnienia.
    /// </returns>
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    /// <summary>
    /// Etap budowy żądania, w którym ustawiana jest osoba, której nadajemy uprawnienia.
    /// </summary>
    public interface ISubjectStep
    {
        /// <summary>
        /// Ustawia identyfikator osoby, której nadawane są uprawnienia.
        /// </summary>
        /// <param name="subject">
        /// Identyfikator osoby (np. PESEL lub inny identyfikator). Nie może być null
        /// i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający określić listę nadawanych uprawnień.
        /// </returns>
        IPermissionsStep WithSubject(GrantPermissionsPersonSubjectIdentifier subject);
    }

    /// <summary>
    /// Etap budowy żądania, w którym określane są uprawnienia nadawane osobie.
    /// </summary>
    public interface IPermissionsStep
    {
        /// <summary>
        /// Ustawia listę uprawnień nadawanych osobie.
        /// </summary>
        /// <param name="permissions">
        /// Co najmniej jedno uprawnienie, które ma zostać nadane.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający ustawić opis nadawanych uprawnień.
        /// </returns>
        IDescriptionStep WithPermissions(params PersonPermissionType[] permissions);
    }

    /// <summary>
    /// Etap budowy żądania, w którym ustawiany jest opis nadawanych uprawnień.
    /// </summary>
    public interface IDescriptionStep
    {
        /// <summary>
        /// Ustawia opis nadawanych uprawnień.
        /// </summary>
        /// <param name="description">
        /// Opis uprawnienia. Nie może być pusty; długość musi mieścić się
        /// w zakresie <see cref="ValidValues.PermissionDescriptionMinLength"/>
        ///–<see cref="ValidValues.PermissionDescriptionMaxLength"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający dodać dane szczegółowe osoby i zbudować żądanie.
        /// </returns>
        IBuildStep WithDescription(string description);
    }

    /// <summary>
    /// Ostatni etap budowy żądania nadania uprawnień osobie.
    /// </summary>
    public interface IBuildStep
    {
        /// <summary>
        /// Ustawia dodatkowe dane osoby, której nadawane są uprawnienia.
        /// </summary>
        /// <param name="subjectDetails">
        /// Szczegóły osoby (np. dane kontaktowe). Mogą być null, jeśli brak dodatkowych danych.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IBuildStep WithSubjectDetails(PersonPermissionSubjectDetails subjectDetails);

        /// <summary>
        /// Tworzy finalne żądanie nadania uprawnień osobie.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="GrantPermissionsPersonRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        GrantPermissionsPersonRequest Build();
    }

    /// <inheritdoc />
    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IDescriptionStep,
        IBuildStep
    {
        private GrantPermissionsPersonSubjectIdentifier _subject;
        private ICollection<PersonPermissionType> _permissions;
        private string _description;
        private PersonPermissionSubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        /// <summary>
        /// Tworzy nową implementację buildera żądania nadania uprawnień osobie.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        /// <inheritdoc />
        public IPermissionsStep WithSubject(GrantPermissionsPersonSubjectIdentifier subject)
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
        public IDescriptionStep WithPermissions(params PersonPermissionType[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
            {
                throw new ArgumentException("Należy podać co najmniej jedno uprawnienie.", nameof(permissions));
            }

            _permissions = permissions;
            return this;
        }

        /// <inheritdoc />
        public IBuildStep WithDescription(string description)
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
        public IBuildStep WithSubjectDetails(PersonPermissionSubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails;
            return this;
        }

        /// <inheritdoc />
        public GrantPermissionsPersonRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            if (_description is null)
            {
                throw new InvalidOperationException("Metoda WithDescription(...) musi zostać wywołana po ustawieniu uprawnień.");
            }

            return new GrantPermissionsPersonRequest
            {
                SubjectIdentifier = _subject,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }
    }
}