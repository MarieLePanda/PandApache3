using pandapache.src.Configuration;
using pandapache.src.LoggingAndMonitoring;
using pandapache.src.Middleware;
using pandapache.src.RequestHandling;
using PandApache3.src.Configuration;
using System.Text;
using System.Security.Cryptography;
using PandApache3.src.ResponseGeneration;
using PandApache3.src.LoggingAndMonitoring;
using PandApache3.src.Module;
using ExecutionContext = PandApache3.src.Module.ExecutionContext;

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
            ExecutionContext.Current.Logger.LogDebug("Authentication Middleware");
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

                    string mainDirectory = ServerConfiguration.Instance.RootDirectory;

                    string filePath = Path.Combine(mainDirectory, Utils.GetFilePath(context.Request.Path));

                    DirectoryConfig directoryConfig = ServerConfiguration.Instance.GetDirectory(filePath);
                    if (IsValidUser(directoryConfig, username, password))
                    {
                        context.isAuth = true;
                    }
                }
            }

            await _next(context);

        }

        private bool IsValidUser(DirectoryConfig directoryConfig, string username, string password)
        {
            if(directoryConfig == null) 
                return false;

            string authUserFile = directoryConfig.AuthUserFile;
            bool exist = FileManagerFactory.Instance().Exists(authUserFile);
            if (string.IsNullOrEmpty(authUserFile) || exist == false)
            {
                ExecutionContext.Current.Logger.LogError($"Auth User File {authUserFile} don't exist");
                return false; 
            }

            ExecutionContext.Current.Logger.LogInfo($"Reading from the auth user file {authUserFile}");
            foreach (string line in File.ReadAllLines(authUserFile))
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    if (parts[0].ToLower().Equals(username.ToLower())){
                        if (parts[1].Equals(HashPassword(password)))
                            return true;
                    }
                    
                }
            }
            return false;
        }

        private string HashPassword(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Créer une instance de l'algorithme de hachage (SHA-256 ou SHA-512)
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                // Calculer le hachage du mot de passe
                byte[] hashedBytes = sha256.ComputeHash(passwordBytes);

                // Convertir le hachage en une chaîne hexadécimale
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashedBytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
