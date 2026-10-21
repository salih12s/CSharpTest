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

## Bilinen eksikler
- Saf unit test yok: henüz test edilecek Domain/Application mantığı bulunmuyor. UnitTests şimdilik yalnızca mimari testi içeriyor.
- `/health` yalnızca "uygulama ayakta" der; veritabanı kontrolü yok (DB henüz yok).
- CI bilerek yok (karar: yalnızca yerel test); testlerin çalıştırılması geliştiricinin sorumluluğunda.
- Coverage ölçümü kurulmadı (`coverlet.collector` pakete dahil ama kullanılmıyor).

## Sırada
- ADIM 04 (kullanıcı onayı bekleniyor).

## Kapsam dışı (henüz yapılmadı)
PostgreSQL, EF Core migration, Authentication/JWT, Product/Order/Inventory iş mantığı, React frontend, Docker/deployment.
