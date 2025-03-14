using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using RH_DataBase.Models;
using RH_DataBase.Services;
using System.Linq;

namespace RH_DataBase.Controllers
{
    public class PartsController
    {
        private readonly SupabaseService _supabaseService;
        private readonly BlockService _blockService;
        private static PartsController _instance;
        
        // Standardbucket für die Speicherung von 3DM-Dateien
        private const string BLOCKS_BUCKET = "uploadrhino";
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
        /// Bereitet das Einfügen eines Teils vor, indem die Datei heruntergeladen und die Blockdefinition importiert wird
        /// </summary>
        /// <returns>Eine Tupel mit blockId und tempFilePath bei Erfolg, oder (-1, null) bei Fehler</returns>
        public async Task<(int blockId, string tempFilePath)> PreparePartInsertionAsync(Part part, RhinoDoc doc)
        {
            try
            {
                RhinoApp.WriteLine($"Bereite Teil {part.Name} (ID: {part.Id}) für das Einfügen vor");
                
                // Prüfe, ob ein Modellpfad vorhanden ist
                if (string.IsNullOrEmpty(part.ModelPath))
                {
                    RhinoApp.WriteLine("Keine Modelldatei für dieses Teil vorhanden. Erstelle Dummy-Box.");
                    return (-1, null);
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
                        return (-1, null);
                    }
                    
                    return (blockId, tempFilePath);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler bei der Vorbereitung des Teils: {ex.Message}");
                return (-1, null);
            }
        }
        
