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
                
                // Erstelle ein neues temporäres 3dm-Dokument
                var file3dm = new File3dm();
                
                // Alle Objekte aus der Blockdefinition hinzufügen
                foreach (var objRef in instanceDef.GetObjects())
                {
                    var geometry = objRef.Geometry;
                    if (geometry != null)
                    {
                        var attributes = objRef.Attributes;
                        file3dm.Objects.Add(geometry, attributes);
                    }
                }
                
                // Füge Blockinformationen als Notiz hinzu
                file3dm.Notes.Notes = $"Blockdefinition: {instanceDef.Name}\nBeschreibung: {instanceDef.Description}";
                
                // Speichere das Dokument
                return file3dm.Write(exportPath, 7); // Version 7 für Rhino 7
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren der Blockdefinition: {ex.Message}");
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
                // Sammle die Objekte für den Block
                var guids = objectIds.ToArray();
                
                // Sammle die Geometrie- und Attribut-Objekte
                List<GeometryBase> geometryList = new List<GeometryBase>();
                List<ObjectAttributes> attributesList = new List<ObjectAttributes>();
                
                foreach (Guid id in guids)
                {
                    var rhinoObject = doc.Objects.Find(id);
                    if (rhinoObject != null)
                    {
                        geometryList.Add(rhinoObject.Geometry);
                        attributesList.Add(rhinoObject.Attributes);
                    }
                }
                
                // Erstelle einen neuen Block
                int definitionIndex = doc.InstanceDefinitions.Add(
                    blockName,
                    "", // Beschreibung
                    basePoint,
                    geometryList,
                    attributesList
                );
                
                if (definitionIndex < 0)
                {
                    RhinoApp.WriteLine("Fehler beim Erstellen des Blocks.");
                    return -1;
                }
                
                // Lösche die Original-Objekte, falls gewünscht
                if (deleteObjects)
                {
                    foreach (var id in guids)
                    {
                        doc.Objects.Delete(id, true);
                    }
                }
                
                return definitionIndex;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Erstellen des Blocks: {ex.Message}");
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