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
- Migration ve entity henüz yok; eklenmeden önce kullanıcıdan onay alınır.
- Bağlantı dizesi yoksa uygulama açılır, yalnızca `/health/ready` 503 verir ve gerçek-DB testi `Skipped` görünür.

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
