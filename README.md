# SM3 DiagTool - Oprogramowanie diagnostyczne dla Scanmatik 3

Profesjonalne narzedzie diagnostyczne dla interfejsu **Scanmatik 3 (SM3)** wykorzystujace **pelny potencjal** urzadzenia poprzez standard **SAE J2534 Pass-Thru API**.

## Funkcjonalnosci

### Polaczenie z SM3
- Automatyczne wykrywanie urzadzen J2534 w rejestrze Windows
- Polaczenie USB lub Wi-Fi
- Odczyt wersji firmware, DLL, API
- Odczyt napiecia akumulatora (V)

### Kody bledow (DTC)
- Odczyt kodow bledow przez OBD-II, UDS i KWP2000
- Kasowanie kodow bledow
- Eksport do CSV
- Baza 200+ kodow bledow z opisami po polsku (kategorie P, C, B, U)

### Dane na zywo (42 parametry OBD-II)
- Skanowanie obslugiwanych PID-ow w pojeznie
- Monitorowanie parametrow w czasie rzeczywistym
- Konfigurowalna czestotliwosc odswiezania (ms)

<details>
<summary>Pelna lista 42 obslugiwanych PID-ow</summary>

| PID | Parametr | Jednostka |
|-----|----------|-----------|
| 0x03 | Stan ukladu paliwowego | - |
| 0x04 | Obciazenie silnika | % |
| 0x05 | Temperatura plynu chlodniczego | °C |
| 0x06 | Korekcja krotkoterminowa B1 | % |
| 0x07 | Korekcja dlugoterminowa B1 | % |
| 0x0A | Cisnienie paliwa | kPa |
| 0x0B | Cisnienie w kolektorze (MAP) | kPa |
| 0x0C | Obroty silnika (RPM) | RPM |
| 0x0D | Predkosc pojazdu | km/h |
| 0x0E | Wyprzedzenie zaplonu | ° |
| 0x0F | Temperatura powietrza dolotowego | °C |
| 0x10 | Przeplyw powietrza (MAF) | g/s |
| 0x11 | Pozycja przepustnicy | % |
| 0x14 | Sonda lambda B1S1 napiecie | V |
| 0x15 | Sonda lambda B1S2 napiecie | V |
| 0x1C | Standard OBD | - |
| 0x1F | Czas pracy silnika | s |
| 0x21 | Dystans z lampka MIL | km |
| 0x22 | Cisnienie szyny paliwowej (wzgledne) | kPa |
| 0x23 | Cisnienie szyny paliwowej (bezwzgledne) | kPa |
| 0x24 | Lambda B1S1 stosunek rownowaznikowy | - |
| 0x2C | Sterowane EGR | % |
| 0x2D | Blad EGR | % |
| 0x2E | Sterowanie odparowaniem (EVAP) | % |
| 0x2F | Poziom paliwa | % |
| 0x30 | Liczba rozgrzewek od skasowania DTC | - |
| 0x31 | Dystans od skasowania DTC | km |
| 0x33 | Cisnienie barometryczne | kPa |
| 0x34 | Sonda lambda B1S1 prad | mA |
| 0x42 | Napiecie modulu sterujacego | V |
| 0x43 | Bezwzgledne obciazenie silnika | % |
| 0x44 | Stosunek paliwowy (lambda) | - |
| 0x45 | Wzgledna pozycja przepustnicy | % |
| 0x46 | Temperatura otoczenia | °C |
| 0x47 | Pozycja przepustnicy B | % |
| 0x49 | Pozycja pedalu gazu D | % |
| 0x4A | Pozycja pedalu gazu E | % |
| 0x4C | Wymagany moment obrotowy | % |
| 0x4D | Czas pracy z lampka MIL | min |
| 0x51 | Typ paliwa | - |
| 0x5A | Wzgledna pozycja pedalu gazu | % |
| 0x5B | Zywotnosc akumulatora hybrydowego | % |
| 0x5C | Temperatura oleju silnikowego | °C |
| 0x5E | Zuzycie paliwa | L/h |

</details>

