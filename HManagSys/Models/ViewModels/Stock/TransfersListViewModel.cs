namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour la liste des transferts
    /// </summary>
    public class TransfersListViewModel
    {
        /// <summary>
        /// Liste des transferts à afficher
        /// </summary>
        public List<TransferViewModel> Transfers { get; set; } = new();

        /// <summary>
        /// Transferts en attente d'approbation pour ce centre
        /// </summary>
        public List<TransferViewModel> PendingApprovals { get; set; } = new();

        /// <summary>
        /// Filtres appliqués
        /// </summary>
        public TransferFilters Filters { get; set; } = new();

        /// <summary>
        /// Informations de pagination
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();

        /// <summary>
        /// Statistiques des transferts
        /// </summary>
        public TransferStatisticsViewModel Statistics { get; set; } = new();

        /// <summary>
        /// Centres disponibles pour les filtres
        /// </summary>
        public List<SelectOption> AvailableCenters { get; set; } = new();

        /// <summary>
        /// Produits disponibles pour les filtres
        /// </summary>
        public List<SelectOption> AvailableProducts { get; set; } = new();
    }

    /// <summary>
    /// ViewModel pour l'historique des transferts
    /// </summary>
    public class TransferHistoryViewModel
    {
        /// <summary>
        /// Liste des transferts à afficher
        /// </summary>
        public List<TransferViewModel> Transfers { get; set; } = new();

        /// <summary>
        /// Filtres appliqués
        /// </summary>
        public TransferFilters Filters { get; set; } = new();

        /// <summary>
        /// Informations de pagination
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();

        /// <summary>
        /// Statistiques des transferts
        /// </summary>
        public TransferStatisticsViewModel Statistics { get; set; } = new();

        /// <summary>
        /// Centres disponibles pour les filtres
        /// </summary>
        public List<SelectOption> AvailableCenters { get; set; } = new();

        /// <summary>
        /// Produits disponibles pour les filtres
        /// </summary>
        public List<SelectOption> AvailableProducts { get; set; } = new();
    }
}