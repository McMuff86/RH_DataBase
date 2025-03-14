using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private Button _testConnectionButton;
        private Button _addSampleDataButton;
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
                    
                    // Zeichnungen-Grid mit Überschrift
                    new TableRow(new Label { Text = "Zeichnungen", Font = new Eto.Drawing.Font(FontFamilies.Sans, 14, FontStyle.Bold) }),
                    new TableRow(_drawingsGridView) { ScaleHeight = true },
                    
                    // Statuszeile und Aktionsbuttons
                    new TableRow(
                        new StackLayout
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                new StackLayoutItem(_statusLabel, true),
                                new StackLayoutItem(_progressBar, true),
                                _insertButton,
                                _deleteButton,
                                _deleteDrawingButton,
                                _exportBlockButton,
                                _createBlockButton,
                                closeButton
                            }
                        }
                    )
                }
            };
            
            // Daten laden
            LoadDataAsync();
            
            // Handler für das Schließen des Fensters
            Closed += (sender, e) => 
            {
                RhinoApp.WriteLine("Parts Manager wurde geschlossen.");
            };
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
            
            // Block-Export-Button
            _exportBlockButton = new Button { Text = "Block exportieren", ToolTip = "Exportiert einen Block aus Rhino in die Datenbank" };
            _exportBlockButton.Click += (sender, e) => ExportBlockDefinition();
            
            // Block-Erstellen-Button
            _createBlockButton = new Button { Text = "Block aus Auswahl erstellen", ToolTip = "Erstellt einen Block aus ausgewählten Objekten und exportiert ihn in die Datenbank" };
            _createBlockButton.Click += (sender, e) => CreateBlockFromSelection();
            
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
                // Aktiviere den Zeichnung-Löschen-Button, wenn eine Zeichnung ausgewählt ist
                _deleteDrawingButton.Enabled = _drawingsGridView.SelectedItem != null;
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
                    
                    // Zeichnungen laden über den Controller
                    var drawings = await _partsController.GetAllDrawingsAsync();
                    
                    // UI-Updates auf dem UI-Thread ausführen
                    await Application.Instance.InvokeAsync(async () =>
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
                
                // Starte den Einfügeprozess in einem separaten Thread
                Task.Run(async () =>
                {
                    try
                    {
                    // Verwende den Controller, um das Teil einzufügen
                    var doc = RhinoDoc.ActiveDoc;
                        bool success = await _partsController.InsertPartIntoDocumentAsync(selectedPart, doc);
                    
                        // UI-Updates auf dem UI-Thread ausführen
                        await Application.Instance.InvokeAsync(() =>
                        {
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
                        });
                }
                catch (Exception ex)
                    {
                        // UI-Updates auf dem UI-Thread ausführen
                        Application.Instance.Invoke(() =>
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
                        
                        string name = nameTextBox.Text;
                        string description = descriptionTextBox.Text;
                        string category = categoryTextBox.Text;
                        string material = materialTextBox.Text;
                        
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
                        new TableRow(new Label { Text = "Name:" }),
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
        private void CreateBlockFromSelection()
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

                // Sammle Object-IDs
                var objectIds = new List<Guid>();
                foreach (var obj in selectedObjects)
                {
                    objectIds.Add(obj.Id);
                }

                // Erstelle einen Dialog für die Block-Erstellung
                var dialog = new Dialog
                {
                    Title = "Block aus Auswahl erstellen",
                    MinimumSize = new Size(500, 400)
                };

                // Eingabefelder für Block-Metadaten
                var nameTextBox = new TextBox();
                var descriptionTextBox = new TextBox();
                var categoryTextBox = new TextBox();
                var materialTextBox = new TextBox();

                // OK und Abbrechen Buttons
                var okButton = new Button { Text = "Erstellen" };
                var cancelButton = new Button { Text = "Abbrechen" };

                cancelButton.Click += (sender, e) => dialog.Close();
                okButton.Click += async (sender, e) => 
                {
                    try
                    {
                        dialog.Close();
                        
                        string name = nameTextBox.Text;
                        string description = descriptionTextBox.Text;
                        string category = categoryTextBox.Text;
                        string material = materialTextBox.Text;
                        
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
                        
                        // Frage nach dem Basispunkt für den Block
                        RhinoApp.WriteLine("Wählen Sie den Basispunkt für den Block:");
                        Point3d basePoint;
                        var getBasePointResult = Rhino.Input.RhinoGet.GetPoint("Basispunkt für den Block wählen", false, out basePoint);
                        if (getBasePointResult != Rhino.Commands.Result.Success)
                        {
                            _statusLabel.Text = "Block-Erstellung abgebrochen";
                            return;
                        }
                        
                        // UI aktualisieren
                        _statusLabel.Text = $"Erstelle Block {name}...";
                        _progressBar.Indeterminate = true;
                        
                        // Erstellung in separatem Thread
                        await Task.Run(async () =>
                        {
                            try
                            {
                                // Erstelle den Block und exportiere ihn
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
                                    _statusLabel.Text = $"Block {name} erfolgreich erstellt und exportiert";
                                    _progressBar.Indeterminate = false;
                                    
                                    MessageBox.Show(
                                        $"Block {name} wurde erfolgreich erstellt und als Teil exportiert.",
                                        "Block-Erstellung erfolgreich",
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
                                    _statusLabel.Text = $"Fehler bei der Block-Erstellung: {ex.Message}";
                                    _progressBar.Indeterminate = false;
                                    
                                    MessageBox.Show(
                                        $"Fehler bei der Block-Erstellung:\n{ex.Message}",
                                        "Erstellungsfehler",
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
                        new TableRow(new Label { Text = $"{objectCount} Objekte ausgewählt" }),
                        new TableRow(new Label { Text = "Name:" }),
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
    }
} 