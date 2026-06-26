# Tactiq API - ASP.NET Core 9

Football takım yönetimi ve istatistik sistemi API'si.

## Gereksinimler

- .NET 9 SDK
- PostgreSQL 14+
- Visual Studio 2022 / VS Code

## Kurulum

### 1. Bağımlılıkları Yükleyin

```bash
cd TactiqAPI
dotnet restore
```

### 2. PostgreSQL'i Başlatın

```bash
# Docker kullanıyorsanız
docker start tactiq-postgres

# veya lokal PostgreSQL kurulu ise
# PostgreSQL servisi çalışıyor olduğundan emin olun
```

### 3. Veritabanı Migrasyonlarını Uygulayın

```bash
dotnet ef database update
```

### 4. Uygulamayı Çalıştırın

```bash
dotnet run
```

API şu adresinde çalışacaktır: `https://localhost:5001`

Swagger UI: `https://localhost:5001/swagger`

## Swagger ile Kullanım

1. `POST /api/auth/register` veya `POST /api/auth/login` çağır.
2. Dönen `token` değerini kopyala.
3. Swagger sağ üstteki `Authorize` butonuna `Bearer {token}` formatında yapıştır.
4. `POST /api/players` ile oyuncu ekle. `id` gönderme; veritabanı otomatik üretir.
5. `GET /api/players` ile oyuncuların otomatik oluşan `id` değerlerini gör.

## Endpoints

### Authentication

- `POST /api/auth/register` - Kullanıcı kayıt
- `POST /api/auth/login` - Kullanıcı girişi

### Players

- `GET /api/players` - Kullanıcıya ait oyuncuları listele
- `GET /api/players/{id}` - Oyuncu detayı
- `POST /api/players` - Oyuncu oluştur
- `PUT /api/players/{id}` - Oyuncu güncelle
- `DELETE /api/players/{id}` - Oyuncu sil

### Matches

- `GET /api/matches` - Kullanıcıya ait maçları listele
- `GET /api/matches/{id}` - Maç detayı
- `POST /api/matches` - Oyuncular ve istatistiklerle maç oluştur
- `PUT /api/matches/{id}` - Maçı, oyuncuları ve istatistikleri güncelle
- `DELETE /api/matches/{id}` - Maç sil

### Team Builder

- `POST /api/team-builder/balance` - 6v6-11v11 arası dengeli takım oluştur

Örnek:

```json
{
  "teamSize": 7,
  "formation": "2-3-1",
  "playerIds": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]
}
```

### Playstyles

- `GET /api/playstyles/players` - Oyuncuların kural bazlı playstyle etiketleri
- `GET /api/playstyles/players/{playerId}` - Tek oyuncu playstyle etiketi

## Yapı

```
TactiqAPI/
├── Models/              # Entity modelleri
├── Data/                # DbContext ve veritabanı konfigürasyonu
├── Controllers/         # API controllers
├── Services/            # Business logic
├── DTOs/                # Data Transfer Objects
├── Migrations/          # EF Core migrasyonları
├── Program.cs           # Uygulama başlangıcı
└── appsettings.json     # Konfigürasyon
```

## Faz 1 - MVP Roadmap

- [x] Proje yapısı
- [x] PostgreSQL bağlantısı
- [x] User authentication (Register/Login/JWT)
- [x] Players endpoints
- [x] Matches endpoints
- [x] Player Statistics endpoints
- [x] Team Builder algoritması

## Notlar

- JWT secret'ı production'da değiştirin
- PostgreSQL connection string'ini environment'a göre ayarlayın
