#nullable enable
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KSeF.Client.Tests.Utils;

public static partial class MiscellaneousUtils
{
    private static readonly Random Random = new();

    // Wagi dla pozycji 1..9
    private static readonly int[] Weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

    /// <summary>
    /// Generuje losowy poprawny NIP (10 cyfr).
    /// Walidacja: [1-9]((\d[1-9])|([1-9]\d))\d{7} + poprawna suma kontrolna.
    /// prefixTwoNumbers: "" | "X" | "XY"
    /// </summary>
    public static string GetRandomNip(string prefixTwoNumbers = "")
    {
#if NETFRAMEWORK
        Random rng = Random;
#else
        Random rng = Random.Shared;
#endif

        while (true) // losuj aż checksum != 10 i regex-constraint spełniony
        {
            int first, second;

            if (prefixTwoNumbers.Length == 1)
            {
                first = ParseDigit(prefixTwoNumbers[0]);
                if (first == 0)
                {
                    throw new ArgumentException("Wartość musi być większa od 0");
                }

                second = rng.Next(0, 10);
            }
            else if (prefixTwoNumbers.Length == 2)
            {
                first = ParseDigit(prefixTwoNumbers[0]);
                second = ParseDigit(prefixTwoNumbers[1]);
                if (first == 0)
                {
                    throw new ArgumentException("Pierwsza cyfra musi być > 0");
                }
            }
            else if (prefixTwoNumbers.Length == 0)
            {
                first = rng.Next(1, 10);   // [1-9]
                second = rng.Next(0, 10);  // [0-9]
            }
            else
            {
                throw new ArgumentException("Prefiks musi mieć długość 0, 1 lub 2 cyfr.");
            }

            int third = rng.Next(0, 10);
            // regex wymaga, żeby para (druga, trzecia) nie była "00"
            if (second == 0 && third == 0)
            {
                third = rng.Next(1, 10);
            }

            // zbuduj pierwsze 9 cyfr (po 3-cyfrowym prefiksie jeszcze 6 losowych)
            int[] digits = new int[10];
            digits[0] = first;
            digits[1] = second;
            digits[2] = third;

            for (int i = 3; i < 9; i++)
            {
                digits[i] = rng.Next(0, 10);
            }

            // policz cyfrę kontrolną
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += digits[i] * Weights[i];
            }

            int check = sum % 11;

            // jeśli 10 => niepoprawny NIP (według specyfikacji), losuj ponownie
            // inaczej dopisz jako 10. cyfrę i zwróć wynik
            if (check != 10)
            {
                digits[9] = check;
                return string.Concat(digits.Select(d => d.ToString(CultureInfo.InvariantCulture)));
            }            
        }
    }

    /// <summary>Walidacja NIP (10 cyfr + checksum mod 11).</summary>
    public static bool IsValidNip(string nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
        {
            return false;
        }

        string digitsOnly = new([.. nip.Where(char.IsDigit)]);
        if (digitsOnly.Length != 10)
        {
            return false;
        }

        int[] digits = [.. digitsOnly.Select(ch => ch - '0')];
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += digits[i] * Weights[i];
        }

        int check = sum % 11;
        return check != 10 && check == digits[9];
    }

    private static int ParseDigit(char c)
    {
        if (c < '0' || c > '9')
        {
            throw new ArgumentException("Prefiks musi zawierać cyfry 0-9.");
        }

        return c - '0';
    }

    /// <summary>
    /// Generuje losowy poprawny PESEL (11 cyfr, z częścią daty w formacie RRMMDD).
    /// Wyrażenie regularne używane do walidacji:
    /// ^\d{2}(?:0[1-9]|1[0-2]|2[1-9]|3[0-2]|4[1-9]|5[0-2]|6[1-9]|7[0-2]|8[1-9]|9[0-2])\d{7}$
    /// </summary>
    public static string GetRandomPesel()
    {
        int year = Random.Next(1900, 2299); // zakres zgodny z systemem PESEL
        int month = Random.Next(1, 12);
        int day = Random.Next(1, DateTime.DaysInMonth(year, month));

        // zakodowanie stulecia w miesiącu
        int encodedMonth = month + (year / 100 - 19) * 20;

        string datePart = $"{year % 100:D2}{encodedMonth:D2}{day:D2}";
        string serial = Random.Next(1000, 9999).ToString("D4",CultureInfo.InvariantCulture);

        // suma kontrolna
        string basePesel = datePart + serial;
        int[] weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
        int sum = basePesel.Select((c, i) => (c - '0') * weights[i]).Sum();
        int control = (10 - (sum % 10)) % 10;

        return basePesel + control;
    }

    /// <summary>
    /// Generuje losowy poprawny identyfikator NIP-VAT UE.
    /// Wyrażenie regularne używane do walidacji:
    /// ^(?&lt;nip&gt;[1-9](\d[1-9]|[1-9]\d)\d{7})-(?&lt;vat&gt;((AT)(U\d{8})|(BE)([01]{1}\d{9})|(BG)(\d{9,10})|(CY)(\d{8}[A-Z])|(CZ)(\d{8,10})|(DE)(\d{9})|(DK)(\d{8})|(EE)(\d{9})|(EL)(\d{9})|(ES)([A-Z]\d{8}|\d{8}[A-Z]|[A-Z]\d{7}[A-Z])|(FI)(\d{8})|(FR)[A-Z0-9]{2}\d{9}|(HR)(\d{11})|(HU)(\d{8})|(IE)(\d{7}[A-Z]{2}|\d[A-Z0-9+*]\d{5}[A-Z])|(IT)(\d{11})|(LT)(\d{9}|\d{12})|(LU)(\d{8})|(LV)(\d{11})|(MT)(\d{8})|(NL)([A-Z0-9+*]{12})|(PT)(\d{9})|(RO)(\d{2,10})|(SE)(\d{12})|(SI)(\d{8})|(SK)(\d{10})|(XI)((\d{9}|(\d{12}))|(GD|HA)(\d{3}))))$
    /// </summary>
    public static string GetRandomNipVatEU(CountryCode countryCode = CountryCode.ES)
    {
        string nip = GetRandomNip();

        string vatPart = countryCode switch
        {
            CountryCode.AT => "U" + Random.Next(10000000, 99999999),
            CountryCode.BE => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            CountryCode.BG => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.CY => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            CountryCode.CZ => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.DE => $"{Random.Next(100000000, 999999999)}",
            CountryCode.DK => $"{Random.Next(10000000, 99999999)}",
            CountryCode.EE => $"{Random.Next(100000000, 999999999)}",
            CountryCode.EL => $"{Random.Next(100000000, 999999999)}",
            CountryCode.ES => GenerateEsVat(),
            CountryCode.FI => $"{Random.Next(10000000, 99999999)}",
            CountryCode.FR => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            CountryCode.HR => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.HU => $"{Random.Next(10000000, 99999999)}",
            CountryCode.IE => GenerateIeVat(),
            CountryCode.IT => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.LT => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            CountryCode.LU => $"{Random.Next(10000000, 99999999)}",
            CountryCode.LV => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.MT => $"{Random.Next(10000000, 99999999)}",
            CountryCode.NL => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            CountryCode.PT => $"{Random.Next(100000000, 999999999)}",
            CountryCode.RO => $"{Random.NextInt64(10, 9999999999)}",
            CountryCode.SE => $"{Random.NextInt64(100000000000, 999999999999)}",
            CountryCode.SI => $"{Random.Next(10000000, 99999999)}",
            CountryCode.SK => $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.XI => GenerateXiVat(),
            CountryCode.PL => GetRandomNip(),
            _ => throw new ArgumentException($"Niewspierany kod kraju {countryCode}")
        };

        return $"{nip}-{countryCode}{vatPart}";
    }

    /// <summary>
    /// Zwraca VAT UE (prefiks kraju + numer) na podstawie lokalnego identyfikatora.
    /// </summary>
    /// <remarks>
    /// Zasady:
    /// - jeśli wejście MA już prefiks (2 litery, np. PL/DE) → zwracamy znormalizowane,
    /// - rozpoznajemy jednoznaczne wzorce z literami: AT (U########), NL (#########B##), CY (########L),
    ///   IE, FR, ES; Polska: poprawny NIP (10 cyfr z sumą) → PL+NIP,
    /// - 12 cyfr (LT/SE) traktujemy domyślnie jako SE (zob. <see cref="GuessSeOrLtFor12Digits"/>),
    /// - dla czysto cyfrowych, niejednoznacznych długości (8/9/10/11) rzucamy wyjątek (bez zgadywania).
    /// </remarks>
    public static string ToVatEuFromDomestic(string localId)
    {
        if (string.IsNullOrWhiteSpace(localId))
        {
            throw new ArgumentException("Wartość nie może być pusta ani składać się wyłącznie z białych znaków.", nameof(localId));
        }

        string raw = Normalize(localId);

        // 0) prefiks kraju już obecny
        if (raw.Length >= 3 && char.IsLetter(raw[0]) && char.IsLetter(raw[1]))
        {
            return raw;
        }

        // 1) wzorce jednoznaczne z literami
        if (AtPattern().IsMatch(raw))                        // AT: U########
        {
            return "AT" + raw;
        }

        if (NlPattern().IsMatch(raw))                        // NL: #########B##
        {
            return "NL" + raw.ToUpperInvariant();
        }

        if (CyPattern().IsMatch(raw))                        // CY: ########L
        {
            return "CY" + raw.ToUpperInvariant();
        }

        if (IePattern().IsMatch(raw))                        // IE
        {
            return "IE" + raw.ToUpperInvariant();
        }

        if (FrPattern().IsMatch(raw))                        // FR
        {
            return "FR" + raw.ToUpperInvariant();
        }

        if (EsPattern().IsMatch(raw))                        // ES
        {
            return "ES" + raw.ToUpperInvariant();
        }

        // 2) unikalne długości (12 cyfr – LT/SE)
        if (Digits12().IsMatch(raw))
        {
            return GuessSeOrLtFor12Digits(raw);
        }

        // 3) Polska (10 cyfr + suma kontrolna) – UŻYJ istniejącej walidacji
        if (IsValidNip(raw))
        {
            return "PL" + raw;
        }

        // 4) długości niejednoznaczne – nie zgadujemy
        if (Digits8().IsMatch(raw))
        {
            throw new ArgumentException("Nie można jednoznacznie określić kraju dla 8 cyfr (DK/FI/HU/LU/MT). Podaj prefiks kraju.");
        }

        if (Digits9().IsMatch(raw))
        {
            throw new ArgumentException("Nie można jednoznacznie określić kraju dla 9 cyfr (DE/PT/EE/EL/LT). Podaj prefiks kraju.");
        }

        if (Digits10().IsMatch(raw))
        {
            throw new ArgumentException("Nie można jednoznacznie określić kraju dla 10 cyfr (BE/CZ/SK/… lub PL z błędną sumą). Podaj prefiks kraju.");
        }

        if (Digits11().IsMatch(raw))
        {
            throw new ArgumentException("Nie można jednoznacznie określić kraju dla 11 cyfr (IT/HR/LV). Podaj prefiks kraju.");
        }

        // 5) brak dopasowania
        throw new ArgumentException("Nieznany lub nieobsługiwany format lokalnego identyfikatora VAT. Podaj prefiks kraju.");
    }


    private static string Normalize(string s) =>
        new(s.Trim()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace(".", "")
                    .ToUpperInvariant()
                    .ToCharArray());

    private static string GuessSeOrLtFor12Digits(string digits12)
    {
        // Status quo: bez dodatkowych reguł kontrolnych nie da się pewnie odróżnić SE (12) od LT (12).
        // Przyjmujemy: 12 cyfr -> SE (najbardziej powszechny pattern), chyba że użytkownik poda LT jawnie.
        // Jeżeli w Twoim przypadku 12 cyfr częściej oznacza LT – odwróć preferencję.
        return "SE" + digits12;
    }

    /// <summary>
    /// Generuje losowy poprawny identyfikator NIP-VAT UE.
    /// Wyrażenie regularne używane do walidacji:
    /// ^(?&lt;nip&gt;[1-9](\d[1-9]|[1-9]\d)\d{7})-(?&lt;vat&gt;((AT)(U\d{8})|(BE)([01]{1}\d{9})|(BG)(\d{9,10})|(CY)(\d{8}[A-Z])|(CZ)(\d{8,10})|(DE)(\d{9})|(DK)(\d{8})|(EE)(\d{9})|(EL)(\d{9})|(ES)([A-Z]\d{8}|\d{8}[A-Z]|[A-Z]\d{7}[A-Z])|(FI)(\d{8})|(FR)[A-Z0-9]{2}\d{9}|(HR)(\d{11})|(HU)(\d{8})|(IE)(\d{7}[A-Z]{2}|\d[A-Z0-9+*]\d{5}[A-Z])|(IT)(\d{11})|(LT)(\d{9}|\d{12})|(LU)(\d{8})|(LV)(\d{11})|(MT)(\d{8})|(NL)([A-Z0-9+*]{12})|(PT)(\d{9})|(RO)(\d{2,10})|(SE)(\d{12})|(SI)(\d{8})|(SK)(\d{10})|(XI)((\d{9}|(\d{12}))|(GD|HA)(\d{3}))))$
    /// </summary>
    public static string GetRandomNipVatEU(string nip, CountryCode countryCode = CountryCode.ES)
    {
        string vatPart = countryCode switch
        {
            CountryCode.AT => "U" + Random.Next(10000000, 99999999),
            CountryCode.BE => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            CountryCode.BG => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.CY => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            CountryCode.CZ => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.DE => $"{Random.Next(100000000, 999999999)}",
            CountryCode.DK => $"{Random.Next(10000000, 99999999)}",
            CountryCode.EE => $"{Random.Next(100000000, 999999999)}",
            CountryCode.EL => $"{Random.Next(100000000, 999999999)}",
            CountryCode.ES => GenerateEsVat(),
            CountryCode.FI => $"{Random.Next(10000000, 99999999)}",
            CountryCode.FR => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            CountryCode.HR => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.HU => $"{Random.Next(10000000, 99999999)}",
            CountryCode.IE => GenerateIeVat(),
            CountryCode.IT => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.LT => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            CountryCode.LU => $"{Random.Next(10000000, 99999999)}",
            CountryCode.LV => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.MT => $"{Random.Next(10000000, 99999999)}",
            CountryCode.NL => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            CountryCode.PT => $"{Random.Next(100000000, 999999999)}",
            CountryCode.RO => $"{Random.NextInt64(10, 9999999999)}",
            CountryCode.SE => $"{Random.NextInt64(100000000000, 999999999999)}",
            CountryCode.SI => $"{Random.Next(10000000, 99999999)}",
            CountryCode.SK => $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.XI => GenerateXiVat(),
            CountryCode.PL => GetRandomNip(),
            _ => throw new ArgumentException($"Niewspierany kod kraju {countryCode}")
        };

        return $"{nip}-{countryCode}{vatPart}";
    }

    /// <summary>
    /// Generuje losowy, poprawny identyfikator VAT UE.
    /// </summary>
    public static string GetRandomVatEU(CountryCode countryCode = CountryCode.ES)
    {
        string vatPart = countryCode switch
        {
            CountryCode.AT => "U" + Random.Next(10000000, 99999999),
            CountryCode.BE => $"{Random.Next(0, 2)}{Random.Next(100000000, 999999999)}",
            CountryCode.BG => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.CY => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            CountryCode.CZ => Random.Next(0, 2) == 0
                ? $"{Random.Next(10000000, 99999999)}"
                : $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.DE => $"{Random.Next(100000000, 999999999)}",
            CountryCode.DK => $"{Random.Next(10000000, 99999999)}",
            CountryCode.EE => $"{Random.Next(100000000, 999999999)}",
            CountryCode.EL => $"{Random.Next(100000000, 999999999)}",
            CountryCode.ES => GenerateEsVat(),
            CountryCode.FI => $"{Random.Next(10000000, 99999999)}",
            CountryCode.FR => $"{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 2)}{Random.Next(100000000, 999999999)}",
            CountryCode.HR => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.HU => $"{Random.Next(10000000, 99999999)}",
            CountryCode.IE => GenerateIeVat(),
            CountryCode.IT => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.LT => Random.Next(0, 2) == 0
                ? $"{Random.Next(100000000, 999999999)}"
                : $"{Random.NextInt64(100000000000, 999999999999)}",
            CountryCode.LU => $"{Random.Next(10000000, 99999999)}",
            CountryCode.LV => $"{Random.NextInt64(10000000000, 99999999999)}",
            CountryCode.MT => $"{Random.Next(10000000, 99999999)}",
            CountryCode.NL => RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 12),
            CountryCode.PT => $"{Random.Next(100000000, 999999999)}",
            CountryCode.RO => $"{Random.NextInt64(10, 9999999999)}",
            CountryCode.SE => $"{Random.NextInt64(100000000000, 999999999999)}",
            CountryCode.SI => $"{Random.Next(10000000, 99999999)}",
            CountryCode.SK => $"{Random.NextInt64(1000000000, 9999999999)}",
            CountryCode.XI => GenerateXiVat(),
            CountryCode.PL => GetRandomNip(),
            _ => throw new ArgumentException($"Niewspierany kod kraju {countryCode}")
        };

        return $"{countryCode}{vatPart}";
    }

    public static string GetNipVatEU(string nip, string vatue)
    {
        return $"{nip}-{vatue}";
    }

    // --- metody pomocnicze ---
    private static string RandomString(string chars, int length) =>
        new([.. Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)])]);

    private static string GenerateEsVat()
    {
        int choice = Random.Next(3);
        return choice switch
        {
            0 => $"{(char)('A' + Random.Next(26))}{Random.Next(10000000, 99999999)}",
            1 => $"{Random.Next(10000000, 99999999)}{(char)('A' + Random.Next(26))}",
            _ => $"{(char)('A' + Random.Next(26))}{Random.Next(1000000, 9999997)}{(char)('A' + Random.Next(26))}"
        };
    }

    private static string GenerateIeVat()
    {
        return Random.Next(2) == 0
            ? $"{Random.Next(1000000, 9999999)}{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 2)}"
            : $"{Random.Next(0, 9)}{RandomString("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+*", 1)}{Random.Next(10000, 99999)}{(char)('A' + Random.Next(26))}";
    }

    private static string GenerateXiVat()
    {
        int choice = Random.Next(3);
        return choice switch
        {
            0 => $"{Random.Next(100000000, 999999999)}",
            1 => $"{Random.NextInt64(100000000000, 999999999999)}",
            _ => $"{(Random.Next(2) == 0 ? "GD" : "HA")}{Random.Next(100, 999)}"
        };
    }

    /// <summary>
    /// Generuje pełny identyfikator wewnętrzny z poprawną sumą kontrolną.
    /// Format: {NIP}-{4 cyfry unikalne}{cyfra kontrolna}
    /// </summary>
    /// <param name="nip">10-cyfrowy NIP podmiotu</param>
    /// <param name="uniqueDigits">4 cyfry unikalnie identyfikujących jednostkę (opcjonalne, domyślnie losowe)</param>
    /// <returns>Pełny identyfikator wewnętrzny z sumą kontrolną</returns>
    public static string GenerateInternalIdentifier(string? nip = null, string? uniqueDigits = null)
    {
        if (string.IsNullOrWhiteSpace(nip))
        {
            nip = GetRandomNip();
        }

        if (string.IsNullOrWhiteSpace(nip) || nip.Length != 10 || !nip.All(char.IsDigit))
        {
			throw new ArgumentException("NIP musi składać się z dokładnie 10 cyfr", nameof(nip));
		}

		// Jeśli nie podano cyfr unikalnych, wygeneruj losowe 4 cyfry
		if (string.IsNullOrWhiteSpace(uniqueDigits))
        {
            Random random = new();
            uniqueDigits = random.Next(1000, 9999).ToString();
        }

        if (uniqueDigits.Length != 4 || !uniqueDigits.All(char.IsDigit))
        {
			throw new ArgumentException("Unikalne cyfry muszą składać się z dokładnie 4 cyfr", nameof(uniqueDigits));
		}

		string baseIdentifier = $"{nip}-{uniqueDigits}";
        int checksum = CalculateChecksum(baseIdentifier);

        return $"{baseIdentifier}{checksum}";
    }

	/// <summary>
	/// Oblicza cyfrę kontrolną dla identyfikatora wewnętrznego.
	/// Algorytm: naprzemienne wagi (1×, 3×, 1×, 3×, ...), suma % 10
	/// </summary>
	/// <param name="identifierWithoutChecksum">Identyfikator bez cyfry kontrolnej (format: NIP-4cyfry)</param>
	/// <returns>Cyfra kontrolna (0-9)</returns>
	public static int CalculateChecksum(string identifierWithoutChecksum)
	{
		// Usuń myślnik i zostaw tylko cyfry
		string digits = identifierWithoutChecksum.Replace("-", "");

		if (digits.Length != 14 || !digits.All(char.IsDigit))
        {
			throw new ArgumentException(
	            "Identyfikator bez sumy kontrolnej musi zawierać dokładnie 14 cyfr (10 NIP + 4 unikalne)",
	            nameof(identifierWithoutChecksum));
		}

		int sum = digits
			.Select((c, i) => int.Parse(c.ToString()) * ((i % 2 == 0) ? 1 : 3))
			.Sum();

		return sum % 10;
	}

	/// <summary>
	/// Weryfikuje poprawność pełnego identyfikatora wewnętrznego.
	/// </summary>
	/// <param name="fullIdentifier">Pełny identyfikator z sumą kontrolną (format: NIP-XXXXZ)</param>
	/// <returns>True jeśli suma kontrolna jest poprawna</returns>
	public static bool ValidateInternalIdentifier(string fullIdentifier)
	{
		if (string.IsNullOrWhiteSpace(fullIdentifier))
        {
			return false;
		}

		// Format: XXXXXXXXXX-XXXXZ gdzie Z to cyfra kontrolna
		string digitsOnly = fullIdentifier.Replace("-", "");

		if (digitsOnly.Length != 15 || !digitsOnly.All(char.IsDigit))
        {
			return false;
		}

		string withoutChecksum = fullIdentifier[..^1]; // Wszystko oprócz ostatniej cyfry
		int expectedChecksum = CalculateChecksum(withoutChecksum);
		int actualChecksum = int.Parse(digitsOnly[^1].ToString()); // Ostatnia cyfra

		return expectedChecksum == actualChecksum;
	}

	/// <summary>
	/// Generuje polski IBAN (PL) z prawidłowymi cyframi kontrolnymi (ISO 13616 / mod 97)
	/// na podstawie 26 cyfr NRB: 8 cyfr bank/oddział + 16 cyfr rachunku (losowo, jeśli null).
	/// </summary>
	/// <param name="bankBranch8">8 cyfr bank/oddział; null → losowe.</param>
	/// <param name="account16">16 cyfr rachunku; null → losowe.</param>
	/// <returns>IBAN w formacie: PLkkBBBBBBBBAAAAAAAAAAAAAAAA.</returns>    
	public static string GeneratePolishIban(string bankBranch8 = "", string account16 = "")
    {
        // Zbuduj 26-cyfrowy BBAN (NRB): 8 cyfr bank/oddział + 16 cyfr numeru rachunku
        string bban = (!string.IsNullOrEmpty(bankBranch8) ? bankBranch8 : RandomDigits(8)) + (!string.IsNullOrEmpty(account16) ? account16 : RandomDigits(16));

        // IBAN check digits: policz dla "PL00" + BBAN → przenieś "PL00" na koniec → mod 97
        string rearranged = bban + LettersToDigits("PL") + "00";
        int mod = Mod97(rearranged);
        int check = 98 - mod;
        string checkStr = check < 10 ? "0" + check : check.ToString(CultureInfo.InvariantCulture);

        return $"PL{checkStr}{bban}";

        static string RandomDigits(int len)
        {
            StringBuilder sb = new(len);
#if NETFRAMEWORK
            byte[] buf = new byte[1];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            for (int i = 0; i < len; i++)
            {
                rng.GetBytes(buf);
                sb.Append((buf[0] % 10).ToString(CultureInfo.InvariantCulture));
            }
#else
            Span<byte> buf = stackalloc byte[1];
            for (int i = 0; i < len; i++)
            {
                // 0–9 równomiernie; prosty mapping z losowego bajtu
                RandomNumberGenerator.Fill(buf);
                sb.Append((buf[0] % 10).ToString(CultureInfo.InvariantCulture));
            }
#endif
            return sb.ToString();
        }

        static string LettersToDigits(string s)
        {
            StringBuilder sb = new(s.Length * 2);
            foreach (char ch in s.ToUpperInvariant())
            {
                int val = ch - 'A' + 10; // A=10 ... Z=35
                sb.Append(val.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        static int Mod97(string numeric)
        {
            // Liczymy iteracyjnie na kawałkach (unikamy big-int)
            int chunkSize = 9;
            int start = 0;
            int rem = 0;
            while (start < numeric.Length)
            {
                int take = Math.Min(chunkSize, numeric.Length - start);
#if NETFRAMEWORK
                string part = string.Concat(rem.ToString(CultureInfo.InvariantCulture), numeric.Substring(start, take));
#else
                string part = string.Concat(rem.ToString(CultureInfo.InvariantCulture), numeric.AsSpan(start, take));
#endif
                rem = (int)(ulong.Parse(part, CultureInfo.InvariantCulture) % 97UL);
                start += take;
            }
            return rem;
        }
    }

#if NETFRAMEWORK
    // net48: [GeneratedRegex] source generator niedostępny — użyj compiled Regex.
    private static Regex MyRegex() => new(@"^U\d{8}$", RegexOptions.Compiled);
    private static Regex AtPattern() => new(@"^U\d{8}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex NlPattern() => new(@"^\d{9}B\d{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex CyPattern() => new(@"^\d{8}[A-Z]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex IePattern() => new(@"^(\d{7}[A-Z]{1,2}|\d[A-Z]\d{5}[A-Z])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex FrPattern() => new(@"^[0-9A-HJ-NP-Z]{2}\d{9}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex EsPattern() => new(@"^[A-Z]\d{7}[A-Z0-9]$|^\d{8}[A-Z]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex Digits12() => new(@"^\d{12}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex Digits8() => new(@"^\d{8}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex Digits9() => new(@"^\d{9}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex Digits10() => new(@"^\d{10}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static Regex Digits11() => new(@"^\d{11}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
#else
    [GeneratedRegex(@"^U\d{8}$")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"^U\d{8}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex AtPattern();

    [GeneratedRegex(@"^\d{9}B\d{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex NlPattern();

    [GeneratedRegex(@"^\d{8}[A-Z]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex CyPattern();

    [GeneratedRegex(@"^(\d{7}[A-Z]{1,2}|\d[A-Z]\d{5}[A-Z])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex IePattern();

    [GeneratedRegex(@"^[0-9A-HJ-NP-Z]{2}\d{9}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex FrPattern();

    [GeneratedRegex(@"^[A-Z]\d{7}[A-Z0-9]$|^\d{8}[A-Z]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex EsPattern();

    [GeneratedRegex(@"^\d{12}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex Digits12();

    [GeneratedRegex(@"^\d{8}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex Digits8();

    [GeneratedRegex(@"^\d{9}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex Digits9();

    [GeneratedRegex(@"^\d{10}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex Digits10();

    [GeneratedRegex(@"^\d{11}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex Digits11();
#endif

}