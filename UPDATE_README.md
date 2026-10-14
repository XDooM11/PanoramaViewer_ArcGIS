# Aktualizace Panorama Viewer 1.1

## Nahrání na GitHub

Nahraďte stejnojmenné soubory v existujícím repozitáři. Složku Web
nahrajte jako složku, aby soubory zůstaly na cestě Web/panorama.html
a Web/google.html. Nezasahujte do funkčního build.yml ani csproj.

Po Commit changes počkejte na GitHub Actions, stáhněte nový artifact
a nainstalujte .esriAddInX při zavřeném ArcGIS Pro. Klíč Mapy.com
zůstává uložený v původním umístění; GUID add-inu se nemění.

## Důležité pro Google

Vlastní okno Google Street View a synchronizace polohy vyžadují
Google Maps API klíč (Maps JavaScript API + fakturace). Klíč Mapy.com
není Google klíč. Zadejte jej v Nastavení; bez něj se Google
stále otevře v běžném prohlížeči, bez synchronizace.

Jde o zdrojovou aktualizaci pro sestavení na GitHubu, ne o hotový
instalátor. Novou C# verzi je ještě nutné sestavit a vyzkoušet.

---

# Panorama Viewer 1.1.0 - aktualizace pro ArcGIS Pro 3.7

Tento balicek obsahuje upravene ZDROJOVE SOUBORY. Neni to instalacni
.esriAddInX. Ten vytvori vase jiz funkcni sestaveni na GitHub Actions.

## Co se zmenilo

- Po nacteni panoramy Mapy.com zmizi informacni text. Chyby a prubeh
  nacitani zustavaji viditelne. Zadna loga ani povinne atributovani
  poskytovatelu se neskryvaji.
- Panorama Mapy.com posila zpet polohu nalezeneho snimku a udalosti
  `pano-place` pri presunu mezi snimky. V puvodni mape ArcGIS Pro se
  vykresluje modry bod s bilym okrajem.
- Google ma volitelne vlastni panoramaticke okno pomoci Maps JavaScript
  API. Vyuziva samostatny Google Maps API klic. S nim se v mape zobrazuje
  oranzovy bod sledujici panorama. Bez Google klice zustava puvodni
  otevreni v systemovem prohlizeci BEZ zpetne synchronizace polohy.
- Pozice jsou docasne graficke prekryvy. Nepridavaji se do vrstev,
  neukladaji se do geodatabaze a nemeni souradnicovy system mapy.
- Body patri mape, ve ktere byl pozadavek vytvoren. Pri zavreni okna
  panoramy se odstrani. Pri nedostupne poloze se skryji; jako nahrada
  se nikdy nepouzije puvodni kliknuty bod.
- Nastaveni obsahuje dve pole pro klice. Neni zde jejich zobrazovani
  ani testovaci tlacitko. Existujici soubor mapy.key i DPAPI ochrana
  zustavaji stejne. Google pouziva oddeleny google.key.

## Jak aktualizovat vase existujici repo

1. Rozbalte ZIP. V GitHub repozitari, kde uz mate PanoramaViewer.csproj
   primo v koreni, zvolte Code > Add file > Upload files.
2. Nahrajte obsah balicku: sest souboru v koreni a SLOZKU Web se dvema
   HTML soubory. Existujici soubory se maji nahradit na stejne ceste.
   Nevytvarejte vedle nich kopie typu PanoramaTools (1).cs.
