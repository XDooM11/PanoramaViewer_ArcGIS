CZ VERSION
# Panorama Viewer 1.2 – ArcGIS Pro 3.7

Doplněk pro otevření Google Street View a Mapy.com Panorama podle místa vybraného v mapě.

## Instalace a použití

Instalační soubor 'PanoramaViewer.esriAddInX' je dostupný v sekci **Releases** a také přímo v repozítáři ve složce **Installer**.

Vyžaduje ArcGIS Pro 3.7, připojení k internetu a Microsoft Edge WebView2 Runtime. Při zavřeném ArcGIS Pro nainstalujte dvojklikem soubor `PanoramaViewer.esriAddInX` a aplikaci znovu spusťte.

V záložce **Panorama** otevřete Nastavení, zadejte potřebné API klíče a uložte je. Potom vyberte Google Street View nebo Mapy.com Panorama a klikněte do mapy. Google Street View funguje i bez API klíče, ovšem bez tohoto klíče se otevře streetview v prohlížeči a nikoliv v samostatném okně tak, jako je to v případě panoramy z mapy.com

Ve vlastních panoramatických oknech je připravena synchronizace polohy s 2D mapou: modrý bod pro Mapy.com, oranžový pro Google. Po zavření okna bod zmizí. Mapa, její souřadnicový systém ani zdrojové vrstvy se nemění.

## Mapy.com Panorama – API klíč

Pro zobrazení panoramat je API klíč Mapy.com povinný. Je možné jej získá vytvořením projektu na portálu developer.mapy.com Podrobnosti: [Návod na získání API klíče](https://developer.mapy.com/rest-api-mapy-cz/how-to-start/).

Panoramata se zobrazují ve vlastním okně přes JS Panorama API.
## Google Street View – API klíč

Pro Street View ve vlastním okně a se synchronizací polohy je potřeba samostatný Google API klíč. V Google Cloud Console vytvořte nebo vyberte projekt, aktivujte Maps JavaScript API, připojte fakturační účet a vytvořte API klíč. U tohoto API klíče je nutné vyplnit údaje v Billing. [Návod na získání API klíče](https://developers.google.com/maps/documentation/javascript/get-api-key).

Bez Google API klíče se Street View otevře v běžném prohlížeči, bez zpětné synchronizace polohy. 

## Správa klíčů

Klíče lze kdykoliv změnit v **Panorama - Nastavení**. Uložení prázdného pole příslušný klíč odstraní. Změna se projeví při dalším kliknutí do mapy.

- Integrovaná okna používají virtuální adresu `https://panorama.local/`. Případná doménová omezení musí tento host povolit; u Google nastavte webové omezení `https://panorama.local/` a API omezení na Maps JavaScript API.

Synchronizace je navržena pro 2D mapy, nikoli 3D Scene.

## Nové ve verzi 1.2

- Aktualizovaný symbol lokalizace: Tečka: 85% krytí. Výseč: 30% krytí, jemný obrys a středová čára.
- Délka výseče reaguje na měřítko mapy (cíl přibližně 48 obrazových bodů, obvykle 5-20 m). Při extrémním přiblížení dostává přednost omezení na obrazovce. Při velkém oddálení se uplatní maximum 20 m.
- Směr a FOV se přebírají z panoramatu.
- Další kliknutí na právě aktivní tlačítko přepne mapu na Explore

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------
EN VERSION
# Panorama Viewer 1.2 – ArcGIS Pro 3.7

An add-in for viewing Google Street View and Mapy.com panoramas by clicking a location on the map in ArcGIS.

## Installation and Usage

The installer file `PanoramaViewer.esriAddInX` is available in the **Releases** section and directly in the repository under the **Installer** folder.

ArcGIS Pro 3.7, an internet connection, and Microsoft Edge WebView2 Runtime are required. With ArcGIS Pro closed, double-click `PanoramaViewer.esriAddInX` to install the add-in, then restart the application.

Open **Settings** in the **Panorama** tab, enter the required API keys, and save them. Then select Google Street View or Mapy.com Panorama and click on the map. Google Street View also works without an API key; however, without a key, Street View opens in a web browser rather than in a separate window, as Mapy.com Panorama does.

The standalone panorama windows support location synchronization with the 2D map: a blue point for Mapy.com and an orange point for Google. The point disappears when the window is closed. The map, its coordinate system, and the source layers remain unchanged.

## Mapy.com Panorama – API Key

An API key is required to display Mapy.com panoramas. You can obtain one by creating a project on the [Mapy.com developer portal](https://developer.mapy.com/).

For details, see the [API key setup guide](https://developer.mapy.com/rest-api-mapy-cz/how-to-start/).

Panoramas are displayed in a separate window using the JS Panorama API.

## Google Street View – API Key

A separate Google API key is required to display Street View in a standalone window with location synchronization. In the Google Cloud Console, create or select a project, enable the Maps JavaScript API, link a billing account, and create an API key. Billing must be configured for this API.

For details, see the [Google API key setup guide](https://developers.google.com/maps/documentation/javascript/get-api-key).

Without a Google API key, Street View opens in a regular web browser without location synchronization back to the map.

## Managing API Keys

API keys can be changed at any time under **Panorama – Settings**. Saving an empty field removes the corresponding key. Changes take effect the next time you click on the map.

- The embedded windows use the virtual address `https://panorama.local/`. Any domain restrictions must allow this host. For Google, configure the website restriction as `https://panorama.local/` and restrict the API key to the Maps JavaScript API.

Location synchronization is designed for 2D maps, not 3D scenes.

## What's New in Version 1.2

- Updated location symbol: the dot has 85% opacity. The directional sector has 30% opacity, a subtle outline, and a center line.
- The sector length responds to the map scale (targeting approximately 48 screen pixels, typically 5–20 m). At extreme zoom levels, screen-space constraints take priority. When zoomed far out, a maximum length of 20 m applies.
- Heading and field of view (FOV) are taken from the panorama.
- Clicking the currently active button again switches the map back to Explore mode.
