# Klotz-Modelle: Anleitung

Dieser Ordner enthält die Blender-Quelldateien für nicht-primitive Klotz-Typen
(z. B. `DoorFrame1x4`, `WindowFrame1x4`) sowie das Skript, das sie nach
`Assets/Models/Resources/NonPrimitiveKlotz/*.fbx` exportiert.

## Ein neues Modell anlegen

1. Neue `.blend`-Datei in diesem Ordner anlegen, benannt wie der `KlotzType`
   (z. B. `RoofTile1x2.blend`).
2. Darin **genau ein** Top-Level-Objekt modellieren, mit demselben Namen wie
   die Datei (`RoofTile1x2`).
3. Maße: `WorldDef.SubKlotzSize` (0.36 × 0.144 × 0.36) multipliziert mit
   `KlotzKB.Size(type)` aus `Klotz.cs`. Bounding Box beginnt bei (0,0,0)
   (Ursprung = untere/linke/hintere Ecke, nicht Zentrum).
4. Achsen: Blender X = Breite, Blender Z = Höhe, Blender Y = Tiefe. Der
   Export mappt das automatisch korrekt auf Unity — nicht antasten.
5. Kein Material/keine Textur nötig — Farbe kommt zur Laufzeit vom
   `PlasteShader` (`KlotzColor` + Variant).
6. Shading auf Flat (`Shade Flat`), passend zum Plastik-Klotz-Look.
7. Falls Studs/Holes/raue Flächen gebraucht werden: siehe unten.
8. Exportieren (siehe unten), dann in Unity einbinden (siehe unten).

## Oberflächen markieren (Studs / Holes / Rough)

Manche Flächen sollen sich wie primitive Klötze verhalten: Studs oben, Holes
unten, oder eine raue (nicht glänzende) Oberfläche.

1. Für jede gewünschte Eigenschaft ein Material anlegen, benannt **exakt**
   wie ein Wert von `KlotzSurfaceFeature` in `Klotz.cs` (Groß-/Kleinschreibung
   egal): `Default`, `HasStuds`, `HasHoles`, `IsRough`. Aussehen/Farbe des
   Materials spielt keine Rolle, es wird nie gerendert.
2. Im Edit Mode (Face Select) die betroffenen Flächen auswählen und im
   Material-Properties-Panel das passende Material zuweisen (`Assign`).
3. Flächen ohne Zuweisung (oder mit `Default`) bekommen automatisch keine
   Besonderheit — dafür ist nichts weiter nötig.

Der Code liest den Material-**Namen** zur Laufzeit über den Renderer aus
(nicht den Slot-Index) — daher ist es unerheblich, in welcher Reihenfolge die
Materialien in Blender angelegt wurden.

## Exportieren

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python ArtSource\regenerate_all.py
```

Oder `ArtSource\regenerate_all.bat` doppelklicken. Exportiert automatisch
**alle** `*.blend`-Dateien in diesem Ordner nach
`Assets/Models/Resources/NonPrimitiveKlotz/`.

## In Unity einbinden

1. Editor-Fenster fokussieren, damit die neue `.fbx` importiert wird.
2. In den Import-Settings der `.fbx` **Read/Write Enabled** aktivieren
   (Model-Tab; steht in der `.meta`, ist standardmäßig aus).

Das war's — kein manueller Eintrag irgendwo nötig. `GameClient` lädt beim
Start automatisch alle Modelle aus diesem Resources-Ordner und ordnet sie
über ihren Objektnamen dem passenden `KlotzType` zu (`Enum.TryParse`). Der
Objektname in Blender muss also exakt wie der `KlotzType` heißen.

## Bekannte Einschränkungen

- `HasHoles` hat aktuell keine sichtbare Wirkung im Shader (weder bei
  primitiven noch nicht-primitiven Klötzen — nie implementiert).
- `IsRough` unterdrückt nur den Specular-Highlight, es gibt keine echte
  Rauheits-Textur/Normal-Map.
