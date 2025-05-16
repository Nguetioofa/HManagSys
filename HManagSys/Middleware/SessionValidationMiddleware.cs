using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Services.Interfaces;

namespace HManagSys.Middleware
{
    /// <summary>
    /// Middleware d'authentification personnalisé pour la validation des sessions
    /// Le gardien intelligent de notre hôpital numérique
    /// </summary>
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionValidationMiddleware> _logger;

        // Routes publiques qui ne nécessitent pas d'authentification
        private readonly string[] _publicRoutes =
        {
            "/auth/login",
            "/auth/changepassword",
            "/home/error",
            "/",
            "/css/",
            "/js/",
            "/lib/",
            "/images/"
        };

        public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Vérifier si la route est publique
            if (IsPublicRoute(path))
            {
                await _next(context);
                return;
            }

            // Récupérer le token de session
            var sessionToken = context.Session.GetString("SessionToken");

            if (string.IsNullOrEmpty(sessionToken))
            {
                _logger.LogWarning("Accès sans session détecté pour {Path} depuis {IP}",
                    context.Request.Path, context.Connection.RemoteIpAddress);

                await RedirectToLogin(context);
                return;
            }

            // Valider la session avec le service d'authentification
            using var scope = context.RequestServices.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            var appLogger = scope.ServiceProvider.GetRequiredService<IApplicationLogger>();

            try
            {
                var sessionValidation = await authService.ValidateSessionAsync(sessionToken);

                if (!sessionValidation.IsValid)
                {
                    if (sessionValidation.IsExpired)
                    {
                        await appLogger.LogWarningAsync("Session", "SessionExpired",
                            "Session expirée détectée",
                            details: new { SessionToken = sessionToken, Path = path });

                        // Nettoyer la session expirée
                        context.Session.Clear();
                    }

                    await RedirectToLogin(context);
                    return;
                }

                // Mettre à jour les informations de session dans le contexte
                var sessionInfo = sessionValidation.SessionInfo;
                if (sessionInfo != null)
                {
                    context.Session.SetInt32("UserId", sessionInfo.UserId);
                    context.Session.SetString("UserName", sessionInfo.UserName);
                    context.Session.SetInt32("CurrentCenterId", sessionInfo.CurrentHospitalCenterId);
                    context.Session.SetString("CurrentCenterName", sessionInfo.CurrentCenterName);
                    context.Session.SetString("CurrentRole", sessionInfo.CurrentRole);

                    // Ajouter les informations à HttpContext.Items pour faciliter l'accès
                    context.Items["CurrentUser"] = sessionInfo;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de session pour {Path}", path);

                await appLogger.LogErrorAsync("Session", "ValidationError",
                    $"Erreur lors de la validation de session pour {path}",
                    details: new { Path = path, Error = ex.Message });

                // En cas d'erreur, rediriger vers la page de connexion
                await RedirectToLogin(context);
            }
        }

        /// <summary>
        /// Vérifie si une route est publique et ne nécessite pas d'authentification
        /// </summary>
        private bool IsPublicRoute(string path)
        {
            return _publicRoutes.Any(route => path.StartsWith(route));
        }

        /// <summary>
        /// Redirige vers la page de connexion en préservant l'URL de retour
        /// </summary>
        private async Task RedirectToLogin(HttpContext context)
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            var loginUrl = $"/Auth/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";

            context.Response.Redirect(loginUrl);
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extension pour enregistrer le middleware dans le pipeline
    /// </summary>
    public static class SessionValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionValidationMiddleware>();
        }
    }
}