### Logger CAN Bus
- Przechwytywanie surowych ramek CAN
- Obsluga CAN 2.0 i CAN-FD
- Predkosci: 125k, 250k, 500k, 1M baud
- Wysylanie wlasnych ramek CAN
- Logowanie do pliku (CSV, ASC — format Vector CANalyzer)
- Eksport przechwyconych danych

### Informacje o ECU
- Odczyt VIN (numer nadwozia) przez UDS i OBD-II
- Identyfikacja ECU: nazwa, producent, numer czesci, numer seryjny
- Wersje oprogramowania i hardware, identyfikator kalibracji
- Skanowanie wszystkich ECU na magistrali CAN (adresy 0x7E0-0x7EF)

### Terminal UDS
- Bezposrednia komunikacja z ECU w formacie hex
- 12 predefiniowanych komend diagnostycznych
- Automatyczna interpretacja odpowiedzi
- Konfigurowalne adresy TX/RX i predkosc

### Flash / Pamiec ECU
- Odczyt pamieci ECU (RequestUpload + TransferData)
- Zapis pamieci ECU (RequestDownload + TransferData)
- Kasowanie pamieci (EraseMemory routine)
- ReadMemoryByAddress / WriteMemoryByAddress
- Edytor hex z podgladem ASCII
- Zapis/odczyt z pliku BIN/HEX
- Pasek postepu transferu
- Automatyczna autoryzacja Security Access przed operacjami

### Sterowanie IO (Input/Output Control)

Aktywacja/dezaktywacja urzadzen wykonawczych (aktuatorow) z mozliwoscia zamrozenia stanu i resetu do sterowania ECU:

| Aktuator | ID |
|----------|-----|
| Wentylator chlodnicy | 0xF005 |
| Sprzeglo klimatyzacji | 0xF006 |
| Pompa paliwa | 0xF009 |
| Lampka MIL (check engine) | 0xF00A |
| Klakson | 0xF00B |
| Swiatla przednie | 0xF00C |
| Swiatla tylne | 0xF00D |
| Swiatla hamowania | 0xF00E |
| Zamek drzwi | 0xF00F |
| Szyba | 0xF010 |
| Wycieraczki | 0xF012 |
| Spryskiwacze | 0xF013 |
| Silnik przepustnicy | 0xF003 |
| Zawor EGR | 0xF004 |
| Grzalka sondy lambda | 0xF008 |

### Procedury serwisowe (Routine Control)

| Procedura | ID | Opis |
|-----------|----|------|
| Reset wartosci wyuczonych | 0x0301 | Przywraca nastawione wartosci |
| Regeneracja DPF | 0x0401 | Wymuszenie regeneracji filtra czastek |
| Odpowietrzanie hamulcow | 0x0501 | Procedura ABS |
| Kalibracja kata skretu | 0x0601 | Czujnik SAS |
| Adaptacja przepustnicy | 0x0701 | Nauka polozenia przepustnicy |
| Kodowanie wtryskiwaczy | 0x0801 | Po wymianie wtryskiwaczy |
| Rejestracja akumulatora | 0x0901 | Po wymianie akumulatora |
| Reset przegladu oleju | 0x0A01 | Po wymianie oleju |
| Reset intervalu serwisowego | 0x0A02 | Po przegladzie |

### Protokol DoIP (Diagnostyka przez Ethernet)
- Wykrywanie modulow DoIP na magistrali
- Aktywacja routingu (RoutingActivation)
- Diagnostyka UDS przez Ethernet
- Odczyt statusu encji i trybu zasilania

### Security Access
- Autoryzacja na wielu poziomach (Level 1-11)

| Algorytm | Zastosowanie |
|----------|-------------|
| SimpleXOR | Podstawowe ECU |
| CRC32 | Ogolne zastosowanie |
| VAG | Volkswagen, Audi, Skoda, SEAT |
| BMW | BMW, MINI |
| Mercedes-Benz | Mercedes, Smart |
| Generic | Uniwersalny |
| Custom callback | Wlasne algorytmy uzytkownika |

## Obslugiwane protokoly

