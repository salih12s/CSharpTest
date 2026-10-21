# TedarikCore

.NET 10 üzerinde Modular Monolith + Clean Architecture ile geliştirilen tedarik backend'i.

## Yapı

```
TedarikCore.slnx
src/
  TedarikCore.Api            HTTP, Controllers, Middleware, Program.cs
  TedarikCore.Application    Use case, service, interface
  TedarikCore.Domain         Entity, value object, temel iş kuralları
  TedarikCore.Infrastructure EF Core, persistence, dış servis implementasyonları
docs/
  PROJECT_STATUS.md          Hangi aşamadayız, sırada ne var
  DECISIONS.md               Mimari kararlar ve nedenleri
  CODE_WALKTHROUGH.md        Türkçe kod okuma rehberi (kod değişince güncellenir)
```

## Domain kuralları
- Para **kuruş** cinsinden `long` tutulur (`PriceKurus`); `decimal`/`double` kullanılmaz.
- Entity'lerin setter'ları `private`; nesne `Create(...)` fabrikasıyla doğrulanarak oluşur. EF Core için `private` parametresiz constructor bulunur.
- Benzersizlik (ör. SKU) tek bir nesneyle doğrulanamaz; veritabanı unique index'i korur.
- Domain projesine EF Core, Npgsql ya da başka paket eklenmez.

## Bağımlılık yönü

- Domain → hiçbir projeye bağımlı değil
- Application → Domain
- Infrastructure → Application + Domain
- Api → Application + Infrastructure

Circular dependency oluşturma. Application, Infrastructure'ı bilmez; interface Application'da, implementasyon Infrastructure'da durur.

## Komutlar

```
dotnet restore TedarikCore.slnx
dotnet build TedarikCore.slnx
dotnet run --project src/TedarikCore.Api --launch-profile http   # http://localhost:5229/health
```

## Veritabanı (PostgreSQL)

