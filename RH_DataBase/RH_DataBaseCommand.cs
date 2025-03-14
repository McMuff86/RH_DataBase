using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RH_DataBase.Views;

namespace RH_DataBase
{
    public class RH_DataBaseCommand : Command
    {
        public RH_DataBaseCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static RH_DataBaseCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "RH_DataBase";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Starte Rhino-Supabase Parts Manager...");
            
            try
            {
                // Erstellen und anzeigen der Hauptansicht
                var mainView = new MainView();
                var result = mainView.ShowModal();
                
                // Je nach Ergebnis können wir hier entsprechenden Code ausführen
                if (result == Eto.Forms.DialogResult.Ok)
                {
                    RhinoApp.WriteLine("Parts Manager wurde mit OK geschlossen.");
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine("Parts Manager wurde abgebrochen oder geschlossen.");
                    return Result.Cancel;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Starten des Parts Managers: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
