using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace RH_DataBase.Services
{
    public class BlockService
    {
        private static BlockService _instance;

        public static BlockService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BlockService();
                }
                return _instance;
            }
        }

        private BlockService()
        {
        }

        /// <summary>
        /// Holt alle Blockdefinitionen aus dem aktuellen Dokument
        /// </summary>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <returns>Liste von Blockdefinitionen mit ihren Namen und IDs</returns>
        public List<BlockDefinitionInfo> GetAllBlockDefinitions(RhinoDoc doc)
        {
            var blockDefinitions = new List<BlockDefinitionInfo>();
            
            for (int i = 0; i < doc.InstanceDefinitions.Count; i++)
            {
                var instanceDef = doc.InstanceDefinitions[i];
                
                // Nur gültige Definitionen hinzufügen
                if (!instanceDef.IsDeleted)
                {
                    blockDefinitions.Add(new BlockDefinitionInfo
                    {
                        Id = instanceDef.Index,
                        Name = instanceDef.Name,
                        Description = instanceDef.Description,
                        ObjectCount = instanceDef.ObjectCount
                    });
                }
            }
            
            return blockDefinitions;
        }

        /// <summary>
        /// Exportiert eine Blockdefinition als eigene 3DM-Datei
        /// </summary>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <param name="blockDefinitionId">Die ID der zu exportierenden Blockdefinition</param>
        /// <param name="exportPath">Der Pfad, wohin die Datei exportiert werden soll</param>
        /// <returns>True, wenn der Export erfolgreich war</returns>
        public bool ExportBlockDefinition(RhinoDoc doc, int blockDefinitionId, string exportPath)
        {
            try
            {
                var instanceDef = doc.InstanceDefinitions[blockDefinitionId];
                if (instanceDef == null || instanceDef.IsDeleted)
                {
                    RhinoApp.WriteLine($"Blockdefinition mit ID {blockDefinitionId} konnte nicht gefunden werden.");
                    return false;
                }
                
                RhinoApp.WriteLine($"Exportiere Block '{instanceDef.Name}', mit {instanceDef.ObjectCount} Objekten nach {exportPath}");
                
                // Methode 1: Direkt die 3DM-Datei erstellen
                var file3dm = new File3dm();
                
                // Setze Dokument-Eigenschaften
                file3dm.Notes.Notes = $"Block: {instanceDef.Name}\nBeschreibung: {instanceDef.Description}\nObjektanzahl: {instanceDef.ObjectCount}";
                
                // Erstelle eine Kopie der Einheiten und Toleranzen vom Hauptdokument
                file3dm.Settings.ModelUnitSystem = doc.ModelUnitSystem;
                file3dm.Settings.PageUnitSystem = doc.PageUnitSystem;
                file3dm.Settings.ModelAbsoluteTolerance = doc.ModelAbsoluteTolerance;
                file3dm.Settings.ModelAngleToleranceRadians = doc.ModelAngleToleranceRadians;
                
                // Alle Objekte aus der Blockdefinition hinzufügen
                int objectCount = 0;
                foreach (var objRef in instanceDef.GetObjects())
                {
                    if (objRef != null)
                    {
                        var geometry = objRef.Geometry;
                        if (geometry != null)
                        {
                            // Erstelle eine tatsächliche Kopie des Objekts
                            var dupGeometry = geometry.Duplicate();
                            if (dupGeometry != null)
                            {
                                // Erstelle neue Attribute basierend auf dem Original aber mit eindeutiger Kopie
                                var attributes = objRef.Attributes.Duplicate();
                                file3dm.Objects.Add(dupGeometry, attributes);
                                objectCount++;
                            }
                            else
                            {
                                RhinoApp.WriteLine($"Warnung: Geometrie von {objRef.Id} konnte nicht dupliziert werden");
                            }
                        }
                        else
                        {
                            RhinoApp.WriteLine($"Warnung: Geometrie von {objRef.Id} ist null");
                        }
                    }
                }
                
                RhinoApp.WriteLine($"Füge {objectCount} Objekte in die exportierte Datei ein");
                
                // Speichere das Dokument
                bool saveResult = file3dm.Write(exportPath, 7); // Version 7 für Rhino 7
                
                if (saveResult)
                {
                    RhinoApp.WriteLine($"Blockdefinition wurde erfolgreich nach {exportPath} exportiert");
                    
                    // Überprüfe die Dateigröße
                    var fileInfo = new FileInfo(exportPath);
                    RhinoApp.WriteLine($"Exportierte Datei: {fileInfo.Length} Bytes");
                }
                else
                {
                    RhinoApp.WriteLine($"Fehler beim Speichern der Datei nach {exportPath}");
                }
                
                return saveResult;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren der Blockdefinition: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Importiert eine Blockdefinition aus einer 3DM-Datei
        /// </summary>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <param name="filePath">Der Pfad zur 3DM-Datei</param>
        /// <param name="blockName">Der Name des neuen Blocks (optional, falls null wird der Dateiname verwendet)</param>
        /// <returns>Die ID der neuen Blockdefinition oder -1 bei Fehler</returns>
        public int ImportBlockDefinition(RhinoDoc doc, string filePath, string blockName = null)
        {
            try
            {
                // Prüfe, ob die Datei existiert
                if (!File.Exists(filePath))
                {
                    RhinoApp.WriteLine($"Die Datei {filePath} existiert nicht.");
                    return -1;
                }
                
                // Lese die 3DM-Datei
                var file3dm = File3dm.Read(filePath);
                if (file3dm == null)
                {
                    RhinoApp.WriteLine($"Fehler beim Lesen der Datei {filePath}.");
                    return -1;
                }
                
                // Erstelle einen Namen für den Block, falls keiner angegeben wurde
                if (string.IsNullOrEmpty(blockName))
                {
                    blockName = Path.GetFileNameWithoutExtension(filePath);
                }
                
                // Sammle alle Geometrien aus der Datei
                var geometryList = new List<GeometryBase>();
                var attributesList = new List<ObjectAttributes>();
                
                foreach (var obj in file3dm.Objects)
                {
                    geometryList.Add(obj.Geometry);
                    attributesList.Add(obj.Attributes);
                }
                
                // Erstelle eine neue Blockdefinition
                var description = file3dm.Notes.Notes;
                int newBlockId = doc.InstanceDefinitions.Add(
                    blockName,
                    description,
                    Point3d.Origin,
                    geometryList,
                    attributesList
                );
                
                return newBlockId;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Importieren der Blockdefinition: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Fügt eine Blockinstanz in das Dokument ein
        /// </summary>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <param name="blockDefinitionId">Die ID der zu verwendenden Blockdefinition</param>
        /// <param name="insertionPoint">Der Einfügepunkt für die Blockinstanz</param>
        /// <returns>Die GUID des neu eingefügten Objekts oder Guid.Empty bei Fehler</returns>
        public Guid InsertBlockInstance(RhinoDoc doc, int blockDefinitionId, Point3d insertionPoint)
        {
            return InsertBlockInstance(doc, blockDefinitionId, insertionPoint, Transform.Translation(insertionPoint.X, insertionPoint.Y, insertionPoint.Z));
        }

        /// <summary>
        /// Fügt eine Blockinstanz in das Dokument ein
        /// </summary>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <param name="blockDefinitionId">Die ID der zu verwendenden Blockdefinition</param>
        /// <param name="insertionPoint">Der Einfügepunkt für die Blockinstanz</param>
        /// <param name="xform">Eine Transformation für die Blockinstanz</param>
        /// <returns>Die GUID des neu eingefügten Objekts oder Guid.Empty bei Fehler</returns>
        public Guid InsertBlockInstance(RhinoDoc doc, int blockDefinitionId, Point3d insertionPoint, Transform xform)
        {
            try
            {
                // Füge die Blockinstanz ein
                var instanceDef = doc.InstanceDefinitions[blockDefinitionId];
                if (instanceDef == null || instanceDef.IsDeleted)
                {
                    RhinoApp.WriteLine($"Blockdefinition mit ID {blockDefinitionId} konnte nicht gefunden werden.");
                    return Guid.Empty;
                }
                
                // Erstelle eine neue Blockinstanz und füge sie ein
                Guid objectId = doc.Objects.AddInstanceObject(instanceDef.Index, xform);
                
                if (objectId == Guid.Empty)
                {
                    RhinoApp.WriteLine("Fehler beim Hinzufügen der Blockinstanz zum Dokument");
                    return Guid.Empty;
                }
                
                // Setze Eigenschaften für das Objekt, falls nötig
                var rhinoObject = doc.Objects.Find(objectId);
                if (rhinoObject != null)
                {
                    rhinoObject.Attributes.Name = instanceDef.Name;
                    rhinoObject.CommitChanges();
                }
                
                // Aktualisiere die Ansicht
                doc.Views.Redraw();
                
                return objectId;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Einfügen der Blockinstanz: {ex.Message}");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Erstellt einen neuen Block aus ausgewählten Objekten
        /// </summary>
        /// <param name="doc">Das aktive Rhino-Dokument</param>
        /// <param name="objectIds">Die IDs der Objekte, die in den Block aufgenommen werden sollen</param>
        /// <param name="blockName">Der Name des neuen Blocks</param>
        /// <param name="basePoint">Der Basispunkt für den Block</param>
        /// <param name="deleteObjects">Gibt an, ob die Originalobjekte gelöscht werden sollen</param>
        /// <returns>Die ID der neuen Blockdefinition oder -1 bei Fehler</returns>
        public int CreateBlockFromObjects(RhinoDoc doc, IEnumerable<Guid> objectIds, string blockName, Point3d basePoint, bool deleteObjects = false)
        {
            try
            {
                // Prüfe, ob der Blockname gültig ist
                if (string.IsNullOrWhiteSpace(blockName))
                {
                    blockName = $"Block_{DateTime.Now:yyyyMMdd_HHmmss}";
                    RhinoApp.WriteLine($"Blockname war leer, verwende automatisch generierten Namen: {blockName}");
                }
                
                // Prüfe, ob der Name bereits vergeben ist
                bool nameExists = false;
                for (int i = 0; i < doc.InstanceDefinitions.Count; i++)
                {
                    if (doc.InstanceDefinitions[i].Name.Equals(blockName, StringComparison.OrdinalIgnoreCase))
                    {
                        nameExists = true;
                        break;
                    }
                }
                
                // Falls der Name schon existiert, füge eine Nummer hinzu
                if (nameExists)
                {
                    string originalName = blockName;
                    int counter = 1;
                    
                    // Versuche, einen eindeutigen Namen zu finden
                    do
                    {
                        blockName = $"{originalName}_{counter++}";
                        nameExists = false;
                        
                        for (int i = 0; i < doc.InstanceDefinitions.Count; i++)
                        {
                            if (doc.InstanceDefinitions[i].Name.Equals(blockName, StringComparison.OrdinalIgnoreCase))
                            {
                                nameExists = true;
                                break;
                            }
                        }
                    } 
                    while (nameExists && counter < 1000); // Sicherheitsabbruch
                    
                    RhinoApp.WriteLine($"Blockname '{originalName}' bereits vergeben, verwende stattdessen: {blockName}");
                }
                
                // Sammle die Objekte für den Block
                var guids = objectIds.ToArray();
                RhinoApp.WriteLine($"Erstelle Block '{blockName}' aus {guids.Length} Objekten");
                
                // Sammle die Geometrie- und Attribut-Objekte
                List<GeometryBase> geometryList = new List<GeometryBase>();
                List<ObjectAttributes> attributesList = new List<ObjectAttributes>();
                
                foreach (Guid id in guids)
                {
                    var rhinoObject = doc.Objects.Find(id);
                    if (rhinoObject != null)
                    {
                        var geometry = rhinoObject.Geometry;
                        if (geometry != null)
                        {
                            // Dupliziere die Geometrie für den Block
                            var geomCopy = geometry.Duplicate();
                            if (geomCopy != null)
                            {
                                geometryList.Add(geomCopy);
                                attributesList.Add(rhinoObject.Attributes);
                            }
                            else
                            {
                                RhinoApp.WriteLine($"Warnung: Geometrie für Objekt {id} konnte nicht dupliziert werden.");
                            }
                        }
                        else
                        {
                            RhinoApp.WriteLine($"Warnung: Geometrie für Objekt {id} ist null.");
                        }
                    }
                    else
                    {
                        RhinoApp.WriteLine($"Warnung: Objekt mit ID {id} konnte nicht gefunden werden.");
                    }
                }
                
                if (geometryList.Count == 0)
                {
                    RhinoApp.WriteLine("Fehler: Keine gültigen Objekte gefunden, um einen Block zu erstellen.");
                    return -1;
                }
                
                RhinoApp.WriteLine($"Erstelle Block mit {geometryList.Count} Objekten");
                
                // Erstelle den Block
                int blockDefinitionId = doc.InstanceDefinitions.Add(
                    blockName,
                    "Automatisch erstellter Block", // Beschreibung
                    basePoint,
                    geometryList,
                    attributesList
                );
                
                if (blockDefinitionId < 0)
                {
                    RhinoApp.WriteLine("Fehler: Block konnte nicht erstellt werden.");
                    return -1;
                }
                
                RhinoApp.WriteLine($"Block '{blockName}' erfolgreich erstellt mit ID {blockDefinitionId}");
                
                // Objekte löschen, falls gewünscht
                if (deleteObjects)
                {
                    foreach (Guid id in guids)
                    {
                        doc.Objects.Delete(id, true);
                    }
                }
                
                // Ansicht aktualisieren
                doc.Views.Redraw();
                
                return blockDefinitionId;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Erstellen des Blocks: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                }
                return -1;
            }
        }
    }

    /// <summary>
    /// Hilfsobjekt zur Darstellung von Blockdefinitionsinformationen
    /// </summary>
    public class BlockDefinitionInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ObjectCount { get; set; }
    }
} 