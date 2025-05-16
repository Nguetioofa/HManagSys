using HManagSys.Models.ViewModels.Users;
using System.Threading.Tasks;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service de hachage de mots de passe sécurisé
    /// Le cryptographe officiel de notre hôpital numérique
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hache un mot de passe avec salt automatique
        /// Utilise les meilleures pratiques de sécurité (BCrypt)
        /// </summary>
        /// <param name="password">Mot de passe en clair</param>
        /// <returns>Hash sécurisé avec salt</returns>
        string HashPassword(string password);

        /// <summary>
        /// Vérifie si un mot de passe correspond à son hash
        /// Résistant aux attaques de timing
        /// </summary>
        /// <param name="password">Mot de passe à vérifier</param>
        /// <param name="hash">Hash stocké</param>
        /// <returns>True si le mot de passe correspond</returns>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Génère un mot de passe temporaire sécurisé mais simple
        /// Format pratique pour communication verbale/SMS
        /// </summary>
        /// <returns>Mot de passe temporaire</returns>
        Task<string> GenerateTemporaryPasswordAsync();

        /// <summary>
        /// Vérifie la complexité d'un mot de passe
        /// Applique les règles de sécurité définies
        /// </summary>
        /// <param name="password">Mot de passe à valider</param>
        /// <returns>Résultat de validation avec détails</returns>
        PasswordValidationResult ValidatePasswordStrength(string password);
    }


}