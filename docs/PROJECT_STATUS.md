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

## Bilinen eksikler
- Test projesi yok; çalıştırılabilir test bulunmuyor.
- Klasör `git` deposu değil (henüz `git init` yapılmadı).

## Sırada
- ADIM 02 (kullanıcı onayı bekleniyor).

## Kapsam dışı (henüz yapılmadı)
PostgreSQL, EF Core migration, Authentication/JWT, Product/Order/Inventory iş mantığı, React frontend, Docker/deployment.
