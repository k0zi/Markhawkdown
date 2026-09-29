<p align="center">
  <img src="src/Markhawkdown/Assets/markhawkdown.png" alt="Markhawkdown" width="128" />
</p>

<h1 align="center">Markhawkdown</h1>

<p align="center">
  Egyszerű, platformfüggetlen Markdown szerkesztő és megjelenítő C#-ban, <a href="https://avaloniaui.net/">Avalonia</a> alapokon.
</p>

## Funkciók

- **Több dokumentum fülekben** – megnyitás, mentés, „Összes mentése”, figyelmeztetés mentetlen változásoknál
- **Három nézet** – szerkesztés, osztott nézet élő előnézettel, csak olvasás
- **Szintaxiskiemelés** a szerkesztőben (AvaloniaEdit + TextMate)
- **Formázó parancsok** – félkövér, dőlt, áthúzott, kód, kódblokk, címsorok, listák, feladatlista, idézet, hivatkozás, kép, táblázat, vízszintes vonal
- **Témák** – világos / sötét mód és több színséma (SukiUI)
- **Fájlok megnyitása** húzással (drag & drop) vagy parancssori argumentumként

## Követelmények

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows, Linux vagy macOS

## Fordítás és futtatás

```bash
git clone https://github.com/<felhasznalo>/Markhawkdown.git
cd Markhawkdown
dotnet run --project src/Markhawkdown
```

Fájl megnyitása indításkor:

```bash
dotnet run --project src/Markhawkdown -- samples/demo.md
```

## Billentyűparancsok

| Parancs | Billentyű |
| --- | --- |
| Új / Megnyitás / Mentés | `Ctrl+N` / `Ctrl+O` / `Ctrl+S` |
| Mentés másként | `Ctrl+Shift+S` |
| Bezárás | `Ctrl+W` |
| Következő / előző fül | `Ctrl+Tab` / `Ctrl+Shift+Tab` |
| Félkövér / Dőlt / Áthúzott | `Ctrl+B` / `Ctrl+I` / `Ctrl+Shift+X` |
| Sorközi kód | `Ctrl+E` |
| Hivatkozás | `Ctrl+K` |
| Címsor 1–3 | `Ctrl+1` – `Ctrl+3` |
| Felsorolás / Számozott lista | `Ctrl+Shift+8` / `Ctrl+Shift+7` |
| Idézet | `Ctrl+Shift+Q` |
| Szerkesztés / Osztott / Olvasás nézet | `F5` / `F6` / `F7` |

## Felhasznált csomagok

[Avalonia](https://github.com/AvaloniaUI/Avalonia) ·
[AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) ·
[Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia) ·
[SukiUI](https://github.com/kikipoulet/SukiUI) ·
[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)

## Licenc

[MIT](LICENSE) © 2026 David Kozma
