using System.Text.Json;

namespace HManagSys.Helpers
{
    /// <summary>
    /// Extensions pour la gestion de la session
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Enregistre un objet dans la session
        /// </summary>
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        /// <summary>
        /// Récupère un objet depuis la session
        /// </summary>
        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
