# Panorama Viewer 1.2 – ArcGIS Pro 3.7

Doplněk pro otevření Google Street View a Mapy.com Panorama podle místa vybraného v mapě.

## Instalace a použití

Vyžaduje ArcGIS Pro 3.7, připojení k internetu a Microsoft Edge WebView2 Runtime. Při zavřeném ArcGIS Pro nainstalujte dvojklikem soubor `PanoramaViewer.esriAddInX` a aplikaci znovu spusťte. ZIP se zdrojovým kódem není instalátor.

V záložce **Panorama** otevřete Nastavení, zadejte potřebné API klíče a uložte je. Potom vyberte Google Street View nebo Mapy.com Panorama a klikněte do mapy. Google Street View funguje i bez API klíče, ovšem bez tohoto klíče se otevře streetview v prohlížeči a nikoliv v samostatném okně tak, jako je to v případě panoramy z mapy.com

Ve vlastních panoramatických oknech je připravena synchronizace polohy s 2D mapou: modrý bod pro Mapy.com, oranžový pro Google. Po zavření okna bod zmizí. Mapa, její souřadnicový systém ani zdrojové vrstvy se nemění.

## Mapy.com Panorama – API klíč

Pro zobrazení panoramat je API klíč Mapy.com povinný. Je možné jej získá vytvořením projektu na portálu developer.mapy.com Podrobnosti: [projekty a API klíče](https://developer.mapy.com/rest-api-mapy-cz/how-to-start/).

Panoramata se zobrazují ve vlastním okně přes JS Panorama API.
## Google Street View – API klíč

Pro Street View ve vlastním okně a se synchronizací polohy je potřeba samostatný Google API klíč. V Google Cloud Console vytvořte nebo vyberte projekt, aktivujte Maps JavaScript API, připojte fakturační účet a vytvořte API klíč. Podrobnosti: [oficiální návod](https://developers.google.com/maps). U tohoto API klíče je nutné vyplnit údaje v Billing.

Bez Google API klíče se Street View otevře v běžném prohlížeči, bez zpětné synchronizace polohy. 

## Správa klíčů

Klíče lze kdykoliv změnit v **Panorama - Nastavení**. Uložení prázdného pole příslušný klíč odstraní. Změna se projeví při dalším kliknutí do mapy.

- Integrovaná okna používají virtuální adresu `https://panorama.local/`. Případná doménová omezení musí tento host povolit; u Google nastavte webové omezení `https://panorama.local/` a API omezení na Maps JavaScript API.

Synchronizace je navržena pro 2D mapy, nikoli 3D Scene.

## Nové ve verzi 1.2

- Tečka: 85% krytí. Výseč: 30% krytí, jemný obrys a středová čára.
- Délka reaguje na měřítko mapy (cíl přibližně 48 obrazových bodů, obvykle 5-20 m). Při extrémním přiblížení dostává přednost omezení na obrazovce; při velkém oddálení se uplatní maximum 20 m.
- Směr a FOV se přebírají z panoramatu; u Google se FOV odvozuje ze zoomu.
- Další kliknutí na právě aktivní tlačítko  přepne mapu na Explore
