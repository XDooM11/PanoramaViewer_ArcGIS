# Panorama Viewer 1.1.1 - oprava zobrazeni Mapy.com

Zdrojova aktualizace pro existujici verzi 1.1 na ArcGIS Pro 3.7.
NENI to instalator. Pouzijte jiz funkcni sestaveni v GitHub Actions.

## Co nahradit

- PanoramaWindow.cs (v koreni vedle PanoramaViewer.csproj)
- Config.daml (v koreni, beze zmeny ID doplnku)
- Web/panorama.html (ve slozce Web, nikoli v koreni)

Souboru build.yml, csproj, ostatnich C# souboru a Web/google.html se
nedotykejte. Nevytvarejte duplicitni C# soubory s jinymi nazvy.

## Zmeny

- Rozmery HTML kontejneru zavisi na velikosti okna, ne na rozmerech
  bitmapy vykreslovaciho platna. Rucni zmena velikosti okna zustava mozna.
- Pri zmene polohy se znovu neodesila hlaseni o nacteni a neresetuje
  se pohled. Synchronizace skutecne polohy panoramy s bodem zustava.
- Mapy.com nema promennou vysku stavoveho radku nad prohlizecem.
  Informace o nacitani a chybach jsou v titulku okna; po nacteni zmizi.
- Zoom cele webove stranky je vypnuty (100 %). Vlastni ovladani
  panoramy mysi, koleckem a sipkami neni blokovano.
- Pocatecni fov = 1.2 rad je nastavena jednou pri vytvoreni panoramy.
  Zamerne nepouzivame casovac ani opakovane volani setCamera,
  ktere by rusilo rucni zoom uzivatele.
- Klice a jejich uloziste, soubory vrstev a souradnicovy system
  projektu se nemeni. Zadne skutecne API klice nejsou v tomto balicku.

## Instalace a test

1. Nahrajte tri zmenene soubory do stejnych cest na GitHubu.
2. Commit changes a pockejte na uspesne Actions.
3. Stahnete vysledny artifact s .esriAddInX.
4. Zavrete ArcGIS Pro i vsechna panoramaticka okna. Nainstalujte novy
   .esriAddInX a ArcGIS znovu spustte. Nemazte API klice ani WebView profil.
5. Otevrete Mapy.com. V titulku musi byt Panorama Viewer 1.1.1.
6. Nechte snimek 20 sekund bez vstupu, pak zmente velikost okna,
   rucne priblizte/oddalte koleckem a prejdete na dalsi panorama.
   Overte take polohu modreho bodu v ArcGIS.

## Rozsah overeni a omezeni

Z poskytnute verze 1.1 je prokazatelne, ze zobrazovani/skryvani stavu
menilo vysku WebView a kontejner panoramy nemel explicitni vlastni
sirku/vysku. Zda je to presna pricina hlaseneho problemu, neni dosud
potvrzene: k posledni zprave nebyl prilozen novy snimek ani video.
Toto je cilena oprava stabilizace zobrazeni, ne potvrzeni reprodukce
chyby v ArcGIS. Byly provedeny offline testy s nahradnim Panorama API,
nikoli prihlaseni ke sluzbe. C# sestaveni a chovani v ArcGIS Pro/WebView2
musi overit vase GitHub Actions a nasledny test.

Oficialni dokumentace:
https://developer.mapy.com/cs/js-panorama/
https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2settings.iszoomcontrolenabled
https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2settings.ispinchzoomenabled
