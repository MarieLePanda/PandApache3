using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src.Middleware
{
    public class AuthenticationMiddleware : IMiddleware
    {
        private readonly Func<HttpContext, Task> _next;

        public AuthenticationMiddleware(Func<HttpContext, Task> next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Récupérer l'en-tête d'authentification
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                string authHeader = context.Request.Headers["Authorization"];
                // Décoder les informations d'identification de base64
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Substring("Basic ".Length).Trim()));
                string[] credentialParts = credentials.Split(':');
                if (credentialParts.Length == 2)
                {
                    string username = credentialParts[0];
                    string password = credentialParts[1];

                    // Vérifier les informations d'identification
                    if (IsValidUser(username, password))
                    {
                        // Authentification réussie, passer au middleware suivant
                        context.isAuth = true;
                    }
                }
            }

            await _next(context);

        }

        private bool IsValidUser(string username, string password)
        {
            // Ici, vous pouvez implémenter la logique de vérification des informations d'identification
            // Par exemple, vérifier les informations d'identification dans une base de données sécurisée
            // ou comparer avec des informations d'identification prédéfinies
            // Pour cet exemple, nous supposons que l'authentification réussit toujours
            return true;
        }
    }
}
