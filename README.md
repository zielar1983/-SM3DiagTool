# SM3 DiagTool - Oprogramowanie diagnostyczne dla Scanmatik 3

Profesjonalne narzedzie diagnostyczne dla interfejsu **Scanmatik 3 (SM3)** wykorzystujace **pelny potencjal** urzadzenia poprzez standard **SAE J2534 Pass-Thru API**.

## Funkcjonalnosci

### Polaczenie z SM3
- Automatyczne wykrywanie urzadzen J2534 w systemie
- Polaczenie USB lub Wi-Fi
- Odczyt wersji firmware, DLL, API
- Odczyt napiecia akumulatora

### Kody bledow (DTC)
- Odczyt kodow bledow przez OBD-II, UDS i KWP2000
- Kasowanie kodow bledow
- Eksport do CSV
- Baza 200+ kodow bledow z opisami po polsku (P, C, B, U)

### Dane na zywo
- Skanowanie obslugiwanych PID-ow
- Monitorowanie 40+ parametrow w czasie rzeczywistym
- Konfigurowalna czestotliwosc odswiezania
- Obsluga standardowych PID-ow OBD-II:
  - Obroty silnika (RPM), predkosc pojazdu
  - Temperatura plynu chlodniczego, oleju, otoczenia
  - Obciazenie silnika, cisnienie w kolektorze
  - Pozycja przepustnicy, pedal gazu
  - Sondy lambda (napiecie, prąd, equiv. ratio)
  - Cisnienie szyny paliwowej (CR/GDI)
  - EGR, EVAP, zuzycie paliwa
  - I wiele innych...

### Logger CAN Bus
- Przechwytywanie surowych ramek CAN
- Obsluga CAN 2.0 i CAN-FD
- Rozne predkosci (125k, 250k, 500k, 1M)
- Wysylanie ramek CAN
- Logowanie do pliku (CSV, ASC)
- Eksport przechwyconych danych

### Informacje o ECU
- Odczyt VIN (numer nadwozia)
- Identyfikacja ECU (producent, numer czesci, wersja SW/HW)
- Skanowanie wszystkich ECU na magistrali CAN
- Odczyt przez UDS i OBD-II

### Terminal UDS
- Bezposrednia komunikacja z ECU
- Predefiniowane komendy diagnostyczne
- Interpretacja odpowiedzi
- Obsluga wielu adresow diagnostycznych

### Flash / Pamiec ECU
- Odczyt pamieci ECU (upload)
- Zapis pamieci ECU (download)
- Kasowanie pamieci (erase)
- Edytor hex z podgladem ASCII
- Zapis/odczyt z pliku BIN/HEX
- Pasek postepu transferu
- Automatyczna autoryzacja Security Access

### Sterowanie IO (Input/Output Control)
- Aktywacja urzadzen wykonawczych (aktuatorow):
  - Wentylator chlodnicy, sprzeglo AC
  - Pompa paliwa, lampka MIL
  - Swiatla, klakson, zamki drzwi
  - Szyby, wycieraczki, spryskiwacze
  - Silnik przepustnicy, zawor EGR, grzalka sondy lambda
- Zamrozenie aktualnego stanu
- Reset do sterowania ECU
- Ustawianie wartosci (0-255)

### Procedury serwisowe (Routine Control)
- Reset wartosci wyuczonych
- Wymuszenie regeneracji DPF
- Odpowietrzanie hamulcow
- Kalibracja czujnika kata skretu
- Adaptacja przepustnicy
- Kodowanie wtryskiwaczy
- Rejestracja akumulatora
- Reset przegladu oleju / intervalu serwisowego

### Protokol DoIP (Diagnostyka przez Ethernet)
- Wykrywanie modulow DoIP
- Aktywacja routingu
- Diagnostyka UDS przez Ethernet
- Odczyt statusu encji
- Odczyt trybu zasilania

