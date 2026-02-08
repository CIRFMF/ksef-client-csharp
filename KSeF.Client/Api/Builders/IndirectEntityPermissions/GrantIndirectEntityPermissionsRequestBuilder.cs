using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.IndirectEntityPermissions;

/// <summary>
/// Buduje żądanie nadania uprawnień pośrednich podmiotowi w KSeF.
/// </summary>
public static class GrantIndirectEntityPermissionsRequestBuilder
{
    /// <summary>
    /// Rozpoczyna budowę żądania nadania uprawnień pośrednich.
    /// </summary>
    /// <returns>
    /// Interfejs pozwalający ustawić podmiot, któremu nadawane są uprawnienia.
    /// </returns>
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    /// <summary>
    /// Etap budowy żądania, w którym ustawiany jest podmiot, któremu nadawane są uprawnienia pośrednie.
    /// </summary>
    public interface ISubjectStep
    {
        /// <summary>
        /// Ustawia identyfikator podmiotu, któremu mają zostać nadane uprawnienia pośrednie.
        /// </summary>
        /// <param name="subject">
        /// Identyfikator podmiotu. Nie może być null i musi przejść walidację
        /// <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający ustawić kontekst uprawnień.
        /// </returns>
        IContextStep WithSubject(IndirectEntitySubjectIdentifier subject);
    }

    /// <summary>
    /// Etap budowy żądania, w którym ustawiany jest kontekst uprawnień pośrednich.
    /// </summary>
    public interface IContextStep
    {
        /// <summary>
        /// Ustawia kontekst uprawnień pośrednich (podmiot, którego dotyczą uprawnienia).
        /// </summary>
        /// <param name="context">
        /// Identyfikator podmiotu, w którego sprawach będzie działał podmiot z uprawnieniami pośrednimi.
        /// Nie może być null i musi przejść walidację <see cref="TypeValueValidator"/>.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający określić listę nadawanych uprawnień.
        /// </returns>
        IPermissionsStep WithContext(IndirectEntityTargetIdentifier context);
    }

    /// <summary>
    /// Etap budowy żądania, w którym określane są uprawnienia nadawane podmiotowi.
    /// </summary>
    public interface IPermissionsStep
    {
        /// <summary>
        /// Ustawia listę uprawnień nadawanych podmiotowi pośredniemu.
        /// </summary>
        /// <param name="permissions">
        /// Co najmniej jedno uprawnienie, które ma zostać nadane.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający dodać opis, dane szczegółowe i zbudować żądanie.
        /// </returns>
        IOptionalStep WithPermissions(params IndirectEntityStandardPermissionType[] permissions);
    }

    /// <summary>
    /// Etap budowy żądania, w którym można dodać opis i dane szczegółowe podmiotu.
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
        /// <returns>
        /// Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.
        /// </returns>
        IOptionalStep WithDescription(string description);

        /// <summary>
        /// Ustawia dodatkowe dane podmiotu, któremu nadawane są uprawnienia pośrednie.
        /// </summary>
        /// <param name="subjectDetails">
        /// Szczegóły podmiotu (na przykład dane kontaktowe). Nie mogą być null.
        /// </param>
        /// <returns>
        /// Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.
        /// </returns>
        IOptionalStep WithSubjectDetails(PermissionsIndirectEntitySubjectDetails subjectDetails);

        /// <summary>
        /// Tworzy finalne żądanie nadania uprawnień pośrednich.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="GrantPermissionsIndirectEntityRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        GrantPermissionsIndirectEntityRequest Build();
    }

    /// <inheritdoc />
    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IContextStep,
        IPermissionsStep,
        IOptionalStep
    {
        private IndirectEntitySubjectIdentifier _subject;
        private ICollection<IndirectEntityStandardPermissionType> _permissions;
        private string _description;
        private IndirectEntityTargetIdentifier _context;
        private PermissionsIndirectEntitySubjectDetails _subjectDetails;

        private GrantPermissionsRequestBuilderImpl() { }

        /// <summary>
        /// Tworzy nową implementację buildera żądania nadania uprawnień pośrednich.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        /// <inheritdoc />
        public IContextStep WithSubject(IndirectEntitySubjectIdentifier subject)
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
        public IOptionalStep WithPermissions(params IndirectEntityStandardPermissionType[] permissions)
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
        public IOptionalStep WithSubjectDetails(PermissionsIndirectEntitySubjectDetails subjectDetails)
        {
            _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
            return this;
        }

        /// <inheritdoc />
        public GrantPermissionsIndirectEntityRequest Build()
        {
            if (_subject is null)
            {
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            }
            if (_context is null)
            {
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            }
            if (_permissions is null)
            {
                throw new InvalidOperationException("Metoda WithPermissions(...) musi zostać wywołana po ustawieniu kontekstu.");
            }

            return new GrantPermissionsIndirectEntityRequest
            {
                SubjectIdentifier = _subject,
                TargetIdentifier = _context,
                Permissions = _permissions,
                Description = _description,
                SubjectDetails = _subjectDetails,
            };
        }

        /// <inheritdoc />
        public IPermissionsStep WithContext(IndirectEntityTargetIdentifier context)
        {
            Guard.ThrowIfNull(context);
            if (!TypeValueValidator.Validate(context))
            {
                throw new ArgumentException($"Nieprawidłowa wartość dla typu {context.Type}", nameof(context));
            }

            _context = context;
            return this;
        }
    }
}