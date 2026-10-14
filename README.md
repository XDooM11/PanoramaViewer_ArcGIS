# Panorama Viewer v1.0 – zdrojový projekt pro ArcGIS Pro 3.7

## Co obsahuje
- Vlastní záložka **Panorama**: Google Street View, Mapy.com Panorama, Nastavení.
- Kliknutí do mapy; pouze vybraný bod se promítne do WGS84. Mapa a zdrojové vrstvy se nemění.
- Google Street View se otevírá v systémovém prohlížeči.
- Mapy.com Panorama se zobrazuje ve vlastním okně WebView2 přes oficiální JS Panorama API.
- API klíč lze opakovaně změnit v Nastavení; je šifrován pomocí Windows DPAPI pro aktuálního uživatele.

## Požadavky
- ArcGIS Pro **3.7** (Windows 11)
- **Visual Studio 2026 v18.4.1+**, .NET **10 SDK**, ArcGIS Pro SDK / Esri NuGet **3.7.0.1901**
- WebView2 Runtime, přístup k api.mapy.com a Google Maps
- API klíč Mapy.com s povolenou příslušnou službou a případnými doménovými omezeními odpovídajícími integraci

## Sestavení
1. Otevřete `PanoramaViewer.csproj` ve Visual Studio 2026 na Windows.
2. Obnovte NuGet balíčky (Restore). Projekt odkazuje na Esri.ArcGISPro.Extensions30 3.7.0.1901 a Microsoft.Web.WebView2.
3. Sestavte projekt jako Release / x64 (Build). Pro správné vytvoření a registraci `.esriAddInX` může být potřeba nainstalovat ArcGIS Pro SDK Visual Studio extension a využít jeho šablonu / build targets; prostý build běžného .NET projektu nemusí add-in zabalit.
4. Pokud build nevyrobí `.esriAddInX`, vytvořte v SDK šabloně **ArcGIS Pro Module Add-in** nový projekt a přeneste soubory, Assets, Web a deklarace z Config.daml. Neoznačujte samotnou DLL ani tento ZIP za instalovatelný add-in.
5. Spusťte ArcGIS Pro 3.7, ověřte záložku Panorama, kliknutí v mapě v S-JTSK a oba prohlížeče.

## Stav a omezení
Toto je **upravený zdrojový prototyp**, nikoli zkompilovaný nebo otestovaný instalační soubor. Nebyl proveden Windows/ArcGIS runtime test, nelze garantovat kompilaci či funkčnost bez případných úprav. Klíč není součástí zdrojového archivu. Přístup Mapy.com API může vyžadovat vhodně nastavená oprávnění/omezení klíče. Při potížích s JS API zkontrolujte Developer Tools / síťové požadavky ve WebView2.

## Oficiální zdroje
- https://github.com/Esri/arcgis-pro-sdk
- https://www.nuget.org/packages/Esri.ArcGISPro.Extensions30/3.7.0.1901
- https://developer.mapy.com/cs/js-panorama/
