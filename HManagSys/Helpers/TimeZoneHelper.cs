namespace HManagSys.Helpers;

/// <summary>
/// Helper pour la gestion du fuseau horaire camerounais (UTC+1)
/// Notre garde-temps officiel pour tout le système
/// </summary>
public static class TimeZoneHelper
{
    // Fuseau horaire du Cameroun (UTC+1)
    private static readonly TimeZoneInfo CameroonTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");

    /// <summary>
    /// Obtient l'heure actuelle en fuseau camerounais
    /// Comme regarder l'horloge murale de l'hôpital
    /// </summary>
    public static DateTime GetCameroonTime()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CameroonTimeZone);
    }

    /// <summary>
    /// Convertit une heure UTC vers le fuseau camerounais
    /// </summary>
    public static DateTime ConvertToCameroonTime(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CameroonTimeZone);
    }

    /// <summary>
    /// Convertit une heure camerounaise vers UTC pour stockage
    /// </summary>
    public static DateTime ConvertToUtc(DateTime cameroonDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(cameroonDateTime, CameroonTimeZone);
    }
}
