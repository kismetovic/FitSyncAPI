# FITSync API

ASP.NET Core 8 Web API za FITSync sistem upravljanja za oersibakbe trenere. Čista arhitektura / DDD sa SQL Serverom, RabbitMQ-om i JWT autentifikacijom.

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

| Uloga | Email | Lozinka |
|-------|-------|---------|
| Administrator | `fitsync@gmail.com` | `Admin123!` |
| Klijent | `user@fitsync.com` | `User123!` |


Za PayPal testiranje koristiti account:
email: sb-k1g47w46242586@business.example.com
password: .O<)H#7m
---

## Struktura projekta

```
FITSync/
├── FITSync/                  # WebAPI — Kontroleri, Program.cs, Dockerfile
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

| Metoda | Putanja | Autorizacija | Opis |
|--------|---------|--------------|------|
| POST | `/api/auth/login` | Ne | Prijava, vraća JWT token |
| POST | `/api/auth/register` | Ne | Registracija novog klijenta |
| GET | `/api/auth/me` | JWT | Dohvati trenutnog korisnika |
| PUT | `/api/auth/change-password` | JWT | Promjena lozinke |
| GET | `/api/Trainings/search` | JWT | Pretraga treninga |
| GET | `/api/Reservations/my` | JWT | Moje rezervacije |
| POST | `/api/Reservations` | JWT | Kreiranje rezervacije |
| GET | `/api/Notifications/mine` | JWT | Moje notifikacije |
| POST | `/api/Payments/paypal/create-order` | JWT | Kreiranje PayPal narudžbe, vraća approvalUrl |
| POST | `/api/Payments/paypal/capture` | JWT | Potvrda odobrene PayPal narudžbe |
| POST | `/api/Payments/confirm` | JWT | Bilježenje uplate nakon PayPal/gotovinska plaćanja |
| GET | `/api/Dashboard/stats` | Admin | Statistike dashboarda |
| GET | `/api/Users` | Admin | Lista korisnika |
| GET | `/api/Reviews` | Admin | Lista recenzija |

Kompletna interaktivna dokumentacija dostupna na `/swagger`.

---

## Servisi

| Servis | Port | Napomena |
|--------|------|----------|
| API | 5000 | Mapira se na kontejnerski port 8080 |
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
