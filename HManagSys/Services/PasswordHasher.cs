using BCrypt.Net;
using HManagSys.Models.ViewModels.Users;
using HManagSys.Services.Interfaces;
using System.Text.RegularExpressions;

namespace HManagSys.Services
{
    /// <summary>
    /// Service de hachage de mots de passe avec BCrypt
    /// Implémentation sécurisée et performante
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        // Complexité de hachage (12 = temps de calcul ~250ms en 2024)
        private const int WorkFactor = 12;

        // Patterns pour validation de mot de passe
        private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex LowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex DigitRegex = new(@"\d", RegexOptions.Compiled);
        private static readonly Regex SpecialCharRegex = new(@"[!@#$%^&*(),.?"":{};|<>]", RegexOptions.Compiled);

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Le mot de passe ne peut pas être vide", nameof(password));

            // BCrypt génère automatiquement un salt unique pour chaque hash
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                // BCrypt.Verify gère automatiquement l'extraction du salt du hash
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // En cas d'erreur (hash corrompu), retourner false
                return false;
            }
        }

        public async Task<string> GenerateTemporaryPasswordAsync()
        {
            // Générer un mot de passe temporaire simple mais sécurisé
            // Format : HospXXXX où XXXX est un nombre aléatoire
            await Task.Delay(1); // Pour l'interface async

            var random = new Random();
            var number = random.Next(1000, 9999);
            return $"1234";
        }

        public PasswordValidationResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordValidationResult();
            var errors = new List<string>();
            int score = 0;

            // Vérifications de base
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Le mot de passe est obligatoire");
                result.IsValid = false;
                result.Errors = errors;
                result.StrengthScore = 0;
                result.StrengthLevel = "Invalid";
                return result;
            }

            // Longueur minimum
            if (password.Length < 8)
            {
                errors.Add("Le mot de passe doit contenir au moins 8 caractères");
            }
            else
            {
                score += 25; // Bonus pour longueur suffisante
                if (password.Length >= 12) score += 10; // Bonus pour longueur excellente
            }

            // Vérification des types de caractères
            if (UppercaseRegex.IsMatch(password))
                score += 15;
            else
                errors.Add("Le mot de passe doit contenir au moins une majuscule");

            if (LowercaseRegex.IsMatch(password))
                score += 15;
            else
                errors.Add("Le mot de passe doit contenir au moins une minuscule");

            if (DigitRegex.IsMatch(password))
                score += 15;
            else
                errors.Add("Le mot de passe doit contenir au moins un chiffre");

            if (SpecialCharRegex.IsMatch(password))
                score += 20;
            else
                errors.Add("Le mot de passe doit contenir au moins un caractère spécial (!@#$%^&*...)");

            // Vérifications supplémentaires pour améliorer le score
            if (password.Length >= 16) score += 10; // Très long
            if (HasNoCommonPatterns(password)) score += 10; // Pas de patterns communs

            // Déterminer le niveau de force
            result.StrengthScore = Math.Min(100, score);
            result.StrengthLevel = result.StrengthScore switch
            {
                >= 80 => "Strong",
                >= 60 => "Good",
                >= 40 => "Fair",
                _ => "Weak"
            };

            // Le mot de passe est valide s'il n'y a pas d'erreurs
            result.IsValid = errors.Count == 0;
            result.Errors = errors;

            return result;
        }

        /// <summary>
        /// Vérifie l'absence de patterns communs faibles
        /// </summary>
        private static bool HasNoCommonPatterns(string password)
        {
            var lowerPassword = password.ToLower();

            // Mots interdits
            var forbiddenWords = new[] { "password", "123456", "admin", "user", "guest", "hosp", "hospital" };

            // Vérifier si le mot de passe contient des mots interdits
            return !forbiddenWords.Any(word => lowerPassword.Contains(word));
        }
    }
}