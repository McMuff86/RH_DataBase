using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly BlockService _blockService;
        private static PartsController _instance;
        
        // Standardbucket für die Speicherung von 3DM-Dateien
        private const string BLOCKS_BUCKET = "blocks";
        // Temporäres Verzeichnis für heruntergeladene Dateien
        private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "RH_DataBase");

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
            _blockService = BlockService.Instance;
            
            // Stelle sicher, dass das temporäre Verzeichnis existiert
            if (!Directory.Exists(TempDirectory))
            {
                Directory.CreateDirectory(TempDirectory);
            }
            
            // Initialisiere den Storage-Bucket asynchron
            Task.Run(async () =>
            {
                try
                {
                    await _supabaseService.CreateBucketIfNotExistsAsync(BLOCKS_BUCKET, true);
                    RhinoApp.WriteLine($"Storage-Bucket '{BLOCKS_BUCKET}' wurde initialisiert");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler bei der Initialisierung des Storage-Buckets: {ex.Message}");
                }
            });
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
            // Zuerst das Teil holen, um den Dateipfad zu bekommen
            var part = await _supabaseService.GetPartByIdAsync(partId);
            
            // Wenn das Teil eine verknüpfte Modelldatei hat, diese auch löschen
            if (!string.IsNullOrEmpty(part.ModelPath))
            {
                try
                {
                    string fileName = Path.GetFileName(part.ModelPath);
                    await _supabaseService.DeleteFileAsync(BLOCKS_BUCKET, fileName);
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler beim Löschen der Modelldatei: {ex.Message}");
                    // Wir werfen hier keinen Fehler, da wir trotzdem versuchen wollen, das Teil zu löschen
                }
            }
            
            // Jetzt das Teil aus der Datenbank löschen
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
        /// Aktualisiert ein Teil in der Datenbank
        /// </summary>
        public async Task<Part> UpdatePartAsync(Part part)
        {
            // Aktuelle Zeit als Updated-Zeitstempel setzen
            part.UpdatedAt = DateTime.UtcNow;
            
            return await _supabaseService.UpdatePartAsync(part);
        }

        /// <summary>
        /// Fügt ein Teil in das aktive Rhino-Dokument ein
        /// </summary>
        public async Task<bool> InsertPartIntoDocumentAsync(Part part, RhinoDoc doc)
        {
            try
            {
                RhinoApp.WriteLine($"Füge Teil {part.Name} (ID: {part.Id}) ein");
                
                // Prüfe, ob ein Modellpfad vorhanden ist
                if (string.IsNullOrEmpty(part.ModelPath))
                {
                    RhinoApp.WriteLine("Keine Modelldatei für dieses Teil vorhanden. Erstelle Dummy-Box.");
                    
                    // Falls kein Modellpfad vorhanden ist, erstelle eine Dummy-Box (wie bisher)
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
                }
                else
                {
                    // Lade die 3DM-Datei aus dem Supabase-Storage herunter
                    string fileName = Path.GetFileName(part.ModelPath);
                    string tempFilePath = Path.Combine(TempDirectory, fileName);
                    
                    // Datei herunterladen
                    RhinoApp.WriteLine($"Lade Modelldatei {fileName} herunter...");
                    await _supabaseService.DownloadFileAsync(BLOCKS_BUCKET, fileName, tempFilePath);
                    
                    // Importiere die Blockdefinition
                    RhinoApp.WriteLine("Importiere Blockdefinition...");
                    int blockId = _blockService.ImportBlockDefinition(doc, tempFilePath, part.Name);
                    
                    if (blockId < 0)
                    {
                        RhinoApp.WriteLine("Fehler beim Importieren der Blockdefinition");
                        return false;
                    }
                    
                    // Frage den Benutzer nach dem Einfügepunkt
                    Point3d insertionPoint;
                    var getPointResult = Rhino.Input.RhinoGet.GetPoint("Einfügepunkt für den Block wählen", false, out insertionPoint);
                    if (getPointResult != Rhino.Commands.Result.Success)
                    {
                        RhinoApp.WriteLine("Einfügen abgebrochen");
                        return false;
                    }
                    
                    // Füge die Blockinstanz ein
                    Guid objectId = _blockService.InsertBlockInstance(doc, blockId, insertionPoint);
                    
                    if (objectId == Guid.Empty)
                    {
                        RhinoApp.WriteLine("Fehler beim Einfügen der Blockinstanz");
                        return false;
                    }
                    
                    // Setze Eigenschaften
                    var rhinoObject = doc.Objects.Find(objectId);
                    if (rhinoObject != null)
                    {
                        rhinoObject.Attributes.SetUserString("PartId", part.Id.ToString());
                        rhinoObject.Attributes.SetUserString("Category", part.Category);
                        rhinoObject.Attributes.SetUserString("Material", part.Material);
                        rhinoObject.CommitChanges();
                    }
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

        /// <summary>
        /// Holt alle Blockdefinitionen aus dem aktuellen Dokument
        /// </summary>
        public List<BlockDefinitionInfo> GetBlockDefinitions(RhinoDoc doc)
        {
            return _blockService.GetAllBlockDefinitions(doc);
        }

        /// <summary>
        /// Exportiert eine Blockdefinition und speichert sie in Supabase
        /// </summary>
        public async Task<Part> ExportBlockDefinitionAsync(RhinoDoc doc, int blockDefinitionId, string name, string description, string category, string material)
        {
            try
            {
                // Hole die Blockdefinition
                var instanceDef = doc.InstanceDefinitions[blockDefinitionId];
                if (instanceDef == null || instanceDef.IsDeleted)
                {
                    throw new Exception($"Blockdefinition mit ID {blockDefinitionId} konnte nicht gefunden werden.");
                }
                
                // Erstelle temporären Dateinamen
                string tempFileName = $"{Guid.NewGuid()}.3dm";
                string tempFilePath = Path.Combine(TempDirectory, tempFileName);
                
                // Exportiere den Block als 3DM-Datei
                bool exportSuccess = _blockService.ExportBlockDefinition(doc, blockDefinitionId, tempFilePath);
                if (!exportSuccess)
                {
                    throw new Exception("Fehler beim Exportieren der Blockdefinition.");
                }
                
                // Lade die Datei zu Supabase hoch
                string fileUrl = await _supabaseService.UploadFileAsync(BLOCKS_BUCKET, tempFilePath, tempFileName);
                
                // Erstelle einen neuen Teil in der Datenbank
                var part = new Part
                {
                    Name = name,
                    Description = description,
                    Category = category,
                    Material = material,
                    Dimensions = $"{GetBlockSize(instanceDef):F2}",
                    ModelPath = fileUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // Speichere das Teil in der Datenbank
                var savedPart = await _supabaseService.CreatePartAsync(part);
                
                // Lösche die temporäre Datei
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // Ignoriere Fehler beim Löschen der temporären Datei
                }
                
                return savedPart;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren des Blocks: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Erstellt einen neuen Block aus ausgewählten Objekten und speichert ihn in Supabase
        /// </summary>
        public async Task<Part> CreateBlockFromSelectionAsync(RhinoDoc doc, IEnumerable<Guid> objectIds, string name, string description, string category, string material, Point3d basePoint)
        {
            try
            {
                // Erstelle einen neuen Block aus den ausgewählten Objekten
                int blockId = _blockService.CreateBlockFromObjects(doc, objectIds, name, basePoint, false);
                if (blockId < 0)
                {
                    throw new Exception("Fehler beim Erstellen des Blocks aus den ausgewählten Objekten.");
                }
                
                // Verwende die bestehende Methode, um den Block zu exportieren
                return await ExportBlockDefinitionAsync(doc, blockId, name, description, category, material);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Erstellen und Exportieren des Blocks: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Hilfsmethode zum Ermitteln der Größe eines Blocks
        /// </summary>
        private double GetBlockSize(InstanceDefinition instanceDef)
        {
            try
            {
                // Sammle alle Objekte aus dem Block
                var boundingBox = BoundingBox.Empty;
                
                foreach (var objRef in instanceDef.GetObjects())
                {
                    if (objRef?.Geometry != null)
                    {
                        // Erweitere die BoundingBox mit jedem Objekt
                        boundingBox.Union(objRef.Geometry.GetBoundingBox(false));
                    }
                }
                
                return boundingBox.Diagonal.Length;
            }
            catch
            {
                return 0.0;
            }
        }
    }
} 