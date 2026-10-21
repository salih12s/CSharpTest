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

## Bilinen eksikler
- Saf unit test yok: henüz test edilecek Domain/Application mantığı bulunmuyor. UnitTests şimdilik yalnızca mimari testi içeriyor.
- `/health` yalnızca "uygulama ayakta" der; veritabanı kontrolü yok (DB henüz yok).
- CI (GitHub Actions) yok; testler yalnızca yerelde çalışıyor.
- Coverage ölçümü kurulmadı (`coverlet.collector` pakete dahil ama kullanılmıyor).

## Sırada
- ADIM 03 (kullanıcı onayı bekleniyor).

## Kapsam dışı (henüz yapılmadı)
PostgreSQL, EF Core migration, Authentication/JWT, Product/Order/Inventory iş mantığı, React frontend, Docker/deployment.
