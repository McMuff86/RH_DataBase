using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RH_DataBase.Models;
using RH_DataBase.Services;
using Rhino;

namespace RH_DataBase.Controllers
{
    /// <summary>
    /// Controller zum Testen der Datenbankverbindung und zum Hinzufügen von Beispieldaten
    /// </summary>
    public class TestDataController
    {
        private readonly SupabaseService _supabaseService;
        private static TestDataController _instance;

        public static TestDataController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TestDataController();
                }
                return _instance;
            }
        }

        private TestDataController()
        {
            _supabaseService = SupabaseService.Instance;
        }

        /// <summary>
        /// Testet die Verbindung zur Datenbank
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Versuche, alle Teile zu laden, um die Verbindung zu testen
                var parts = await _supabaseService.GetAllPartsAsync();
                RhinoApp.WriteLine($"Verbindung erfolgreich. {parts.Count} Teile in der Datenbank gefunden.");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Verbindungsfehler: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fügt Beispieldaten zur Datenbank hinzu
        /// </summary>
        public async Task<bool> AddSampleDataAsync()
        {
            try
            {
                // Erstelle Beispiel-Teile
                var sampleParts = new List<Part>
                {
                    new Part
                    {
                        Name = "Grundplatte",
                        Description = "Basisplatte für modulare Konstruktionen",
                        Category = "Grundelemente",
                        Material = "Stahl",
                        Dimensions = "100x100x10",
                        Weight = 2.5,
                        ModelPath = "/models/baseplate.obj",
                        ThumbnailUrl = "/thumbnails/baseplate.png"
                    },
                    new Part
                    {
                        Name = "Verbindungsstück",
                        Description = "Verbindungselement für Konstruktionen",
                        Category = "Verbindungselemente",
                        Material = "Aluminium",
                        Dimensions = "50x20x20",
                        Weight = 0.8,
                        ModelPath = "/models/connector.obj",
                        ThumbnailUrl = "/thumbnails/connector.png"
                    },
                    new Part
                    {
                        Name = "Halterung",
                        Description = "Universelle Halterung für verschiedene Anwendungen",
                        Category = "Befestigungselemente",
                        Material = "Edelstahl",
                        Dimensions = "80x40x30",
                        Weight = 1.2,
                        ModelPath = "/models/bracket.obj",
                        ThumbnailUrl = "/thumbnails/bracket.png"
                    }
                };

                // Füge die Teile zur Datenbank hinzu
                foreach (var part in sampleParts)
                {
                    var createdPart = await _supabaseService.CreatePartAsync(part);
                    RhinoApp.WriteLine($"Teil '{createdPart.Name}' mit ID {createdPart.Id} erstellt.");

                    // Erstelle Beispielzeichnungen für jedes Teil
                    var drawing = new Drawing
                    {
                        Title = $"Technische Zeichnung: {createdPart.Name}",
                        Description = $"Detaillierte technische Zeichnung für {createdPart.Name}",
                        DrawingNumber = $"DRW-{createdPart.Id:D4}",
                        Revision = "A",
                        FileType = "PDF",
                        FilePath = $"/drawings/{createdPart.Name.ToLower().Replace(" ", "_")}.pdf",
                        CreatedBy = "System",
                        ApprovedBy = "Admin",
                        PartId = createdPart.Id
                    };

                    var createdDrawing = await _supabaseService.CreateDrawingAsync(drawing);
                    RhinoApp.WriteLine($"Zeichnung '{createdDrawing.Title}' mit ID {createdDrawing.Id} erstellt.");
                }

                RhinoApp.WriteLine("Beispieldaten wurden erfolgreich hinzugefügt.");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Hinzufügen von Beispieldaten: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
} 