using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.SubEntityPermissions
{
    /// <summary>
    /// Buduje żądanie nadania uprawnień jednostce podrzędnej (subunit) w KSeF.
    /// </summary>
    public static class GrantSubunitPermissionsRequestBuilder
    {
        /// <summary>
        /// Rozpoczyna budowę żądania nadania uprawnień jednostce podrzędnej.
        /// </summary>
        /// <returns>
        /// Interfejs pozwalający ustawić podmiot, któremu nadawane są uprawnienia.
        /// </returns>
        public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

        /// <summary>
        /// Etap budowy żądania, w którym definiowany jest podmiot, któremu nadawane są uprawnienia.
        /// </summary>
        public interface ISubjectStep
        {
            /// <summary>
            /// Określa identyfikator podmiotu, któremu nadawane są uprawnienia jednostki podrzędnej.
            /// </summary>
            /// <param name="subject">
            /// Identyfikator podmiotu (np. NIP lub inny identyfikator). Nie może być null
            /// i musi przejść walidację <see cref="TypeValueValidator"/>.
            /// </param>
            /// <returns>
            /// Interfejs pozwalający ustawić kontekst jednostki podrzędnej.
            /// </returns>
            IContextStep WithSubject(SubunitSubjectIdentifier subject);
        }

        /// <summary>
        /// Etap budowy żądania, w którym ustawiany jest kontekst jednostki podrzędnej.
        /// </summary>
        public interface IContextStep
        {
            /// <summary>
            /// Ustawia kontekst jednostki podrzędnej (np. identyfikator wewnętrzny lub inny rodzaj powiązania).
            /// </summary>
            /// <param name="context">
            /// Identyfikator kontekstu jednostki podrzędnej. Nie może być null
            /// i musi przejść walidację <see cref="TypeValueValidator"/>.
            /// </param>
            /// <returns>
            /// Interfejs pozwalający ustawić opis, nazwę jednostki i dane szczegółowe.
            /// </returns>
            IOptionalStep WithContext(SubunitContextIdentifier context);
        }

        /// <summary>
        /// Etap budowy żądania, w którym ustawiana jest nazwa jednostki podrzędnej.
        /// </summary>
        public interface ISubunitNameStep
        {
            /// <summary>
            /// Ustawia nazwę jednostki podrzędnej.
            /// </summary>
            /// <param name="subunitName">
            /// Nazwa jednostki podrzędnej. Wymagana i ograniczona długością,
            /// jeśli kontekst wymaga nazwy (np. dla typu <see cref="SubunitContextIdentifierType.InternalId"/>).
            /// </param>
            /// <returns>
            /// Interfejs pozwalający ustawić opis i dane szczegółowe oraz zbudować żądanie.
            /// </returns>
            IOptionalStep WithSubunitName(string subunitName);
        }

        /// <summary>
        /// Etap budowy żądania, w którym można ustawić opis, nazwę jednostki i dane szczegółowe.
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
            /// Ustawia nazwę jednostki podrzędnej.
            /// </summary>
            /// <param name="subunitName">
            /// Nazwa jednostki podrzędnej. Dla typu
            /// <see cref="SubunitContextIdentifierType.InternalId"/> nie może być pusta
            /// i musi mieścić się w zakresie długości
            /// <see cref="ValidValues.SubunitNameMinLength"/>–<see cref="ValidValues.SubunitNameMaxLength"/>.
            /// </param>
            /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
            IOptionalStep WithSubunitName(string subunitName);

            /// <summary>
            /// Ustawia dodatkowe dane podmiotu, któremu nadawane są uprawnienia jednostki podrzędnej.
            /// </summary>
            /// <param name="subjectDetails">
            /// Szczegóły podmiotu (np. dane kontaktowe). Nie mogą być null.
            /// </param>
            /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
            IOptionalStep WithSubjectDetails(SubunitSubjectDetails subjectDetails);

            /// <summary>
            /// Tworzy finalne żądanie nadania uprawnień jednostce podrzędnej.
            /// </summary>
            /// <returns>
            /// Obiekt <see cref="GrantPermissionsSubunitRequest"/> gotowy do wysłania do KSeF.
            /// </returns>
            GrantPermissionsSubunitRequest Build();
        }

        /// <inheritdoc />
        private sealed class GrantPermissionsRequestBuilderImpl :
            ISubjectStep,
            IContextStep,
            IOptionalStep
        {
            private SubunitSubjectIdentifier _subject;
            private SubunitContextIdentifier _context;
            private string _description;
            private string _subunitName;
            private SubunitSubjectDetails _subjectDetails;

            private GrantPermissionsRequestBuilderImpl() { }

            /// <summary>
            /// Tworzy nową implementację buildera żądania nadania uprawnień jednostce podrzędnej.
            /// </summary>
            /// <returns>Interfejs startowy buildera.</returns>
            internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

            /// <inheritdoc />
            public IContextStep WithSubject(SubunitSubjectIdentifier subject)
            {
                Guard.ThrowIfNull(subject);
                if (!TypeValueValidator.Validate(subject))
                {
                    throw new ArgumentException($"Nieprawidłowa wartość dla typu {subject.Type}", nameof(subject));
                }

                _subject = subject ?? throw new ArgumentNullException(nameof(subject));
                return this;
            }

            /// <inheritdoc />
            public IOptionalStep WithContext(SubunitContextIdentifier context)
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
            public IOptionalStep WithSubunitName(string subunitName)
            {
                if (_context.Type == SubunitContextIdentifierType.InternalId)
                {
                    if (string.IsNullOrWhiteSpace(subunitName))
                    {
                        throw new InvalidOperationException($"Dla typu {nameof(SubunitContextIdentifierType.InternalId)} parametr {nameof(subunitName)} nie może być pusty");
                    }
                    if (subunitName.Length < ValidValues.SubunitNameMinLength)
                    {
                        throw new ArgumentException($"Nazwa jednostki podrzędnej za krótka, minimalna długość: {ValidValues.SubunitNameMinLength} znaków.", nameof(subunitName));
                    }
                    if (subunitName.Length > ValidValues.SubunitNameMaxLength)
                    {
                        throw new ArgumentException($"Nazwa jednostki podrzędnej za długa, maksymalna długość: {ValidValues.SubunitNameMaxLength} znaków.", nameof(subunitName));
                    }
                }

                _subunitName = subunitName;
                return this;
            }

            /// <inheritdoc />
            public IOptionalStep WithSubjectDetails(SubunitSubjectDetails subjectDetails)
            {
                _subjectDetails = subjectDetails ?? throw new ArgumentNullException(nameof(subjectDetails));
                return this;
            }

            /// <inheritdoc />
            public GrantPermissionsSubunitRequest Build()
            {
                if (_subject is null)
                {
                    throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
                }

                if (_context is null)
                {
                    throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
                }

                if (_context.Type == SubunitContextIdentifierType.InternalId && string.IsNullOrWhiteSpace(_subunitName))
                {
                    throw new InvalidOperationException($"Dla typu {nameof(SubunitContextIdentifierType.InternalId)} metoda WithSubunitName(...) musi zostać wywołana przed Build().");
                }

                return new GrantPermissionsSubunitRequest
                {
                    SubjectIdentifier = _subject,
                    ContextIdentifier = _context,
                    Description = _description,
                    SubunitName = _subunitName,
                    SubjectDetails = _subjectDetails
                };
            }
        }
    }
}