namespace HManagSys.Helpers;

public static class RoleHelper
{
    public const string SUPER_ADMIN = "SuperAdmin";
    public const string MEDICAL_STAFF = "MedicalStaff";

    /// <summary>
    /// Vérifie si l'utilisateur est SuperAdmin
    /// </summary>
    public static bool IsSuperAdmin(string? currentRole)
    {
        return currentRole == SUPER_ADMIN;
    }

    /// <summary>
    /// Vérifie si l'utilisateur peut gérer les utilisateurs (SuperAdmin only)
    /// </summary>
    public static bool CanManageUsers(string? currentRole)
    {
        return IsSuperAdmin(currentRole);
    }

    /// <summary>
    /// Vérifie si l'utilisateur peut faire des ventes/soins (tous)
    /// </summary>
    public static bool CanHandlePatients(string? currentRole)
    {
        return currentRole != null; // Tous les utilisateurs connectés
    }

    /// <summary>
    /// Vérifie si l'utilisateur peut gérer les stocks (SuperAdmin + entrées, MedicalStaff utilisation)
    /// </summary>
    public static bool CanManageStock(string? currentRole)
    {
        return currentRole != null;
    }

    /// <summary>
    /// Vérifie si l'utilisateur peut faire des ajustements de stock (SuperAdmin only)
    /// </summary>
    public static bool CanAdjustStock(string? currentRole)
    {
        return IsSuperAdmin(currentRole);
    }
}
