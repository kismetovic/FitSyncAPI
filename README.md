# FITSync API

ASP.NET Core 8 Web API za FITSync — sistem za upravljanje teretanom i personalnim treninzima.
Čista arhitektura / DDD, SQL Server, RabbitMQ i JWT autentifikacija.

---

## Dokumentacija

| Dokument | Sadržaj |
|---|---|
| [`docs/RECOMMENDER.md`](docs/RECOMMENDER.md) | Kako radi sistem preporuka — signali, bodovanje, fallback i zašto je neki trening preporučen korisniku |
| `/swagger` | Interaktivna dokumentacija svih endpointa, dostupna dok API radi |

---

## Preduvjeti

| Alat | Verzija |
|------|---------|
| Docker | ≥ 24.x |
| Docker Compose | ≥ 2.x |

Za lokalni razvoj bez Dockera:

| Alat | Verzija |
|------|---------|
| .NET SDK | 8.x |
| SQL Server | 2019+ ili localdb |

---

## Brzi početak (Docker — Preporučeno)

```bash
cd FITSync

# 1. Kopiraj primjer env fajla
copy .env.example .env   # Windows
cp .env.example .env     # Mac/Linux

# 2. Pregledaj .env — podrazumijevane vrijednosti rade za lokalni razvoj

# 3. Pokreni sve servise
docker-compose up

# API dostupan na:    http://localhost:5000
# Swagger UI:         http://localhost:5000/swagger
# RabbitMQ UI:        http://localhost:15672  (guest / guest)
```

Baza podataka se automatski kreira pri prvom pokretanju i popunjava početnim podacima.

---

## Varijable okoline

Svi osjetljivi podaci upravljaju se putem `.env` fajla kojeg treba exportovati i dodati u isti folder u kojem je i docker-compose.yml file.
---

## Podrazumijevani početni podaci

| Uloga | Email | Lozinka | Ime u aplikaciji |
|-------|-------|---------|------------------|
| Administrator | `fitsync@gmail.com` | `Admin123!` | Glavni Administrator |
| Klijent | `user@fitsync.com` | `User123!` | Amar Selimović |

Prijava ide na polje `userNameOrEmail`, ne `email`.

Seeder puni **praznu** bazu i ne radi ništa na bazi koja već ima podatke — svaki korak
ima provjeru postojanja, pa je ponovno pokretanje API-ja bezopasno. Sav sadržaj koji
korisnik čita je na bosanskom, jer obje aplikacije startaju na tom jeziku.

Osim kataloga (8 tipova treninga, 15 treninga, 3 trenera sa dostupnošću, 5 dodatnih
usluga, 4 paketa, 10 čestih pitanja i kontakt podrške), seed kreira i stvarnu aktivnost
za klijenta, tako da nijedan ekran ne otvara prazan: 8 rezervacija koje pokrivaju sve
statuse, jedan plaćen mjesečni paket sa 2 od 12 iskorištenih termina, tri naplaćene
uplate (ukupno 174.00 KM), dvije recenzije i četiri notifikacije.

Seedirana aktivnost poštuje ista pravila koja servisi provode, pa u bazi nema stanja koje
aplikacija sama ne bi mogla proizvesti: rezervacija u statusu `Paid` ili `Completed` ima
naplaćenu uplatu iza sebe, paket je aktivan samo zato što je plaćen, recenzija postoji
samo za odrađen termin, a klijent ne drži dva paketa koja pokrivaju iste treninge.



---

## PayPal (sandbox)

Plaćanje ide kroz PayPal **sandbox**, ne kroz pravi PayPal — ništa se stvarno ne
naplaćuje.

Ključevi aplikacije (trgovca) su u `.env` — `PAYPAL_CLIENT_ID` i `PAYPAL_CLIENT_SECRET`.

Za odobravanje plaćanja na checkout stranici prijavljuje se **personal** (kupac) nalog:

| Nalog | Uloga | Email | Lozinka |
|---|---|---|---|
| Personal (kupac) | **Ovim se plaća.** Njime se prijavljujete kada aplikacija otvori PayPal checkout | `sb-9hysk52646708@personal.example.com` | `4E5g#1L3` |

Business nalogom se **ne kupuje** — on je nalog teretane koji prima uplate i služi za
prijavu na sandbox dashboard. Pokušaj prijave business nalogom na checkout stranici ne
prolazi; to je najčešća zamjena.

*Ovim nalogom je uplata i stvarno prošla:* rezervacija od 16.00 KM naplaćena kao 8.18 EUR,
capture `COMPLETED`, transaction id upisan, rezervacija prešla u „Plaćeno".

