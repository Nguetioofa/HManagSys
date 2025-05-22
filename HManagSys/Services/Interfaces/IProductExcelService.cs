using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using HManagSys.Services.Implementations;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des imports Excel de produits et stock
    /// </summary>
    public interface IProductExcelService
    {
        /// <summary>
        /// Génère un template Excel pour l'importation des produits et entrées en stock
        /// </summary>
        /// <param name="hospitalCenterId">ID du centre hospitalier pour lequel le template est généré</param>
        /// <returns>Contenu du fichier Excel en bytes</returns>
        Task<byte[]> GenerateImportTemplate(int hospitalCenterId);

        /// <summary>
        /// Traite un fichier Excel importé pour en extraire les données des produits et entrées en stock
        /// </summary>
        /// <param name="fileStream">Stream du fichier Excel</param>
        /// <param name="hospitalCenterId">ID du centre hospitalier pour lequel l'import est effectué</param>
        /// <returns>Liste des produits et entrées de stock extraits du fichier, ainsi que les erreurs éventuelles</returns>
        Task<(List<ProductImportDTO> Products, List<StockEntryImportDTO> StockEntries, List<string> Errors)>
            ProcessImportedExcel(Stream fileStream, int hospitalCenterId);
    }
}