| Protokol | Standard | Zastosowanie |
|----------|----------|-------------|
| ISO 15765 (CAN-TP) | SAE J2534 | Diagnostyka UDS, OBD-II (wiekszosc aut 2008+) |
| CAN 2.0 | J2534 | Surowy monitoring magistrali |
| CAN-FD | J2534 | Nowoczesne pojazdy z szybszym CAN |
| ISO 14230 (KWP2000) | J2534 | Starsze pojazdy europejskie (2000-2008) |
| ISO 9141 | J2534 | Starsza diagnostyka K-Line |
| J1850 VPW | J2534 | Starsze pojazdy GM (USA) |
| J1850 PWM | J2534 | Starsze pojazdy Ford (USA) |
| Single Wire CAN | J2534 | Pojazdy GM (GMLAN) |
| DoIP (ISO 13400) | J2534 | Diagnostyka przez Ethernet (najnowsze auta) |

## Wymagania systemowe

### Oprogramowanie
- **System operacyjny:** Windows 10/11 (x64)
- **Framework:** .NET 8.0 SDK (do budowania) lub .NET 8.0 Runtime (do uruchomienia)
- **IDE (opcjonalnie):** Visual Studio 2022 (wersja 17.8+)
- **Sterownik SM3:** Scanmatik J2534 Pass-Thru Driver (wersja 3.x lub nowsza)
  - Pobierz z: https://scanmatik.pro/pages/downloads
  - Sterownik rejestruje sie w `HKLM\SOFTWARE\PassThruSupport.04.04`
  - Obslugiwane firmware SM3: 3.0.x i nowsze

### Sprzet
- **Interfejs:** Scanmatik 3 (SM3)
- **Polaczenie:** USB 2.0/3.0 (zalecane do flash) lub Wi-Fi
- **Port USB:** wolny port USB-A lub adapter USB-C
- **Kabel OBD-II:** dostarczany z SM3

### Uprawnienia systemowe
- **Administrator:** wymagane uruchomienie jako Administrator (dostep do rejestru Windows i sterownikow J2534)
- **Windows Defender / Antywirus:** aplikacja moze byc blednie rozpoznana jako zagrożenie ze wzgledu na bezposredni dostep do sterownikow. Jesli Windows Defender blokuje uruchomienie, dodaj folder aplikacji do wykluczeń:
  - Ustawienia → Aktualizacja i zabezpieczenia → Zabezpieczenia Windows → Ochrona przed wirusami → Ustawienia → Wyklaczenia → Dodaj wyklaczenie → Folder
- **Zapora sieciowa:** jesli uzywasz polaczenia Wi-Fi z SM3, upewnij sie ze zapora nie blokuje komunikacji na portach SM3

## Instalacja i uruchomienie

### 1. Zainstaluj .NET 8.0 SDK
Pobierz z: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Zainstaluj sterownik Scanmatik 3
Pobierz z: https://scanmatik.pro/pages/downloads  
Po instalacji sprawdz w Menedzerze urzadzen, czy SM3 jest widoczny.

### 3. Sklonuj repozytorium
```bash
git clone https://github.com/zielar1983/-SM3DiagTool.git
```
```bash
cd -- -SM3DiagTool
```

> **Uwaga:** Nazwa katalogu zaczyna sie od myslnika. Uzyj `cd -- -SM3DiagTool` lub `cd ./-SM3DiagTool`.

### 4. Zbuduj projekt
```bash
dotnet build SM3DiagTool.sln -c Release
```

### 5. Uruchom aplikacje
```bash
dotnet run --project src/SM3DiagTool/SM3DiagTool.csproj
```

Lub otwórz `SM3DiagTool.sln` w Visual Studio 2022 i nacisnij F5.

> **Wazne:** Uruchom aplikacje jako Administrator (prawy klik → "Uruchom jako administrator").

## Weryfikacja instalacji

Po pierwszym uruchomieniu wykonaj te kroki, zeby sprawdzic, czy wszystko dziala:

### Test 1: Wykrywanie urzadzenia
1. Podlacz SM3 przez USB
2. Uruchom aplikacje jako Administrator
3. Kliknij **"Skanuj urzadzenia"** w zakladce Polaczenie
4. SM3 powinien pojawic sie na liscie jako "Scanmatik 3"
5. Jesli nie widac urzadzenia — sprawdz sterownik w Menedzerze urzadzen

