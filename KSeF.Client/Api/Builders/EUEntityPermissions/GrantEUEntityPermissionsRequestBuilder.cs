using KSeF.Client.Core.Models.Permissions.EUEntity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.EuEntityPermissions;

/// <summary>
/// Buduje żądanie nadania uprawnień podmiotowi z UE w KSeF.
/// </summary>
public static class GrantEuEntityPermissionsRequestBuilder
{
    /// <summary>
    /// Rozpoczyna budowę żądania nadania uprawnień podmiotowi z UE.
    /// </summary>
    /// <returns>
    /// Interfejs pozwalający ustawić identyfikator podmiotu, któremu nadajemy uprawnienia.
    /// </returns>
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    /// <summary>
    /// Etap budowy żądania, w którym ustawiany jest podmiot z UE.
    /// </summary>
    public interface ISubjectStep
    {
        /// <summary>
        /// Ustawia identyfikator podmiotu z UE, któremu nadawane są uprawnienia.
        /// </summary>
        /// <param name="subject">
        /// Identyfikator podmiotu (np. numer VAT UE). Nie może być null
        /// i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający ustawić nazwę podmiotu.
        /// </returns>
        ISubjectNameStep WithSubject(EuEntitySubjectIdentifier subject);
    }

    /// <summary>
    /// Etap budowy żądania, w którym ustawiana jest nazwa podmiotu z UE.
    /// </summary>
    public interface ISubjectNameStep
    {
        /// <summary>
        /// Ustawia nazwę podmiotu z UE (np. nazwę firmy).
        /// </summary>
        /// <param name="subjectName">
        /// Nazwa podmiotu. Nie może być pusta ani składać się wyłącznie z białych znaków.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający ustawić kontekst uprawnień.
        /// </returns>
        IPermissionsStep WithSubjectName(string subjectName);
    }

    /// <summary>
    /// Etap budowy żądania, w którym ustawiany jest kontekst uprawnień.
    /// </summary>
    public interface IPermissionsStep
    {
        /// <summary>
        /// Ustawia kontekst uprawnień (np. relację podmiotu z UE do właściciela).
        /// </summary>
        /// <param name="subject">
        /// Identyfikator kontekstu uprawnień. Nie może być null
        /// i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający ustawić opis nadawanych uprawnień.
        /// </returns>
        IDescriptionStep WithContext(EuEntityContextIdentifier subject);
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
        /// Interfejs pozwalający ustawić dane szczegółowe i zbudować żądanie.
        /// </returns>
        IBuildStep WithDescription(string description);
    }

    /// <summary>
    /// Ostatni etap budowy żądania – ustawienie danych szczegółowych i utworzenie żądania.
    /// </summary>
    public interface IBuildStep
    {
        /// <summary>
        /// Ustawia dodatkowe dane podmiotu z UE (np. dane kontaktowe).
        /// </summary>
        /// <param name="subjectDetails">
        /// Szczegóły podmiotu z UE. Mogą być null, jeśli brak dodatkowych danych.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IBuildStep WithSubjectDetails(PermissionsEuEntitySubjectDetails subjectDetails);

        /// <summary>
        /// Ustawia dodatkowe dane o podmiocie UE, którego dotyczą uprawnienia.
        /// </summary>
        /// <param name="euEntityDetails">
        /// Szczegóły podmiotu z UE, któremu nadawane są uprawnienia.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        IBuildStep WithEuEntityDetails(PermissionsEuEntityDetails euEntityDetails);

        /// <summary>
        /// Tworzy finalne żądanie nadania uprawnień podmiotowi z UE.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="GrantPermissionsEuEntityRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        GrantPermissionsEuEntityRequest Build();
    }

    /// <inheritdoc />
    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        ISubjectNameStep,
        IPermissionsStep,
        IDescriptionStep,
        IBuildStep
    {
        private EuEntitySubjectIdentifier _subject;
        private EuEntityContextIdentifier _context;
        private string _description;
        private string _subjectName;
        private PermissionsEuEntitySubjectDetails _subjectDetails;
        private PermissionsEuEntityDetails _euEntityDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        /// <summary>
        /// Tworzy nową implementację buildera żądania nadania uprawnień podmiotowi z UE.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        /// <inheritdoc />
        public ISubjectNameStep WithSubject(EuEntitySubjectIdentifier subject)
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
        public IPermissionsStep WithSubjectName(string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                throw new ArgumentException("Wartość nie może być pusta ani zawierać wyłącznie białych znaków.", nameof(subjectName));
            }

            _subjectName = subjectName;
            return this;
        }

        /// <inheritdoc />
        public IDescriptionStep WithContext(EuEntityContextIdentifier context)
        {
            Guard.ThrowIfNull(context);
            if (!TypeValueValidator.Validate(context))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {context.Type}", nameof(context));
            }

            _context = context;
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
        public IBuildStep WithSubjectDetails(PermissionsEuEntitySubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails;
            return this;
        }

        /// <inheritdoc />
        public IBuildStep WithEuEntityDetails(PermissionsEuEntityDetails euEntityDetails)
        {
            _euEntityDetails = euEntityDetails;
            return this;
        }

        /// <inheritdoc />
        public GrantPermissionsEuEntityRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_context is null)
            {
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            if (_description is null)
            {
                throw new InvalidOperationException("Metoda WithDescription(...) musi zostać wywołana po ustawieniu uprawnień.");
            }

            return new GrantPermissionsEuEntityRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
                EuEntityName = _subjectName,
                SubjectDetails = _subjectDetails,
                EuEntityDetails = _euEntityDetails
            };
        }
    }
}