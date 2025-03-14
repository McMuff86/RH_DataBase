# Kopier-Skript für .env.local in alle notwendigen Verzeichnisse
# Dieses Skript stellt sicher, dass die .env.local-Datei in allen relevanten Verzeichnissen vorhanden ist

Write-Host "Kopiere .env.local in Ausgabeverzeichnisse..."

# Überprüfe, ob die .env.local-Datei im aktuellen Verzeichnis existiert
if (-not (Test-Path ".env.local")) {
    Write-Error "Fehler: .env.local wurde im aktuellen Verzeichnis nicht gefunden!"
    exit 1
}

# Liste der Zielverzeichnisse
$targetDirs = @(
    "bin\Debug\net48",
    "bin\Debug\net7.0-windows",
    "bin\Release\net48",
    "bin\Release\net7.0-windows"
)

# Kopiere die Datei in jedes Zielverzeichnis
foreach ($dir in $targetDirs) {
    # Erstelle das Verzeichnis, falls es noch nicht existiert
    if (-not (Test-Path $dir)) {
        Write-Host "Erstelle Verzeichnis $dir..."
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    
    # Kopiere die .env.local-Datei
    Write-Host "Kopiere .env.local nach $dir..."
    Copy-Item -Path ".env.local" -Destination "$dir\.env.local" -Force
}

# Überprüfe die kopierten Dateien
Write-Host "`nÜberprüfe kopierte Dateien:"
foreach ($dir in $targetDirs) {
    if (Test-Path "$dir\.env.local") {
        Write-Host "√ $dir\.env.local existiert."
    } else {
        Write-Host "× $dir\.env.local fehlt!"
    }
}

Write-Host "`nVorgang abgeschlossen." 