### Test 2: Polaczenie z SM3
1. Wybierz SM3 z listy i kliknij **"Polacz"**
2. Powinny wyswietlic sie: wersja firmware, wersja DLL, wersja API
3. Napiecie akumulatora powinno pokazac wartosc (np. 12.4V jesli podlaczony do OBD)

### Test 3: Odczyt OBD-II (z pojazdem)
1. Podlacz SM3 do zlacza OBD-II w pojeznie (stacyjka ON lub silnik uruchomiony)
2. Przejdz do **"Kody bledow (DTC)"** → wybierz OBD-II → kliknij **"Odczytaj DTC"**
3. Przejdz do **"Dane na zywo"** → kliknij **"Skanuj PID-y"** → **"Start"**
4. Obroty i predkosc powinny sie aktualizowac

### Rozwiazywanie problemow
| Problem | Rozwiazanie |
|---------|-------------|
| SM3 nie widoczny po skanowaniu | Sprawdz sterownik w Menedzerze urzadzen, reinstaluj driver |
| Blad "Access Denied" | Uruchom jako Administrator |
| Brak odpowiedzi z pojazdu | Sprawdz polaczenie OBD, upewnij sie ze stacyjka jest ON |
| Blad J2534 ERR_DEVICE_NOT_CONNECTED | Odlacz i podlacz ponownie USB, restartuj aplikacje |
| Timeout przy odczycie | Zwieksz timeout lub sprawdz protokol (CAN vs K-Line) |

## Kompatybilnosc

### Scanmatik 3
- **Firmware:** 3.0.x, 3.1.x, 3.2.x i nowsze
- **Sterownik J2534:** wersja 4.04 (SAE J2534-1/2)
- **Polaczenie USB:** pelna predkosc, zalecane do operacji flash
- **Polaczenie Wi-Fi:** pelna funkcjonalnosc diagnostyki, nie zalecane do flash

### Pojazdy
Aplikacja jest kompatybilna z pojazdami osobowymi obslugujacymi standard OBD-II (pojazdy benzynowe od 2001, diesel od 2004 w EU). Funkcje zaawansowane (UDS, KWP2000, IO Control) zaleza od konkretnego producenta i modelu.

## Architektura

