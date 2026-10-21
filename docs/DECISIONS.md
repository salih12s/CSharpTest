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
