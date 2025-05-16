using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Users;

/// <summary>
/// Modèle pour l'écran de connexion
/// Comme un formulaire d'entrée dans l'hôpital
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
    public string? ReturnUrl { get; set; }
}