```
-SM3DiagTool/
├── SM3DiagTool.sln              # Plik rozwiazania Visual Studio
├── .gitignore                   # Ignorowane pliki
├── README.md                    # Dokumentacja
├── src/SM3DiagTool/
│   ├── SM3DiagTool.csproj       # Konfiguracja projektu (.NET 8.0, WPF)
│   ├── App.xaml / App.xaml.cs   # Punkt wejscia, konfiguracja Serilog, DataTemplates
│   ├── MainWindow.xaml / .cs    # Glowne okno z nawigacja
│   ├── J2534/                   # Warstwa P/Invoke J2534 API
│   │   ├── J2534Api.cs          # Glowny wrapper DLL (LoadLibrary, delegates)
│   │   ├── J2534Device.cs       # Zarzadzanie urzadzeniem
│   │   ├── J2534Channel.cs      # Zarzadzanie kanalem protokolowym
│   │   ├── J2534Definitions.cs  # Enumy (Protocol, Error, Ioctl, Flags)
│   │   ├── J2534Structs.cs      # Struktury (PassThruMsg, SConfig)
│   │   └── J2534Exception.cs    # Wyjatki z kodami bledow
│   ├── Protocols/               # Implementacje protokolow diagnostycznych
│   │   ├── ObdII.cs             # OBD-II (SAE J1979)
│   │   ├── Uds.cs               # UDS (ISO 14229)
│   │   ├── Kwp2000.cs           # KWP2000 (ISO 14230)
│   │   ├── CanBus.cs            # Surowy CAN / CAN-FD
│   │   ├── DoIP.cs              # Diagnostyka Ethernet (ISO 13400)
│   │   ├── J1850.cs             # J1850 VPW/PWM
│   │   ├── Iso9141.cs           # ISO 9141
│   │   ├── SingleWireCan.cs     # GMLAN Single Wire CAN
│   │   ├── FlashManager.cs      # Flash ECU (upload/download/erase)
│   │   ├── IoControl.cs         # Sterowanie IO + RoutineManager
│   │   └── SecurityManager.cs   # Security Access z algorytmami
│   ├── Models/                  # Modele danych
│   │   ├── DiagnosticTroubleCode.cs  # Kody bledow + enum DtcStatus
│   │   ├── PidData.cs                # Definicje 42 PID-ow OBD-II
│   │   ├── KnownDtcCodes.cs          # Baza 200+ kodow DTC (PL)
│   │   ├── CanMessage.cs             # Model ramki CAN
│   │   └── EcuInformation.cs         # Informacje ECU + VehicleInfo
│   ├── ViewModels/              # MVVM ViewModels (8 widokow)
│   │   ├── MainViewModel.cs
│   │   ├── ConnectionViewModel.cs
│   │   ├── DtcViewModel.cs
│   │   ├── LiveDataViewModel.cs
│   │   ├── CanLoggerViewModel.cs
│   │   ├── EcuInfoViewModel.cs
│   │   ├── UdsTerminalViewModel.cs
│   │   ├── FlashViewModel.cs
│   │   └── IoControlViewModel.cs
│   ├── Views/                   # WPF XAML Views
│   │   ├── ConnectionView.xaml / .cs
│   │   ├── DtcView.xaml / .cs
│   │   ├── LiveDataView.xaml / .cs
│   │   ├── CanLoggerView.xaml / .cs
│   │   ├── EcuInfoView.xaml / .cs
│   │   ├── UdsTerminalView.xaml / .cs
│   │   ├── FlashView.xaml / .cs
│   │   └── IoControlView.xaml / .cs
│   ├── Logging/                 # System logowania
│   │   ├── CanLogger.cs         # Logger CAN (CSV, ASC)
│   │   └── SessionLogger.cs    # Logger sesji diagnostycznej
│   └── Helpers/                 # Klasy pomocnicze MVVM
│       ├── ObservableObject.cs
│       ├── RelayCommand.cs
│       └── BoolToVisibilityConverter.cs
└── docs/
    └── preview.html             # Podglad GUI (mockup HTML)
tests/SM3DiagTool.Tests/
├── SM3DiagTool.Tests.csproj     # Projekt testow (xUnit + Moq)
├── SecurityManagerTests.cs      # Testy algorytmow Security Access
├── KnownDtcCodesTests.cs        # Testy bazy kodow DTC
└── PidDataTests.cs              # Testy parserow PID-ow OBD-II
```

### 8 widokow GUI (MVVM)

| Widok | ViewModel | Opis |
|-------|-----------|------|
| ConnectionView | ConnectionViewModel | Skanowanie urzadzen, polaczenie, firmware |
| DtcView | DtcViewModel | Odczyt/kasowanie kodow bledow, eksport CSV |
| LiveDataView | LiveDataViewModel | Monitorowanie PID-ow w czasie rzeczywistym |
| CanLoggerView | CanLoggerViewModel | Przechwytywanie/wysylanie ramek CAN |
| EcuInfoView | EcuInfoViewModel | VIN, identyfikacja ECU, skanowanie modulow |
| UdsTerminalView | UdsTerminalViewModel | Bezposrednia komunikacja hex z ECU |
| FlashView | FlashViewModel | Odczyt/zapis/kasowanie pamieci ECU |
| IoControlView | IoControlViewModel | Sterowanie aktuatorami, procedury serwisowe |

### Status implementacji protokolow

| Protokol | Plik | Status |
|----------|------|--------|
| OBD-II (ISO 15031) | ObdII.cs | Kompletny — DTC, PID-y, VIN, kalibracja |
| UDS (ISO 14229) | Uds.cs | Kompletny — sesje, DID, DTC, security, routine, transfer |
| KWP2000 (ISO 14230) | Kwp2000.cs | Kompletny — K-Line i CAN, obie inicjalizacje |
| CAN Bus | CanBus.cs | Kompletny — CAN 2.0, CAN-FD, surowe ramki |
| DoIP (ISO 13400) | DoIP.cs | Kompletny — discovery, routing, UDS over ETH |
| J1850 VPW | J1850.cs | Kompletny — 10400 baud (GM) |
| J1850 PWM | J1850.cs | Kompletny — 41600 baud (Ford) |
| ISO 9141 | Iso9141.cs | Kompletny — 5-baud init, passive listen |
| Single Wire CAN | SingleWireCan.cs | Kompletny — GMLAN 33333/500000 baud |
| Flash/Memory | FlashManager.cs | Kompletny — download, upload, erase, block transfer |
| IO Control | IoControl.cs | Kompletny — 15 aktuatorow, freeze, reset |
| Routine Control | IoControl.cs | Kompletny — 9 procedur serwisowych |
| Security Access | SecurityManager.cs | Kompletny — 6 algorytmow + custom callback |