> **Zemlja naloga je bitna.** Prvi par sandbox naloga bio je registrovan na **BiH**, i
> capture je uvijek padao sa `COMPLIANCE_VIOLATION`: narudžba bi uredno došla do
> `APPROVED`, ali PayPal ne dozvoljava nalogu iz BiH da *primi* uplatu. Isti kvar javljao
> se i u EUR i u USD, i sa i bez adrese za dostavu — dakle nije do valute ni do zahtjeva,
> nego do zemlje. Sa **američkim** parom naloga capture prolazi iz prve. Ako praviš nove
> sandbox naloge, biraj zemlju koja smije primati uplate.

### Tok plaćanja

1. Aplikacija zove `POST /api/Payments/paypal/create-order` sa **samo** `reservationId`.
   Server čita iznos iz rezervacije i otvara narudžbu kod PayPal-a.
2. Aplikacija otvara zvanični `approvalUrl` u sistemskom pregledniku. Klijent se prijavljuje
   svojim PayPal nalogom — aplikacija nikada ne vidi ni ne traži tu lozinku.
3. Povratak u aplikaciju je signal da je korisnik gotov: provjera kreće sama, server radi
   capture i tek nakon provjere statusa, iznosa, valute i reference upisuje uplatu i
   prebacuje rezervaciju u `Paid`.

Cijene su u KM. PayPal tu valutu ne podržava, pa server konvertuje u eure po fiksnom kursu
(1 EUR = 1.95583 KM) i prije otvaranja PayPal-a prikaže koliko će stvarno biti naplaćeno.

### Provjera statusa narudžbe

Ako plaćanje ne prođe, status narudžbe se može provjeriti direktno kod PayPal-a, bez
prijave na bilo koji nalog — ključevima iz `.env`:

```bash
TOKEN=$(curl -s -u "$PAYPAL_CLIENT_ID:$PAYPAL_CLIENT_SECRET" \
  -d "grant_type=client_credentials" \
  https://api-m.sandbox.paypal.com/v1/oauth2/token | jq -r .access_token)

curl -s -H "Authorization: Bearer $TOKEN" \
  https://api-m.sandbox.paypal.com/v2/checkout/orders/<ORDER_ID> | jq .status
```

| Status | Značenje |
|---|---|
| `CREATED` | Narudžba je otvorena, klijent je **nije odobrio** — ništa nije naplaćeno |
| `APPROVED` | Klijent je odobrio, ali capture još nije urađen |
| `COMPLETED` | Capture je prošao, uplata je evidentirana |

---

## Struktura projekta

```
FITSync/
├── FITSync/                  # WebAPI — Kontroleri, Program.cs, SignalR hub, Dockerfile
├── FITSync.Worker/           # Zaseban worker proces — konzumira RabbitMQ i šalje email
├── FITSync.Contracts/        # DTO-ovi (request/response modeli)
├── FITSync.Domain/           # Entiteti, Enumovi, Domain definicije
├── FITSync.Infrastructure/   # Servisi, Repozitoriji, DbContext, Auth, Seeding
├── docker-compose.yml
├── .env                      # Sadrzan u zip file-u
```

---

## Lokalni razvoj (bez Dockera)

