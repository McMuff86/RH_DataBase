using System;
using System.IO;
using DotNetEnv;

namespace RH_DataBase.Config
{
    public static class SupabaseConfig
    {
        static SupabaseConfig()
        {
            // Lade die Umgebungsvariablen aus der .env.local-Datei
            // Prüfe zuerst den Pfad relativ zum aktuellen Verzeichnis
            string envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env.local");
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }
            else
            {
                // Alternativ: Versuche, die Datei im Projektverzeichnis zu finden
                string projectDir = Directory.GetCurrentDirectory();
                envPath = Path.Combine(projectDir, ".env.local");
                if (File.Exists(envPath))
                {
                    Env.Load(envPath);
                }
                else
                {
                    // Versuche es noch eine Verzeichnisebene höher
                    string parentDir = Directory.GetParent(projectDir)?.FullName;
                    if (parentDir != null)
                    {
                        envPath = Path.Combine(parentDir, ".env.local");
                        if (File.Exists(envPath))
                        {
                            Env.Load(envPath);
                        }
                    }
                }
            }
        }

        // Supabase URL aus der .env.local-Datei
        public static readonly string SupabaseUrl = Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL") 
            ?? "https://vxkzmvugmzjnjiqysnmw.supabase.co"; // Fallback-Wert nur als Notlösung
        
        // Anonymer Schlüssel (anon key) aus der .env.local-Datei - eingeschränkte Berechtigungen
        public static readonly string SupabaseAnonKey = Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_ANON_KEY") 
            ?? string.Empty; // Leerer String als Fallback
        
        // Service Role Key für administrative Operationen (empfohlen für Bucket-Operationen) - volle Berechtigungen
        public static readonly string SupabaseServiceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") 
            ?? string.Empty; // Leerer String als Fallback
        
        // JWT Secret für benutzerdefinierte Authentifizierung
        public static readonly string JwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? string.Empty; // Leerer String als Fallback
        
        // Der zu verwendende Standard-Schlüssel - für mehr Rechte ServiceKey verwenden
        public static readonly string DefaultKey = SupabaseServiceKey;
    }
} 