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
        private static MainView _mainView;

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
                // In Eto.Forms kann ein geschlossenes Fenster nicht wiederverwendet werden
                // Wir erstellen bei jedem Aufruf eine neue Instanz
                if (_mainView != null && !_mainView.IsDisposed && _mainView.Visible)
                {
                    // Wenn das Fenster noch aktiv und sichtbar ist, bringen wir es in den Vordergrund
                    _mainView.BringToFront();
                    RhinoApp.WriteLine("Parts Manager wurde in den Vordergrund geholt.");
                    return Result.Success;
                }
                
                // Erstelle in allen anderen Fällen ein neues Fenster
                _mainView = new MainView();
                
                // Füge einen Event-Handler hinzu, der die Referenz löscht, wenn das Fenster geschlossen wird
                _mainView.Closed += (sender, e) => 
                {
                    RhinoApp.WriteLine("Parts Manager wurde geschlossen. Setze Referenz zurück.");
                    _mainView = null;
                };
                
                _mainView.Show();
                
                RhinoApp.WriteLine("Parts Manager wurde neu geöffnet.");
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Starten des Parts Managers: {ex.Message}");
                // Bei einem Fehler die Referenz auf null setzen, um einen sauberen Neustart zu ermöglichen
                _mainView = null;
                return Result.Failure;
            }
        }
    }
}
