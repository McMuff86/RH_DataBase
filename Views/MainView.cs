using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using RH_DataBase.Models;
using RH_DataBase.Controllers;
using RH_DataBase.Services;

namespace RH_DataBase.Views
{
    public class MainView : Form
    {
        private PartsController _partsController;
        private TestDataController _testDataController;
        private GridView _partsGridView;
        private GridView _drawingsGridView;
        private TextBox _searchBox;
        private Button _refreshButton;
        private Button _insertButton;
        private Button _deleteButton;
        private Button _deleteDrawingButton;
        private Button _exportBlockButton;
        private Button _createBlockButton;
        private Button _importBlockButton;
        private Button _autoImportButton;
        private Button _testConnectionButton;
        private Button _addSampleDataButton;
        private Button _importDrawingButton;
        private Button _openDrawingButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        
        private List<Part> _parts = new List<Part>();
        private List<Drawing> _drawings = new List<Drawing>();
        
        public MainView()
        {
            Title = "Rhino-Supabase Parts Manager";
            MinimumSize = new Size(800, 600);
            Resizable = true;
            Maximizable = true;
            
            // Füge Close-Button hinzu
            var closeButton = new Button { Text = "Schließen" };
            closeButton.Click += (sender, e) => Close();
            
            _partsController = PartsController.Instance;
            _testDataController = TestDataController.Instance;
            
            InitializeComponents();
            
            // Layout der Benutzeroberfläche erstellen
            Content = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow(
                        new TableLayout
                        {
                            Padding = new Padding(5),
                            Rows =
                            {
                                new TableRow(new Label { Text = "Teilemanager", Font = new Eto.Drawing.Font(FontFamilies.Sans, 16, FontStyle.Bold) }),
                                new TableRow(new Label { Text = "Verbunden mit Supabase-Projekt" }),
                                new TableRow(
                                    new StackLayout
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Spacing = 5,
                                        Items =
                                        {
                                            new StackLayoutItem(_testConnectionButton, true),
                                            new StackLayoutItem(_addSampleDataButton, true)
                                        }
                                    }
                                ),
                                new TableRow(
                                    new StackLayout
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Spacing = 5,
                                        Items =
                                        {
                                            new Label { Text = "Suche:" },
                                            new StackLayoutItem(_searchBox, true),
                                            _refreshButton
                                        }
                                    }
                                )
                            }
                        }
                    ),
                    
