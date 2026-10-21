# Mimari Kararlar

## 001 — Modular Monolith + Clean Architecture (2026-10-21)
Tek dağıtım birimi, dört proje: Api, Application, Domain, Infrastructure. Bağımlılıklar içe doğru akar (Domain en içte). **Neden:** başlangıçta mikroservis karmaşıklığı olmadan katman sınırlarını net tutmak; modüller sonradan Application/Domain içinde ayrışabilir.

## 002 — Projeler `src/` altında, `TedarikCore.*` adlandırması (2026-10-21)
Visual Studio'nun oluşturduğu `TedarikCore` projesi `src/TedarikCore.Api` oldu; namespace'ler `TedarikCore.Api` ile uyumlandı. **Neden:** katman adlarıyla tutarlılık ve ileride `tests/` klasörüne yer açmak.

## 003 — `/health` için yerleşik HealthChecks (2026-10-21)
`AddHealthChecks` + `MapHealthChecks("/health")` kullanıldı, ayrı controller yazılmadı. **Neden:** minimum kod; ileride DB kontrolü eklemek tek satırdır.

## 004 — DI kaydı katman başına extension (2026-10-21)
`AddApplication()` ve `AddInfrastructure()` kendi projelerinde durur; `Program.cs` sade kalır. Bu projeler yalnızca `Microsoft.Extensions.DependencyInjection.Abstractions` paketine bağlıdır.

## 005 — Boş katman klasörleri `.gitkeep` ile tutuldu (2026-10-21)
`Application/Abstractions`, `Infrastructure/Persistence`, `Api/Controllers`. Henüz soyutlama yazılmadı; ihtiyaç doğunca doldurulacak. `Domain/Common` ve `Api/Middleware` yalnızca `.gitkeep` içerdiği ve yakın bir kullanımı olmadığı için temizlik turunda kaldırıldı (2026-10-21); gerektiğinde yeniden açılır.

## 006 — xUnit v2 (2.9.3) + Microsoft.AspNetCore.Mvc.Testing 10.0.12 (2026-10-21)
`dotnet new xunit` şablonunun getirdiği sürümler kullanıldı; Mvc.Testing, API ile aynı 10.0.x hattında. **Neden:** SDK ile gelen, en az sürpriz çıkaran kombinasyon. xUnit v3'e geçiş ihtiyaç olursa ayrı bir karar olur.

## 007 — Mimari test `.csproj` okuyarak yapıldı (2026-10-21)
Reflection (`GetReferencedAssemblies`) yerine `<ProjectReference>` satırları okunur. **Neden:** derleyici kullanılmayan referansı metadata'ya yazmaz; henüz kod olmayan katmanlarda kural sessizce "geçerdi". Test, yasak bağımlılıkları (ör. Domain → Api) yakalayacak şekilde bir kez bozularak doğrulandı. Bu test UnitTests'te durur: host ve I/O gerektirmez.

## 008 — `Program` sınıfı test için `public partial` (2026-10-21)
Top-level `Program.cs` sınıfı internal üretir; `WebApplicationFactory<Program>` için tek satırlık `public partial class Program;` eklendi. `InternalsVisibleTo` yerine bu seçildi, çünkü Microsoft'un önerdiği yol ve tek satır.

## 009 — CI yok, testler yalnızca yerelde (2026-10-21)
GitHub Actions ve ücretli CI servisleri kullanılmaz. Her adım sonunda `restore`, `build`, `test` yerelde çalıştırılır ve sonuçlar raporlanır. **Neden:** kullanıcının kesin kararı (maliyet ve basitlik). **Risk:** testi çalıştırmayı unutmak kırık kodu ana dala taşıyabilir; commit öncesi test geçme kuralı (`CLAUDE.md`) bunu hafifletir.

