using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;
using Postgrest;
using RH_DataBase.Models;
using RH_DataBase.Config;
using System.Linq;

namespace RH_DataBase.Services
{
    public class SupabaseService
    {
        private static Supabase.Client _client;
        private static SupabaseService _instance;

        public static SupabaseService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SupabaseService();
                }
                return _instance;
            }
        }

        private SupabaseService()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                if (string.IsNullOrEmpty(SupabaseConfig.SupabaseUrl))
                {
                    throw new InvalidOperationException("Die Supabase-URL ist nicht konfiguriert.");
                }

                if (string.IsNullOrEmpty(SupabaseConfig.DefaultKey))
                {
                    throw new InvalidOperationException("Der zu verwendende Schlüssel (DefaultKey) ist nicht konfiguriert.");
                }

                // Setze grundlegende Optionen für die Verbindung
                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true
                    // Die folgenden Optionen sind in der aktuellen Version nicht verfügbar
                    // ShouldInitializeRealtime = true,
                    // RealtimeTimeout = TimeSpan.FromSeconds(10)
                };

                // Client initialisieren und den Service-Role-Key verwenden
                try 
                {
                    Rhino.RhinoApp.WriteLine($"Initialisiere Supabase-Client mit URL: {SupabaseConfig.SupabaseUrl}");
                    Rhino.RhinoApp.WriteLine($"Verwende {(SupabaseConfig.DefaultKey == SupabaseConfig.SupabaseServiceKey ? "Service-Role-Key" : "Anon-Key")} für Authentifizierung");
                }
                catch
                {
                    Console.WriteLine($"Initialisiere Supabase-Client mit URL: {SupabaseConfig.SupabaseUrl}");
                }
                
                _client = new Supabase.Client(SupabaseConfig.SupabaseUrl, SupabaseConfig.DefaultKey, options);
                
                // Versuche, eine einfache Datenbank-Abfrage durchzuführen, um die Verbindung zu testen
                Task.Run(async () => {
                    try 
                    {
                        // Statt GetClientHealth, führen wir eine einfache Abfrage zur Überprüfung durch
                        var testQuery = await _client.From<RH_DataBase.Models.Part>().Select("id").Limit(1).Get();
                        try 
                        {
                            Rhino.RhinoApp.WriteLine($"Supabase-Verbindungstest erfolgreich: {testQuery.ResponseMessage.IsSuccessStatusCode}");
                        }
                        catch
                        {
                            Console.WriteLine($"Supabase-Verbindungstest erfolgreich: {testQuery.ResponseMessage.IsSuccessStatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Rhino.RhinoApp.WriteLine($"Warnung: Supabase-Verbindungstest fehlgeschlagen: {ex.Message}");
                        }
                        catch
                        {
                            Console.WriteLine($"Warnung: Supabase-Verbindungstest fehlgeschlagen: {ex.Message}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                try
                {
                    Rhino.RhinoApp.WriteLine($"KRITISCH: Supabase-Client konnte nicht initialisiert werden: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Rhino.RhinoApp.WriteLine($"  Details: {ex.InnerException.Message}");
                    }
                }
                catch
                {
                    Console.WriteLine($"KRITISCH: Supabase-Client konnte nicht initialisiert werden: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"  Details: {ex.InnerException.Message}");
                    }
                }
                throw new Exception($"Failed to initialize Supabase client: {ex.Message}", ex);
            }
        }

        #region Part Operations

        public async Task<List<Part>> GetAllPartsAsync()
        {
            try
            {
                var response = await _client.From<Part>()
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch parts: {ex.Message}");
            }
        }

        public async Task<Part> GetPartByIdAsync(int id)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Where(p => p.Id == id)
                    .Single();
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch part with ID {id}: {ex.Message}");
            }
        }

        public async Task<List<Part>> SearchPartsByNameAsync(string searchTerm)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Filter("name", Postgrest.Constants.Operator.ILike, $"%{searchTerm}%")
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to search parts: {ex.Message}");
            }
        }

        public async Task<Part> CreatePartAsync(Part part)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Insert(part);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create part: {ex.Message}");
            }
        }

        public async Task<Part> UpdatePartAsync(Part part)
        {
            try
            {
                var response = await _client.From<Part>()
                    .Where(p => p.Id == part.Id)
                    .Update(part);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update part: {ex.Message}");
            }
        }

        public async Task DeletePartAsync(int id)
        {
            try
            {
                await _client.From<Part>()
                    .Where(p => p.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete part with ID {id}: {ex.Message}");
            }
        }

        #endregion

        #region Drawing Operations

        public async Task<List<Drawing>> GetAllDrawingsAsync()
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch drawings: {ex.Message}");
            }
        }

        public async Task<Drawing> GetDrawingByIdAsync(int id)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Where(d => d.Id == id)
                    .Single();
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch drawing with ID {id}: {ex.Message}");
            }
        }

        public async Task<List<Drawing>> GetDrawingsForPartAsync(int partId)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Where(d => d.PartId == partId)
                    .Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch drawings for part with ID {partId}: {ex.Message}");
            }
        }

        public async Task<Drawing> CreateDrawingAsync(Drawing drawing)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Insert(drawing);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create drawing: {ex.Message}");
            }
        }

        public async Task<Drawing> UpdateDrawingAsync(Drawing drawing)
        {
            try
            {
                var response = await _client.From<Drawing>()
                    .Where(d => d.Id == drawing.Id)
                    .Update(drawing);
                return response.Models[0];
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update drawing: {ex.Message}");
            }
        }

        public async Task DeleteDrawingAsync(int id)
        {
            try
            {
                await _client.From<Drawing>()
                    .Where(d => d.Id == id)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete drawing with ID {id}: {ex.Message}");
            }
        }

        #endregion

        #region File Storage Operations

        /// <summary>
        /// Erstellt einen Storage-Bucket, falls er noch nicht existiert
        /// </summary>
        public async Task<bool> CreateBucketIfNotExistsAsync(string bucketName, bool isPublic = true)
        {
            try
            {
                // Prüfe, ob der Bucket bereits existiert
                var buckets = await _client.Storage.ListBuckets();
                if (buckets.Any(b => b.Name == bucketName))
                {
                    return true; // Bucket existiert bereits
                }
                
                // Erstelle den Bucket
                await _client.Storage.CreateBucket(bucketName, new Supabase.Storage.BucketUpsertOptions
                {
                    Public = isPublic
                });
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create bucket: {ex.Message}");
            }
        }

        public async Task<string> UploadFileAsync(string bucketName, string filePath, string fileName)
        {
            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var response = await _client.Storage
                    .From(bucketName)
                    .Upload(fileBytes, fileName);
                
                // Generiere eine öffentliche URL für die Datei
                var fileUrl = _client.Storage
                    .From(bucketName)
                    .GetPublicUrl(fileName);
                
                return fileUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload file: {ex.Message}");
            }
        }

        public async Task<string> DownloadFileAsync(string bucketName, string fileName, string destinationPath)
        {
            try
            {
                // Ältere Versionen der Supabase-Client-Bibliothek verwenden andere Parameter
                // Prüfe zuerst, ob wir die File API korrekt aufrufen können
                var fileData = await _client.Storage
                    .From(bucketName)
                    .Download(fileName, null);
                
                // Speichere die heruntergeladene Datei
                System.IO.File.WriteAllBytes(destinationPath, fileData);
                
                return destinationPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download file: {ex.Message}");
            }
        }

        public async Task DeleteFileAsync(string bucketName, string fileName)
        {
            try
            {
                // Die Remove-Methode erwartet ein Array von Dateinamen
                var fileNames = new List<string> { fileName };
                
                await _client.Storage
                    .From(bucketName)
                    .Remove(fileNames);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete file: {ex.Message}");
            }
        }

        #endregion

        #region Authentifizierung

        /// <summary>
        /// Erzeugt ein JWT-Token basierend auf benutzerdefiniertem Secret
        /// </summary>
        /// <param name="userId">Die Benutzer-ID</param>
        /// <param name="additionalClaims">Zusätzliche Claims, die im Token enthalten sein sollen</param>
        /// <returns>Ein gültiges JWT-Token</returns>
        public string CreateJwtToken(string userId, Dictionary<string, object> additionalClaims = null)
        {
            try
            {
                // Hier könnte eine JWT-Bibliothek verwendet werden, um ein Token zu erstellen
                // Beispiel mit System.IdentityModel.Tokens.Jwt (muss als NuGet-Paket hinzugefügt werden)
                /*
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Convert.FromBase64String(SupabaseConfig.JwtSecret);
                
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                };
                
                if (additionalClaims != null)
                {
                    foreach (var claim in additionalClaims)
                    {
                        claims.Add(new Claim(claim.Key, claim.Value.ToString()));
                    }
                }
                
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
                */
                
                // Platzhalter - fügen Sie die tatsächliche JWT-Generierungslogik hinzu
                return "JWT-Token würde hier generiert werden mit Secret: " + SupabaseConfig.JwtSecret;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create JWT token: {ex.Message}");
            }
        }

        #endregion
    }
} 