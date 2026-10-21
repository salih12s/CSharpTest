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
- `Infrastructure`: `DbSet<Product>`, `Persistence/Configurations/ProductConfiguration.cs` (tablo `products`, snake_case kolonlar, `ix_products_sku` unique index, `ck_products_price_kurus_positive` CHECK `price_kurus > 0`).
- İlk migration `InitialCatalog` **üretildi** (`Persistence/Migrations`). Yalnızca `products` tablosunu, `ix_products_sku` unique indeksini ve `ck_products_price_kurus_positive` CHECK kısıtını oluşturur; DROP/DELETE/ALTER yok. Hiçbir veritabanına uygulanmadığı için CHECK kısıtı ikinci bir migration yerine aynı `InitialCatalog` içine işlendi.
- ADIM 05 sonunda migration hiçbir veritabanına uygulanmamıştı (`tedarikcore_dev` boştu, salt-okunur sorguyla doğrulandı). Sonraki durum için ADIM 06'ya bakın: yalnızca `tedarikcore_test`'e uygulandı.
- `dotnet-ef` 10.0.12 yerel araç olarak eklendi (`dotnet-tools.json`); Api projesine `Microsoft.EntityFrameworkCore.Design` 10.0.12 (`PrivateAssets=all`).
- Testler 15 → 59 (UnitTests 10 → 54, IntegrationTests 5): Domain kuralları (28), EF eşlemesi (DB'siz, 11) ve migration (5: varlık, model–snapshot uyumu, `Up` içeriği, yıkıcı işlem yokluğu, `Down`).

### ADIM 06 — Gerçek PostgreSQL migration ve constraint testleri (`feature/adim-05-product` dalı, PR #2 ile `main`'e merge edildi)
- `tedarikcore_test` veritabanı **kullanıcı onayıyla** oluşturuldu (`CREATE DATABASE tedarikcore_test;`, rol/yetki değişikliği yok). Önce yokluğu salt-okunur sorguyla doğrulandı.
- `ConnectionStrings:Test` User Secrets'a yazıldı; `ConnectionStrings:Default` değişmedi.
- `InitialCatalog` migration'ı **kullanıcının ayrı onayıyla yalnızca `tedarikcore_test`'e uygulandı** (`dotnet ef database update`). Uygulanan yapılar PostgreSQL'den doğrulandı: `products` (5 kolon), `pk_products`, `ix_products_sku` (UNIQUE), `ck_products_price_kurus_positive` (`CHECK ((price_kurus > 0))`), `__EFMigrationsHistory`.
- `tedarikcore_dev` ve diğer veritabanları değişmedi; `tedarikcore_dev` hâlâ **0 tablo** (migration orada uygulanmadı).
- Gerçek PostgreSQL integration testleri: yaz/oku, fiyat değişikliğinin kalıcılığı, farklı SKU'lar, aynı SKU → `23505`, fiyat 0 / negatif / güncelleme → `23514`, eşzamanlı aynı SKU (biri kazanır, diğeri `23505`), harf büyüklüğü davranışı, rollback izolasyonu, şema doğrulaması.
- Güvenlik koruması (`PostgresTestDatabase`): hedef `tedarikcore_test` değilse yazma yok; koruma testle kanıtlandı.
- Testler 59 → 89 (UnitTests 54, IntegrationTests 35).

### ADIM 07 — SKU kuralı, yeni migration ve test güvenliği (`feature/adim-07-sku-database` dalı)
- Kural eklenmeden önce C# `ToUpperInvariant()` ile PostgreSQL `upper()` tüm Unicode (1.112.063 kod noktası) için karşılaştırıldı: C# çıktısı hiç reddedilmiyor (0 uyumsuzluk). PostgreSQL 478 egzotik küçük harfi büyütmüyor, yani CHECK ASCII ve Türkçe harflerde tam, bu karakterlerde eksik koruma (DECISIONS 026). Ölçüm kalıcı test olarak durur.
- `ProductConfiguration`: `ck_products_sku_uppercase` (`sku = upper(sku)`). `ix_products_sku` korundu.
- Yeni migration **`EnforceUppercaseSku`**: yalnızca `ADD CONSTRAINT`; `InitialCatalog` değiştirilmedi. SQL incelendi (DROP/DELETE/UPDATE/CREATE yok).
- Migration **kullanıcının ayrı onayıyla yalnızca `tedarikcore_test`'e uygulandı** ve PostgreSQL'den doğrulandı: 2 migration kaydı, 3 kısıt (`pk_products`, `ck_products_price_kurus_positive`, `ck_products_sku_uppercase`), `ix_products_sku` UNIQUE, `products` 0 satır. `tedarikcore_dev` **değişmedi (0 tablo)**.
- Test altyapısı: hata durumunda bağlantı dispose, `RunWithCleanupAsync` (asıl hata ve temizlik hatası kaybolmaz), tam-SKU temizliği, seri xUnit koleksiyonu (DECISIONS 027).
- Gerçek veritabanı testleri güncellendi: küçük harfli ham SQL `23514` (`ck_products_sku_uppercase`), `Product.Create` ile Türkçe/özel karakterli SKU'lar kaydedilir, ADIM 06'daki "indeks harfe duyarlı" testi yeni davranışa çevrildi.
- Uygulama öncesi kırmızı taban: migration uygulanmadan yeni kurala bağlı 7 test kırmızıydı; uygulandıktan sonra yeşile döndü.
- Testler 89 → 114 (UnitTests 57, IntegrationTests 57).
## Bilinen eksikler
- Application katmanında henüz iş mantığı ve dolayısıyla unit test yok. Domain'de `Product` kuralları 28 testle kapsanıyor; UnitTests ayrıca mimari, EF eşleme ve migration testlerini içeriyor.
- Geliştirme ve test bağlantıları `postgres` yönetici kullanıcısı ve basit bir demo parolasıyla kuruldu (kullanıcı kararı: canlıya çıkılmayacak demo). Canlı kullanımdan önce yetkisi sınırlı ayrı bir kullanıcı gerekir.
- `/health/ready` gerçek-DB testi (ADIM 04) `Default` (`tedarikcore_dev`) bağlantısını yalnızca okuma/bağlanma amacıyla kullanıyor; yazma testleri `tedarikcore_test`'te.
- `tedarikcore_dev` boş (0 tablo): iki migration orada **uygulanmadı**, ayrı onay bekliyor. Dev'de veri varsa `ADD CONSTRAINT` mevcut satırları doğrular; küçük harfli SKU bulunursa migration tümüyle geri alınır, uygulamadan önce `select sku from products where sku <> upper(sku)` ile kontrol edilmeli.
- SKU CHECK'i ASCII ve Türkçe harflerde tamdır; PostgreSQL'in büyütmediği ~478 egzotik karakterde (ör. `ý`/`Ý`) ham SQL ile büyük/küçük çift geçebilir. Gerekirse `upper(sku)` üzerinde unique index (DECISIONS 026). Kural PostgreSQL yerel ayarına bağlı: farklı sunucuda uyumluluk testi (`SkuUppercaseCompatibilityPostgresTests`) mutlaka çalıştırılmalı.
- Windows PowerShell 5.1 ile `psql`/yerel komutlarda çift tırnak ve `throw` ön-kontrol tuzakları var (DECISIONS 027); yeni betiklerde SQL `-f` dosyasıyla verilmeli.
- CI bilerek yok (karar: yalnızca yerel test); testlerin çalıştırılması geliştiricinin sorumluluğunda.
- Coverage ölçümü kurulmadı (`coverlet.collector` pakete dahil ama kullanılmıyor).
## Sırada
- ADIM 08 (kullanıcı onayı bekleniyor). ADIM 07 değişiklikleri `feature/adim-07-sku-database` dalında yerel commit; push/merge onay bekliyor.

## Kapsam dışı (henüz yapılmadı)
Product API ve Application servisleri, Order/Inventory iş mantığı, Authentication/JWT, React frontend, Docker/deployment, iki migration'ın `tedarikcore_dev`'e uygulanması ve (gerekirse) `upper(sku)` unique index kararı.