### Security Access
- Autoryzacja na wielu poziomach (Level 1-11)
- Algorytmy obliczania kluczy:
  - SimpleXOR, CRC32
  - VAG (Volkswagen Group)
  - BMW, Mercedes-Benz
  - Generic
  - Wlasne algorytmy (custom callback)

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

- **System operacyjny:** Windows 10/11 (x64)
- **Framework:** .NET 8.0 Runtime
- **Interfejs:** Scanmatik 3 (SM3) z zainstalowanym sterownikiem J2534
- **Polaczenie:** USB lub Wi-Fi

## Instalacja i uruchomienie

### 1. Zainstaluj .NET 8.0 Runtime
Pobierz z: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Zainstaluj sterownik Scanmatik 3
Pobierz z: https://scanmatik.pro/pages/downloads

### 3. Sklonuj repozytorium
```bash
git clone https://github.com/zielar1983/-SM3DiagTool.git
cd -SM3DiagTool
```

### 4. Zbuduj projekt
```bash
dotnet build SM3DiagTool.sln -c Release
```

### 5. Uruchom aplikacje
```bash
dotnet run --project src/SM3DiagTool/SM3DiagTool.csproj
```

Lub otwórz `SM3DiagTool.sln` w Visual Studio 2022 i nacisnij F5.

## Architektura

```
SM3DiagTool/
├── J2534/           # Warstwa P/Invoke J2534 API
│   ├── J2534Api.cs          # Glowny wrapper DLL
│   ├── J2534Device.cs       # Zarzadzanie urzadzeniem
│   ├── J2534Channel.cs      # Zarzadzanie kanalem
│   ├── J2534Definitions.cs  # Enum-y i stale
│   └── J2534Structs.cs      # Struktury danych
├── Protocols/       # Implementacje protokolow
│   ├── ObdII.cs             # OBD-II (SAE J1979)
│   ├── Uds.cs               # UDS (ISO 14229)
│   ├── Kwp2000.cs           # KWP2000 (ISO 14230)
│   ├── CanBus.cs            # Surowy CAN
│   ├── DoIP.cs              # Diagnostyka Ethernet (ISO 13400)
│   ├── J1850.cs             # J1850 VPW/PWM
│   ├── Iso9141.cs           # ISO 9141
│   ├── SingleWireCan.cs     # GMLAN Single Wire CAN
│   ├── FlashManager.cs      # Flash/pamiec ECU (upload/download)
│   ├── IoControl.cs         # Sterowanie IO + Routine Control
│   └── SecurityManager.cs   # Security Access z algorytmami
├── Models/          # Modele danych
│   ├── DiagnosticTroubleCode.cs  # Kody bledow
│   ├── PidData.cs                # PID-y OBD-II (40+)
│   ├── KnownDtcCodes.cs          # Baza 200+ kodow DTC
│   ├── CanMessage.cs             # Ramki CAN
│   └── EcuInformation.cs        # Informacje o ECU
├── ViewModels/      # MVVM ViewModels (8 widokow)
├── Views/           # WPF XAML Views
├── Logging/         # System logowania (CSV, ASC)
└── Helpers/         # Klasy pomocnicze (MVVM)
```

## Technologie

- **C# / .NET 8.0** - Platforma
- **WPF** - Interfejs uzytkownika (ciemny motyw)
- **MVVM** - Wzorzec architektoniczny
- **Serilog** - Logowanie strukturalne
- **CommunityToolkit.Mvvm** - Narzedzia MVVM

## Licencja

Ten projekt jest udostepniany do uzytku osobistego i edukacyjnego.

## Uwagi

- Przed uzyciem funkcji programowania ECU upewnij sie, ze masz odpowiednia wiedze.
- Nieprawidlowe operacje na ECU moga spowodowac uszkodzenie sterownika.
- Flashowanie ECU wykonuj na wlasne ryzyko — zawsze rob backup oryginalnego oprogramowania.
- Sterowanie IO (aktuatorami) moze miec wplyw na dzialanie pojazdu — uzywaj ostroznie.
- Autor nie ponosi odpowiedzialnosci za szkody wynikle z uzycia tego oprogramowania.