- Geliştirme veritabanı: `tedarikcore_dev` (yerel PostgreSQL 15, `localhost:5432`). Bilgisayardaki diğer veritabanlarına dokunulmaz.
- Bağlantı dizesi (`ConnectionStrings:Default`) Git'e girmez. Her makinede bir kez:
  `dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=tedarikcore_dev;Username=...;Password=..." --project src/TedarikCore.Api`
  Alternatif: `ConnectionStrings__Default` ortam değişkeni (User Secrets'ın önüne geçer).
- `GET /health` yalnızca uygulamanın ayakta olduğunu söyler, veritabanına bakmaz. `GET /health/ready` veritabanına gerçekten bağlanmayı dener (200 / 503).
- **Migration kuralları:**
  - `dotnet ef database update`, `Database.Migrate()`, `EnsureCreated()` ve `EnsureDeleted()` yalnızca kullanıcının açık onayıyla çalıştırılır; hiçbir veritabanına otomatik uygulanmaz.
  - Mevcut PostgreSQL veritabanlarına (tedarikcore_dev dışında hepsi başka projelerin) dokunulmaz. Ayrı test veritabanı oluşturmak ve DB kullanıcı yetkilerini değiştirmek onay ister.
  - `dotnet-ef` yerel araçtır (`dotnet-tools.json`, sürüm 10.0.12, EF paketleriyle aynı); `dotnet tool restore` ile kurulur.
  - Migration üretirken veritabanına yanlışlıkla bağlanılmasın diye bağlantıyı ulaşılamaz bir adrese çevir (ortam değişkeni User Secrets'ın önüne geçer), sonra SQL scriptini üretip `DROP/DELETE/ALTER` taraması yap:
    ```
    $env:ConnectionStrings__Default = "Host=localhost;Port=1;Database=yok;Username=yok;Password=yok;Timeout=1"
    dotnet ef migrations add <Ad> --project src/TedarikCore.Infrastructure --startup-project src/TedarikCore.Api --output-dir Persistence/Migrations
    dotnet ef migrations script --project src/TedarikCore.Infrastructure --startup-project src/TedarikCore.Api --no-build -o <dosya>.sql
    ```
  - Entity/eşleme değişince yeni migration üretmeyi unutma; `MigrationTests` model ile snapshot uyuşmazlığını yakalar.
- Bağlantı dizesi yoksa uygulama açılır, yalnızca `/health/ready` 503 verir ve gerçek-DB testi `Skipped` görünür.

## Gerçek PostgreSQL testleri (`tedarikcore_test`)

- Kısıtlar (unique index, CHECK) yalnızca gerçek PostgreSQL'de kanıtlanır; mock ya da bellek içi sağlayıcı bunu gösteremez. Bu yüzden yazma yapan testler **ayrı** bir veritabanında çalışır: `tedarikcore_test`. `tedarikcore_dev` geliştirme verisini içerir ve testler ona hiç yazmaz.
- Bağlantı: `ConnectionStrings:Test` (User Secrets ya da `ConnectionStrings__Test`). `ConnectionStrings:Default` ile karıştırılmaz.
  `dotnet user-secrets set "ConnectionStrings:Test" "Host=localhost;Port=5432;Database=tedarikcore_test;Username=...;Password=..." --project src/TedarikCore.Api`
- **Güvenlik kuralı** (`PostgresTestDatabase`): `ConnectionStrings:Test` yoksa gerçek-DB testleri `Skipped`. Varsa ama veritabanı adı tam olarak `tedarikcore_test` değilse ya da host yerel değilse test `Failed` olur ve hiçbir şey yazılmaz. Bağlandıktan sonra ayrıca `select current_database()` ile doğrulanır. Bu korumanın kendisi de test edilir.
- Testler migration **uygulamaz**. Test veritabanı bir kez elle hazırlanır (kullanıcı onayıyla):
  `dotnet ef database update --connection "<Test dizesi>" --project src/TedarikCore.Infrastructure --startup-project src/TedarikCore.Api` (aynı komutta `ConnectionStrings__Default` de test dizesine çevrilir ki dev'e yönlenemesin). Uygulanmamışsa gerçek-DB testleri `Failed` olur (`42P01`).
- Veri izolasyonu: her test benzersiz SKU (`T06-...-<guid>`) kullanır ve yazdıklarını commit etmeden transaction'ı geri alır. Tek istisna eşzamanlılık testidir (iki bağlantı commit ister); o test kendi benzersiz SKU'sunu silerek temizler.
- Test veritabanı oluşturmak/silmek, rol ve yetki değiştirmek ve migration uygulamak her seferinde kullanıcı onayı ister.

## Testler

```
dotnet test TedarikCore.slnx
```

- `tests/TedarikCore.UnitTests` — xUnit. Saf, hızlı testler (Domain/Application mantığı, mimari kurallar). DB, ağ ve host yok.
- `tests/TedarikCore.IntegrationTests` — xUnit + `WebApplicationFactory<Program>`. HTTP hattından test; istemci `https://localhost` adresine gider ve yönlendirme takip etmez.
- **GitHub Actions ve ücretli CI servisi kullanılmaz.** Her geliştirme adımı sonunda `dotnet restore TedarikCore.slnx`, `dotnet build TedarikCore.slnx` ve `dotnet test TedarikCore.slnx` yerelde çalıştırılır. Test sonuçları (başarılı/başarısız sayısı) açıkça raporlanır.
- Yeni bir özellik, ilgili testiyle birlikte gelir. Bulunan bug için önce onu yakalayan test yazılır, sonra düzeltilir.
- Bağımlılık kuralı `UnitTests/ArchitectureTests` altında test edilir; yeni proje referansı eklerken bu testi de güncelle.
- Başarısız testi yeşil gösterme, atlama (`Skip`) gerekçesiz kullanma.

## Git ve commit

- Ana dal `main`. Commit mesajı kısa ve Türkçe/İngilizce tutarlı; "ne" yerine "neden"i yaz.
- Commit'ten önce `dotnet build` ve `dotnet test` geçmeli.
- `bin/`, `obj/`, `.vs/`, `*.user`, `.env*`, `secrets.json`, test çıktıları commit edilmez (`.gitignore`).
- Push yalnızca kullanıcı isteyince yapılır.
- Commit mesajı dosyasını BOM'suz UTF-8 yaz (PowerShell 5.1'de `Set-Content -Encoding utf8` BOM ekler).

## Kalıcı kurallar

- Her geliştirme aşaması küçük ve test edilebilir olmalı.
- Katmanların sorumluluklarına uyulmalı (iş kuralı Domain/Application'da, Controller ince kalır).
- Kod değişikliklerinde nedenler açıklanmalı.
- Kullanılmayan, gereksiz soyutlamalar oluşturulmamalı.
- Gizli bilgiler (bağlantı dizesi, anahtar, parola) kaynak koda yazılmaz; user-secrets veya ortam değişkeni kullanılır.
- Kullanıcı onayı olmadan sonraki aşamaya geçilmemeli.
- Var olan çalışan kod gereksiz yere silinmemeli.
- Git geçmişi silinmez, force-push yapılmaz; kullanıcı istemeden push yapılmaz.
- Her aşama sonunda `docs/PROJECT_STATUS.md` güncellenir; mimari kararlar `docs/DECISIONS.md`'ye eklenir.
