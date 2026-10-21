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
`Domain/Common`, `Application/Abstractions`, `Infrastructure/Persistence`, `Api/Middleware`. Henüz soyutlama yazılmadı; ihtiyaç doğunca doldurulacak.