3. Potvrdte jednim Commit changes. Neupravujte .github/workflows/build.yml
   ani vas funkcni PanoramaViewer.csproj. Novy .cs soubor se zahrne
   automaticky a soubor Web/google.html pokryva stavajici Web/*.html.
4. V Actions vyckejte na dokonceni. Z uspesneho behu stahnete Artifacts >
   PanoramaViewer-ArcGISPro-3.7. Rozbalte a instalujte .esriAddInX pri
   zavrenem ArcGIS Pro. Add-in ma STEJNE ID, novou verzi 1.1.0.
5. Po restartu overte funkcnost nejprve v bezne 2D mape.

Pozor: nove HTML soubory patri do Web, nikoliv do korene repozitare!

## Google: vlastni okno a API klic

V Google Cloud potrebujete vlastni projekt, povolenou Maps JavaScript API
aplikaci a zapnutou fakturaci. Dynamicke Street View muze byt zpoplatneno.
Klic Mapy.com se pro Google pouzit neda. Realne klice v tomto archivu nejsou.
Klic zadejte az v Nastaveni add-inu, nikoliv do GitHubu nebo do chatu.

Pro novy Google klic nastavte API restriction na Maps JavaScript API.
V pripade omezeni podle webu povolte tuto adresu lokalniho WebView2 rozhrani:

    https://panorama.local/*

Jde o virtualni HTTPS host namapovany na lokalni HTML v add-inu,
ne o verejny web, na ktery se nahravaji vase data.
Puvodni Mapy.com host zustal nezmeneny. Pokud nastavite omezeni jeho
klice podle webu, jde take o host panorama.local.

Pokud Google klic odstranime (ulozime prazdne pole), Google se pri dalsim
kliknuti opet otevre v beznem prohlizeci. Zmena klice se projevi pri dalsim
kliknuti do mapy; Google pri zmene klice znovu nacte sve panoramaticke okno.

Do poskytovatelu se posilaji souradnice vybraneho bodu a jejich prislusny
API klic. Add-in nenacita skutecnou GPS polohu uzivatele a neodesila
vrstvy mapy. API klic je sifrovany na disku pomoci Windows DPAPI pro
aktualniho uzivatele; pro volani API se pouziva rozkodovany v pameti,
coz je nutne. Sifrovani souboru nenahrazuje omezeni a kvoty API klice.
Pred firemnim nasazenim overte podminky poskytovatelu, zejmena podminky
Google pro vas fakturacni region, pouziti obsahu s mapou, ochranu soukromi
a povinne atributovani. Toto je technicky prototyp, nikoliv pravni audit.

## Co otestovat

- Klik v 2D mape s WGS 84, Web Mercatorem nebo S-JTSK.
- Po nacteni neni text 'Panorama nacteno ...'.
- Modry bod lezi na SKUTECNE poloze snimku Mapy.com, ktera muze byt vedle
  kliknuteho mista (vyhledavani je do vzdalenosti 80 m).
- Pri klikani na sipky v panorame se modry bod presouva v ArcGIS Pro.
- Po zavreni okna bod zmizi; vrstvy a souradnicovy system jsou beze zmeny.
- Zmena Mapy.com klice v Nastaveni, novy klik v mape, restart aplikace.
- Bez Google klice funguje dosavadni externi prohlizec.
- Se spravnym Google klicem se otevre Street View uvnitr vlastniho okna;
  pri prochazeni se pohybuje oranzovy bod.
- Pri nedostupnem panoramatu, neplatnem klici nebo internetu zustane
  viditelna chyba, nikoliv falesna poloha nebo hlaska o uspechu.
- Pri rychlem opakovanem klikani zustane aktivni posledni pozadovana poloha.

Synchronizace je navrzena pro 2D MapView. Zobrazeni bodu v 3D Scene,
automaticke posouvani mapy za bodem a sipka smeru pohledu nejsou soucasti
 teto aktualizace. Bod ve vzdalenem miste muze byt mimo aktualni vyrez mapy.

## Opravdu provedene kontroly

- Platnost XML projektu a Config.daml, shoda DAML tridy / C# tridy.
- Zachovani puvodniho ID a spravneho nastaveni Config.daml pro baleni.
- 12 offline testu JavaScriptu s nahradnimi objekty API: udalosti pozice,
  nacitani, chyby, zmena klice, stare asynchronni odpovedi, zadne klice
  ve zpravach zpet do nativniho okna a znovupouziti Google vieweru.

Nebylo provedeno: C# sestaveni tohoto updatu ve Windows, instalace nebo
spusteni v ArcGIS Pro 3.7, skutecne overeni obou API. Udalostni data
Mapy.com jsou zpracovana obranne podle poli lon/lat a beznych obalu;
konkretni podobu `pano-place` i realny posun bodu musi potvrdit test
v ArcGIS Pro. Zadne volani s vasim skutecnym API klicem nebylo provedeno.

## Zdroje dokumentace pouzite pro implementaci

Mapy.com JS Panorama (info.lon/lat, pano-place, chybove kody):
https://developer.mapy.com/cs/js-panorama/
Google Street View a jeho udalosti:
https://developers.google.com/maps/documentation/javascript/streetview
https://developers.google.com/maps/documentation/javascript/examples/streetview-events
Google klice a fakturace:
https://developers.google.com/maps/documentation/javascript/usage-and-billing
ArcGIS Pro 3.7, docasna grafika AddOverlay / UpdateOverlay:
https://doc.esri.com/en/arcgis-pro/latest/sdk/api-reference/topic27596.html
