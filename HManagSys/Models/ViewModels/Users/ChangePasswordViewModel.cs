using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Users
{
    /// <summary>
    /// Modèle pour le changement de mot de passe
    /// Utilisé après réinitialisation ou changement volontaire
    /// </summary>
    public class ChangePasswordViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsForced { get; set; } = false;

        //[Required(ErrorMessage = "Le mot de passe actuel est obligatoire")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir entre 8 et 100 caractères")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation est obligatoire")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

}