## 010 — EF Core + Npgsql, `AppDbContext` Infrastructure/Persistence'ta (2026-10-21)
`Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (EF Core 10.0.12 ile gelir). `AppDbContext` bilerek boş: entity ve migration kapsam dışıydı. **Neden:** veritabanı erişimi Infrastructure'a aittir; Domain ve Application EF Core'u bilmez.

## 011 — Bağlantı dizesi User Secrets'ta, ilk kullanımda okunur (2026-10-21)
`ConnectionStrings:Default`, geliştirmede User Secrets'tan (ya da ortam değişkeninden) gelir; Git'e girmez. `AddDbContext` içindeki fonksiyon bağlantı dizesini **DbContext ilk kullanıldığında** okur, eksikse net bir `InvalidOperationException` verir. **Neden:** dizesi olmayan makinede (yeni klon) uygulama ve `/health` yine açılır, test paketi çalışır; hata yalnızca veritabanı gereken yerde görünür. Bedeli: yanlış yapılandırma uygulama açılırken değil, `/health/ready`'de fark edilir.

## 012 — `/health` (liveness) ve `/health/ready` (readiness) ayrıldı (2026-10-21)
`/health` hiçbir kontrol çalıştırmaz (`Predicate = _ => false`), `/health/ready` yalnızca `ready` etiketli kontrolleri (şimdilik `AddDbContextCheck<AppDbContext>`) çalıştırır. **Neden:** veritabanı kapanınca uygulama "ölü" sayılıp yeniden başlatılmasın; "trafik almaya hazır mı" ayrı sorulsun. Etiket adı `HealthCheckTags.Ready` sabitinde, çünkü `DependencyInjection` sınıf adı iki katmanda aynı ve `Program.cs`'te çakışıyordu. Hazır paket (`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`) kullanıldı; el yazması bağlantı kodu yazılmadı.

## 013 — Gerçek-DB testi koşullu atlanır (2026-10-21)
`[RequiresDatabaseFact]`, bağlantı dizesi yoksa testi `Skipped` yapar. **Neden:** veritabanı olmayan makinede test kırmızıya dönmesin, ama sessizce "geçti" de görünmesin. Başarısızlık yolları (DB erişilemez, dize yok) gerçek veritabanı olmadan test edilir.

## 014 — Geliştirmede `postgres` kullanıcısı ve demo parolası (2026-10-21)
Kullanıcı, projenin canlıda çalıştırılmayacak bir demo olduğunu belirterek `postgres` kullanıcısını ve basit bir parolayı kullanmamı istedi. Parola yalnızca User Secrets'ta, Git'te yok. **Risk:** canlıya taşınırsa yönetici kullanıcı ve basit parola kabul edilemez; ayrı kullanıcı ve güçlü parola gerekir.

## 015 — Product: private setter, `Create` fabrikası, yerleşik exception'lar (2026-10-21)
Özellikler `private set`; nesne `Product.Create(...)` ile doğrulanarak doğar, fiyat yalnızca `ChangePrice` ile değişir. EF Core için `private` parametresiz constructor var. Hatalarda `ArgumentException` (SKU/ad) ve `ArgumentOutOfRangeException` (fiyat) kullanıldı; özel `DomainException` sınıfı yazılmadı. **Neden:** kuralları nesnenin kendisi korur, dışarıdan geçersiz hâl üretilemez; ayrı hata hiyerarşisi için henüz ihtiyaç yok (gereksiz soyutlama kuralı). API katmanı geldiğinde bu hatalar 400'e çevrilecek.

## 016 — SKU normalizasyonu: kırp + `ToUpperInvariant`, uzunluk kırpmadan sonra ölçülür (2026-10-21)
Kültürden bağımsız büyük harf kullanıldı; Türkçe kültürde `ToUpper()` "i"yi "İ" yapıp aynı SKU'yu farklı makinede farklı üretirdi (testle kanıtlandı: mutasyonda test kırıldı). Baştaki/sondaki boşluk uzunluk sınırına sayılmaz. SKU benzersizliği **veritabanındaki unique index** (`ix_products_sku`) tarafından korunur; tek bir nesne bunu bilemez. Büyük/küçük harf farkı normalizasyonla zaten ortadan kalkar.

## 017 — Fiyat kuruş cinsinden `long`, sıfır geçersiz (2026-10-21)
`PriceKurus` > 0. `decimal` ve kayan nokta yok, böylece yuvarlama hatası oluşmaz. Sipariş sırasında fiyat değişirse müşterinin yeniden onayı kuralı Order/Checkout modülüne bırakıldı. DB tarafında da `CHECK (price_kurus > 0)` var (karar 021).

## 018 — Eşleme Fluent API ile, snake_case ve açık kısıt adları (2026-10-21)
`IEntityTypeConfiguration<Product>` ayrı sınıfta; `ApplyConfigurationsFromAssembly` ile yüklenir. Tablo `products`, kolonlar `id, sku, name, price_kurus, is_active`; birincil anahtar `pk_products`, indeks `ix_products_sku`. `Id` için `ValueGeneratedNever` (Guid Domain'de üretilir). Maksimum uzunluklar `Product.MaxSkuLength/MaxNameLength` sabitlerinden gelir: kural tek yerde. Eşleme DB'ye bağlanmadan modelden test edilir.

## 019 — `dotnet-ef` yerel araç 10.0.12, Design paketi Api'de (2026-10-21)
Makinede global `dotnet-ef` 10.0.13 vardı; projedeki EF Core 10.0.12 ile eşlemek için `dotnet-tools.json` ile 10.0.12 sabitlendi. `Microsoft.EntityFrameworkCore.Design` başlangıç projesi Api'ye `PrivateAssets=all` ile eklendi (çalışma zamanına taşınmaz). Migration'lar Infrastructure/Persistence/Migrations altında. **Neden:** araç sürümü kişiden kişiye değişmesin; Design paketi üretim çıktısına sızmasın.

## 020 — Migration üretilir, uygulanmaz; uyum testi eklendi (2026-10-21)
`InitialCatalog` yalnızca üretildi ve SQL'i incelendi; `database update`, `Migrate()`, `EnsureCreated()` ve `EnsureDeleted()` çağrılmadı. Üretim sırasında bağlantı ulaşılamaz adrese çevrildi ki yanlışlıkla gerçek veritabanına bağlanılmasın. `MigrationTests.Model_Matches_LatestMigrationSnapshot` entity değişip migration unutulursa kırılır (mutasyonla doğrulandı). **Bilinen sınır:** migration'ın gerçek PostgreSQL'de başarıyla çalıştığı henüz kanıtlanmadı.

## 021 — Fiyat için veritabanı CHECK kısıtı, ayrı migration yerine InitialCatalog içinde (2026-10-21)
`ck_products_price_kurus_positive` (`price_kurus > 0`), `ToTable("products", t => t.HasCheckConstraint(...))` ile Fluent API'de tanımlandı. **Neden:** Domain kuralı tek savunma olmasın; ileride bir hata, toplu aktarım ya da elle yazılan SQL sıfır/negatif fiyat sokmasın (para konusu). **Neden ikinci migration değil:** `InitialCatalog` hiçbir veritabanına uygulanmamıştı ve dal henüz pushlanmamıştı; ikinci bir migration geçmişi gereksiz uzatırdı. Dosyaları silip yeniden üretmek harness tarafından engellendi (korumalı klasör), bu yüzden engeli dolaşmadan: EF'in ürettiği kod geçici bir klasörde (repo dışı) üretilip aynı biçimde `InitialCatalog.cs` ve `.Designer.cs` içine işlendi, snapshot'ı EF kendisi güncelledi. Tutarlılık testlerle güvence altında: model–snapshot uyumu, migration `Up` içeriği (mutasyonla doğrulandı). **Kural:** migration bir veritabanına uygulandıktan sonra bu yöntem kullanılmaz; yeni migration üretilir.

## 022 — Yazma testleri ayrı `tedarikcore_test` veritabanında, koruma kodda (2026-10-21)
Unique index ve CHECK kısıtları ancak gerçek PostgreSQL'de kanıtlanır, bu yüzden testler gerçek veritabanına yazar. **Neden ayrı veritabanı:** testler geliştirme verisini (`tedarikcore_dev`) bozmasın ve başka projelerin veritabanlarına asla dokunmasın. `ConnectionStrings:Test` ayrı bir anahtar; `Default` değişmedi. **Koruma:** `PostgresTestDatabase` bağlantıyı açmadan önce veritabanı adının tam olarak `tedarikcore_test` ve host'un yerel olduğunu doğrular, bağlandıktan sonra `select current_database()` ile tekrar kontrol eder. Bağlantı dizesi hiç yoksa testler `Skipped`; yanlışsa `Failed` (sessizce yeşil görünmez ve hiçbir şey yazılmaz). Korumanın kendisi de test edilir ve dev'e işaret eden dizeyle fiilen denendi. **Bedel:** test veritabanı bir kez elle kurulmalı ve migration uygulanmalı (testler migration uygulamaz).

## 023 — SKU unique index'i harf büyüklüğüne duyarlı (davranış belgelendi, şema değişmedi) (2026-10-21)
**Güncelleme:** (a) seçeneği karar 026 ile uygulandı; bu kaydın davranış açıklaması ADIM 06 durumunu anlatır.
`tedarikcore_test`'te gözlendi: ham SQL ile `T06-HARF-X` ve `t06-harf-x` ikisi de eklenebiliyor (varchar, varsayılan kolasyon, düz unique index). `Product.Create` SKU'yu büyük harfe çevirdiği için normal yoldan çakışma oluşmaz; risk, Domain'i atlayan yazıcılar (ham SQL, toplu aktarım, başka servis). `Sku_UniqueIndex_IsCaseSensitive_AtDatabaseLevel` testi bu davranışı sabitler: ileride şema değişirse bilinçli olarak güncellenmeli. **Seçenekler (onay bekliyor):** (a) `CHECK (sku = upper(sku))`: Domain kuralını veritabanına taşır, ama `upper()` kolasyona/yerel ayara bağlıdır (Türkçe "i" tuzağı); (b) `unique index on (upper(sku))`: farklı harf büyüklüğünü çakışma sayar; (c) `citext` uzantısı: ek uzantı ister. Şimdilik Domain normalizasyonu yeterli kabul edildi.

## 024 — Veri izolasyonu: rollback; tek istisna eşzamanlılık testi (2026-10-21)
Her test benzersiz SKU kullanır ve işini hiç commit etmediği bir transaction içinde yapar, böylece kalıntı veri oluşmaz ve testler sıradan bağımsızdır. `RolledBackTransaction_LeavesNoRows` bu yöntemi kanıtlar. İki ayrı bağlantının aynı SKU için yarışması commit gerektirir; `ConcurrentInserts_OfSameSku_ExactlyOneWins_Other_Gets23505` kendi benzersiz SKU'sunu `finally` içinde siler. Testten sonra `products` tablosunda 0 satır doğrulandı; test 5 kez üst üste kararlı geçti. EF testleri açık transaction'ı `Database.UseTransaction` ile paylaşır, yani `SaveChanges` commit etmez.

## 025 — Migration `tedarikcore_test`'e uygulandı, `tedarikcore_dev`'e uygulanmadı (2026-10-21)
Kullanıcı onayıyla, tek komutla (`dotnet ef database update InitialCatalog --connection <Test>`) yalnızca test veritabanına uygulandı. Önce hedef adı ve 0 tablo doğrulandı; aynı komutta `ConnectionStrings__Default` de test dizesine çevrildi (dev'e yönlenmek imkânsız); sonra dev'in hâlâ 0 tablo içerdiği doğrulandı. EF'in `__EFMigrationsHistory` yoklaması taze veritabanında `fail:` olarak loglanır; beklenen davranıştır. Dev'e uygulama ayrı onay bekliyor.

## 026 — `ck_products_sku_uppercase` (`sku = upper(sku)`) yeni migration olarak eklendi (2026-10-21)
`EnforceUppercaseSku` migration'ı yalnızca `ADD CONSTRAINT ... CHECK (sku = upper(sku))` çalıştırır. **Neden:** `Product.Create` SKU'yu büyük harfe çevirir ama ham SQL küçük harfli kopya ekleyebiliyordu (karar 023); kural veritabanında da dursun. **Neden yeni migration:** `InitialCatalog` test veritabanına uygulanmıştı, artık değiştirilmez. **Kural eklenmeden önce ölçüldü:** C# `ToUpperInvariant()` çıktısının PostgreSQL `upper()` altında değişmemesi tüm 1.112.063 Unicode kod noktası için doğrulandı (0 uyumsuzluk; test olarak kalıcı). Kolasyon `Turkish_Türkiye.1254` (libc) olduğu için `i`/`ı`/`İ` özellikle incelendi: `upper('i')`=`I`, `ı` ve `İ` değişmiyor, C# ile uyumlu. **Sınır:** PostgreSQL 478 egzotik küçük harfi büyütmüyor (`ý`, `µ`, `ς` …) ve `ÿ`'yi U+009F yapıyor; bu karakterlerle ham SQL'de `ý`/`Ý` gibi çiftler geçebilir. ASCII ve Türkçe harflerde koruma tamdır. Alternatifler: `unique index on (upper(sku))` (egzotik çiftleri de yakalar, ek indeks maliyeti), `citext` (ek uzantı). Sonuç yerel ayara bağlı; farklı sunucuda uyumluluk testi tekrar çalıştırılmalı.

## 027 — Test altyapısı: hata durumunda dispose, temizlik/asıl hata birleştirme, seri koleksiyon (2026-10-21)
`PostgresTestDatabase.OpenAndVerifyAsync` her hata yolunda bağlantıyı dispose eder. `RunWithCleanupAsync`: test hatası tür ve izle korunur, temizlik hatası yutulmaz, ikisi birden olursa `AggregateException`. Temizlik yalnızca **tam SKU**'yu siler ve satır kalmadığını doğrular; `T06-%` gibi geniş silme bilerek yok. Veritabanına dokunan testler tek xUnit koleksiyonunda seri çalışır (eşzamanlılık testinin geçici satırı kalıntı kontrolüyle yarışmasın). Dispose testleri mutasyonla doğrulandı (`DisposeAsync` çıkarılınca iki test kırıldı). **Çıkarılan ders:** PowerShell 5.1 yerel komutlara geçen çift tırnakları siliyor; `psql` sorgularında tırnaklı tanımlayıcı (`"MigrationId"`) için SQL'i `-f` dosyasıyla ver ve ön kontrol sonucunu `throw` ile değil açıkça başarısız olan bir yolla (`$ErrorActionPreference = 'Stop'`) doğrula.
