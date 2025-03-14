using System;

namespace RH_DataBase.Config
{
    public static class SupabaseConfig
    {
        // Supabase URL aus der .env.local-Datei
        public static readonly string SupabaseUrl = "https://vxkzmvugmzjnjiqysnmw.supabase.co";
        
        // Anonymer Schlüssel (anon key) aus der .env.local-Datei
        public static readonly string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ4a3ptdnVnbXpqbmppcXlzbm13Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDE3MTYwNTYsImV4cCI6MjA1NzI5MjA1Nn0.BF2WKpv1gdX7HFSx11Q6w-V8K09mVoD2XuDqRwgX0D4";
        
        // Service Role Key für serverseitige Operationen - Vorsicht bei der Verwendung!
        public static readonly string SupabaseServiceKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZ4a3ptdnVnbXpqbmppcXlzbm13Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0MTcxNjA1NiwiZXhwIjoyMDU3MjkyMDU2fQ.J893uCqFnI-SigJnSVvNRbgTkNuODzmr0IW_1UxPAl0";
    }
} 