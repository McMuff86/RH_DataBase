using System;
using System.IO;
using DotNetEnv;
using Rhino;

namespace RH_DataBase.Config
{
    public static class SupabaseConfig
    {
        // Flag zur Nachverfolgung, ob die Datei erfolgreich geladen wurde
        public static bool EnvFileLoaded { get; private set; } = false;
        public static string LoadedEnvPath { get; private set; } = "Keine .env.local gefunden";

        static SupabaseConfig()
        {
            // Lade die Umgebungsvariablen aus der .env.local-Datei
            // Liste möglicher Pfade
            var possiblePaths = new[]
            {
                // 1. Im gleichen Verzeichnis wie die ausführbare Datei (für Produktion)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env.local"),
                
                // 2. Im aktuellen Arbeitsverzeichnis
                Path.Combine(Directory.GetCurrentDirectory(), ".env.local"),
                
                // 3. Im Projektverzeichnis (für Entwicklung)
                Path.Combine(AppContext.BaseDirectory, ".env.local"),
                
                // 4. Eine Ebene höher vom aktuellen Verzeichnis
                Directory.GetParent(Directory.GetCurrentDirectory())?.FullName != null
                    ? Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, ".env.local")
                    : null,
                
                // 5. Zwei Ebenen höher vom aktuellen Verzeichnis
                Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.FullName != null
                    ? Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName, ".env.local")
                    : null
            };

            // Gehe alle möglichen Pfade durch und versuche, die Datei zu laden
            foreach (var path in possiblePaths)
            {
                if (path != null && File.Exists(path))
                {
                    try 
                    {
                        Env.Load(path);
                        EnvFileLoaded = true;
                        LoadedEnvPath = path;
                        
                        // Debug-Ausgabe
                        try {
                            RhinoApp.WriteLine($".env.local erfolgreich geladen von: {path}");
                        } catch {
                            // Falls RhinoApp noch nicht verfügbar ist
                            Console.WriteLine($".env.local erfolgreich geladen von: {path}");
                        }
                        
                        break;
                    }
                    catch (Exception ex)
                    {
                        try {
                            RhinoApp.WriteLine($"Fehler beim Laden von .env.local aus {path}: {ex.Message}");
                        } catch {
                            Console.WriteLine($"Fehler beim Laden von .env.local aus {path}: {ex.Message}");
                        }
                    }
                }
            }

            if (!EnvFileLoaded)
            {
                try {
                    RhinoApp.WriteLine("WARNUNG: Keine .env.local-Datei gefunden. Verwende Standardwerte.");
                    RhinoApp.WriteLine("Gesuchte Pfade:");
                    foreach (var path in possiblePaths)
                    {
                        if (path != null)
                        {
                            RhinoApp.WriteLine($"  - {path} (existiert: {File.Exists(path)})");
                        }
                    }
                } catch {
                    Console.WriteLine("WARNUNG: Keine .env.local-Datei gefunden. Verwende Standardwerte.");
                }
            }
        }

        // Supabase URL aus der .env.local-Datei
        public static readonly string SupabaseUrl = Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_URL") 
            ?? "https://vxkzmvugmzjnjiqysnmw.supabase.co"; // Fallback-Wert nur als Notlösung
        
        // Anonymer Schlüssel (anon key) aus der .env.local-Datei - eingeschränkte Berechtigungen
        public static readonly string SupabaseAnonKey = Environment.GetEnvironmentVariable("NEXT_PUBLIC_SUPABASE_ANON_KEY") 
            ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ4a3ptdnVnbXpqbmppcXlzbm13Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDE3MTYwNTYsImV4cCI6MjA1NzI5MjA1Nn0.BF2WKpv1gdX7HFSx11Q6w-V8K09mVoD2XuDqRwgX0D4"; // Fallback-Wert als Notlösung
        
        // Service Role Key für administrative Operationen (empfohlen für Bucket-Operationen) - volle Berechtigungen
        public static readonly string SupabaseServiceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") 
            ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ4a3ptdnVnbXpqbmppcXlzbm13Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MTcxNjA1NiwiZXhwIjoyMDU3MjkyMDU2fQ.J893uCqFnI-SigJnSVvNRbgTkNuODzmr0IW_1UxPAl0"; // Fallback-Wert als Notlösung
        
        // API Token für Management API
        public static readonly string SupabaseApiToken = Environment.GetEnvironmentVariable("SUPABASE_API_TOKEN") 
            ?? "sbp_fc8eb3dc5a457c94ac9cd5fbd510a227c29eea24"; // Fallback-Wert als Notlösung
        
        // JWT Secret für benutzerdefinierte Authentifizierung
        public static readonly string JwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
            ?? "WMOk0aqFsvVJfBS8CJa8mFOiEQ7DkgQRFybWIwtEPvh9tR9FOZ0AjMYj9zFdj5FB1c6ocMSDb9ZxS/Uhzzy47w=="; // Fallback-Wert als Notlösung
        
        // Der zu verwendende Standard-Schlüssel - für mehr Rechte ServiceKey verwenden
        public static readonly string DefaultKey = SupabaseServiceKey;
        
        // Debug-Methode zum Drucken der Konfiguration (ohne sensible Werte)
        public static void PrintConfigStatus()
        {
            try {
                RhinoApp.WriteLine($"Supabase Konfigurationsstatus:");
                RhinoApp.WriteLine($"  .env.local geladen: {EnvFileLoaded}");
                RhinoApp.WriteLine($"  Geladener Pfad: {LoadedEnvPath}");
                RhinoApp.WriteLine($"  URL definiert: {!string.IsNullOrEmpty(SupabaseUrl)}");
                RhinoApp.WriteLine($"  Anon Key definiert: {!string.IsNullOrEmpty(SupabaseAnonKey)}");
                RhinoApp.WriteLine($"  Service Key definiert: {!string.IsNullOrEmpty(SupabaseServiceKey)}");
                RhinoApp.WriteLine($"  API Token definiert: {!string.IsNullOrEmpty(SupabaseApiToken)}");
                RhinoApp.WriteLine($"  JWT Secret definiert: {!string.IsNullOrEmpty(JwtSecret)}");
                RhinoApp.WriteLine($"  Verwendeter Key: {(DefaultKey == SupabaseServiceKey ? "Service Key" : "Anon Key")}");
            } catch {
                // Falls RhinoApp noch nicht verfügbar ist
                Console.WriteLine($"Supabase Konfigurationsstatus:");
                Console.WriteLine($"  .env.local geladen: {EnvFileLoaded}");
                Console.WriteLine($"  Geladener Pfad: {LoadedEnvPath}");
                Console.WriteLine($"  URL definiert: {!string.IsNullOrEmpty(SupabaseUrl)}");
                Console.WriteLine($"  Anon Key definiert: {!string.IsNullOrEmpty(SupabaseAnonKey)}");
                Console.WriteLine($"  Service Key definiert: {!string.IsNullOrEmpty(SupabaseServiceKey)}");
                Console.WriteLine($"  API Token definiert: {!string.IsNullOrEmpty(SupabaseApiToken)}");
                Console.WriteLine($"  JWT Secret definiert: {!string.IsNullOrEmpty(JwtSecret)}");
                Console.WriteLine($"  Verwendeter Key: {(DefaultKey == SupabaseServiceKey ? "Service Key" : "Anon Key")}");
            }
        }
    }
} 