                    // Teile-Grid mit Überschrift
                    new TableRow(new Label { Text = "Teile", Font = new Eto.Drawing.Font(FontFamilies.Sans, 14, FontStyle.Bold) }),
                    new TableRow(_partsGridView) { ScaleHeight = true },
                    new TableRow(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                _insertButton,
                                _deleteButton,
                                _exportBlockButton,
                                _createBlockButton,
                                _importBlockButton,
                                _autoImportButton
                            }
                        }
                    ),
                    
                    // Zeichnungen-Grid mit Überschrift
                    new TableRow(new Label { Text = "Zeichnungen", Font = new Eto.Drawing.Font(FontFamilies.Sans, 14, FontStyle.Bold) }),
                    new TableRow(_drawingsGridView) { ScaleHeight = true },
                    new TableRow(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                _importDrawingButton,
                                _openDrawingButton,
                                _deleteDrawingButton
                            }
                        }
                    ),
                    
                    // Statuszeile
                    new TableRow(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                new StackLayoutItem(_statusLabel, true),
                                new StackLayoutItem(_progressBar, true),
                                closeButton
                            }
                        }
                    )
                }
            };
            
            // Daten laden
            LoadDataAsync();
            
            // Prüfe nach fehlenden Dateien im Bucket
            Task.Run(async () =>
            {
                try
                {
                    // Warte kurz, bis die Daten geladen sind
                    await Task.Delay(2000);
                    
                    // Prüfe, ob Dateien fehlen
                    var missingFiles = await _partsController.GetFilesWithoutPartAsync();
                    
                    if (missingFiles.Count > 0)
                    {
                        // Informiere den Benutzer auf dem UI-Thread
                        await Application.Instance.InvokeAsync(() =>
                        {
                            var result = MessageBox.Show(
                                $"Es wurden {missingFiles.Count} Dateien im Bucket gefunden, die noch nicht als Teile in der Datenbank registriert sind. " +
                                $"Möchten Sie diese Dateien jetzt automatisch importieren?",
                                "Fehlende Teile gefunden",
                                MessageBoxButtons.YesNo,
                                MessageBoxType.Question
                            );
                            
                            if (result == DialogResult.Yes)
                            {
                                // Auto-Import starten
                                AutoImport();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler bei der Überprüfung auf fehlende Dateien: {ex.Message}");
                }
            });
            
            // Der Closed-Event-Handler wird jetzt im RH_DataBaseCommand verwaltet
        }
        
        private void InitializeComponents()
        {
            // Test-Buttons
            _testConnectionButton = new Button { Text = "Verbindung testen" };
            _testConnectionButton.Click += (sender, e) => TestConnectionAsync();
            
            _addSampleDataButton = new Button { Text = "Beispieldaten hinzufügen" };
            _addSampleDataButton.Click += (sender, e) => AddSampleDataAsync();
            
            // Such-Box
            _searchBox = new TextBox { PlaceholderText = "Teile nach Namen suchen..." };
            _searchBox.KeyDown += (sender, e) => {
                if (e.Key == Keys.Enter)
                {
                    SearchPartsAsync(_searchBox.Text);
                }
            };
            
            // Refresh-Button
            _refreshButton = new Button { Text = "Aktualisieren" };
            _refreshButton.Click += (sender, e) => LoadDataAsync();
            
            // Insert-Button
            _insertButton = new Button { Text = "Ausgewähltes Teil einfügen" };
            _insertButton.Click += (sender, e) => InsertSelectedPart();
            _insertButton.Enabled = false;
            
            // Löschen-Button
            _deleteButton = new Button { Text = "Ausgewähltes Teil löschen", ToolTip = "Löscht das ausgewählte Teil aus der Datenbank" };
            _deleteButton.Click += (sender, e) => DeleteSelectedPart();
            _deleteButton.Enabled = false;
            
            // Zeichnung-Löschen-Button
            _deleteDrawingButton = new Button { Text = "Zeichnung löschen", ToolTip = "Löscht die ausgewählte Zeichnung aus der Datenbank" };
            _deleteDrawingButton.Click += (sender, e) => DeleteSelectedDrawing();
            _deleteDrawingButton.Enabled = false;
            
            // Button zum Importieren einer Zeichnung
            _importDrawingButton = new Button { Text = "Zeichnung importieren", ToolTip = "Lädt eine Zeichnungsdatei in die Datenbank hoch" };
            _importDrawingButton.Click += (sender, e) => ImportDrawingFromFile();
            
            // Button zum Öffnen einer Zeichnung
            _openDrawingButton = new Button { Text = "Zeichnung öffnen", ToolTip = "Öffnet die ausgewählte Zeichnung in Rhino" };
            _openDrawingButton.Click += (sender, e) => OpenSelectedDrawing();
            _openDrawingButton.Enabled = false;
            
            // Block-Export-Button
            _exportBlockButton = new Button { Text = "Block exportieren", ToolTip = "Exportiert einen Block aus Rhino in die Datenbank" };
            _exportBlockButton.Click += (sender, e) => ExportBlockDefinition();
            
            // Block-Erstellen-Button
            _createBlockButton = new Button { Text = "Block aus Auswahl erstellen", ToolTip = "Erstellt einen Block aus ausgewählten Objekten und exportiert ihn in die Datenbank" };
            _createBlockButton.Click += (sender, e) => CreateBlockFromSelection();
            
            // Import-Block-Button
            _importBlockButton = new Button { Text = "Block importieren", ToolTip = "Lädt einen Block aus der Datenbank in Rhino" };
            _importBlockButton.Click += (sender, e) => ImportBlockToRhino();
            
            // Auto-Import-Button
            _autoImportButton = new Button { Text = "Auto-Import", ToolTip = "Importiert alle fehlenden Dateien automatisch" };
            _autoImportButton.Click += (sender, e) => AutoImport();
            
            // Status-Anzeigen
            _progressBar = new ProgressBar();
            _statusLabel = new Label { Text = "Bereit" };
            
            // Teile-GridView
            _partsGridView = new GridView
            {
                AllowMultipleSelection = false,
                AllowColumnReordering = true,
                AllowEmptySelection = true,
                GridLines = GridLines.Both // Zeige Rasterlinien für bessere Sichtbarkeit
            };
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "ID",
                DataCell = new TextBoxCell("Id"),
                Width = 50,
                Editable = false // ID kann nicht bearbeitet werden
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Name",
                DataCell = new TextBoxCell("Name"),
                Width = 200,
                Editable = true // Name kann bearbeitet werden
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Beschreibung",
                DataCell = new TextBoxCell("Description"),
                Width = 300,
                Editable = true // Beschreibung kann bearbeitet werden
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Kategorie",
                DataCell = new TextBoxCell("Category"),
                Width = 150,
                Editable = true // Kategorie kann bearbeitet werden
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Material",
                DataCell = new TextBoxCell("Material"),
                Width = 150,
                Editable = true // Material kann bearbeitet werden
            });
            
            _partsGridView.SelectionChanged += (sender, e) => UpdateDrawingsForSelectedPart();
            
            // Event-Handler für Zellenbearbeitung
            _partsGridView.CellEdited += async (sender, e) => {
                if (e.Item is Part part)
                {
                    try
                    {
                        _statusLabel.Text = $"Aktualisiere Teil {part.Name}...";
                        
                        // Aktualisiere den Teil in der Datenbank
                        await _partsController.UpdatePartAsync(part);
                        
                        _statusLabel.Text = $"Teil {part.Name} wurde aktualisiert";
                    }
                    catch (Exception ex)
                    {
                        _statusLabel.Text = $"Fehler beim Aktualisieren: {ex.Message}";
                        
                        MessageBox.Show(
                            $"Fehler beim Aktualisieren des Teils:\n{ex.Message}",
                            "Aktualisierungsfehler",
                            MessageBoxButtons.OK,
                            MessageBoxType.Error
                        );
                        
                        // Datenliste neu laden, um die ursprünglichen Werte wiederherzustellen
                        LoadDataAsync();
                    }
                }
            };
            
            // Zeichnungen-GridView
            _drawingsGridView = new GridView
            {
                AllowMultipleSelection = false,
                AllowColumnReordering = true,
                AllowEmptySelection = true
            };
            
            _drawingsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "ID",
                DataCell = new TextBoxCell("Id"),
                Width = 50
            });
            
            _drawingsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Titel",
                DataCell = new TextBoxCell("Title"),
                Width = 200
            });
            
            _drawingsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Zeichnungsnummer",
                DataCell = new TextBoxCell("DrawingNumber"),
                Width = 150
            });
            
            _drawingsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Revision",
                DataCell = new TextBoxCell("Revision"),
                Width = 100
            });
            
            _drawingsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Dateityp",
                DataCell = new TextBoxCell("FileType"),
                Width = 100
            });

            _drawingsGridView.SelectionChanged += (sender, e) => {
                // Aktiviere die Zeichnungs-bezogenen Buttons, wenn eine Zeichnung ausgewählt ist
                var selectionExists = _drawingsGridView.SelectedItem != null;
                _deleteDrawingButton.Enabled = selectionExists;
                _openDrawingButton.Enabled = selectionExists && (_drawingsGridView.SelectedItem as Drawing)?.FilePath != null;
            };
        }
        
        private void LoadDataAsync()
        {
            // UI-Thread aktualisieren
                _statusLabel.Text = "Lade Daten...";
                _progressBar.Indeterminate = true;
                
            // Starte den Prozess in einem separaten Thread
            Task.Run(async () => 
            {
                try
                {
                // Teile laden über den Controller
                    var parts = await _partsController.GetAllPartsAsync();
                    RhinoApp.WriteLine($"DEBUG: {parts.Count} Teile aus der Datenbank geladen.");
                    
                    if (parts.Count > 0)
                    {
                        RhinoApp.WriteLine($"DEBUG: Erstes Teil: Name={parts[0].Name}, ModelPath={parts[0].ModelPath}");
                    }
                    else
                    {
                        RhinoApp.WriteLine("DEBUG: Keine Teile in der Datenbank gefunden!");
                    }
                    
                    // Zeichnungen laden über den Controller
                    var drawings = await _partsController.GetAllDrawingsAsync();
                    RhinoApp.WriteLine($"DEBUG: {drawings.Count} Zeichnungen aus der Datenbank geladen.");
                    
                    // UI-Updates auf dem UI-Thread ausführen
                    await Application.Instance.InvokeAsync(() =>
                    {
                        _parts = parts;
                _partsGridView.DataStore = _parts;
                
                        _drawings = drawings;
                // Zeichnungen werden erst angezeigt, wenn ein Teil ausgewählt wird
                
                _statusLabel.Text = $"Bereit | {_parts.Count} Teile geladen";
                _progressBar.Indeterminate = false;
                    });
            }
            catch (Exception ex)
                {
                    // UI-Updates auf dem UI-Thread ausführen
                    await Application.Instance.InvokeAsync(() =>
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                _progressBar.Indeterminate = false;
                        
                        RhinoApp.WriteLine($"FEHLER beim Laden der Daten: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                        }
                
                MessageBox.Show(
                    "Fehler beim Laden der Daten aus Supabase:\n" + ex.Message,
                    "Datenbankfehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
                    });
                }
            });
        }
        
        private void TestConnectionAsync()
        {
            // Starte asynchronen Task, ohne auf ihn zu warten
            Task.Run(async () =>
            {
                try
                {
                    // UI-Updates auf dem UI-Thread ausführen
                    Application.Instance.Invoke(() =>
            {
                _statusLabel.Text = "Teste Datenbankverbindung...";
                _progressBar.Indeterminate = true;
                _testConnectionButton.Enabled = false;
                    });
                
                bool success = await _testDataController.TestConnectionAsync();
                
                    // UI-Updates auf dem UI-Thread ausführen
                    Application.Instance.Invoke(() =>
                    {
                if (success)
                {
                    _statusLabel.Text = "Verbindung zur Datenbank erfolgreich";
                    MessageBox.Show(
                        "Die Verbindung zur Supabase-Datenbank wurde erfolgreich hergestellt.",
                        "Verbindungstest",
                        MessageBoxButtons.OK,
                        MessageBoxType.Information
                    );
                }
                else
                {
                    _statusLabel.Text = "Verbindungsfehler";
                    MessageBox.Show(
                        "Die Verbindung zur Supabase-Datenbank konnte nicht hergestellt werden.",
                        "Verbindungstest",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                }
                        
                        _progressBar.Indeterminate = false;
                        _testConnectionButton.Enabled = true;
                    });
            }
            catch (Exception ex)
                {
                    // UI-Updates auf dem UI-Thread ausführen
                    Application.Instance.Invoke(() =>
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                MessageBox.Show(
                    "Fehler beim Testen der Verbindung:\n" + ex.Message,
                    "Verbindungsfehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
                        
                _progressBar.Indeterminate = false;
                _testConnectionButton.Enabled = true;
                    });
            }
            });
        }
        
        private void AddSampleDataAsync()
        {
            // UI-Thread aktualisieren
                _statusLabel.Text = "Füge Beispieldaten hinzu...";
                _progressBar.Indeterminate = true;
                _addSampleDataButton.Enabled = false;
                
            // Starte den Prozess in einem separaten Thread
            Task.Run(async () => 
            {
                try
                {
                bool success = await _testDataController.AddSampleDataAsync();
                
                    // UI-Updates auf dem UI-Thread ausführen
                    await Application.Instance.InvokeAsync(async () =>
                    {
                if (success)
                {
                    _statusLabel.Text = "Beispieldaten wurden hinzugefügt";
                    MessageBox.Show(
                        "Die Beispieldaten wurden erfolgreich zur Datenbank hinzugefügt.",
                        "Beispieldaten",
                        MessageBoxButtons.OK,
                        MessageBoxType.Information
                    );
                    
                    // Aktualisiere die Anzeige
                    await Task.Delay(500); // Kurz warten, damit die Daten in der Datenbank aktualisiert werden
                    LoadDataAsync();
                }
                else
                {
                    _statusLabel.Text = "Fehler beim Hinzufügen der Beispieldaten";
                    MessageBox.Show(
                        "Die Beispieldaten konnten nicht hinzugefügt werden.",
                        "Beispieldaten",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                }
                        
                        _progressBar.Indeterminate = false;
                        _addSampleDataButton.Enabled = true;
                    });
            }
            catch (Exception ex)
                {
                    // UI-Updates auf dem UI-Thread ausführen
                    Application.Instance.Invoke(() =>
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                MessageBox.Show(
                    "Fehler beim Hinzufügen der Beispieldaten:\n" + ex.Message,
                            "Datenfehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
                        
                _progressBar.Indeterminate = false;
                _addSampleDataButton.Enabled = true;
                    });
            }
            });
        }
        
        private async void SearchPartsAsync(string searchTerm)
        {
            try
            {
                _statusLabel.Text = "Suche...";
                _progressBar.Indeterminate = true;
                
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Bei leerem Suchbegriff alle Teile anzeigen
                    _parts = await _partsController.GetAllPartsAsync();
                }
                else
                {
                    // Nach Namen suchen über den Controller
                    _parts = await _partsController.SearchPartsByNameAsync(searchTerm);
                }
                
                _partsGridView.DataStore = _parts;
                _statusLabel.Text = $"Bereit | {_parts.Count} Teile gefunden";
                _progressBar.Indeterminate = false;
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                _progressBar.Indeterminate = false;
                
                MessageBox.Show(
                    "Fehler bei der Suche:\n" + ex.Message,
                    "Suchfehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
            }
        }
        
        private void UpdateDrawingsForSelectedPart()
        {
            if (_partsGridView.SelectedItem is Part selectedPart)
            {
                // Zeichnungen für das ausgewählte Teil anzeigen
                var partDrawings = _drawings.Where(d => d.PartId == selectedPart.Id).ToList();
                _drawingsGridView.DataStore = partDrawings;
                
                _statusLabel.Text = $"Teil {selectedPart.Name} ausgewählt | {partDrawings.Count} Zeichnungen";
                _insertButton.Enabled = true;
                _deleteButton.Enabled = true;
            }
            else
            {
                _drawingsGridView.DataStore = null;
                _insertButton.Enabled = false;
                _deleteButton.Enabled = false;
            }
        }
        
        private void InsertSelectedPart()
        {
            if (_partsGridView.SelectedItem is Part selectedPart)
            {
                // UI-Thread aktualisieren
                _statusLabel.Text = $"Füge Teil {selectedPart.Name} ein...";
                _insertButton.Enabled = false;
                
                // Prüfe, ob ein aktives Dokument vorhanden ist
                var doc = RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    MessageBox.Show(
                        "Es ist kein aktives Rhino-Dokument geöffnet.",
                        "Fehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                    _statusLabel.Text = "Bereit";
                    _insertButton.Enabled = true;
                    return;
                }
                
                // Phase 1: Datei herunterladen und Block vorbereiten (im Hintergrund)
                Task.Run(async () =>
                {
                    try
                    {
                        // Verwende die neue Vorbereitungsmethode
                        var result = await _partsController.PreparePartInsertionAsync(selectedPart, doc);
                        int blockId = result.blockId;
                        string tempFilePath = result.tempFilePath;
                        
                        // Phase 2: Benutzerinteraktion und Einfügen (im UI-Thread)
                        // Wichtig: Dieser Teil muss im UI-Thread laufen!
                        await Application.Instance.InvokeAsync(() =>
                        {
                            try
                            {
                                _statusLabel.Text = $"Wählen Sie den Einfügepunkt für {selectedPart.Name}...";
                                
                                // Diese Methode enthält die Benutzerinteraktion und muss im UI-Thread laufen
                                bool success = _partsController.CompletePartInsertion(selectedPart, doc, blockId);
                                
                    if (success)
                    {
                        _statusLabel.Text = $"Teil {selectedPart.Name} erfolgreich eingefügt";
                        
                        MessageBox.Show(
                            $"Teil {selectedPart.Name} wurde erfolgreich in das aktive Rhino-Dokument eingefügt.",
                            "Teil eingefügt",
                            MessageBoxButtons.OK,
                            MessageBoxType.Information
                        );
                    }
                    else
                    {
                        _statusLabel.Text = $"Fehler beim Einfügen von Teil {selectedPart.Name}";
                    }
                            
                            _insertButton.Enabled = true;
                            }
                            catch (Exception ex)
                            {
                                _statusLabel.Text = $"Fehler beim Einfügen: {ex.Message}";
                                
                                MessageBox.Show(
                                    "Fehler beim Einfügen des Teils:\n" + ex.Message,
                                    "Einfügefehler",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Error
                                );
                                
                                _insertButton.Enabled = true;
                            }
                        });
                }
                catch (Exception ex)
                    {
                        // UI-Updates auf dem UI-Thread ausführen
                        await Application.Instance.InvokeAsync(() =>
                {
                    _statusLabel.Text = $"Fehler beim Einfügen: {ex.Message}";
                    
                    MessageBox.Show(
                        "Fehler beim Einfügen des Teils:\n" + ex.Message,
                        "Einfügefehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                            
                            _insertButton.Enabled = true;
                        });
                    }
                });
            }
        }
        
        private void DeleteSelectedPart()
        {
            if (_partsGridView.SelectedItem is Part selectedPart)
            {
                // Prüfen, ob Zeichnungen für dieses Teil existieren
                var relatedDrawings = _drawings.Where(d => d.PartId == selectedPart.Id).ToList();
                string warningMessage = $"Möchten Sie das Teil '{selectedPart.Name}' wirklich aus der Datenbank löschen?";
                
                if (relatedDrawings.Count > 0)
                {
                    warningMessage += $"\n\nDas Teil hat {relatedDrawings.Count} zugehörige Zeichnung(en), die ebenfalls gelöscht werden.";
                }
                
                warningMessage += "\n\nDiese Aktion kann nicht rückgängig gemacht werden.";
                
                // Bestätigungsdialog anzeigen
                var result = MessageBox.Show(
                    warningMessage,
                    "Teil löschen",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Warning
                );
                
                if (result == DialogResult.Yes)
                {
                    // UI-Thread aktualisieren
                    _statusLabel.Text = $"Lösche Teil {selectedPart.Name}...";
                    _deleteButton.Enabled = false;
                    
                    // Starte den Löschprozess in einem separaten Thread
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Zuerst alle zugehörigen Zeichnungen löschen
                            if (relatedDrawings.Count > 0)
                            {
                                foreach (var drawing in relatedDrawings)
                                {
                                    await _partsController.DeleteDrawingAsync(drawing.Id);
                                }
                            }
                            
                            // Dann das Teil löschen
                            await _partsController.DeletePartAsync(selectedPart.Id);
                            
                            // UI-Updates auf dem UI-Thread ausführen
                            await Application.Instance.InvokeAsync(() =>
                            {
                                _statusLabel.Text = $"Teil {selectedPart.Name} wurde gelöscht";
                                
                                // Aktualisiere die Datenliste
                                LoadDataAsync();
                            });
                        }
                        catch (Exception ex)
                        {
                            // UI-Updates auf dem UI-Thread ausführen
                            Application.Instance.Invoke(() =>
                            {
                                _statusLabel.Text = $"Fehler beim Löschen: {ex.Message}";
                                
                                MessageBox.Show(
                                    $"Fehler beim Löschen des Teils:\n{ex.Message}",
                                    "Löschfehler",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Error
                                );
                                
                                _deleteButton.Enabled = true;
                            });
                        }
                    });
                }
            }
        }

        // Neue Methode zum Löschen der ausgewählten Zeichnung
        private void DeleteSelectedDrawing()
        {
            if (_drawingsGridView.SelectedItem is Drawing selectedDrawing)
            {
                // Bestätigungsdialog anzeigen
                var result = MessageBox.Show(
                    $"Möchten Sie die Zeichnung '{selectedDrawing.Title}' wirklich aus der Datenbank löschen?\n\nDiese Aktion kann nicht rückgängig gemacht werden.",
                    "Zeichnung löschen",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Warning
                );
                
                if (result == DialogResult.Yes)
                {
                    // UI-Thread aktualisieren
                    _statusLabel.Text = $"Lösche Zeichnung {selectedDrawing.Title}...";
                    _deleteDrawingButton.Enabled = false;
                    
                    // Starte den Löschprozess in einem separaten Thread
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Zeichnung löschen
                            await _partsController.DeleteDrawingAsync(selectedDrawing.Id);
                            
                            // UI-Updates auf dem UI-Thread ausführen
                            await Application.Instance.InvokeAsync(() =>
                            {
                                _statusLabel.Text = $"Zeichnung {selectedDrawing.Title} wurde gelöscht";
                                
                                // Aktualisiere nur die Zeichnungsliste
                                if (_partsGridView.SelectedItem is Part selectedPart)
                                {
                                    // Lade alle Zeichnungen neu
                                    Task.Run(async () => {
                                        var drawings = await _partsController.GetAllDrawingsAsync();
                                        
                                        await Application.Instance.InvokeAsync(() => {
                                            _drawings = drawings;
                                            UpdateDrawingsForSelectedPart();
                                        });
                                    });
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // UI-Updates auf dem UI-Thread ausführen
                            Application.Instance.Invoke(() =>
                            {
                                _statusLabel.Text = $"Fehler beim Löschen: {ex.Message}";
                                
                                MessageBox.Show(
                                    $"Fehler beim Löschen der Zeichnung:\n{ex.Message}",
                                    "Löschfehler",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Error
                                );
                                
                                _deleteDrawingButton.Enabled = true;
                            });
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Aktualisiert die Daten in der Ansicht
        /// </summary>
        public void RefreshData()
        {
            try
            {
                // Aktualisiere Status und UI
                _statusLabel.Text = "Aktualisiere Daten...";
                _progressBar.Indeterminate = true;
                
                // Lade Daten neu
                LoadDataAsync();
                
                RhinoApp.WriteLine("Daten wurden aktualisiert.");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Aktualisieren der Daten: {ex.Message}");
                _statusLabel.Text = "Fehler beim Aktualisieren der Daten.";
            }
        }

        // Neue Methode zum Exportieren eines Blocks
        private void ExportBlockDefinition()
        {
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    MessageBox.Show(
                        "Es ist kein aktives Rhino-Dokument geöffnet.",
                        "Fehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                    return;
                }

                // Hole alle vorhandenen Blockdefinitionen
                var blockDefinitions = _partsController.GetBlockDefinitions(doc);
                if (blockDefinitions.Count == 0)
                {
                    MessageBox.Show(
                        "Es sind keine Blockdefinitionen im aktuellen Dokument vorhanden.",
                        "Keine Blöcke",
                        MessageBoxButtons.OK,
                        MessageBoxType.Warning
                    );
                    return;
                }

                // Erstelle einen Dialog zur Auswahl eines Blocks
                var dialog = new Dialog
                {
                    Title = "Block exportieren",
                    MinimumSize = new Size(500, 400)
                };

                // Dropdown für Blockdefinitionen
                var blockDropDown = new DropDown();
                foreach (var blockDef in blockDefinitions)
                {
                    blockDropDown.Items.Add(new ListItem { Text = $"{blockDef.Name} ({blockDef.ObjectCount} Objekte)", Tag = blockDef });
                }
                blockDropDown.SelectedIndex = 0;

                // Eingabefelder für Teil-Metadaten
                var nameTextBox = new TextBox { Text = blockDefinitions[0].Name };
                var descriptionTextBox = new TextBox();
                var categoryTextBox = new TextBox();
                var materialTextBox = new TextBox();

                // Aktualisiere den Namen, wenn ein anderer Block ausgewählt wird
                blockDropDown.SelectedIndexChanged += (sender, e) => {
                    if (blockDropDown.SelectedIndex >= 0 && blockDropDown.SelectedIndex < blockDefinitions.Count) {
                        var selectedDef = blockDefinitions[blockDropDown.SelectedIndex];
                        // Aktualisiere das Namensfeld nur, wenn es noch den ursprünglichen Wert hat oder leer ist
                        if (nameTextBox.Text == "" || 
                            (blockDropDown.SelectedIndex > 0 && nameTextBox.Text == blockDefinitions[blockDropDown.SelectedIndex - 1].Name)) {
                            nameTextBox.Text = selectedDef.Name;
                        }
                    }
                };

                // OK und Abbrechen Buttons
                var okButton = new Button { Text = "Exportieren" };
                var cancelButton = new Button { Text = "Abbrechen" };

                cancelButton.Click += (sender, e) => dialog.Close();
                okButton.Click += async (sender, e) => 
                {
                    try
                    {
                        dialog.Close();
                        
                        // Sicherer Zugriff auf das ausgewählte Element
                        BlockDefinitionInfo selectedBlockDef = null;
                        if (blockDropDown.SelectedIndex >= 0 && blockDropDown.SelectedIndex < blockDefinitions.Count)
                        {
                            selectedBlockDef = blockDefinitions[blockDropDown.SelectedIndex];
                        }
                        
                        if (selectedBlockDef == null)
                        {
                            MessageBox.Show(
                                "Bitte wählen Sie einen Block aus.",
                                "Fehlende Auswahl",
                                MessageBoxButtons.OK,
                                MessageBoxType.Warning
                            );
                            return;
                        }
                        
                        string name = nameTextBox.Text.Trim();
                        string description = descriptionTextBox.Text.Trim();
                        string category = categoryTextBox.Text.Trim();
                        string material = materialTextBox.Text.Trim();
                        
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            // Wenn kein Name eingegeben wurde, verwende den Blocknamen
                            name = selectedBlockDef.Name;
                            RhinoApp.WriteLine($"Kein Name eingegeben, verwende Blocknamen: {name}");
                        }
                        
                        // UI aktualisieren
                        _statusLabel.Text = $"Exportiere Block {selectedBlockDef.Name}...";
                        _progressBar.Indeterminate = true;
                        
                        // Exportieren in separatem Thread
                        await Task.Run(async () =>
                        {
                            try
                            {
                                // Exportiere den Block
                                var savedPart = await _partsController.ExportBlockDefinitionAsync(
                                    doc,
                                    selectedBlockDef.Id,
                                    name,
                                    description,
                                    category,
                                    material
                                );
                                
                                // UI-Updates auf dem UI-Thread ausführen
                                await Application.Instance.InvokeAsync(() =>
                                {
                                    _statusLabel.Text = $"Block {selectedBlockDef.Name} erfolgreich exportiert";
                                    _progressBar.Indeterminate = false;
                                    
                                    MessageBox.Show(
                                        $"Block {selectedBlockDef.Name} wurde erfolgreich als Teil exportiert.",
                                        "Export erfolgreich",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Information
                                    );
                                    
                                    // Aktualisiere die Teile-Liste
                                    LoadDataAsync();
                                });
                            }
                            catch (Exception ex)
                            {
                                // UI-Updates auf dem UI-Thread ausführen
                                Application.Instance.Invoke(() =>
                                {
                                    _statusLabel.Text = $"Fehler beim Exportieren: {ex.Message}";
                                    _progressBar.Indeterminate = false;
                                    
                                    MessageBox.Show(
                                        $"Fehler beim Exportieren des Blocks:\n{ex.Message}",
                                        "Exportfehler",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Error
                                    );
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Fehler: {ex.Message}",
                            "Fehler",
                            MessageBoxButtons.OK,
                            MessageBoxType.Error
                        );
                    }
                };

                // Dialog-Layout erstellen
                dialog.Content = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(5, 5),
                    Rows =
                    {
                        new TableRow(new Label { Text = "Block auswählen:" }),
                        new TableRow(blockDropDown),
                        new TableRow(new Label { Text = "Name: (Standardmäßig wird der Blockname verwendet)" }),
                        new TableRow(nameTextBox),
                        new TableRow(new Label { Text = "Beschreibung:" }),
                        new TableRow(descriptionTextBox),
                        new TableRow(new Label { Text = "Kategorie:" }),
                        new TableRow(categoryTextBox),
                        new TableRow(new Label { Text = "Material:" }),
                        new TableRow(materialTextBox),
                        null, // Abstand
                        new TableRow(
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 5,
                                Items = { null, okButton, cancelButton }
                            }
                        )
                    }
                };

                dialog.ShowModal(this);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                
                MessageBox.Show(
                    $"Fehler: {ex.Message}",
                    "Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
            }
        }

        // Neue Methode zum Erstellen eines Blocks aus ausgewählten Objekten
        private async void CreateBlockFromSelection()
        {
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    MessageBox.Show(
                        "Es ist kein aktives Rhino-Dokument geöffnet.",
                        "Fehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                    return;
                }

                // Hole ausgewählte Objekte
                var selectedObjects = doc.Objects.GetSelectedObjects(false, false);
                
                // Zähle die Objekte sicher
                int objectCount = 0;
                if (selectedObjects != null)
                {
                    objectCount = selectedObjects.Count();
                }
                
                if (objectCount == 0)
                {
                    MessageBox.Show(
                        "Bitte wählen Sie mindestens ein Objekt in Rhino aus.",
                        "Keine Auswahl",
                        MessageBoxButtons.OK,
                        MessageBoxType.Warning
                    );
                    return;
                }

                RhinoApp.WriteLine($"Ausgewählte Objekte: {objectCount}");

                // Sammle Object-IDs
                var objectIds = new List<Guid>();
                foreach (var obj in selectedObjects)
                {
                    objectIds.Add(obj.Id);
                    RhinoApp.WriteLine($"  - Objekt hinzugefügt: {obj.Id} ({obj.Geometry?.GetType().Name ?? "unbekannt"})");
                }

                // Erstelle einen Dialog für die Block-Erstellung
                var dialog = new Dialog
                {
                    Title = "Block aus Auswahl erstellen",
                    MinimumSize = new Size(500, 400),
                    Padding = new Padding(10)
                };

                // Beschreibungstext
                var descriptionPanel = new Panel();
                var descriptionLabel = new Label
                {
                    Text = $"Sie erstellen einen Block aus {objectCount} ausgewählten Objekten." +
                           $"\nBitte geben Sie die Metadaten für den Block ein."
                };
                descriptionPanel.Content = descriptionLabel;

                // Eingabefelder für Block-Metadaten mit Labels
                var nameLabel = new Label { Text = "Name:" };
                var nameTextBox = new TextBox { PlaceholderText = "Name des Blocks" };
                
                var descriptionTextLabel = new Label { Text = "Beschreibung:" };
                var descriptionTextBox = new TextBox { PlaceholderText = "Beschreibung des Blocks" };
                
                var categoryLabel = new Label { Text = "Kategorie:" };
                var categoryTextBox = new TextBox { PlaceholderText = "z.B. Möbel, Armaturen, etc." };
                
                var materialLabel = new Label { Text = "Material:" };
                var materialTextBox = new TextBox { PlaceholderText = "z.B. Holz, Metall, etc." };

                // OK und Abbrechen Buttons
                var okButton = new Button { Text = "Erstellen" };
                var cancelButton = new Button { Text = "Abbrechen" };

                // Layout erstellen
                var layout = new TableLayout
                {
                    Padding = new Padding(10),
                    Spacing = new Size(5, 10),
                    Rows =
                    {
                        new TableRow(descriptionPanel),
                        new TableRow(nameLabel),
                        new TableRow(nameTextBox),
                        new TableRow(descriptionTextLabel),
                        new TableRow(descriptionTextBox),
                        new TableRow(categoryLabel),
                        new TableRow(categoryTextBox),
                        new TableRow(materialLabel),
                        new TableRow(materialTextBox),
                        new TableRow(
                            new TableLayout
                            {
                                Padding = new Padding(0, 10, 0, 0),
                                Spacing = new Size(5, 0),
                                Rows = { new TableRow(null, cancelButton, okButton) }
                            }
                        )
                    }
                };

                dialog.Content = layout;
                
                // Event-Handler
                cancelButton.Click += (sender, e) => 
                {
                    RhinoApp.WriteLine("Block-Erstellung abgebrochen");
                    dialog.Close();
                };
                
                okButton.Click += async (sender, e) => 
                {
                    try
                    {
                        string name = nameTextBox.Text.Trim();
                        string description = descriptionTextBox.Text.Trim();
                        string category = categoryTextBox.Text.Trim();
                        string material = materialTextBox.Text.Trim();
                        
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            MessageBox.Show(
                                "Bitte geben Sie einen Namen für den Block an.",
                                "Fehlende Daten",
                                MessageBoxButtons.OK,
                                MessageBoxType.Warning
                            );
                            return;
                        }
                        
                        // Dialog schließen, bevor wir mit der Abfrage fortfahren
                        dialog.Close();
                        
                        // Frage nach dem Basispunkt für den Block
                        RhinoApp.WriteLine("Wählen Sie den Basispunkt für den Block:");
                        Point3d basePoint;
                        var getBasePointResult = Rhino.Input.RhinoGet.GetPoint("Basispunkt für den Block wählen", false, out basePoint);
                        if (getBasePointResult != Rhino.Commands.Result.Success)
                        {
                            RhinoApp.WriteLine("Block-Erstellung abgebrochen: Kein Basispunkt ausgewählt");
                            _statusLabel.Text = "Block-Erstellung abgebrochen";
                            return;
                        }
                        
                        RhinoApp.WriteLine($"Basispunkt ausgewählt: {basePoint}");
                        
                        // UI aktualisieren
                        _statusLabel.Text = $"Erstelle Block '{name}'...";
                        _progressBar.Indeterminate = true;
                        
                        // Block erstellen und in die Datenbank speichern
                                var savedPart = await _partsController.CreateBlockFromSelectionAsync(
                                    doc,
                                    objectIds,
                                    name,
                                    description,
                                    category,
                                    material,
                                    basePoint
                                );
                                
                                // UI-Updates auf dem UI-Thread ausführen
                                await Application.Instance.InvokeAsync(() =>
                                {
                                    _progressBar.Indeterminate = false;
                                    
                            if (savedPart != null)
                            {
                                _statusLabel.Text = $"Block '{name}' erfolgreich erstellt und gespeichert";
                                RhinoApp.WriteLine($"Block '{name}' erfolgreich erstellt und in Datenbank gespeichert");
                                
                                // Aktualisiere die Teileliste
                                LoadDataAsync();
                            }
                            else
                            {
                                _statusLabel.Text = "Fehler beim Erstellen des Blocks";
                                    MessageBox.Show(
                                    "Der Block wurde erstellt, konnte aber nicht in der Datenbank gespeichert werden.",
                                    "Fehler beim Speichern",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Error
                                );
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _progressBar.Indeterminate = false;
                        _statusLabel.Text = "Fehler bei der Block-Erstellung";
                        
                        RhinoApp.WriteLine($"Fehler bei der Block-Erstellung: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                        }
                        
                        MessageBox.Show(
                            $"Fehler beim Erstellen des Blocks: {ex.Message}",
                            "Fehler",
                            MessageBoxButtons.OK,
                            MessageBoxType.Error
                        );
                    }
                };
                
                // Dialog anzeigen
                dialog.ShowModal(this);
            }
            catch (Exception ex)
            {
                _progressBar.Indeterminate = false;
                _statusLabel.Text = "Fehler bei der Block-Erstellung";
                
                RhinoApp.WriteLine($"Fehler bei der Block-Erstellung: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Details: {ex.InnerException.Message}");
                }
                
                MessageBox.Show(
                    $"Fehler beim Erstellen des Blocks: {ex.Message}",
                    "Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
            }
        }

        // Methode zum Importieren einer Zeichnung aus einer Datei
        private void ImportDrawingFromFile()
        {
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    MessageBox.Show(
                        "Es ist kein aktives Rhino-Dokument geöffnet.",
                        "Fehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                    return;
                }

                // Prüfe, ob ein Teil ausgewählt ist (optional)
                Part selectedPart = null;
                if (_partsGridView.SelectedItem is Part part)
                {
                    selectedPart = part;
                }

                // Erstelle einen Dialog für den Datei-Import
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Zeichnungsdatei auswählen",
                    Filters = { new FileFilter("Rhino-Dateien", "*.3dm") },
                    MultiSelect = false
                };

                if (openFileDialog.ShowDialog(this) == DialogResult.Ok)
                {
                    string filePath = openFileDialog.FileName;

                    // Erstelle einen Dialog für die Zeichnungs-Metadaten
                    var dialog = new Dialog
                    {
                        Title = "Zeichnung importieren",
                        MinimumSize = new Size(500, 400),
                        Padding = new Padding(10)
                    };

                    // Eingabefelder für Zeichnungs-Metadaten
                    var titleLabel = new Label { Text = "Titel:" };
                    var titleTextBox = new TextBox { Text = Path.GetFileNameWithoutExtension(filePath) };
                    
                    var drawingNumberLabel = new Label { Text = "Zeichnungsnummer:" };
                    var drawingNumberTextBox = new TextBox { PlaceholderText = "z.B. Z-1001" };
                    
                    var revisionLabel = new Label { Text = "Revision:" };
                    var revisionTextBox = new TextBox { Text = "A", PlaceholderText = "z.B. A, 1.0, etc." };
                    
                    // Dropdown für Teil-Zuordnung (falls vorhanden)
                    var partLabel = new Label { Text = "Zugehöriges Teil (optional):" };
                    var partDropDown = new DropDown();
                    var partsList = new List<Part> { null }; // Der erste Eintrag ist null für "Kein Teil"
                    
                    // Füge eine leere Option hinzu
                    partDropDown.Items.Add("-- Kein Teil --");
                    
                    // Füge alle verfügbaren Teile hinzu
                    foreach (var p in _parts)
                    {
                        partsList.Add(p);
                        partDropDown.Items.Add($"{p.Name} (ID: {p.Id})");
                    }
                    
                    // Wenn ein Teil ausgewählt ist, wähle es auch im Dropdown
                    if (selectedPart != null)
                    {
                        for (int i = 1; i < partsList.Count; i++) // Index 0 überspringen (Kein Teil)
                        {
                            if (partsList[i]?.Id == selectedPart.Id)
                            {
                                partDropDown.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        partDropDown.SelectedIndex = 0; // Standardmäßig "Kein Teil" auswählen
                    }

                    // OK und Abbrechen Buttons
                    var okButton = new Button { Text = "Importieren" };
                    var cancelButton = new Button { Text = "Abbrechen" };

                    cancelButton.Click += (sender, e) => dialog.Close();
                    okButton.Click += async (sender, e) => 
                    {
                        try
                        {
                            string title = titleTextBox.Text.Trim();
                            string drawingNumber = drawingNumberTextBox.Text.Trim();
                            string revision = revisionTextBox.Text.Trim();
                            
                            if (string.IsNullOrWhiteSpace(title))
                            {
                                MessageBox.Show(
                                    "Bitte geben Sie einen Titel für die Zeichnung an.",
                                    "Fehlende Daten",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Warning
                                );
                                return;
                            }
                            
                            if (string.IsNullOrWhiteSpace(drawingNumber))
                            {
                                MessageBox.Show(
                                    "Bitte geben Sie eine Zeichnungsnummer an.",
                                    "Fehlende Daten",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Warning
                                );
                                return;
                            }
                            
                            // Schließe den Dialog
                            dialog.Close();
                            
                            // UI aktualisieren
                            _statusLabel.Text = $"Importiere Zeichnung {title}...";
                            _progressBar.Indeterminate = true;
                            
                            // Bestimme die Teil-ID, falls ein Teil ausgewählt wurde
                            int? partId = null;
                            if (partDropDown.SelectedIndex > 0)
                            {
                                Part selectedPartFromDropdown = partsList[partDropDown.SelectedIndex];
                                if (selectedPartFromDropdown != null)
                                {
                                    partId = selectedPartFromDropdown.Id;
                                }
                            }
                            
                            // Importiere die Zeichnung
                            await Task.Run(async () => 
                            {
                                try
                                {
                                    var savedDrawing = await _partsController.ImportDrawingAsync(
                                        filePath,
                                        title,
                                        drawingNumber,
                                        revision,
                                        partId
                                    );
                                    
                                    // UI-Updates auf dem UI-Thread ausführen
                                    await Application.Instance.InvokeAsync(() => 
                                    {
                                        _progressBar.Indeterminate = false;
                                        _statusLabel.Text = $"Zeichnung {title} erfolgreich importiert";
                                        
                                        MessageBox.Show(
                                            $"Zeichnung {title} wurde erfolgreich in die Datenbank importiert.",
                                            "Import erfolgreich",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Information
                                    );
                                    
                                        // Aktualisiere die Datenlisten
                                    LoadDataAsync();
                                });
                            }
                            catch (Exception ex)
                            {
                                // UI-Updates auf dem UI-Thread ausführen
                                    await Application.Instance.InvokeAsync(() => 
                                {
                                    _progressBar.Indeterminate = false;
                                        _statusLabel.Text = $"Fehler beim Importieren: {ex.Message}";
                                    
                                    MessageBox.Show(
                                            $"Fehler beim Importieren der Zeichnung:\n{ex.Message}",
                                            "Importfehler",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Error
                                    );
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Fehler: {ex.Message}",
                            "Fehler",
                            MessageBoxButtons.OK,
                            MessageBoxType.Error
                        );
                    }
                };

                // Dialog-Layout erstellen
                dialog.Content = new TableLayout
                {
                    Padding = new Padding(10),
                        Spacing = new Size(5, 10),
                    Rows =
                    {
                            new TableRow(titleLabel),
                            new TableRow(titleTextBox),
                            new TableRow(drawingNumberLabel),
                            new TableRow(drawingNumberTextBox),
                            new TableRow(revisionLabel),
                            new TableRow(revisionTextBox),
                            new TableRow(partLabel),
                            new TableRow(partDropDown),
                            new TableRow(
                                new TableLayout
                                {
                                    Padding = new Padding(0, 10, 0, 0),
                                    Spacing = new Size(5, 0),
                                    Rows = { new TableRow(null, cancelButton, okButton) }
                                }
                            )
                        }
                    };

                    dialog.ShowModal(this);
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                
                MessageBox.Show(
                    $"Fehler: {ex.Message}",
                    "Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
            }
        }

        // Methode zum Öffnen einer ausgewählten Zeichnung
        private void OpenSelectedDrawing()
        {
            if (_drawingsGridView.SelectedItem is Drawing selectedDrawing)
            {
                var doc = RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    MessageBox.Show(
                        "Es ist kein aktives Rhino-Dokument geöffnet.",
                        "Fehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                    return;
                }

                // UI aktualisieren
                _statusLabel.Text = $"Öffne Zeichnung {selectedDrawing.Title}...";
                _progressBar.Indeterminate = true;
                _openDrawingButton.Enabled = false;
                
                // Phase 1: Vorbereitung im Hintergrund-Thread
                Task.Run(async () => 
                {
                    try
                    {
                        // Verwende die neue Vorbereitungsmethode
                        var result = await _partsController.PrepareDrawingImportAsync(selectedDrawing, doc);
                        int blockId = result.blockId;
                        string tempFilePath = result.tempFilePath;
                        
                        // Phase 2: UI-Interaktion im UI-Thread
                        await Application.Instance.InvokeAsync(() => 
                        {
                            try
                            {
                                _statusLabel.Text = $"Wählen Sie den Einfügepunkt für {selectedDrawing.Title}...";
                                
                                // Diese Methode enthält die Benutzerinteraktion und muss im UI-Thread laufen
                                bool success = _partsController.CompleteDrawingImport(selectedDrawing, doc, blockId);
                                
                                _progressBar.Indeterminate = false;
                                _openDrawingButton.Enabled = true;
                                
                                if (success)
                                {
                                    _statusLabel.Text = $"Zeichnung {selectedDrawing.Title} erfolgreich geöffnet";
                                }
                                else
                                {
                                    _statusLabel.Text = "Fehler beim Öffnen der Zeichnung";
                                    
                                    MessageBox.Show(
                                        "Die Zeichnung konnte nicht in Rhino geöffnet werden.",
                                        "Öffnungsfehler",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Error
                                    );
                                }
                            }
                            catch (Exception ex)
                            {
                                _progressBar.Indeterminate = false;
                                _openDrawingButton.Enabled = true;
                                _statusLabel.Text = $"Fehler: {ex.Message}";
                                
                                MessageBox.Show(
                                    $"Fehler beim Öffnen der Zeichnung:\n{ex.Message}",
                                    "Öffnungsfehler",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Error
                                );
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        // UI-Updates auf dem UI-Thread ausführen
                        await Application.Instance.InvokeAsync(() => 
                        {
                            _progressBar.Indeterminate = false;
                            _openDrawingButton.Enabled = true;
                            _statusLabel.Text = $"Fehler: {ex.Message}";
                            
                            MessageBox.Show(
                                $"Fehler beim Öffnen der Zeichnung:\n{ex.Message}",
                                "Öffnungsfehler",
                                MessageBoxButtons.OK,
                                MessageBoxType.Error
                            );
                        });
                    }
                });
            }
        }

        // Neue Methode zum Importieren eines Blocks aus dem Supabase Bucket
        private void ImportBlockToRhino()
        {
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    MessageBox.Show(
                        "Es ist kein aktives Rhino-Dokument geöffnet.",
                        "Fehler",
                        MessageBoxButtons.OK,
                        MessageBoxType.Error
                    );
                    return;
                }

                // UI aktualisieren
                _statusLabel.Text = "Lade Dateien aus dem Bucket...";
                _progressBar.Indeterminate = true;

                // Hole alle Dateien aus dem Bucket in einem separaten Thread
                Task.Run(async () =>
                {
                    try
                    {
                        var files = await _partsController.ListFilesInBucketAsync();
                        
                        if (files == null || files.Count == 0)
                        {
                            await Application.Instance.InvokeAsync(() =>
                            {
                                _progressBar.Indeterminate = false;
                                _statusLabel.Text = "Keine Dateien im Bucket gefunden";
                                
                                MessageBox.Show(
                                    "Es wurden keine Dateien im Supabase-Bucket gefunden.",
                                    "Keine Dateien",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Warning
                                );
                            });
                            return;
                        }
                        
                        // Filtere nur .3dm Dateien
                        var rhinoFiles = files.Where(f => f.Name.EndsWith(".3dm", StringComparison.OrdinalIgnoreCase)).ToList();
                        
                        if (rhinoFiles.Count == 0)
                        {
                            await Application.Instance.InvokeAsync(() =>
                            {
                                _progressBar.Indeterminate = false;
                                _statusLabel.Text = "Keine Rhino-Dateien im Bucket gefunden";
                                
                                MessageBox.Show(
                                    "Es wurden keine Rhino-Dateien (.3dm) im Supabase-Bucket gefunden.",
                                    "Keine Dateien",
                                    MessageBoxButtons.OK,
                                    MessageBoxType.Warning
                                );
                            });
                            return;
                        }
                        
                        // Zeige Dialog mit den gefundenen Dateien
                        await Application.Instance.InvokeAsync(() =>
                        {
                            _progressBar.Indeterminate = false;
                            _statusLabel.Text = $"{rhinoFiles.Count} Rhino-Dateien im Bucket gefunden";
                            
                            ShowBucketFileSelectionDialog(rhinoFiles, doc);
                        });
                    }
                    catch (Exception ex)
                    {
                        await Application.Instance.InvokeAsync(() =>
                        {
                            _progressBar.Indeterminate = false;
                            _statusLabel.Text = $"Fehler: {ex.Message}";
                            
                            MessageBox.Show(
                                $"Fehler beim Laden der Dateien aus dem Bucket:\n{ex.Message}",
                                "Fehler",
                                MessageBoxButtons.OK,
                                MessageBoxType.Error
                            );
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Fehler: {ex.Message}";
                
                MessageBox.Show(
                    $"Fehler: {ex.Message}",
                    "Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
            }
        }
        
        // Hilfsmethode zum Anzeigen eines Dialogs mit den gefundenen Dateien
        private void ShowBucketFileSelectionDialog(List<Supabase.Storage.FileObject> files, RhinoDoc doc)
        {
            var dialog = new Dialog
            {
                Title = "Block aus Supabase-Bucket importieren",
                MinimumSize = new Size(600, 500)
            };
            
            // Erstelle eine Listbox mit den Dateien
            var fileListBox = new ListBox();
            foreach (var file in files)
            {
                // Entferne die .3dm-Erweiterung und Unterstriche aus dem Dateinamen für die Anzeige
                string displayName = Path.GetFileNameWithoutExtension(file.Name).Replace('_', ' ');
                fileListBox.Items.Add(new ListItem { Text = displayName, Tag = file });
            }
            
            if (fileListBox.Items.Count > 0)
            {
                fileListBox.SelectedIndex = 0;
            }
            
            // Eingabefelder für die Metadaten des Teils
            var nameLabel = new Label { Text = "Name des Teils:" };
            var nameTextBox = new TextBox();
            
            var descriptionLabel = new Label { Text = "Beschreibung:" };
            var descriptionTextBox = new TextBox();
            
            var categoryLabel = new Label { Text = "Kategorie:" };
            var categoryTextBox = new TextBox();
            
            var materialLabel = new Label { Text = "Material:" };
            var materialTextBox = new TextBox();
            
            // Event-Handler für die Auswahl einer Datei
            fileListBox.SelectedIndexChanged += (sender, e) =>
            {
                if (fileListBox.SelectedIndex >= 0)
                {
                    var selectedFile = fileListBox.SelectedValue as Supabase.Storage.FileObject;
                    if (selectedFile != null)
                    {
                        // Setze den Namen basierend auf dem Dateinamen
                        string suggestedName = Path.GetFileNameWithoutExtension(selectedFile.Name).Replace('_', ' ');
                        nameTextBox.Text = suggestedName;
                    }
                }
            };
            
            // Löse die Auswahl des ersten Elements aus
            if (fileListBox.Items.Count > 0)
            {
                fileListBox.SelectedIndex = 0;
            }
            
            // Buttons
            var importButton = new Button { Text = "Importieren", Enabled = fileListBox.Items.Count > 0 };
            var cancelButton = new Button { Text = "Abbrechen" };
            
            // Event-Handler für die Buttons
            cancelButton.Click += (sender, e) => dialog.Close();
            
            importButton.Click += async (sender, e) =>
            {
                if (fileListBox.SelectedIndex >= 0)
                {
                    var selectedFile = (fileListBox.SelectedValue as Supabase.Storage.FileObject);
                    if (selectedFile != null)
                    {
                        string name = nameTextBox.Text.Trim();
                        string description = descriptionTextBox.Text.Trim();
                        string category = categoryTextBox.Text.Trim();
                        string material = materialTextBox.Text.Trim();
                        
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            MessageBox.Show(
                                "Bitte geben Sie einen Namen für das Teil an.",
                                "Fehlende Daten",
                                MessageBoxButtons.OK,
                                MessageBoxType.Warning
                            );
                            return;
                        }
                        
                        // Dialog schließen
                        dialog.Close();
                        
                        // UI aktualisieren
                        _statusLabel.Text = $"Importiere Block {selectedFile.Name}...";
                        _progressBar.Indeterminate = true;
                        
                        // Starte den Import-Prozess in einem separaten Thread
                        await Task.Run(async () =>
                        {
                            try
                            {
                                // Importiere die Datei aus dem Bucket als neues Teil
                                var part = await _partsController.ImportFileFromBucketAsync(
                                    selectedFile.Name,
                                    name,
                                    description,
                                    category,
                                    material
                                );
                                
                                // UI-Updates auf dem UI-Thread ausführen
                                await Application.Instance.InvokeAsync(() =>
                                {
                                    _progressBar.Indeterminate = false;
                                    _statusLabel.Text = $"Block {selectedFile.Name} erfolgreich als Teil importiert";
                                    
                                    MessageBox.Show(
                                        $"Die Datei '{selectedFile.Name}' wurde erfolgreich als Teil '{name}' importiert.",
                                        "Import erfolgreich",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Information
                                    );
                                    
                                    // Lade die Daten neu
                                    LoadDataAsync();
                                });
                            }
                            catch (Exception ex)
                            {
                                // UI-Updates auf dem UI-Thread ausführen
                                await Application.Instance.InvokeAsync(() =>
                                {
                                    _progressBar.Indeterminate = false;
                                    _statusLabel.Text = $"Fehler beim Importieren: {ex.Message}";
                                    
                                    MessageBox.Show(
                                        $"Fehler beim Importieren der Datei:\n{ex.Message}",
                                        "Importfehler",
                                        MessageBoxButtons.OK,
                                        MessageBoxType.Error
                                    );
                                });
                            }
                        });
                    }
                }
            };
            
            // Dialog-Layout erstellen
            dialog.Content = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(5, 10),
                Rows =
                {
                    new TableRow(new Label { Text = "Verfügbare Dateien im Bucket:" }),
                    new TableRow(fileListBox) { ScaleHeight = true },
                    new TableRow(nameLabel),
                        new TableRow(nameTextBox),
                    new TableRow(descriptionLabel),
                        new TableRow(descriptionTextBox),
                    new TableRow(categoryLabel),
                        new TableRow(categoryTextBox),
                    new TableRow(materialLabel),
                        new TableRow(materialTextBox),
                        new TableRow(
                            new StackLayout
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 5,
                            Items = { null, cancelButton, importButton }
                            }
                        )
                    }
                };

                dialog.ShowModal(this);
        }

        // Neue Methode zum Auto-Importieren fehlender Dateien
        private void AutoImport()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                MessageBox.Show(
                    "Es ist kein aktives Rhino-Dokument geöffnet.",
                    "Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
                return;
            }

            // UI aktualisieren
            _statusLabel.Text = "Starte Auto-Import...";
            _progressBar.Indeterminate = true;
            _autoImportButton.Enabled = false;

            // Starte den Auto-Import in einem separaten Thread
            Task.Run(async () =>
            {
                try
                {
                    int importCount = await _partsController.ImportAllMissingFilesAsync();
                    
                    // UI-Updates auf dem UI-Thread ausführen
                    await Application.Instance.InvokeAsync(() =>
                    {
                        _progressBar.Indeterminate = false;
                        _autoImportButton.Enabled = true;
                        
                        if (importCount > 0)
                        {
                            _statusLabel.Text = $"{importCount} Teile erfolgreich importiert";
                            
                            MessageBox.Show(
                                $"Es wurden {importCount} fehlende Teile aus dem Bucket erfolgreich in die Datenbank importiert.",
                                "Auto-Import erfolgreich",
                                MessageBoxButtons.OK,
                                MessageBoxType.Information
                            );
                            
                            // Daten neu laden
                            LoadDataAsync();
                        }
                        else
                        {
                            _statusLabel.Text = "Keine Teile zum Importieren gefunden";
                            
                            MessageBox.Show(
                                "Es wurden keine fehlenden Teile zum Importieren gefunden. Alle Dateien im Bucket sind bereits in der Datenbank registriert.",
                                "Auto-Import",
                                MessageBoxButtons.OK,
                                MessageBoxType.Information
                            );
                        }
                    });
            }
            catch (Exception ex)
            {
                    // UI-Updates auf dem UI-Thread ausführen
                    await Application.Instance.InvokeAsync(() =>
                    {
                        _progressBar.Indeterminate = false;
                        _autoImportButton.Enabled = true;
                        _statusLabel.Text = $"Fehler beim Auto-Import: {ex.Message}";
                
                MessageBox.Show(
                            $"Fehler beim Auto-Import:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxType.Error
                );
                    });
            }
            });
        }
    }
} 