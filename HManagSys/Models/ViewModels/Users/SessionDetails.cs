using HManagSys.Models.EfModels;

namespace HManagSys.Models.ViewModels.Users
{

    /// <summary>
    /// Détails complets d'une session
    /// Vision 360° pour l'administration
    /// </summary>
    public class SessionDetails
    {
        public SessionInfo SessionInfo { get; set; } = new();
        public User User { get; set; } = new();
        public HospitalCenter CurrentCenter { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public List<CenterAssignmentInfo> AccessibleCenters { get; set; } = new();
    }
}
