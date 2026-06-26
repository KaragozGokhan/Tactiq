# Tactiq Product Plan

## Hedef

Tactiq, hali saha ve amator futbol gruplari icin oyuncu profili, mac istatistigi ve dengeli kadro kurma uygulamasidir.

Mimari:

```text
Flutter -> ASP.NET Core API -> PostgreSQL -> Docker -> AWS EC2
```

## Faz 1 - MVP

Amac: Kullanici oyuncu eklesin, mac istatistigi girsin, backend dengeli takim olustursun.

Durum: Buyuk olcude tamamlandi.

- Kullanici sistemi: register, login, JWT
- Oyuncular: ekle, listele, detay, guncelle, sil
- Maclar: tarih, sure, skor
- Mac istatistikleri: gol, asist, kaleyi bulan sut, basarili pas, top calma, kurtaris
- Takim olusturucu: 6v6-11v11, opsiyonel dizilis, pozisyon dengesi, guc dengesi, denge yuzdesi
- Swagger JWT authorize destegi

Faz 1'de bilerek yapilmayanlar:

- Oyuncuya saha ici koordinat atama
- Detayli FIFA kart attribute sistemi
- AI analizi
- Flutter uygulamasi

## Faz 1.5 - Kadro Kurucu v2

Amac: Kullanici 6v6-11v11 arasi mac tipini ve dizilisi secsin.

Ozellikler:

- Takim boyutu: 6v6, 7v7, 8v8, 9v9, 10v10, 11v11
- Dizilis secimi:
  - 7v7: 2-3-1, 3-2-1, 3-3-0
  - 8v8: 3-3-1, 2-4-1
  - 11v11: 4-3-3, 4-4-2, 3-5-2
- Pozisyon dengesi:
  - 2 striker varsa 1-1
  - 6 midfielder varsa 3-3
  - tek kalan pozisyonlarda fark en fazla 1
- Oyuncu karti:
  - isim
  - pozisyon
  - guclu ayak
  - playstyle etiketi
  - overall
  - form

Ilk teknik adim:

- `Player` modeline `Overall`, `Form`, `PrimaryPlaystyle` alanlari ekle
- `PlayerStats` modeline `Rating` alanini ekle
- Team Builder sonucunda oyunculari dizilis slotlarina yerlestir

## Faz 2 - Playstyle ve Overall

Amac: AI olmadan, kurallarla oyuncu kimligi cikarmak.

Playstyle ornekleri:

- Bitirici Forvet: son 10 macta gol yuksek, asist dusuk
- Oyun Kurucu: asist ve basarili pas yuksek
- Hizli Calimci Kanat: kanat pozisyonu ve hucum katkisi yuksek
- Top Kazanan: top calma yuksek
- Refleks Kaleci: kurtaris yuksek

Overall yaklasimi:

```text
base overall
+ son 5 mac form etkisi
+ son 10 mac uretkenlik
+ pozisyona gore onemli istatistikler
```

Mac sonu girilecek ek MVP alani:

- oyuncu puani: 1-10

## Faz 3 - AI Analizi

Amac: AI karar verici degil, aciklayici olsun.

Ornek cikti:

```text
Son 5 macta performansinda %18 artis gorunuyor. Hucum katkin yukselmis ancak pas katkin dusmus. Daha fazla takim oyunu oynarsan takim gucune katkin artabilir.
```

AI'a gonderilecek veri:

- son 5 mac ozeti
- son 10 mac toplamlari
- playstyle
- form trendi
- overall degisimi

## Su Anki API

Auth:

- `POST /api/auth/register`
- `POST /api/auth/login`

Users:

- `GET /api/users/me`
- `PUT /api/users/me`
- `DELETE /api/users/me`

Players:

- `GET /api/players`
- `GET /api/players/{id}`
- `POST /api/players`
- `PUT /api/players/{id}`
- `DELETE /api/players/{id}`

Matches:

- `GET /api/matches`
- `GET /api/matches/{id}`
- `POST /api/matches`
- `PUT /api/matches/{id}`
- `DELETE /api/matches/{id}`

Team Builder:

- `POST /api/team-builder/balance`

```json
{
  "teamSize": 7,
  "formation": "2-3-1",
  "playerIds": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]
}
```

Playstyles:

- `GET /api/playstyles/players`
- `GET /api/playstyles/players/{playerId}`

## Sonraki Sprint

Sprint 2.1 - Mobil MVP Akisi:

- Mobil login/register akisi
- Kadro kurma: oyuncu sec, oyuncu ekle, takim boyutu, dizilis, saha gorunumu
- Mac yukleme: oyuncu bazli rating/gol/asist/sut/pas/top calma/kurtaris
- Oyuncular sekmesi: oyuncu formu, liste, secili sil
- Admin/debug: health, api base, bulk JSON
- AI fallback endpointleri:
  - `POST /api/ai/player-analysis/{playerId}`
  - `POST /api/ai/team-analysis`

Sprint 2.2:

- Mobil UI polish
- Takim sonucunu paylasilabilir gorsel hale getirme
- Oyuncu karti detay ekrani
- AI aciklama metinlerini OpenAI ile guclendirme
