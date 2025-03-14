using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using Rhino;
using Rhino.Geometry;
using RH_DataBase.Models;
using RH_DataBase.Controllers;

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
                                new TableRow(new Label { Text = "Teilemanager", Font = new Font(FontFamilies.Sans, 16, FontStyle.Bold) }),
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
                    new TableRow(new Label { Text = "Teile", Font = new Font(FontFamilies.Sans, 14, FontStyle.Bold) }),
                    new TableRow(_partsGridView) { ScaleHeight = true },
                    
                    // Zeichnungen-Grid mit Überschrift
                    new TableRow(new Label { Text = "Zeichnungen", Font = new Font(FontFamilies.Sans, 14, FontStyle.Bold) }),
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
            
            // Status-Anzeigen
            _progressBar = new ProgressBar();
            _statusLabel = new Label { Text = "Bereit" };
            
            // Teile-GridView
            _partsGridView = new GridView
            {
                AllowMultipleSelection = false,
                AllowColumnReordering = true,
                AllowEmptySelection = true
            };
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "ID",
                DataCell = new TextBoxCell("Id"),
                Width = 50
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Name",
                DataCell = new TextBoxCell("Name"),
                Width = 200
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Beschreibung",
                DataCell = new TextBoxCell("Description"),
                Width = 300
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Kategorie",
                DataCell = new TextBoxCell("Category"),
                Width = 150
            });
            
            _partsGridView.Columns.Add(new GridColumn
            {
                HeaderText = "Material",
                DataCell = new TextBoxCell("Material"),
                Width = 150
            });
            
            _partsGridView.SelectionChanged += (sender, e) => UpdateDrawingsForSelectedPart();
            
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
                    Application.Instance.Invoke(() =>
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
                    Application.Instance.Invoke(() =>
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
                    Application.Instance.Invoke(async () =>
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
                Task.Run(() =>
                {
                    try
                    {
                        // Verwende den Controller, um das Teil einzufügen
                        var doc = RhinoDoc.ActiveDoc;
                        bool success = _partsController.InsertPartIntoDocument(selectedPart, doc);
                        
                        // UI-Updates auf dem UI-Thread ausführen
                        Application.Instance.Invoke(() =>
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
                            Application.Instance.Invoke(() =>
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
    }
} 