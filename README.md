# Warehouse Management System

Eine webbasierte Anwendung zur Verwaltung von Artikeln, Lagerbeständen und Lagerbewegungen.

Das Projekt wurde mit **ASP.NET Core MVC** und **Entity Framework Core** entwickelt und dient als Beispiel für die Umsetzung einer klassischen Business-Anwendung mit Benutzerverwaltung, Datenbankanbindung, Such- und Filterfunktionen sowie Auswertungen.

## Funktionen

### Artikelverwaltung

* Anlegen neuer Artikel
* Bearbeiten bestehender Artikel
* Löschen von Artikeln
* Anzeige von Artikelinformationen
* Zuordnung zu Kategorien
* Suche und Filterung
* Sortierung
* Seitenweise Anzeige der Ergebnisse

### Lagerbewegungen

Die Anwendung unterstützt die Erfassung und Verwaltung von Lagerbewegungen:

* Wareneingänge
* Warenentnahmen
* Bestandsänderungen
* Anzeige der Bewegungshistorie
* Filterung nach Zeitraum und Kategorie

### Dashboard

Das Dashboard bietet eine Übersicht über die wichtigsten Lagerinformationen:

* Anzahl der Artikel
* aktuelle Bestände
* Ein- und Ausgänge
* Auswertung der Lagerbewegungen
* grafische Darstellung der Daten

Für die grafische Darstellung wird **Chart.js** verwendet.

### Benutzerverwaltung und Authentifizierung

Die Anwendung verfügt über eine einfache rollenbasierte Benutzerverwaltung.

Es werden unterschiedliche Benutzerrollen unterstützt, beispielsweise:

* Administrator
* Standardbenutzer

Abhängig von der Benutzerrolle stehen unterschiedliche Funktionen zur Verfügung.

### PDF-Export

Ausgewählte Daten und Auswertungen können für die weitere Verarbeitung als PDF exportiert werden.

---

## Technischer Aufbau

Das Projekt basiert auf folgenden Technologien:

* **C#**
* **.NET / ASP.NET Core MVC**
* **Entity Framework Core**
* **SQL Server**
* **Razor Views**
* **HTML / CSS**
* **JavaScript**
* **Chart.js**
* **Bootstrap**

### Architektur

Die Anwendung verwendet die klassische MVC-Struktur:

```text
Controllers
    ↓
Services / Application Logic
    ↓
Entity Framework Core
    ↓
SQL Server
```

Die Datenmodelle und ViewModels werden getrennt verwendet, um Datenbankmodell und Benutzeroberfläche voneinander zu entkoppeln.

Beispielhafte Struktur:

```text
WarehouseApp
│
├── Controllers
├── Data
├── Models
├── ViewModels
├── Services
├── Views
├── wwwroot
└── Program.cs
```

---

## Datenbank

Als Datenbanksystem wird **Microsoft SQL Server** verwendet.

Die Anwendung verwendet **Entity Framework Core** für:

* Datenbankzugriff
* Entity Mapping
* LINQ-Abfragen
* CRUD-Operationen
* Datenbankmigrationen

Für eine lokale Installation kann beispielsweise eine lokale SQL-Server-Instanz oder SQL Server Express verwendet werden.

> Die im Repository verwendete Datenbankverbindung dient ausschließlich als Beispiel. Für eine eigene Installation sollte die Connection String-Konfiguration entsprechend der lokalen Umgebung angepasst werden.

---

## Such-, Filter- und Sortierfunktionen

Die Artikelverwaltung unterstützt verschiedene Möglichkeiten zur Navigation durch größere Datenmengen:

* Freitextsuche
* Filter nach Kategorie
* Sortierung nach verschiedenen Eigenschaften
* Pagination
* Auswahl der Anzahl der Datensätze pro Seite

Dadurch bleibt die Benutzeroberfläche auch bei größeren Datenmengen übersichtlich.

---

## Ziel des Projekts

Das Projekt wurde entwickelt, um typische Anforderungen einer internen Business-Anwendung abzubilden.

Dabei standen insbesondere folgende Themen im Vordergrund:

* Entwicklung einer ASP.NET-Core-Anwendung
* Datenbankmodellierung
* Entity Framework Core
* CRUD-Funktionalität
* Benutzer- und Rollenverwaltung
* Verarbeitung von Geschäftsdaten
* Such- und Filterfunktionen
* Reporting und Visualisierung
* saubere Trennung von Datenmodellen und ViewModels
* strukturierte Entwicklung einer Business-Anwendung

---

## Lokale Installation

### Voraussetzungen

Für die Ausführung werden benötigt:

* .NET SDK
* Microsoft SQL Server oder SQL Server Express
* Visual Studio oder Visual Studio Code

### 1. Repository klonen

```bash
git clone https://github.com/Petro1988/WarehouseApp.git
cd WarehouseApp
```

### 2. Datenbank konfigurieren

Die Connection String-Konfiguration befindet sich in:

```text
appsettings.json
```

Beispiel:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=WarehouseDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Datenbank erstellen

Falls das Projekt mit Entity Framework Core Migrationen verwendet:

```bash
dotnet ef database update
```

### 4. Anwendung starten

```bash
dotnet run
```

Anschließend kann die Anwendung über die von ASP.NET Core angegebene lokale URL geöffnet werden.

---

## Sicherheitshinweis

Dieses Repository enthält **keine produktiven Zugangsdaten oder vertraulichen Unternehmensdaten**.

Für eine produktive Umgebung sollten insbesondere folgende Werte nicht direkt im Quellcode gespeichert werden:

* Passwörter
* API Keys
* Tokens
* produktive Connection Strings
* Zertifikate oder private Schlüssel

Für Entwicklungs- und Testumgebungen sollten entsprechende Konfigurationsmechanismen verwendet werden.

---

## Projektstatus

Das Projekt ist als **Portfolio- und Demonstrationsprojekt** gedacht.

Es zeigt praktische Erfahrungen in der Entwicklung von Business-Anwendungen mit dem Microsoft-.NET-Stack und kann als Grundlage für weitere Erweiterungen dienen.

---

## Mögliche zukünftige Erweiterungen

* REST API
* ASP.NET Core Identity
* feinere Berechtigungsverwaltung
* automatisierte Tests
* Logging und Monitoring
* Docker-Unterstützung
* CI/CD mit GitHub Actions
* erweitertes Reporting
* Import und Export von Excel-Dateien
* Anbindung weiterer Unternehmenssysteme

---

## Technologien

| Bereich            | Technologie                  |
| ------------------ | ---------------------------- |
| Programmiersprache | C#                           |
| Framework          | ASP.NET Core                 |
| Architektur        | MVC                          |
| ORM                | Entity Framework Core        |
| Datenbank          | Microsoft SQL Server         |
| Frontend           | Razor, HTML, CSS, JavaScript |
| UI                 | Bootstrap                    |
| Charts             | Chart.js                     |
| Versionsverwaltung | Git / GitHub                 |

---

## Autor

**Petru Gavriliuc**

IT-System Engineer / Business Applications Developer

Schwerpunkte:

* .NET / C#
* ASP.NET Core
* SQL Server
* Microsoft-Technologien
* Business Applications
* Prozessdigitalisierung
* IT-Systemadministration
