namespace HManagSys.Models.ViewModels.HospitalCenter
{
    /// <summary>
    /// Analyse des impacts de désactivation d'un centre
    /// </summary>
    public class CenterDeactivationImpact
    {
        public List<string> WarningMessages { get; set; } = new();
        public List<string> BlockingIssues { get; set; } = new();
        public bool HasBlockingIssues { get; set; }

        public string? WarningMessage => WarningMessages.Any() ? string.Join("; ", WarningMessages) : null;
        public string? BlockingMessage => BlockingIssues.Any() ? string.Join("; ", BlockingIssues) : null;
    }
}
