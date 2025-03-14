using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using RH_DataBase.Controllers;

namespace RH_DataBase.Tests
{
    public class DatabaseConnectionTest : Command
    {
        public override string EnglishName => "TestDatabaseConnection";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Teste asynchron die Verbindung zur Datenbank
            Task.Run(async () => await TestConnection()).Wait();
            return Result.Success;
        }

        private async Task TestConnection()
        {
            try
            {
                // Teste die Verbindung
                var testController = TestDataController.Instance;
                bool connectionSuccess = await testController.TestConnectionAsync();
                
                if (connectionSuccess)
                {
                    RhinoApp.WriteLine("Verbindung zur Datenbank erfolgreich hergestellt.");
                    
                    // Versuche, Beispieldaten hinzuzufügen
                    RhinoApp.WriteLine("Füge Beispieldaten hinzu...");
                    bool dataSuccess = await testController.AddSampleDataAsync();
                    
                    if (dataSuccess)
                    {
                        RhinoApp.WriteLine("Beispieldaten wurden erfolgreich hinzugefügt.");
                    }
                    else
                    {
                        RhinoApp.WriteLine("Fehler beim Hinzufügen von Beispieldaten.");
                    }
                }
                else
                {
                    RhinoApp.WriteLine("Verbindung zur Datenbank konnte nicht hergestellt werden.");
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
} 