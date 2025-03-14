using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using RH_DataBase.Models;
using RH_DataBase.Services;

namespace RH_DataBase.Controllers
{
    public class PartsController
    {
        private readonly SupabaseService _supabaseService;
        private static PartsController _instance;

        public static PartsController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PartsController();
                }
                return _instance;
            }
        }

        private PartsController()
        {
            _supabaseService = SupabaseService.Instance;
        }

        /// <summary>
        /// Holt alle Teile aus der Datenbank
        /// </summary>
        public async Task<List<Part>> GetAllPartsAsync()
        {
            return await _supabaseService.GetAllPartsAsync();
        }

        /// <summary>
        /// Holt alle Zeichnungen aus der Datenbank
        /// </summary>
        public async Task<List<Drawing>> GetAllDrawingsAsync()
        {
            return await _supabaseService.GetAllDrawingsAsync();
        }

        /// <summary>
        /// Sucht nach Teilen mit dem angegebenen Namen
        /// </summary>
        public async Task<List<Part>> SearchPartsByNameAsync(string searchTerm)
        {
            return await _supabaseService.SearchPartsByNameAsync(searchTerm);
        }

        /// <summary>
        /// Holt alle Zeichnungen für ein bestimmtes Teil
        /// </summary>
        public async Task<List<Drawing>> GetDrawingsForPartAsync(int partId)
        {
            return await _supabaseService.GetDrawingsForPartAsync(partId);
        }

        /// <summary>
        /// Löscht ein Teil aus der Datenbank anhand seiner ID
        /// </summary>
        public async Task DeletePartAsync(int partId)
        {
            await _supabaseService.DeletePartAsync(partId);
        }

        /// <summary>
        /// Löscht eine Zeichnung aus der Datenbank anhand ihrer ID
        /// </summary>
        public async Task DeleteDrawingAsync(int drawingId)
        {
            await _supabaseService.DeleteDrawingAsync(drawingId);
        }

        /// <summary>
        /// Fügt ein Teil in das aktive Rhino-Dokument ein
        /// </summary>
        public bool InsertPartIntoDocument(Part part, RhinoDoc doc)
        {
            try
            {
                // In einer realen Implementierung würden wir hier das 3D-Modell des Teils laden
                // und in das Rhino-Dokument einfügen. Da wir nicht über tatsächliche Modelldaten
                // verfügen, erzeugen wir zu Demonstrationszwecken ein einfaches Objekt.
                
                RhinoApp.WriteLine($"Füge Teil {part.Name} (ID: {part.Id}) ein");
                
                // Erstelle einen Dummy-Block für das Teil (in einer echten Implementierung
                // würden wir hier eine Datei laden)
                var box = new Box(new BoundingBox(-10, -10, -10, 10, 10, 10));
                
                // Füge das Objekt dem Dokument hinzu
                var objectId = doc.Objects.AddBox(box);
                if (objectId == Guid.Empty)
                {
                    RhinoApp.WriteLine("Fehler beim Hinzufügen des Objekts zum Dokument");
                    return false;
                }
                
                // Optional: Setze Eigenschaften wie Name, Layer, usw.
                var rhinoObject = doc.Objects.Find(objectId);
                if (rhinoObject != null)
                {
                    rhinoObject.Attributes.Name = part.Name;
                    rhinoObject.Attributes.SetUserString("PartId", part.Id.ToString());
                    rhinoObject.Attributes.SetUserString("Category", part.Category);
                    rhinoObject.Attributes.SetUserString("Material", part.Material);
                    rhinoObject.CommitChanges();
                }
                
                // Aktualisiere die Ansicht
                doc.Views.Redraw();
                
                RhinoApp.WriteLine($"Teil {part.Name} wurde erfolgreich eingefügt");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Einfügen des Teils: {ex.Message}");
                return false;
            }
        }
    }
} 