```bash
cd FITSync/FITSync

# Vrati pakete
dotnet restore

# appsettings.json sadrži localdb konekcijski string za lokalni razvoj

# Pokreni
dotnet run

# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

Za lokalne tajne, koristi .NET User Secrets:
```bash
cd FITSync/FITSync
dotnet user-secrets set "JwtSettings:SecretKey" "tvoj-tajni-kljuc"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "tvoj-konekcijski-string"
```

ASP.NET Core automatski čita varijable okoline — `JwtSettings__SecretKey` ima prioritet nad `JwtSettings.SecretKey` iz `appsettings.json`.

PAZITI DA PODACI UNUTAR appsettings.json BUDU UP-TO-DATE SA PODACIMA IZ .env file-a.

---

## Ključni API endpointi

### Autentifikacija i korisnici

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| POST | `/api/Auth/login` | Ne | Prijava, vraća JWT token. Polje je `userNameOrEmail` |
| POST | `/api/Auth/register` | Ne | Registracija novog klijenta |
| GET | `/api/Auth/me` | JWT | Trenutni korisnik |
| PUT | `/api/Auth/change-password` | JWT | Promjena lozinke |
| GET | `/api/Users/search` | Admin | Pretraga korisnika, filter `role` i `name`, paginirano |

### Treninzi i katalog

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| GET | `/api/Trainings/search` | JWT | Pretraga treninga, paginirano |
| GET | `/api/Trainings/recommendations` | JWT | Preporuke sa bodom, strategijom i obrazloženjem |
| GET | `/api/TrainingTypes` | JWT | Tipovi treninga |
| GET | `/api/AdditionalServices` | JWT | Dodatne usluge (pisanje samo admin) |
| GET | `/api/Trainers` | JWT | Treneri i njihova dostupnost (pisanje samo admin) |

### Rezervacije

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| GET | `/api/Reservations/mine` | JWT | Moje rezervacije |
| POST | `/api/Reservations` | JWT | Kreiranje. Vlasnik i početni status dolaze sa servera |
| PATCH | `/api/Reservations/{id}/cancel` | JWT | Otkazivanje uz obavezan razlog |
| PATCH | `/api/Reservations/{id}/approve` | Admin | Odobravanje zahtjeva van rasporeda |
| PATCH | `/api/Reservations/{id}/complete` | Admin | Označavanje odrađenog termina |
| GET | `/api/Reservations/search` | Admin | Pretraga svih rezervacija, paginirano |

> `DELETE /api/Reservations/{id}` namjerno vraća **405**: rezervacije se otkazuju, ne brišu.

### Plaćanja

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| GET | `/api/Payments/mine` | JWT | Moje uplate — i za rezervacije i za pakete |
| POST | `/api/Payments/paypal/create-order` | JWT | PayPal narudžba za rezervaciju, vraća `approvalUrl` |
| POST | `/api/Payments/paypal/capture` | JWT | Server radi capture i verifikaciju |
| POST | `/api/Payments/cash/select` | JWT | Klijent bira plaćanje na recepciji |
| POST | `/api/Payments/cash/confirm` | Admin | Osoblje potvrđuje da je gotovina naplaćena |
| POST | `/api/Payments/membership/paypal/create-order` | JWT | Isto, za kupljeni paket |
| POST | `/api/Payments/membership/paypal/capture` | JWT | Isto, za kupljeni paket |
| POST | `/api/Payments/membership/cash/select` | JWT | Isto, za kupljeni paket |
| POST | `/api/Payments/membership/cash/confirm` | Admin | Isto, za kupljeni paket |
| GET | `/api/Payments/summary` | Admin | Ukupan prihod i broj transakcija |

> Klijent nikada ne šalje iznos ni valutu — server ih čita iz rezervacije odnosno paketa —
> i nikada sam ne potvrđuje da je uplata prošla. Raniji `POST /api/Payments/confirm` je
> uklonjen i vraća **405**.

### Mjesečni paketi

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| GET | `/api/Memberships/packages` | JWT | Paketi u prodaji |
| GET | `/api/Memberships/mine` | JWT | Moji paketi |
| POST | `/api/Memberships/purchase` | JWT | Kupovina. Paket nastaje **neplaćen** |
| PATCH | `/api/Memberships/mine/{id}/cancel` | JWT | Otkazivanje nekorištenog paketa |

### Notifikacije, pomoć i izvještaji

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| GET | `/api/Notifications/mine` | JWT | Moje notifikacije |
| PATCH | `/api/Notifications/{id}/read` | JWT | Označavanje kao pročitano |
| GET | `/api/Faqs/active` | JWT | Objavljena česta pitanja |
| GET | `/api/Support/contact` | JWT | Kontakt podrške |
| PUT | `/api/Support/contact` | Admin | Izmjena kontakta |
| GET | `/api/Reviews` | JWT | Recenzije (izmjena tuđe samo admin) |
| GET | `/api/Dashboard/stats` | Admin | Statistike dashboarda |
| GET | `/api/Reports/reservations` | Admin | Izvještaj o rezervacijama po periodu |
| GET | `/api/Reports/revenue` | Admin | Izvještaj o prihodu po treningu i paketu |

SignalR hub za notifikacije u realnom vremenu: `/hubs/notifications`.

Kompletna interaktivna dokumentacija dostupna na `/swagger`.

---

## Servisi

| Servis | Port | Napomena |
|--------|------|----------|
| API | 5000 | Mapira se na kontejnerski port 8080. Samo objavljuje poruke na RabbitMQ |
| Worker | — | `FITSync.Worker`, konzumira queue i šalje email. Nema pristup bazi |
| SQL Server | 1433 | SA prijava, lozinka iz `SA_PASSWORD` |
| RabbitMQ | 5672 | AMQP |
| RabbitMQ UI | 15672 | `RABBITMQ_USER` / `RABBITMQ_PASS` |

---

## Zaustavljanje / Resetovanje

```bash
# Zaustavi kontejnere
docker-compose down

# Zaustavi i obriši sve podatke (svježa instalacija)
docker-compose down -v
```
