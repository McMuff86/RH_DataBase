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
            // Prüfe, ob die Konfiguration aus .env.local erfolgreich geladen wurde
            try 
            {
                // Zeige Konfigurationsstatus - nur Debug-Info, keine sensiblen Daten
                bool supabaseUrlLoaded = !string.IsNullOrEmpty(Config.SupabaseConfig.SupabaseUrl);
                bool anonKeyLoaded = !string.IsNullOrEmpty(Config.SupabaseConfig.SupabaseAnonKey);
                bool serviceKeyLoaded = !string.IsNullOrEmpty(Config.SupabaseConfig.SupabaseServiceKey);
                bool jwtSecretLoaded = !string.IsNullOrEmpty(Config.SupabaseConfig.JwtSecret);
                
                RhinoApp.WriteLine($"Konfigurationsstatus: URL={supabaseUrlLoaded}, AnonKey={anonKeyLoaded}, ServiceKey={serviceKeyLoaded}, JWT={jwtSecretLoaded}");
                
                if (!anonKeyLoaded || !serviceKeyLoaded)
                {
                    RhinoApp.WriteLine("WARNUNG: Einige Konfigurationswerte konnten nicht aus .env.local geladen werden.");
                    RhinoApp.WriteLine("Die .env.local-Datei sollte im Verzeichnis der Anwendung liegen.");
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Laden der Konfiguration: {ex.Message}");
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