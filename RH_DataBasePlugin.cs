using System;
using Rhino;
using Eto.Forms;
using Rhino.UI;
using Rhino.PlugIns;
using RH_DataBase.Controllers;
using System.Threading.Tasks;

namespace RH_DataBase
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class RH_DataBasePlugin : Rhino.PlugIns.PlugIn
    {
        public RH_DataBasePlugin()
        {
            Instance = this;
        }
        
        ///<summary>Gets the only instance of the RH_DataBasePlugin plug-in.</summary>
        public static RH_DataBasePlugin Instance { get; private set; }

        /// <summary>
        /// Called when the plugin is being loaded
        /// </summary>
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            RhinoApp.WriteLine("Initialisiere Rhino-Supabase Plugin...");
            
            // Ausführliche Konfigurationsinformationen ausgeben
            try 
            {
                // Detaillierte Konfiguration-Debug-Informationen ausgeben
                Config.SupabaseConfig.PrintConfigStatus();
                
                // Überprüfe, ob die Konfiguration gültig ist
                if (string.IsNullOrEmpty(Config.SupabaseConfig.SupabaseUrl) || 
                    (string.IsNullOrEmpty(Config.SupabaseConfig.SupabaseAnonKey) && 
                     string.IsNullOrEmpty(Config.SupabaseConfig.SupabaseServiceKey)))
                {
                    RhinoApp.WriteLine("KRITISCHER FEHLER: Supabase-Konfiguration ist unvollständig.");
                    RhinoApp.WriteLine("Weder Anon-Key noch Service-Key ist verfügbar.");
                    errorMessage = "Supabase-Konfiguration fehlt. Bitte stellen Sie sicher, dass die .env.local-Datei korrekt eingerichtet ist.";
                    return LoadReturnCode.ErrorShowDialog;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Laden der Konfiguration: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"  Details: {ex.InnerException.Message}");
                }
            }
            
            // Test the database connection on plugin startup
            TestDatabaseConnection();
            
            // Füge einen Menüpunkt zu Rhino hinzu
            try
            {
                // Registriere den Befehl
                RhinoApp.WriteLine("Rhino-Supabase Plugin wurde geladen. Benutzen Sie den Befehl RH_DataBase, um den Parts Manager zu öffnen.");
            }
            catch (Exception ex)
            {
                errorMessage = $"Fehler beim Laden des Plugins: {ex.Message}";
                return LoadReturnCode.ErrorShowDialog;
            }
            
            return LoadReturnCode.Success;
        }

        private void TestDatabaseConnection()
        {
            RhinoApp.WriteLine("Teste Datenbankverbindung...");
            
            Task.Run(async () =>
            {
                try
                {
                    var testController = TestDataController.Instance;
                    bool connectionSuccess = await testController.TestConnectionAsync();
                    
                    if (connectionSuccess)
                    {
                        RhinoApp.WriteLine("Verbindung zur Datenbank erfolgreich hergestellt.");
                    }
                    else
                    {
                        RhinoApp.WriteLine("Verbindung zur Datenbank konnte nicht hergestellt werden.");
                        RhinoApp.WriteLine("Versuche, Beispieldaten hinzuzufügen, um Tabellen zu erstellen...");
                        
                        bool dataSuccess = await testController.AddSampleDataAsync();
                        if (dataSuccess)
                        {
                            RhinoApp.WriteLine("Beispieldaten wurden erfolgreich hinzugefügt. Tabellen wurden erstellt.");
                        }
                        else
                        {
                            RhinoApp.WriteLine("Fehler beim Hinzufügen von Beispieldaten und Erstellen der Tabellen.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler bei der Datenbankverbindung: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        RhinoApp.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                }
            });
        }

        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.
    }
}