        /// <summary>
        /// Fügt einen vorbereiteten Block in das Dokument ein (muss im UI-Thread aufgerufen werden!)
        /// </summary>
        /// <returns>true bei Erfolg, false bei Fehler</returns>
        public bool CompletePartInsertion(Part part, RhinoDoc doc, int blockId)
        {
            if (blockId < 0)
            {
                // Wenn kein gültiger Block, erstelle eine Dummy-Box
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
                return true;
            }
            else
            {
                try
                {
                    // Wichtig: Diese Funktion muss im UI-Thread ausgeführt werden!
                    // Frage nach dem Einfügepunkt
                    RhinoApp.WriteLine("Warte auf Benutzereingabe für den Einfügepunkt...");
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

        /// <summary>
        /// Fügt ein Teil in das aktive Rhino-Dokument ein - dies ist die alte Methode, die jetzt aufgeteilt ist
        /// Diese Methode sollte nicht mehr verwendet werden!
        /// </summary>
        [Obsolete("Verwenden Sie stattdessen PreparePartInsertionAsync und CompletePartInsertion")]
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
                    
                    // Der kritische Teil: Frage den Benutzer nach dem Einfügepunkt
                    // Dies muss im UI-Thread passieren
                    RhinoApp.WriteLine("Warte auf Benutzereingabe für den Einfügepunkt...");
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
                
                // Wenn kein Name angegeben wurde, verwende den Blocknamen
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = instanceDef.Name;
                    RhinoApp.WriteLine($"Kein Name angegeben, verwende Blocknamen: {name}");
                }
                
                // Verwende den Blocknamen als Basis für den Dateinamen
                // Entferne ungültige Dateizeichen und ersetze sie durch Unterstriche
                string safeBlockName = string.Join("_", instanceDef.Name.Split(Path.GetInvalidFileNameChars()));
                
                // Stelle sicher, dass der Name nicht leer ist
                if (string.IsNullOrWhiteSpace(safeBlockName))
                {
                    safeBlockName = "Block";
                }
                
                // Generiere einen eindeutigen Dateinamen
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string tempFileName = $"{safeBlockName}_{timestamp}.3dm";
                
                // Für den Fall, dass der Name zu lang wird, kürze ihn
                if (tempFileName.Length > 100)
                {
                    safeBlockName = safeBlockName.Substring(0, Math.Min(safeBlockName.Length, 50));
                    tempFileName = $"{safeBlockName}_{timestamp}.3dm";
                }
                
                RhinoApp.WriteLine($"Exportiere Block '{instanceDef.Name}' als '{tempFileName}'");
                string tempFilePath = Path.Combine(TempDirectory, tempFileName);
                
                // Exportiere den Block als 3DM-Datei
                bool exportSuccess = _blockService.ExportBlockDefinition(doc, blockDefinitionId, tempFilePath);
                if (!exportSuccess)
                {
                    throw new Exception("Fehler beim Exportieren der Blockdefinition.");
                }
                
                // Füge Debug-Informationen hinzu
                var fileInfo = new FileInfo(tempFilePath);
                RhinoApp.WriteLine($"Exportierte Datei: {fileInfo.Length} Bytes");
                
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

        /// <summary>
        /// Bereitet den Import einer Zeichnung nach Rhino vor
        /// </summary>
        /// <param name="drawing">Die Zeichnung, die importiert werden soll</param>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <returns>Eine Tupel mit blockId und tempFilePath bei Erfolg, oder (-1, null) bei Fehler</returns>
        public async Task<(int blockId, string tempFilePath)> PrepareDrawingImportAsync(Drawing drawing, RhinoDoc doc)
        {
            try
            {
                RhinoApp.WriteLine($"Bereite Import von Zeichnung {drawing.Title} (ID: {drawing.Id}) vor");
                
                // Prüfe, ob ein Dateipfad vorhanden ist
                if (string.IsNullOrEmpty(drawing.FilePath))
                {
                    RhinoApp.WriteLine("Kein Dateipfad für diese Zeichnung vorhanden.");
                    return (-1, null);
                }
                
                // Hole den Dateinamen aus dem Pfad
                string fileName = Path.GetFileName(drawing.FilePath);
                string tempFilePath = Path.Combine(TempDirectory, fileName);
                
                // Datei herunterladen
                RhinoApp.WriteLine($"Lade Zeichnungsdatei {fileName} herunter...");
                await _supabaseService.DownloadFileAsync(BLOCKS_BUCKET, fileName, tempFilePath);
                
                // Prüfe, ob es sich um eine 3DM-Datei handelt
                if (Path.GetExtension(tempFilePath).ToLower() == ".3dm")
                {
                    // Importiere die 3DM-Datei als Block
                    RhinoApp.WriteLine("Importiere Zeichnung als Blockdefinition...");
                    int blockId = _blockService.ImportBlockDefinition(doc, tempFilePath, drawing.Title);
                    
                    if (blockId < 0)
                    {
                        RhinoApp.WriteLine("Fehler beim Importieren der Blockdefinition");
                        return (-1, null);
                    }
                    
                    return (blockId, tempFilePath);
                }
                else
                {
                    RhinoApp.WriteLine($"Die Datei ist keine Rhino-3DM-Datei, sondern {Path.GetExtension(tempFilePath)}");
                    return (-1, null);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler bei der Vorbereitung der Zeichnung: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                }
                return (-1, null);
            }
        }
        
        /// <summary>
        /// Schließt den Import einer Zeichnung ab, indem der Benutzer nach dem Einfügepunkt gefragt wird
        /// (muss im UI-Thread aufgerufen werden!)
        /// </summary>
        public bool CompleteDrawingImport(Drawing drawing, RhinoDoc doc, int blockId)
        {
            try
            {
                if (blockId < 0)
                {
                    RhinoApp.WriteLine("Ungültige Block-ID für die Zeichnung.");
                    return false;
                }
                
                // Wichtig: Diese Funktion muss im UI-Thread ausgeführt werden!
                // Frage nach dem Einfügepunkt
                RhinoApp.WriteLine("Warte auf Benutzereingabe für den Einfügepunkt...");
                Point3d insertionPoint;
                var getPointResult = Rhino.Input.RhinoGet.GetPoint("Einfügepunkt für die Zeichnung wählen", false, out insertionPoint);
                
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
                    rhinoObject.Attributes.Name = drawing.Title;
                    rhinoObject.Attributes.SetUserString("DrawingId", drawing.Id.ToString());
                    rhinoObject.Attributes.SetUserString("DrawingNumber", drawing.DrawingNumber);
                    rhinoObject.Attributes.SetUserString("Revision", drawing.Revision);
                    
                    // Falls die Zeichnung mit einem Teil verknüpft ist, füge auch diese Information hinzu
                    if (drawing.PartId.HasValue)
                    {
                        rhinoObject.Attributes.SetUserString("PartId", drawing.PartId.ToString());
                    }
                    
                    rhinoObject.CommitChanges();
                }
                
                // Aktualisiere die Ansicht
                doc.Views.Redraw();
                
                RhinoApp.WriteLine($"Zeichnung {drawing.Title} wurde erfolgreich eingefügt");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Importieren der Zeichnung: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Listet alle Dateien im Standard-Bucket auf
        /// </summary>
        /// <returns>Eine Liste aller Dateien im Bucket</returns>
        public async Task<List<Supabase.Storage.FileObject>> ListFilesInBucketAsync()
        {
            try
            {
                return await _supabaseService.ListFilesInBucketAsync(BLOCKS_BUCKET);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Auflisten der Dateien im Bucket: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Importiert eine Datei aus dem Bucket als Teil in die Datenbank
        /// </summary>
        /// <param name="fileName">Der Dateiname im Bucket</param>
        /// <param name="name">Der Name des Teils</param>
        /// <param name="description">Die Beschreibung des Teils</param>
        /// <param name="category">Die Kategorie des Teils</param>
        /// <param name="material">Das Material des Teils</param>
        /// <returns>Das importierte Teil</returns>
        public async Task<Part> ImportFileFromBucketAsync(string fileName, string name, string description, string category, string material)
        {
            try
            {
                RhinoApp.WriteLine($"Importiere Datei {fileName} als Teil {name}");
                
                // Generiere die öffentliche URL für die Datei
                string fileUrl = _supabaseService.GetPublicUrlForFile(BLOCKS_BUCKET, fileName);
                
                // Erstelle ein neues Teil
                var part = new Part
                {
                    Name = name,
                    Description = description,
                    Category = category,
                    Material = material,
                    ModelPath = fileUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                // Speichere das Teil in der Datenbank
                var savedPart = await _supabaseService.CreatePartAsync(part);
                
                RhinoApp.WriteLine($"Teil {name} erfolgreich importiert (ID: {savedPart.Id})");
                return savedPart;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Importieren der Datei aus dem Bucket: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Überprüft, welche Dateien im Bucket nicht als Teile in der Datenbank vorhanden sind
        /// </summary>
        /// <returns>Eine Liste von Dateien, die nicht als Teile in der Datenbank sind</returns>
        public async Task<List<Supabase.Storage.FileObject>> GetFilesWithoutPartAsync()
        {
            try
            {
                RhinoApp.WriteLine("Starte Überprüfung der Dateien im Bucket...");
                
                // Alle Dateien im Bucket abrufen
                var allFiles = await ListFilesInBucketAsync();
                if (allFiles == null || allFiles.Count == 0)
                {
                    RhinoApp.WriteLine("Keine Dateien im Bucket gefunden.");
                    return new List<Supabase.Storage.FileObject>();
                }
                
                // Filtere nur .3dm-Dateien
                var rhinoFiles = allFiles.Where(f => f.Name.EndsWith(".3dm", StringComparison.OrdinalIgnoreCase)).ToList();
                RhinoApp.WriteLine($"Gefunden: {rhinoFiles.Count} Rhino-Dateien im Bucket");
                
                if (rhinoFiles.Count == 0)
                {
                    return new List<Supabase.Storage.FileObject>();
                }
                
                // Alle Teile aus der Datenbank abrufen
                var allParts = await _supabaseService.GetAllPartsAsync();
                RhinoApp.WriteLine($"Gefunden: {allParts.Count} Teile in der Datenbank");
                
                // Erstelle eine Liste der fehlenden Dateien
                var missingFiles = new List<Supabase.Storage.FileObject>();
                
                foreach (var file in rhinoFiles)
                {
                    // Erstelle die öffentliche URL für die Datei
                    string fileUrl = _supabaseService.GetPublicUrlForFile(BLOCKS_BUCKET, file.Name);
                    
                    // Prüfe, ob ein Teil mit dieser URL existiert
                    bool found = allParts.Any(p => !string.IsNullOrEmpty(p.ModelPath) && 
                                                  (p.ModelPath.EndsWith(file.Name) || 
                                                   p.ModelPath == fileUrl));
                    
                    if (!found)
                    {
                        missingFiles.Add(file);
                        RhinoApp.WriteLine($"Datei nicht in der Datenbank gefunden: {file.Name}");
                    }
                }
                
                RhinoApp.WriteLine($"Insgesamt {missingFiles.Count} Dateien nicht in der Datenbank gefunden.");
                return missingFiles;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler bei der Überprüfung der Dateien: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Importiert alle fehenden Dateien aus dem Bucket als Teile in die Datenbank
        /// </summary>
        /// <returns>Die Anzahl der importierten Teile</returns>
        public async Task<int> ImportAllMissingFilesAsync()
        {
            try
            {
                RhinoApp.WriteLine("Starte Import aller fehlenden Dateien...");
                
                // Prüfe, welche Dateien fehlen
                var missingFiles = await GetFilesWithoutPartAsync();
                
                if (missingFiles.Count == 0)
                {
                    RhinoApp.WriteLine("Keine fehlenden Dateien zum Importieren gefunden.");
                    return 0;
                }
                
                int importCount = 0;
                
                // Importiere jede Datei als Teil
                foreach (var file in missingFiles)
                {
                    try
                    {
                        // Erstelle einen sinnvollen Namen aus dem Dateinamen
                        string name = Path.GetFileNameWithoutExtension(file.Name);
                        name = name.Replace('_', ' '); // Ersetze Unterstriche durch Leerzeichen
                        
                        // Entferne Zeitstempel, falls vorhanden (z.B. "_20230815_123456")
                        if (name.Contains(" 202"))
                        {
                            int timestampIndex = name.IndexOf(" 202");
                            if (timestampIndex > 0)
                            {
                                name = name.Substring(0, timestampIndex);
                            }
                        }
                        
                        // Importiere die Datei als Teil
                        await ImportFileFromBucketAsync(
                            file.Name,
                            name,  // Verwende den bereinigten Namen
                            $"Automatisch importiert aus Datei {file.Name}",  // Standardbeschreibung
                            "Automatisch importiert",  // Standardkategorie
                            "Unbekannt"  // Standardmaterial
                        );
                        
                        importCount++;
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Fehler beim Importieren der Datei {file.Name}: {ex.Message}");
                        // Fahre mit der nächsten Datei fort
                    }
                }
                
                RhinoApp.WriteLine($"Insgesamt {importCount} von {missingFiles.Count} Dateien erfolgreich importiert.");
                return importCount;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Importieren der fehlenden Dateien: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Erstellt oder aktualisiert eine Zeichnung und verknüpft sie mit einer 3DM-Datei
        /// </summary>
        /// <param name="filePath">Der Pfad zur 3DM-Datei</param>
        /// <param name="title">Der Titel der Zeichnung</param>
        /// <param name="drawingNumber">Die Zeichnungsnummer</param>
        /// <param name="revision">Die Revision</param>
        /// <param name="partId">Die ID des verknüpften Teils (optional)</param>
        /// <returns>Die erstellte oder aktualisierte Zeichnung</returns>
        public async Task<Drawing> ImportDrawingAsync(string filePath, string title, string drawingNumber, string revision, int? partId = null)
        {
            try
            {
                RhinoApp.WriteLine($"Importiere Zeichnung '{title}' (Nr. {drawingNumber}, Rev. {revision})");
                
                // Prüfe, ob die Datei existiert
                if (!File.Exists(filePath))
                {
                    throw new Exception($"Die Datei {filePath} existiert nicht.");
                }
                
                // Erstelle ein neues Zeichnungsobjekt
                var drawing = new Drawing
                {
                    Title = title,
                    DrawingNumber = drawingNumber,
                    Revision = revision,
                    PartId = partId,
                    FileType = Path.GetExtension(filePath).TrimStart('.'),
                    CreatedBy = System.Environment.UserName, // Verwende den aktuellen Benutzernamen
                };
                
                // Lade die Datei hoch und verknüpfe sie mit der Zeichnung
                var savedDrawing = await _supabaseService.UploadAndLinkDrawingAsync(filePath, drawing);
                
                // Wenn ein Teil angegeben ist, aktualisiere die Referenz in der Datenbank
                if (partId.HasValue)
                {
                    var part = await _supabaseService.GetPartByIdAsync(partId.Value);
                    if (part != null)
                    {
                        // Füge die Zeichnungs-ID zur Liste der Zeichnungen für dieses Teil hinzu
                        if (part.DrawingIds == null)
                        {
                            part.DrawingIds = new List<int>();
                        }
                        
                        if (!part.DrawingIds.Contains(savedDrawing.Id))
                        {
                            part.DrawingIds.Add(savedDrawing.Id);
                            part.UpdatedAt = DateTime.UtcNow;
                            await _supabaseService.UpdatePartAsync(part);
                        }
                    }
                }
                
                RhinoApp.WriteLine($"Zeichnung erfolgreich importiert (ID: {savedDrawing.Id})");
                return savedDrawing;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Importieren der Zeichnung: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
} 