using System.Reflection;

using KSeF.Client.Core.Models.RateLimits;

namespace KSeF.Client.Tests.Models.RateLimits;

/// <summary>
/// Testy jednostkowe dla rozszerzeń klasy <see cref="EffectiveApiRateLimits"/> oraz kontraktu grup limitów.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2263", Justification = "Projekt wspiera net48, gdzie generyczne przeciążenie nie jest dostępne.")]
public class EffectiveApiRateLimitsExtensionsTests
{
    /// <summary>
    /// Sprawdza, czy <see cref="EffectiveApiRateLimitsExtensions.GetAllRateLimitValues"/> zwraca każdą grupę limitów
    /// oraz właściwy obiekt wartości. Lista grup jest pobierana z enuma, więc nowa poprawnie obsłużona
    /// grupa jest automatycznie objęta tym testem.
    /// </summary>
    [Fact]
    public void GetAll_ShouldContainEveryRateLimitGroup()
    {
        EffectiveApiRateLimits limits = CreateLimits();
        IReadOnlyDictionary<KsefClientRateLimitGroup, EffectiveApiRateLimitValues> result = limits.GetAllRateLimitValues();
        KsefClientRateLimitGroup[] expectedGroups = GetRateLimitGroups().ToArray();

        Assert.Equal(expectedGroups.Length, result.Count);
        Assert.Equal(expectedGroups.OrderBy(group => group), result.Keys.OrderBy(group => group));

        foreach (KsefClientRateLimitGroup group in expectedGroups)
        {
            EffectiveApiRateLimitValues expected = GetPropertyValue(limits, group);
            Assert.Same(expected, result[group]);
        }
    }

    /// <summary>
    /// Sprawdza, czy każda właściwość limitu ma odpowiadającą wartość w enumie grup.
    /// </summary>
    [Fact]
    public void RateLimitProperties_ShouldHaveMatchingRateLimitGroups()
    {
        string[] propertyNames = GetRateLimitProperties()
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();

        string[] groupNames = Enum.GetNames(typeof(KsefClientRateLimitGroup))
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(propertyNames, groupNames);
    }

    /// <summary>
    /// Sprawdza mapowanie każdej grupy do właściwej właściwości modelu i wszystkich jej wartości.
    /// Dane testowe są generowane na podstawie właściwości modelu, dlatego nowa grupa nie wymaga
    /// dopisywania osobnego przypadku testowego.
    /// </summary>
    [Fact]
    public void Get_ShouldReturnValuesForEveryRateLimitGroup()
    {
        EffectiveApiRateLimits limits = CreateLimits();

        foreach (KsefClientRateLimitGroup group in GetRateLimitGroups())
        {
            EffectiveApiRateLimitValues expected = GetPropertyValue(limits, group);
            EffectiveApiRateLimitValues actual = limits.GetRateLimitValues(group);

            Assert.Same(expected, actual);
            Assert.Equal(expected.PerSecond, actual.PerSecond);
            Assert.Equal(expected.PerMinute, actual.PerMinute);
            Assert.Equal(expected.PerHour, actual.PerHour);
        }
    }

    [Fact]
    public void Get_ShouldThrowWhenLimitsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            EffectiveApiRateLimitsExtensions.GetRateLimitValues(null!, KsefClientRateLimitGroup.Other));
    }

    [Fact]
    public void Get_ShouldThrowWhenGroupIsUnsupported()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateLimits().GetRateLimitValues((KsefClientRateLimitGroup)999));
    }

    [Fact]
    public void GetAll_ShouldThrowWhenLimitsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => EffectiveApiRateLimitsExtensions.GetAllRateLimitValues(null!));
    }

    /// <summary>
    /// Zwraca wszystkie grupy zdefiniowane w <see cref="KsefClientRateLimitGroup"/>.
    /// Użycie enuma jako źródła danych sprawia, że nowe grupy są automatycznie testowane.
    /// </summary>
    private static IEnumerable<KsefClientRateLimitGroup> GetRateLimitGroups()
    {
        return Enum.GetValues(typeof(KsefClientRateLimitGroup))
            .Cast<KsefClientRateLimitGroup>();
    }

    /// <summary>
    /// Zwraca właściwości modelu reprezentujące wartości limitów.
    /// Test kontraktowy zakłada, że każda z nich ma właściwość o takiej samej nazwie w enumie grup.
    /// </summary>
    private static IEnumerable<PropertyInfo> GetRateLimitProperties()
    {
        return typeof(EffectiveApiRateLimits)
            .GetProperties()
            .Where(property => property.PropertyType == typeof(EffectiveApiRateLimitValues));
    }

    /// <summary>
    /// Tworzy model z unikalnymi wartościami dla każdej właściwości limitu.
    /// Dzięki temu błędne mapowanie grup jest wykrywane bez ręcznego dopisywania danych testowych.
    /// </summary>
    private static EffectiveApiRateLimits CreateLimits()
    {
        EffectiveApiRateLimits limits = new();
        int seed = 1;

        foreach (PropertyInfo property in GetRateLimitProperties())
        {
            property.SetValue(limits, CreateValues(seed));
            seed++;
        }

        return limits;
    }

    /// <summary>
    /// Pobiera oczekiwaną właściwość modelu na podstawie nazwy grupy.
    /// Brak właściwości oznacza niespójność kontraktu i powoduje jednoznaczne niepowodzenie testu.
    /// </summary>
    private static EffectiveApiRateLimitValues GetPropertyValue(
        EffectiveApiRateLimits limits,
        KsefClientRateLimitGroup group)
    {
        PropertyInfo property = typeof(EffectiveApiRateLimits).GetProperty(group.ToString())
            ?? throw new InvalidOperationException($"Brak właściwości dla grupy limitów '{group}'.");

        return Assert.IsType<EffectiveApiRateLimitValues>(property.GetValue(limits));
    }

    /// <summary>
    /// Tworzy wartości limitu z różnymi wartościami dla sekundy, minuty i godziny.
    /// </summary>
    private static EffectiveApiRateLimitValues CreateValues(int seed)
    {
        return new EffectiveApiRateLimitValues
        {
            PerSecond = seed,
            PerMinute = seed + 100,
            PerHour = seed + 1000
        };
    }
}
