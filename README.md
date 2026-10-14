# Panorama Viewer 1.1 – ArcGIS Pro 3.7

Doplněk pro otevření Google Street View a Mapy.com Panorama podle místa vybraného v mapě.

## Instalace a použití

Vyžaduje ArcGIS Pro 3.7, připojení k internetu a Microsoft Edge WebView2 Runtime. Při zavřeném ArcGIS Pro nainstalujte dvojklikem soubor `PanoramaViewer.esriAddInX` a aplikaci znovu spusťte. ZIP se zdrojovým kódem není instalátor.

V záložce **Panorama** otevřete Nastavení, zadejte potřebné API klíče a uložte je. Potom vyberte Google Street View nebo Mapy.com Panorama a klikněte do mapy.

Ve vlastních panoramatických oknech je připravena synchronizace polohy s 2D mapou: modrý bod pro Mapy.com, oranžový pro Google. Po zavření okna bod zmizí. Mapa, její souřadnicový systém ani zdrojové vrstvy se nemění.

## Mapy.com Panorama – API klíč

Pro zobrazení panoramat je API klíč Mapy.com povinný. Správce aplikace jej získá vytvořením projektu v portálu Mapy.com – Můj účet. Podrobnosti: [projekty a API klíče](https://api.mapy.com/).

Panoramata se zobrazují ve vlastním okně přes JS Panorama API. Používání čerpá kredity podle ceníku Mapy.com; po vyčerpání volných kreditů je další provoz podmíněn povolenou placenou spotřebou.

## Google Street View – API klíč

Pro Street View ve vlastním okně a se synchronizací polohy je potřeba samostatný Google API klíč. V Google Cloud Console vytvořte nebo vyberte projekt, aktivujte Maps JavaScript API, připojte fakturační účet a vytvořte API klíč. Podrobnosti: [oficiální návod](https://developers.google.com/maps).

Používání může být zpoplatněno podle ceníku Google Maps Platform. Bez Google klíče se Street View otevře v běžném prohlížeči, bez zpětné synchronizace polohy. Klíč Mapy.com pro Google nefunguje.

## Správa klíčů a omezení

Klíče lze kdykoliv změnit v **Panorama → Nastavení**. Uložení prázdného pole příslušný klíč odstraní; změna se projeví při dalším kliknutí do mapy.

Klíče nejsou součástí instalátoru. Ukládají se šifrovaně pomocí Windows DPAPI pro aktuálního uživatele. Nevkládejte je do repozitáře.

- Integrovaná okna používají virtuální adresu `https://panorama.local/`. Případná doménová omezení musí tento host povolit; u Google nastavte webové omezení `https://panorama.local/` a API omezení na Maps JavaScript API.

Používání a distribuce podléhají podmínkám poskytovatelů. Mapy.com u hromadně distribuovaných aplikací vyžadují společný API projekt spravovaný nositelem práv k aplikaci, nikoli samostatné projekty zakládané koncovými uživateli: [podmínky Mapy.com](https://api.mapy.com/).

Nové funkce verze 1.1 je potřeba ověřit v cílovém prostředí; synchronizace je navržena pro 2D mapy, nikoli 3D Scene.