## Testy jednostkowe

Projekt zawiera testy jednostkowe dla kluczowych komponentow:

```bash
dotnet test SM3DiagTool.sln
```

| Plik testow | Co testuje |
|-------------|-----------|
| SecurityManagerTests.cs | 8 testow algorytmow seed-key (SimpleXOR, CRC32, VAG, BMW, Mercedes, Generic) |
| KnownDtcCodesTests.cs | 7 testow bazy DTC (200+ kodow, kategorie P/C/B/U, format, opisy) |
| PidDataTests.cs | 10 testow parserow PID (RPM, predkosc, temperatura, lambda, zuzycie paliwa) |

Testy nie wymagaja podlaczonego SM3 — testuja logike obliczen i dane statyczne.

## Technologie

- **C# / .NET 8.0** - Platforma
- **WPF** - Interfejs uzytkownika (ciemny motyw)
- **MVVM** - Wzorzec architektoniczny
- **Serilog** - Logowanie strukturalne (pliki w `%LOCALAPPDATA%\SM3DiagTool\logs\`)
- **CommunityToolkit.Mvvm** - Narzedzia MVVM

## Licencja

Ten projekt jest udostepniany do uzytku osobistego i edukacyjnego.

## Ostrzezenia i bezpieczenstwo

**OGOLNE:**
- Uruchom aplikacje jako **Administrator** — jest to wymagane do dostepu do sterownikow J2534.
- Zawsze polaczaj SM3 przez **USB** do operacji flashowania — Wi-Fi moze byc niestabilne.
- Przed kazda operacja flash/zapisu zrob **backup oryginalnego oprogramowania ECU**.

**FLASH / PAMIEC ECU:**
- Nieprawidlowy zapis pamieci moze **trwale uszkodzic sterownik (brick ECU)**.
- Nigdy nie odlaczaj zasilania ani kabla USB podczas operacji flash.
- Upewnij sie, ze akumulator pojazdu jest naladowany (min. 12.5V).
- Operacje flash wymagaja odpowiedniej wiedzy o strukturze pamieci danego ECU.

**STEROWANIE IO (AKTUATORY):**
- Aktywacja aktuatorow moze miec **natychmiastowy wplyw na pojazd** (np. odblokowanie zamkow, uruchomienie wentylatora).
- **Nie uzywaj sterowania IO podczas jazdy** — tylko na postoju z zaciagnietym hamulcem.
- Po zakonczeniu testow zawsze kliknij **"Reset do ECU"** aby przywrocic normalne sterowanie.

**PROCEDURY SERWISOWE:**
- Regeneracja DPF wymaga spelnienia warunkow (temperatura silnika, poziom oleju).
- Kodowanie wtryskiwaczy wymaga prawidlowych kodow IMA z nowych wtryskiwaczy.
- Bledy w procedurach moga wplynac na dzialanie ukladu hamulcowego (odpowietrzanie ABS), ukladu kierowniczego (kalibracja SAS) i innych systemow bezpieczenstwa.

**KOMPATYBILNOSC Z MARKAMI:**
- Algorytmy Security Access (VAG, BMW, Mercedes) sa **przyblizeniami** — konkretne ECU moga uzywac innych wariantow.
- Niektore funkcje zaawansowane (IO Control, Routine Control) moga nie byc obslugiwane przez wszystkie marki i modele.
- W przypadku pojazdow z zabezpieczeniami (np. BMW EWS/CAS, VAG immobilizer) wymagane moze byc dodatkowe oprogramowanie OEM.

**Autor nie ponosi odpowiedzialnosci za szkody wynikle z uzycia tego oprogramowania.**
