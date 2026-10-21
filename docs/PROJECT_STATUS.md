# Proje Durumu

Son güncelleme: 2026-10-21

## Tamamlanan

### ADIM 01 — Backend solution ve mimari iskelet
- `TedarikCore` API projesi `src/TedarikCore.Api` altına taşındı ve yeniden adlandırıldı.
- `Domain`, `Application`, `Infrastructure` projeleri oluşturuldu; referanslar bağlandı.
- `DependencyInjection` extension sınıfları (`AddApplication`, `AddInfrastructure`) hazır, henüz boş.
- `GET /health` eklendi (ASP.NET Core HealthChecks), HTTP 200 döner.
- WeatherForecast örneği `/health` doğrulandıktan sonra kaldırıldı.
- Kök `CLAUDE.md`, `docs/PROJECT_STATUS.md`, `docs/DECISIONS.md` oluşturuldu.
- Git deposu `main` dalında kuruldu, kullanıcının isteğiyle `origin` (GitHub) üzerine pushlandı.

### ADIM 02 — Git ve test altyapısı
- `tests/TedarikCore.UnitTests` ve `tests/TedarikCore.IntegrationTests` (xUnit) çözüme eklendi.
- Integration: `/health` → 200 + `Healthy`, bilinmeyen yol → 404 (`WebApplicationFactory<Program>`, https istemci, yönlendirme kapalı).
- Mimari test: proje referansları Clean Architecture yönüne uygun mu (`.csproj` üzerinden).
- `Program.cs` sonuna `public partial class Program;` eklendi (test erişimi için).
- `.gitignore` test çıktıları ve `.env.*` ile genişletildi; `CLAUDE.md`'ye test ve commit kuralları eklendi.

### ADIM 03 — GitHub ve yerel test düzeni
- Remote (`origin`) ve commit geçmişi gizli bilgi açısından kontrol edildi; temiz.
- Commit'ler normal `git push` ile `origin/main`'e gönderildi (force-push yok).
- **Kural:** GitHub Actions ve ücretli CI kullanılmayacak. Her geliştirme adımı sonunda `dotnet restore`, `dotnet build` ve `dotnet test` (`TedarikCore.slnx`) yerelde çalıştırılacak; test sonuçları açıkça raporlanacak.

### Kod okuma ve klasör temizliği (ADIM 03 sonrası)
- `docs/CODE_WALKTHROUGH.md` eklendi: Türkçe kod okuma rehberi.
- Yalnızca `.gitkeep` içeren `Domain/Common` ve `Api/Middleware` klasörleri kaldırıldı. `Application/Abstractions`, `Infrastructure/Persistence` ve `Api/Controllers` korundu.
- Kod, `.csproj` ve ProjectReference bağlantılarına dokunulmadı.

### ADIM 04 — PostgreSQL ve EF Core (`feature/adim-04-efcore` dalı)
- Yerel PostgreSQL 15.6 çalışıyor; `tedarikcore_dev` veritabanı oluşturuldu (mevcut diğer veritabanlarına dokunulmadı).
- `Infrastructure`: `AppDbContext` (boş), `AddInfrastructure(services, configuration)` ile EF Core + Npgsql kaydı, `HealthCheckTags`.
- Bağlantı dizesi User Secrets'ta (`ConnectionStrings:Default`); Git'e girmedi. Api `.csproj`'a `UserSecretsId` eklendi.
- `GET /health` (yalnızca ayakta mı, DB'ye bakmaz) ve `GET /health/ready` (gerçek PostgreSQL kontrolü: 200 / 503).
- Paketler: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 10.0.12 (EF Core 10.0.12 buradan gelir). Test: Microsoft.Extensions.Configuration 10.0.12, Microsoft.Extensions.Configuration.UserSecrets 10.0.12.
- Testler 10 → 15: DB erişilemezken `/health` 200 ve `/health/ready` 503, bağlantı dizesi yokken net hata, gerçek DB ile `/health/ready` 200 (DB yoksa `Skipped`).
- Migration, entity ve iş modülleri yapılmadı (kapsam dışı).

### ADIM 05 — Product domain ve ilk migration (`feature/adim-05-product` dalı)
- `Domain/Catalog/Product.cs`: `Id`, `Sku`, `Name`, `PriceKurus`, `IsActive`; `Product.Create(...)` ve `ChangePrice(...)` ile doğrulama; setter'lar private.
- `Infrastructure`: `DbSet<Product>`, `Persistence/Configurations/ProductConfiguration.cs` (tablo `products`, snake_case kolonlar, `ix_products_sku` unique index).
- İlk migration `InitialCatalog` **üretildi** (`Persistence/Migrations`). Yalnızca `products` tablosu ve `ix_products_sku` indeksi oluşturur; DROP/DELETE/ALTER yok.
- **Migration hiçbir veritabanına uygulanmadı.** `tedarikcore_dev` içinde tablo yok (salt-okunur sorguyla doğrulandı).
- `dotnet-ef` 10.0.12 yerel araç olarak eklendi (`dotnet-tools.json`); Api projesine `Microsoft.EntityFrameworkCore.Design` 10.0.12 (`PrivateAssets=all`).
- Testler 15 → 55 (UnitTests 10 → 50, IntegrationTests 5): Domain kuralları (28), EF eşlemesi (DB'siz, 10) ve model–migration uyumu (2).

## Bilinen eksikler
- Saf unit test yok: henüz test edilecek Domain/Application mantığı bulunmuyor. UnitTests şimdilik yalnızca mimari testi içeriyor.
- Geliştirme bağlantısı `postgres` yönetici kullanıcısı ve basit bir demo parolasıyla kuruldu (kullanıcı kararı: canlıya çıkılmayacak demo). Canlı kullanımdan önce yetkisi sınırlı ayrı bir kullanıcı gerekir.
- Gerçek-DB testi `tedarikcore_dev`'e bağlanıyor (yalnızca bağlantı denemesi). Veri yazan testlerden önce ayrı test veritabanı gerekir.
- `tedarikcore_dev` boş: `InitialCatalog` migration'ı yazıldı ama **uygulanmadı**. Gerçek PostgreSQL'de çalıştığı henüz kanıtlanmadı; SQL scriptinin üretilmiş olması uygulandığı anlamına gelmez.
- SKU benzersizliği yalnızca unique index'le korunuyor; gerçek veritabanıyla yazılmış test henüz yok (ayrı test veritabanı onay bekliyor).
- Veritabanında fiyat için CHECK kısıtı (`price_kurus > 0`) yok; kural yalnızca Domain'de. Öneri olarak bırakıldı.
- CI bilerek yok (karar: yalnızca yerel test); testlerin çalıştırılması geliştiricinin sorumluluğunda.
- Coverage ölçümü kurulmadı (`coverlet.collector` pakete dahil ama kullanılmıyor).

## Sırada
- ADIM 06 (kullanıcı onayı bekleniyor). `feature/adim-05-product` dalı henüz push/merge edilmedi.

## Kapsam dışı (henüz yapılmadı)
EF Core migration/entity, Authentication/JWT, Product/Order/Inventory iş mantığı, React frontend, Docker/deployment.
