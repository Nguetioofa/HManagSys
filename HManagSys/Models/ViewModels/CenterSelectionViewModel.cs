namespace HManagSys.Models.ViewModels
{
    /// <summary>
    /// Modèle pour la sélection de centre après connexion
    /// Comme choisir dans quel bâtiment de l'hôpital travailler aujourd'hui
    /// </summary>
    public class CenterSelectionViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<CenterAssignmentInfo> AvailableCenters { get; set; } = new();
        public int? LastSelectedCenterId { get; set; }
    }


    /// <summary>
    /// Informations sur les centres assignés à un utilisateur
    /// Version enrichie pour l'interface utilisateur
    /// </summary>
    public class CenterAssignmentInfo
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public string CenterAddress { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public bool IsLastSelected { get; set; }
        public DateTime AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }
    }
}
