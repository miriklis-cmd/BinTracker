namespace BinTracker.Core;

/// <summary>Provider-neutral validation shared by application entry points.</summary>
public static class ProviderNeutralValidation
{
    public static void RequireDefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(parameterName, value, $"{typeof(TEnum).Name} is invalid.");
    }

    public static void RequireDefined<TEnum>(TEnum? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (value.HasValue)
            RequireDefined(value.Value, parameterName);
    }
}

/// <summary>Authoritative application maxima for customer and container master data.</summary>
public static class MasterDataLimits
{
    public const int CustomerCode = 50;
    public const int CustomerName = 200;
    public const int CustomerContactName = 150;
    public const int CustomerPhone = 50;
    public const int CustomerMobileNumber = 50;
    public const int CustomerEmail = 200;
    public const int CustomerAddress = 500;
    public const int CustomerNotes = 2000;

    public const int ContainerName = 100;
    public const int ContainerShortCode = 30;
    public const int ContainerSystemCode = 50;
    public const int ContainerDescription = 500;
    public const int ContainerNotes = 2000;
    public const int ContainerDashboardColour = 50;

    public static string RequireMaximum(string value, int maximum, string parameterName)
    {
        if (value.Length > maximum)
            throw new ArgumentException($"{parameterName} must be no more than {maximum} characters.", parameterName);
        return value;
    }

    public static string? RequireOptionalMaximum(string? value, int maximum, string parameterName)
    {
        if (value is not null && value.Length > maximum)
            throw new ArgumentException($"{parameterName} must be no more than {maximum} characters.", parameterName);
        return value;
    }
}

/// <summary>Checked arithmetic for quantities that are exposed through int application contracts.</summary>
public static class QuantityMath
{
    public static int ToInt32(long value) => checked((int)value);
    public static int Add(int left, int right) => checked(left + right);
    public static int Subtract(int left, int right) => checked(left - right);
    public static int Negate(int value) => checked(-value);
    public static int Absolute(int value) => value >= 0 ? value : checked(-value);

    public static int Sum(IEnumerable<int> values)
    {
        var total = 0L;
        foreach (var value in values)
            total = checked(total + value);
        return ToInt32(total);
    }

    public static int SumToInt32(params long[] values)
    {
        var total = 0L;
        foreach (var value in values)
            total = checked(total + value);
        return ToInt32(total);
    }

    public static int SumSigned<T>(IEnumerable<T> values, Func<T, int> quantity,
        Func<T, bool> positive)
    {
        var total = 0L;
        foreach (var value in values)
        {
            var item = quantity(value);
            total = positive(value)
                ? checked(total + item)
                : checked(total - item);
        }
        return ToInt32(total);
    }
}
