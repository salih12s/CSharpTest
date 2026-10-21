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
