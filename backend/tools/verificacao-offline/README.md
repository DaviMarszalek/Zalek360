# Verificação offline histórica

Esta ferramenta foi usada na entrega inicial, quando o NuGet estava indisponível. Os stubs reproduzem assinaturas e não executam EF, Npgsql ou a geração de documentos.

Os stubs **não cobrem QuestPDF nem WebApplicationFactory** e o script original não compila a entrega atual completa. Ele foi preservado como evidência histórica; não é um critério de aceite atual. Com acesso ao NuGet, execute em `backend/`:

```sh
dotnet test Zalek360.sln
```

Sem NuGet, apenas Domain e Application podem ser compilados isoladamente pelo SDK, pois não têm dependências externas. Isso não substitui os testes da solução, do PDF, do endpoint ou do Docker.
