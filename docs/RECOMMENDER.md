# FitSync — Sistem preporuka (Recommender)

Dokumentacija hibridnog sistema preporuka treninga.

**Implementacija:** `FITSync.Infrastructure/Services/RecommendationService.cs`
**Endpoint:** `GET /api/Trainings/recommendations?limit=10` (zahtijeva autentikaciju)
**Odgovor:** `RecommendedTrainingResponse` — trening + `Score`, `Strategy`, `Reason`, `MatchedSignals`

---

## 1. Zašto hibridni pristup

U prijavi teme naveden je **content-based + collaborative** pristup. Oba imaju poznata
ograničenja kada rade sami:

| Pristup | Prednost | Slabost |
|---|---|---|
| Content-based | Radi i za korisnika kojeg niko ne "liči" | Zatvara korisnika u ono što već radi (filter bubble) |
| Collaborative | Otkriva nove treninge preko sličnih korisnika | Ne radi za novog korisnika (cold start) |

FitSync kombinuje oba u jedan skor i dodaje *fallback* na popularnost, tako da
preporuke postoje i za korisnika koji se tek registrovao.

---

## 2. Signali koje sistem prikuplja

Signali se pišu u tabelu **`UserActions`** (`UserActionService.LogActionAsync`).
Ranije je ta usluga bila prazan `Task.CompletedTask`, pa je preporuka mogla raditi
samo sa završenim rezervacijama.

| Signal (`UserActionType`) | Gdje se bilježi | Težina |
|---|---|---:|
| `CompletedTraining` | `ReservationService.CompleteAsync` | **+5** |
| `ReservedTraining` | `ReservationService.CreateForUserAsync` | **+4** |
| `ReviewedTraining` | `ReviewService.CreateForUserAsync` | **+3** |
| `ViewedTraining` | `TrainingsController.GetByIdAsync` | **+1** |
| `SearchedTraining` | `TrainingsController.Search` | **+1** |
| `CancelledTraining` | `ReservationService.CancelAsync` | **−2** |

Otkazivanje ima **negativnu** težinu: ako korisnik uporno otkazuje jedan tip
treninga, to je signal da mu taj tip ne odgovara.

Agregacija se radi u SQL-u (`UserActionRepository.GetTrainingTypeWeightsAsync`), ne u
memoriji.

Uz `UserActions`, afinitet prema tipu treninga dopunjuju i:

- svaka rezervacija: **+4** na tip treninga,
- svaka recenzija: **+ ocjena (1–5)** na tip treninga.

---

## 3. Formiranje skupa kandidata

Kandidati se dohvaćaju u **najviše tri batch upita** (nikada jedan upit po treningu):

1. `GetByTrainingTypeIdsAsync(preferirani tipovi)` — content-based jezgro
2. `GetByIdsAsync(nedavno pregledani)` — bihevioralni signal
3. `GetAsync()` — dopuna, samo ako prva dva daju manje od 20 kandidata

Iz skupa se uklanjaju treninzi koje je korisnik **već rezervisao ili recenzirao**.

---

## 4. Scoring formula

```
Score =  ContentTypeWeight   × normalizirani_afinitet_tipa      (1.0)
       + CollaborativeWeight × normalizirani_peer_skor          (0.8)
       + RecentViewWeight    × [trening nedavno pregledan]      (0.5)
       + RatingWeight        × (prosječna_ocjena / 5)           (0.4)
       + PopularityWeight    × stabilni_tie-breaker             (0.2)
```

Težine su imenovane konstante na vrhu `RecommendationService`, da se dokumentacija i
kod ne mogu razići.

### 4.1 Content-based dio

`normalizirani_afinitet_tipa = afinitet(tip) / max(afinitet)`

Afinitet dolazi iz sekcije 2. Trening čiji tip korisnik najviše koristi dobija 1.0.

### 4.2 Collaborative dio

1. Nađi korisnike koji su rezervisali **iste** treninge kao naš korisnik
   (`GetPeerReservationsAsync` — filtriranje se radi u bazi, ne učitava se cijela tabela).
2. Težina svakog "peera" = broj zajedničkih treninga.
3. Skor kandidata = suma težina peerova koji su ga rezervisali.
4. Normalizuje se na `max(peer_skor)`.

### 4.3 Kvalitet i tie-breaker

Prosječna ocjena gura dobro ocijenjene treninge naprijed. Zadnji član je mali,
determinističan tie-breaker, tako da je redoslijed stabilan između poziva.

---

## 5. Fallback (cold start)

| Situacija | Ponašanje | `Strategy` |
|---|---|---|
| Korisnik ima historiju i postoje slični korisnici | Puni hibrid | `ContentBased` / `Collaborative` |
| Korisnik ima historiju, nema sličnih korisnika | Samo content-based + ocjene | `ContentBased` |
| Novi korisnik, bez ijednog signala | Dobro ocijenjeni i popularni treninzi | `Popular` / `Fallback` |
| Nema nijednog kandidata | Prazna lista (nikad greška) | — |

---

## 6. Objašnjenje preporuke (`Reason`)

Svaka preporuka nosi rečenicu koju mobilna aplikacija prikazuje korisniku:

| Strategija | Tekst |
|---|---|
| `ContentBased` | „Jer često rezervišete treninge tipa "Yoga"." |
| `Collaborative` | „Jer su ga rezervisali korisnici sa sličnim navikama kao Vi." |
| `Popular` | „Preporučeno na osnovu: prosječna ocjena 4.6, nedavno pregledano." |
| `Fallback` | „Popularan trening u teretani koji još niste probali." |

Pored toga, `MatchedSignals` sadrži pojedinačne signale koji su ušli u skor
(tip treninga, broj sličnih korisnika, prosječna ocjena, nedavni pregled), što služi
i za demonstraciju i za debugging.

---

## 7. Performanse

| Aspekt | Rješenje |
|---|---|
| N+1 upiti | Uklonjeni — `GetByIdsAsync` i `GetByTrainingTypeIdsAsync` su batch upiti |
| Učitavanje cijele tabele rezervacija | Uklonjeno — peer skup se filtrira u SQL-u |
| Agregacija signala | `GROUP BY` u bazi, ne u memoriji |
| Ograničenje rezultata | `limit` se ograničava na 1–50 |

Ukupno: **konstantan broj upita** bez obzira na broj treninga ili korisnika.

---

## 8. Ograničenja i mogući nastavak

- Peer sličnost je jednostavan broj preklapanja; kosinusna sličnost ili Jaccard bi
  bili precizniji na većem skupu podataka.
- Nema vremenskog opadanja (time decay) — rezervacija od prije godinu dana vrijedi
  koliko i jučerašnja.
- Nema A/B testiranja niti mjerenja klikova na preporuku.

Ovo su svjesne odluke za obim seminarskog rada, a ne previdi.
