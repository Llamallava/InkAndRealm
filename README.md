# Ink & Realm

Ink & Realm is a fantasy map editor built for worldbuilders, dungeon masters, and anyone who wants to design detailed maps for their stories or campaigns. Users can create maps, place and paint terrain, structures, trees, water, and characters onto a canvas, save their work, and share or publish maps for others to view. The app runs in the browser using Blazor WebAssembly with a .NET backend and a SQL Server database.

---

## Features

### Map Editing

The editor gives you a set of tools to build maps:

- **Point Tool** places a single feature where you click.
- **Brush Tool** paints features across an area with controls for size, density, and tightness.
- **Polygon Tool** draws filled shapes for things like lakes or farmland regions.
- **Eraser Tool** removes features from the canvas.
- **Edit Tool** lets you select and reposition existing features or change their properties.
- **Duplicate Tool** clones selected features.

The editor supports undo/redo, a toggleable grid, background color customization, and auto-save.

### Feature Types

Features are organized into categories:

- **Land**: Grass, dirt, sand, rock, farmland (dry, lush, coarse, mud variants)
- **Trees**: Oak, pine, palm, willow (with size options)
- **Terrain**: Mountains, hills, valleys, ravines
- **Water**: Rivers, lakes, ponds (polygon-based)
- **Structures**: Cottages, farmhouses, taverns, inns, barns, outposts
- **Towns**: Villages, cities, castles
- **Bridges**
- **Characters**: Commoner, farmer, noble, knight, mercenary
- **Titles**: Text labels with optional descriptions placed on the map

### Character System

Characters placed on a map can have metadata attached: name, background, occupation, and personality. You can also define relationships between characters. The relationship viewer displays a force-directed graph where nodes are draggable and edges describe the nature of each connection.

### Sharing and Publishing

Maps can be shared two ways:

1. **Share codes**: A 12-character code is generated that lets anyone view a read-only copy of your map. Codes expire after 7 days.
2. **Publishing**: Maps can be published to a public gallery where other users can browse, view, and like them. Published maps show view and like counts.

### Map Viewer and Gallery

The viewer is a read-only canvas with pan and zoom controls. The public gallery lists published maps and lets users explore work from others.

### Export

Maps can be exported as PNG files directly from the editor.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Blazor WebAssembly (.NET 10) |
| Backend | ASP.NET Core Web API (.NET 10) |
| Database | SQL Server / LocalDB via Entity Framework Core 10 |
| Auth | Session tokens with BCrypt password hashing |
| Rendering | HTML5 Canvas (JavaScript) |
| Styling | Bootstrap |

---

## Project Structure

```
InkAndRealm/
├── InkAndRealm.Server/     Backend API (controllers, database, migrations)
├── InkAndRealm.Client/     Blazor WASM frontend (pages, components, canvas JS)
└── InkAndRealm.Shared/     Shared DTOs used by both client and server
```

---

## Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server Express (LocalDB works)

### 1. Configure the database connection

In `InkAndRealm.Server/appsettings.json`, set the connection string to point at your SQL Server instance. The default expects a local server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=InkAndRealmDB;Trusted_Connection=True;"
}
```

The database schema is created automatically on first run via EF Core migrations.

### 2. Start the backend

```bash
cd InkAndRealm.Server
dotnet run
```

The API starts at `http://localhost:5072`. Swagger UI is available at `/swagger`.

### 3. Start the frontend

In a separate terminal:

```bash
cd InkAndRealm.Client
dotnet watch
```

The client runs at `http://localhost:5216`.

---

## Authentication

Users register and log in with a username and password. Sessions are stored as tokens with a 14-day expiration. Session state is persisted in localStorage so refreshing the page does not log you out.
