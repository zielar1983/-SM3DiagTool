# SM3 DiagTool - Oprogramowanie diagnostyczne dla Scanmatik 3

Profesjonalne narzedzie diagnostyczne dla interfejsu **Scanmatik 3 (SM3)** wykorzystujace standard **SAE J2534 Pass-Thru API**.

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
- Opis bledow po polsku

### Dane na zywo
- Skanowanie obslugiwanych PID-ow
- Monitorowanie parametrow w czasie rzeczywistym
- Konfigurowalna czestotliwosc odswiezania
- Obsluga standardowych PID-ow OBD-II:
  - Obroty silnika (RPM)
  - Predkosc pojazdu
  - Temperatura plynu chlodniczego
  - Obciazenie silnika
  - Cisnienie w kolektorze
  - Pozycja przepustnicy
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
git clone https://github.com/zielar1983/SM3DiagTool.git
cd SM3DiagTool
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

## Obslugiwane protokoly

| Protokol | Opis | Zastosowanie |
|----------|------|-------------|
| ISO 15765 (CAN-TP) | Transport CAN z segmentacja | Diagnostyka UDS, OBD-II |
| CAN 2.0 | Surowe ramki CAN | Monitoring magistrali |
| CAN-FD | CAN z elastyczna predkoscia danych | Nowoczesne pojazdy |
| ISO 14230 (KWP2000) | Diagnostyka K-Line | Starsze pojazdy (2000+) |
| ISO 9141 | Starsza diagnostyka K-Line | Pojazdy sprzed 2008 |
| J1850 VPW/PWM | Protokoly GM/Ford | Starsze pojazdy US |
| DoIP | Diagnostyka przez Ethernet | Najnowsze pojazdy |

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
│   └── CanBus.cs            # Surowy CAN
├── Models/          # Modele danych
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF XAML Views
├── Logging/         # System logowania
└── Helpers/         # Klasy pomocnicze
```

## Technologie

- **C# / .NET 8.0** - Platforma
- **WPF** - Interfejs uzytkownika
- **MVVM** - Wzorzec architektoniczny
- **Serilog** - Logowanie
- **CommunityToolkit.Mvvm** - Narzedzia MVVM

## Licencja

Ten projekt jest udostepniany do uzytku osobistego i edukacyjnego.

## Uwagi

- Przed uzyciem funkcji programowania ECU upewnij sie, ze masz odpowiednia wiedze.
- Nieprawidlowe operacje na ECU moga spowodowac uszkodzenie sterownika.
- Autor nie ponosi odpowiedzialnosci za szkody wynikle z uzycia tego oprogramowania.
