using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Infrastructure.Persistence.Seed;

/// <summary>
/// Dados de demonstração coerentes com os protótipos. Todos os registros são criados pelos MESMOS métodos de domínio
/// usados pela aplicação (validações, cálculo, transições e histórico automáticos), apenas com "agora" controlado.
///
/// Datas relativas: os protótipos usam 24/09/2026 como "hoje". O seed desloca todas as datas para que esse dia
/// corresponda ao dia em que o seed é executado — assim "vence amanhã", "apresentado há 6 dias" etc. continuam válidos.
///
/// Decisões documentadas no README: (1) CPF/CNPJ dos protótipos têm dígitos verificadores inválidos, então foram
/// corrigidos mantendo os mesmos 12/9 primeiros dígitos; (2) a Academia Movimento NÃO é pré-cadastrada, para que o
/// roteiro "pesquisar → não encontrado → cadastrar" do protótipo seja reproduzível; (3) o maior orçamento semeado é o
/// #000123 e o maior pedido o #000056, para que os próximos sejam #000124 e #000057, como nos protótipos.
/// </summary>
internal sealed class DbSeeder
{
    private static readonly DateOnly HojeNosPrototipos = new(2026, 9, 24);
    private const string CondicaoPadrao = "50% na aprovação + 50% na entrega (PIX)";

    private readonly ZalekDbContext _db;
    private readonly IClock _clock;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger _logger;
    private int _deslocamentoDias;

    public DbSeeder(ZalekDbContext db, IClock clock, IPasswordHasher hasher, ILogger logger)
    {
        _db = db;
        _clock = clock;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task ExecutarAsync(string senhaDemo, CancellationToken ct)
    {
        if (await _db.Usuarios.AnyAsync(ct))
        {
            _logger.LogInformation("Seed ignorado: o banco já possui dados.");
            return;
        }

        _deslocamentoDias = _clock.HojeLocal.DayNumber - HojeNosPrototipos.DayNumber;
        await using var transacao = await _db.Database.BeginTransactionAsync(ct);

        // ------------------------------------------------------------------ usuários (senha única de demonstração)
        var hash = _hasher.Gerar(senhaDemo);
        var desde = Momento(2024, 1, 10, 9, 0);
        var maria = Usuario.Criar("Maria Silva", "maria.silva@zalekpersonalizados.com.br", hash, PerfilUsuario.Atendente, desde);
        var carlos = Usuario.Criar("Carlos Souza", "carlos.souza@zalekpersonalizados.com.br", hash, PerfilUsuario.Atendente, desde);
        var fernanda = Usuario.Criar("Fernanda Lima", "fernanda.lima@zalekpersonalizados.com.br", hash, PerfilUsuario.Atendente, desde);
        var admin = Usuario.Criar("Administrador Zalek", "admin@zalekpersonalizados.com.br", hash, PerfilUsuario.Administrador, desde);
        _db.Usuarios.AddRange(maria, carlos, fernanda, admin);

        // ------------------------------------------------------------------ catálogo
        var dryFit = Produto.Criar("Camiseta Dry Fit", "Camiseta", "Camiseta Dry Fit unissex, malha 100% poliéster 160 g/m², gola careca");
        var algodao = Produto.Criar("Camiseta Algodão", "Camiseta", "Camiseta 100% algodão fio 30.1 penteado, gola careca");
        var polo = Produto.Criar("Camisa Polo", "Camiseta", "Camisa polo piquet 50% algodão / 50% poliéster");
        var caneca = Produto.Criar("Caneca personalizada", "Caneca", "Caneca de porcelana branca 325 ml, acabamento brilhante");
        var copo = Produto.Criar("Copo térmico", "Copo térmico", "Copo térmico de inox 473 ml com tampa");
        var squeeze = Produto.Criar("Squeeze", "Squeeze", "Squeeze de alumínio 500 ml com mosquetão");
        var ecobag = Produto.Criar("Ecobag", "Ecobag", "Ecobag de algodão cru 37 × 41 cm, alça longa");
        var bone = Produto.Criar("Boné", "Boné", "Boné trucker, aba curva, regulagem snapback");
        _db.Produtos.AddRange(dryFit, algodao, polo, caneca, copo, squeeze, ecobag, bone);

        // ------------------------------------------------------------------ clientes
        var metalurgica = NovoCliente(carlos, Momento(2024, 3, 12, 10, 0), new DadosCliente(
            TipoPessoa.PessoaJuridica, "12345678000195", "Metalúrgica Horizonte Ltda.", null, "Luciana Prates", "Compras",
            "4734224410", "47998123304", "compras@metalhorizonte.com.br", "89219600", "Rua Dona Francisca", "8300", "Galpão 4",
            "Distrito Industrial", "Joinville", "SC"));
        var construtora = NovoCliente(fernanda, Momento(2025, 2, 3, 14, 20), new DadosCliente(
            TipoPessoa.PessoaJuridica, "45908112000183", "Construtora Vale Sul Ltda.", "Construtora Vale Sul", "Ricardo Almeida", "Suprimentos",
            "4730378820", "47984401290", "suprimentos@construtoravalesul.com.br", "89010100", "Rua XV de Novembro", "1200", "Sala 804",
            "Centro", "Blumenau", "SC"));
        var colegio = NovoCliente(fernanda, Momento(2025, 1, 20, 9, 30), new DadosCliente(
            TipoPessoa.PessoaJuridica, "07231554000149", "Associação Educacional Aurora", "Colégio Aurora", "Patrícia Nunes", "Coordenação",
            "4733716600", "47991234480", "secretaria@colegioaurora.com.br", "89251000", "Rua Reinoldo Rau", "450", null,
            "Centro", "Jaraguá do Sul", "SC"));
        var clinica = NovoCliente(maria, Momento(2025, 6, 9, 11, 0), new DadosCliente(
            TipoPessoa.PessoaJuridica, "29117340000189", "Clínica Bem Viver Ltda.", "Clínica Bem Viver", "Helena Duarte", "Diretora administrativa",
            "4130217755", "41997021145", "administrativo@clinicabemviver.com.br", "80420090", "Avenida Sete de Setembro", "4214", "Conjunto 12",
            "Batel", "Curitiba", "PR"));
        var joao = NovoCliente(maria, Momento(2025, 11, 4, 16, 45), new DadosCliente(
            TipoPessoa.PessoaFisica, "31844296016", "João Martins", null, null, null,
            null, "47996547781", "joao.martins@gmail.com", "89201000", "Rua Visconde de Taunay", "310", "Apto 52",
            "Centro", "Joinville", "SC"));
        var anaPaula = NovoCliente(maria, Momento(2026, 3, 18, 10, 15), new DadosCliente(
            TipoPessoa.PessoaFisica, "02781533050", "Ana Paula Rocha", null, null, null,
            null, "47982335671", "anapaula.rocha@outlook.com", "89216000", "Rua Blumenau", "1580", null,
            "América", "Joinville", "SC"));
        var rotaLitoral = NovoCliente(carlos, Momento(2026, 9, 10, 15, 30), new DadosCliente(
            TipoPessoa.PessoaJuridica, "33604219000159", "Transportes Rota Litoral Ltda.", null, "Marcos Vieira", "Financeiro",
            "4733481180", "47998770021", "financeiro@rotalitoral.com.br", "88301000", "Rua Hercílio Luz", "760", null,
            "Centro", "Itajaí", "SC"));

        Observacao(metalurgica, carlos, Momento(2024, 3, 12, 10, 5),
            "Cliente recorrente. Prefere falar com a Luciana (compras) pelo WhatsApp. Pagamento sempre via PIX.");
        Observacao(metalurgica, maria, Momento(2026, 7, 15, 11, 0),
            "Enviar prova digital da arte para aprovação antes de iniciar a produção.");
        Observacao(construtora, fernanda, Momento(2025, 2, 3, 14, 25),
            "Pedidos grandes para obras. A nota fiscal deve sair com o CNPJ da matriz.");
        Observacao(colegio, fernanda, Momento(2025, 1, 20, 9, 35),
            "Compras concentradas no início de cada semestre (uniformes e brindes de eventos).");
        Observacao(joao, maria, Momento(2025, 11, 4, 16, 50), "Cliente pessoa física; prefere contato pelo WhatsApp.");
        Observacao(rotaLitoral, carlos, Momento(2026, 9, 10, 15, 35),
            "Cadastro feito após ligação do Marcos. Interesse em camisetas para motoristas; ainda sem orçamento.");

        await _db.SaveChangesAsync(ct);

        // ------------------------------------------------------------------ orçamentos e pedidos
        // #000098 — Metalúrgica · aprovado presencialmente → pedido #000044 entregue
        var o098 = NovoOrcamento(98, metalurgica, carlos, Momento(2026, 7, 15, 10, 0), Data(2026, 7, 30), 12, CondicaoPadrao,
            "Uniformes para a equipe de produção.", 0m,
            Item(dryFit, 220, 22m, 7m, "Serigrafia (frente e costas)", "Azul marinho", "2 cores (branco e laranja)", "28 × 10 cm",
                "Peito esquerdo e costas", "Logo oficial enviado pela Luciana", "Grade: P 30, M 90, G 70, GG 30"));
        o098.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 7, 15), Momento(2026, 7, 15, 15, 0), carlos.Id);
        o098.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Presencial, Momento(2026, 7, 18, 11, 0), Data(2026, 7, 18),
            null, "Aprovado em reunião com a diretoria.", Momento(2026, 7, 18, 11, 0), carlos.Id);
        var p044 = NovoPedido(o098, 44, carlos, Momento(2026, 7, 18, 11, 0));
        p044.AtualizarSituacao(SituacaoPedido.EmProducao, "Arte aprovada; produção iniciada.", Momento(2026, 7, 20, 9, 0), carlos.Id);
        p044.AtualizarSituacao(SituacaoPedido.Pronto, "Peças conferidas e embaladas.", Momento(2026, 8, 3, 16, 0), carlos.Id);
        p044.AtualizarSituacao(SituacaoPedido.Entregue, "Entregue na portaria da fábrica.", Momento(2026, 8, 5, 10, 0), carlos.Id);

        // #000104 — Metalúrgica · sem retorno até o fim da validade → expirado automaticamente
        var o104 = NovoOrcamento(104, metalurgica, carlos, Momento(2026, 8, 10, 14, 0), Data(2026, 8, 25), 10, CondicaoPadrao, null, 0m,
            Item(ecobag, 100, 15m, 4m, "Serigrafia (1 lado)", "Cru", "1 cor (azul)", "25 × 25 cm", "Frente", "Logo Metalúrgica Horizonte"));
        o104.MarcarAguardandoRetorno(MeioContato.WhatsApp, Data(2026, 8, 10), Momento(2026, 8, 10, 16, 0), carlos.Id);
        Expirar(o104);

        // #000108 — João Martins · aprovado por WhatsApp → pedido #000051 cancelado
        var o108 = NovoOrcamento(108, joao, maria, Momento(2026, 8, 17, 9, 30), Data(2026, 9, 1), 8, "100% na aprovação (PIX)",
            "Canecas para o aniversário de casamento dos pais.", 0m,
            Item(caneca, 20, 24m, 8m, "Sublimação", "Branca", "Colorida (foto)", "20 × 9 cm", "Contorno total", "Foto enviada pelo WhatsApp"));
        o108.MarcarAguardandoRetorno(MeioContato.WhatsApp, Data(2026, 8, 17), Momento(2026, 8, 17, 10, 0), maria.Id);
        o108.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, Momento(2026, 8, 26, 14, 0), Data(2026, 8, 26),
            null, "Aprovou a arte com a foto dos pais.", Momento(2026, 8, 26, 14, 0), maria.Id);
        var p051 = NovoPedido(o108, 51, maria, Momento(2026, 8, 26, 14, 0));
        p051.AtualizarSituacao(SituacaoPedido.Cancelado, "Cliente adiou a comemoração e pediu o cancelamento antes da produção.",
            Momento(2026, 8, 28, 11, 0), maria.Id);

        // #000109 — Construtora · aguardando retorno, vence amanhã
        var o109 = NovoOrcamento(109, construtora, fernanda, Momento(2026, 8, 19, 10, 0), Data(2026, 9, 25), 12, "30/60 dias (boleto)",
            "Brindes para a entrega do Residencial Vale Verde.", 0m,
            Item(copo, 50, 58m, 19.80m, "Gravação a laser", "Inox escovado", "Monocromática", "6 × 3 cm", "Corpo do copo", "Logo Vale Sul"));
        o109.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 9, 18), Momento(2026, 9, 18, 9, 0), fernanda.Id);
        Contato(o109, fernanda, MeioContato.Ligacao, Momento(2026, 9, 22, 14, 0),
            "Ricardo pediu mais alguns dias para validar com a diretoria de obras.");

        // #000110 — Construtora · aprovado por ligação → pedido #000052 entregue
        var o110 = NovoOrcamento(110, construtora, fernanda, Momento(2026, 8, 20, 9, 0), Data(2026, 9, 4), 12, "30/60 dias (boleto)",
            "Kit para a equipe de campo.", 0m,
            Item(algodao, 200, 24m, 6m, "Serigrafia (frente)", "Cinza mescla", "1 cor (preto)", "30 × 12 cm", "Frente", "Logo Vale Sul"),
            Item(bone, 167, 15m, 5m, "Bordado", "Preto", "2 cores", "8 × 4 cm", "Frente", "Logo Vale Sul"));
        o110.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 8, 20), Momento(2026, 8, 20, 16, 0), fernanda.Id);
        o110.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Ligacao, Momento(2026, 8, 29, 10, 0), Data(2026, 8, 29),
            null, null, Momento(2026, 8, 29, 10, 0), fernanda.Id);
        var p052 = NovoPedido(o110, 52, fernanda, Momento(2026, 8, 29, 10, 0));
        p052.AtualizarSituacao(SituacaoPedido.EmProducao, null, Momento(2026, 8, 31, 8, 30), fernanda.Id);
        p052.AtualizarSituacao(SituacaoPedido.Pronto, null, Momento(2026, 9, 11, 17, 0), fernanda.Id);
        p052.AtualizarSituacao(SituacaoPedido.Entregue, "Retirado pelo motorista da construtora.", Momento(2026, 9, 14, 10, 30), fernanda.Id);

        // #000112 — Metalúrgica · aprovado presencialmente → pedido #000053 entregue
        var o112 = NovoOrcamento(112, metalurgica, carlos, Momento(2026, 8, 21, 11, 0), Data(2026, 9, 5), 11, CondicaoPadrao,
            "Camisas polo para a equipe comercial.", 0m,
            Item(polo, 180, 36m, 8m, "Bordado", "Branca", "2 cores (azul e laranja)", "9 × 5 cm", "Peito esquerdo", "Logo oficial"));
        o112.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 8, 21), Momento(2026, 8, 21, 15, 0), carlos.Id);
        o112.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Presencial, Momento(2026, 9, 3, 10, 0), Data(2026, 9, 3),
            null, null, Momento(2026, 9, 3, 10, 0), carlos.Id);
        var p053 = NovoPedido(o112, 53, carlos, Momento(2026, 9, 3, 10, 0));
        p053.AtualizarSituacao(SituacaoPedido.EmProducao, null, Momento(2026, 9, 4, 8, 30), carlos.Id);
        p053.AtualizarSituacao(SituacaoPedido.Pronto, null, Momento(2026, 9, 16, 17, 0), carlos.Id);
        p053.AtualizarSituacao(SituacaoPedido.Entregue, null, Momento(2026, 9, 18, 11, 0), carlos.Id);

        // #000114 — Metalúrgica · aguardando retorno (apresentado há 4 dias)
        var o114 = NovoOrcamento(114, metalurgica, carlos, Momento(2026, 9, 4, 10, 0), Data(2026, 10, 5), 10, CondicaoPadrao, null, 0m,
            Item(squeeze, 100, 16m, 5.40m, "Gravação a laser", "Prata", "Monocromática", "5 × 5 cm", "Corpo", "Logo oficial"));
        o114.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 9, 20), Momento(2026, 9, 20, 10, 0), carlos.Id);

        // #000116 — João Martins · expirado sem decisão
        var o116 = NovoOrcamento(116, joao, maria, Momento(2026, 9, 5, 9, 12), Data(2026, 9, 20), 8, "100% na aprovação (PIX)",
            "Canecas com fotos da família para presente de Natal.", 0m,
            Item(caneca, 30, 21m, 7m, "Sublimação", "Branca", "Colorida (foto)", "20 × 9 cm", "Contorno total", "foto-familia-martins.jpg"));
        o116.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 9, 5), Momento(2026, 9, 5, 9, 40), maria.Id);
        Contato(o116, maria, MeioContato.Ligacao, Momento(2026, 9, 12, 10, 5),
            "João pediu mais uns dias para decidir as fotos da caneca.");
        Expirar(o116);

        // #000117 — Colégio Aurora · aprovado por e-mail → pedido #000054 pronto
        var o117 = NovoOrcamento(117, colegio, fernanda, Momento(2026, 9, 12, 10, 30), Data(2026, 9, 27), 10, "30 dias (boleto)",
            "Camisetas e squeezes para a gincana da primavera.", 0m,
            Item(algodao, 80, 26m, 6m, "Serigrafia (frente)", "Amarela", "2 cores", "28 × 20 cm", "Frente", "Arte da gincana"),
            Item(squeeze, 20, 22m, 6m, "Gravação a laser", "Azul", "Monocromática", "5 × 5 cm", "Corpo", "Brasão do colégio"));
        o117.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 9, 12), Momento(2026, 9, 12, 16, 0), fernanda.Id);
        o117.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Email, Momento(2026, 9, 15, 9, 20), Data(2026, 9, 15),
            null, "Aprovado pela coordenação pedagógica.", Momento(2026, 9, 15, 9, 20), fernanda.Id);
        var p054 = NovoPedido(o117, 54, fernanda, Momento(2026, 9, 15, 9, 20));
        p054.AtualizarSituacao(SituacaoPedido.EmProducao, null, Momento(2026, 9, 16, 8, 0), fernanda.Id);
        p054.AtualizarSituacao(SituacaoPedido.Pronto, "Aguardando retirada pelo colégio.", Momento(2026, 9, 23, 16, 30), fernanda.Id);

        // #000118 — Ana Paula · cancelado pelo cliente
        var o118 = NovoOrcamento(118, anaPaula, maria, Momento(2026, 9, 16, 14, 0), Data(2026, 10, 1), 8, "100% na aprovação (PIX)", null, 0m,
            Item(caneca, 18, 20m, 7m, "Sublimação", "Branca", "Colorida", "20 × 9 cm", "Contorno total", "Frases para chá de panela"));
        o118.MarcarAguardandoRetorno(MeioContato.WhatsApp, Data(2026, 9, 16), Momento(2026, 9, 16, 15, 0), maria.Id);
        o118.RegistrarDecisao(ResultadoDecisao.Cancelado, MeioContato.WhatsApp, Momento(2026, 9, 18, 10, 0), Data(2026, 9, 18),
            "Desistiu da compra", "Cliente desistiu do presente.", Momento(2026, 9, 18, 10, 0), maria.Id);

        // #000119 — Construtora · recusado por preço
        var o119 = NovoOrcamento(119, construtora, fernanda, Momento(2026, 9, 18, 9, 0), Data(2026, 10, 3), 15, "30/60 dias (boleto)",
            "Kit de boas-vindas para novos colaboradores.", 0m,
            Item(algodao, 300, 22m, 6m, "Serigrafia (frente)", "Branca", "1 cor", "30 × 12 cm", "Frente", "Logo Vale Sul"),
            Item(bone, 300, 18m, 5m, "Bordado", "Azul marinho", "2 cores", "8 × 4 cm", "Frente", "Logo Vale Sul"),
            Item(squeeze, 100, 20m, 4.60m, "Gravação a laser", "Prata", "Monocromática", "5 × 5 cm", "Corpo", "Logo Vale Sul"),
            Item(ecobag, 50, 16m, 4m, "Serigrafia (1 lado)", "Cru", "1 cor", "25 × 25 cm", "Frente", "Logo Vale Sul"));
        o119.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 9, 18), Momento(2026, 9, 18, 14, 0), fernanda.Id);
        Contato(o119, fernanda, MeioContato.WhatsApp, Momento(2026, 9, 19, 15, 0),
            "Ricardo informou que está comparando com outros dois fornecedores.");
        o119.RegistrarDecisao(ResultadoDecisao.Recusado, MeioContato.Ligacao, Momento(2026, 9, 22, 11, 30), Data(2026, 9, 22),
            "Preço", "Cliente fechou com um fornecedor local por preço menor.", Momento(2026, 9, 22, 11, 30), fernanda.Id);

        // #000120 — Metalúrgica · aprovado na visita à fábrica → pedido #000055 em produção
        var o120 = NovoOrcamento(120, metalurgica, carlos, Momento(2026, 9, 19, 9, 40), Data(2026, 10, 4), 10, CondicaoPadrao,
            "Copos térmicos para o evento de fim de ano.", 0m,
            Item(copo, 89, 38m, 12m, "Gravação a laser", "Preto fosco", "Monocromática", "6 × 3 cm", "Corpo do copo", "Logo oficial"));
        o120.MarcarAguardandoRetorno(MeioContato.Presencial, Data(2026, 9, 19), Momento(2026, 9, 19, 10, 30), carlos.Id);
        o120.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.Presencial, Momento(2026, 9, 19, 17, 45), Data(2026, 9, 19),
            null, "Orçamento #000120 aprovado na visita à fábrica.", Momento(2026, 9, 19, 17, 45), carlos.Id);
        var p055 = NovoPedido(o120, 55, carlos, Momento(2026, 9, 19, 17, 45));
        p055.AtualizarSituacao(SituacaoPedido.EmProducao, "Gravação agendada.", Momento(2026, 9, 22, 8, 15), carlos.Id);

        // #000121 — Clínica Bem Viver · aprovado por WhatsApp → pedido #000056 em produção
        var o121 = NovoOrcamento(121, clinica, maria, Momento(2026, 9, 21, 10, 0), Data(2026, 10, 6), 10, "50% na aprovação + 50% na entrega (boleto)",
            "Brindes para a campanha Outubro Rosa.", 0m,
            Item(ecobag, 100, 14m, 4.80m, "Serigrafia (1 lado)", "Cru", "1 cor (rosa)", "25 × 25 cm", "Frente", "Laço da campanha"),
            Item(copo, 20, 42m, 13m, "Gravação a laser", "Rosa", "Monocromática", "6 × 3 cm", "Corpo do copo", "Logo da clínica"));
        o121.MarcarAguardandoRetorno(MeioContato.WhatsApp, Data(2026, 9, 21), Momento(2026, 9, 21, 11, 0), maria.Id);
        Contato(o121, maria, MeioContato.WhatsApp, Momento(2026, 9, 21, 16, 0), "Helena confirmou as cores do logo da clínica.");
        o121.RegistrarDecisao(ResultadoDecisao.Aprovado, MeioContato.WhatsApp, Momento(2026, 9, 22, 9, 15), Data(2026, 9, 22),
            null, null, Momento(2026, 9, 22, 9, 15), maria.Id);
        var p056 = NovoPedido(o121, 56, maria, Momento(2026, 9, 22, 9, 15));
        p056.AtualizarSituacao(SituacaoPedido.EmProducao, null, Momento(2026, 9, 23, 9, 0), maria.Id);

        // #000122 — Colégio Aurora · aguardando retorno, vence em 3 dias
        var o122 = NovoOrcamento(122, colegio, fernanda, Momento(2026, 9, 22, 10, 0), Data(2026, 9, 27), 12, "30 dias (boleto)",
            "Uniformes de educação física e canecas para professores.", 0m,
            Item(algodao, 180, 24m, 6m, "Serigrafia (frente e costas)", "Branca", "2 cores", "28 × 20 cm", "Frente e costas", "Brasão do colégio"),
            Item(caneca, 25, 26m, 6.60m, "Sublimação", "Branca", "Colorida", "20 × 9 cm", "Contorno total", "Arte Dia do Professor"));
        o122.MarcarAguardandoRetorno(MeioContato.Email, Data(2026, 9, 22), Momento(2026, 9, 22, 15, 0), fernanda.Id);

        // #000123 — Metalúrgica · em elaboração, com revisão de quantidade e dois contatos
        var o123 = NovoOrcamento(123, metalurgica, carlos, Momento(2026, 9, 23, 9, 5), Data(2026, 10, 8), 15, CondicaoPadrao,
            "Kit comemorativo dos 30 anos da empresa.", 0m,
            Item(polo, 120, 38m, 8m, "Bordado", "Azul marinho", "2 cores (branco e laranja)", "9 × 5 cm", "Peito esquerdo", "Selo 30 anos"),
            Item(bone, 150, 22m, 6m, "Bordado", "Azul marinho", "2 cores", "8 × 4 cm", "Frente", "Selo 30 anos"),
            Item(squeeze, 60, 24m, 5m, "Gravação a laser", "Prata", "Monocromática", "5 × 5 cm", "Corpo", "Selo 30 anos"));
        Contato(o123, carlos, MeioContato.Ligacao, Momento(2026, 9, 23, 9, 30),
            "Luciana pediu o orçamento para o evento de 30 anos da empresa, cerca de 150 kits.");
        AlterarQuantidade(o123, carlos, ordem: 1, novaQuantidade: 150, Momento(2026, 9, 23, 14, 40));
        Contato(o123, carlos, MeioContato.Email, Momento(2026, 9, 23, 15, 10),
            "Enviada a revisão com 3 itens (camisas polo, bonés e squeezes) para aprovação da diretoria.");

        await _db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        _logger.LogInformation("Seed concluído: 4 usuários, 8 produtos, 7 clientes, 15 orçamentos e 7 pedidos (deslocamento de {Dias} dia(s)).",
            _deslocamentoDias);
    }

    // ------------------------------------------------------------------ auxiliares

    private DateOnly Data(int ano, int mes, int dia) => new DateOnly(ano, mes, dia).AddDays(_deslocamentoDias);

    /// <summary>Data/hora local (fuso de negócio) convertida para UTC, já com o deslocamento aplicado.</summary>
    private DateTime Momento(int ano, int mes, int dia, int hora, int minuto) =>
        _clock.InicioDoDiaLocalEmUtc(Data(ano, mes, dia)).AddHours(hora).AddMinutes(minuto);

    private Cliente NovoCliente(Usuario autor, DateTime quando, DadosCliente dados)
    {
        var cliente = Cliente.Criar(dados, quando, autor.Id);
        _db.Clientes.Add(cliente);
        return cliente;
    }

    private void Observacao(Cliente cliente, Usuario autor, DateTime quando, string texto) =>
        _db.ObservacoesCliente.Add(ObservacaoCliente.Criar(cliente.Id, texto, quando, autor.Id));

    private static DadosItemOrcamento Item(Produto produto, int quantidade, decimal valorUnitario, decimal valorPersonalizacao,
        string tipo, string cor, string? coresArte, string? medidas, string? local, string? referencia, string? observacoesTecnicas = null) =>
        new(null, produto.Id, produto.Nome, produto.DescricaoBase, quantidade, valorUnitario, valorPersonalizacao,
            new DadosPersonalizacao(tipo, cor, coresArte, medidas, local, referencia, observacoesTecnicas),
            Array.Empty<ArquivoAnexo>());

    private Orcamento NovoOrcamento(int numero, Cliente cliente, Usuario responsavel, DateTime criadoEm, DateOnly validade, int prazo,
        string condicao, string? observacoes, decimal desconto, params DadosItemOrcamento[] itens)
    {
        var orcamento = Orcamento.Criar(numero, cliente.Id, responsavel.Id, _clock.ParaDataLocal(criadoEm),
            new DadosComerciaisOrcamento(validade, prazo, condicao, observacoes, desconto), itens, criadoEm, responsavel.Id);
        _db.Orcamentos.Add(orcamento);
        return orcamento;
    }

    private void Contato(Orcamento orcamento, Usuario autor, MeioContato meio, DateTime quando, string texto) =>
        orcamento.RegistrarContato(meio, quando, _clock.ParaDataLocal(quando), texto, quando, autor.Id);

    private void AlterarQuantidade(Orcamento orcamento, Usuario autor, int ordem, int novaQuantidade, DateTime quando)
    {
        var itens = orcamento.Itens.OrderBy(i => i.Ordem).Select(i => new DadosItemOrcamento(
            i.Id, i.ProdutoId, i.ProdutoNome, i.Descricao, i.Ordem == ordem ? novaQuantidade : i.Quantidade,
            i.ValorUnitario, i.ValorPersonalizacaoUnitario,
            new DadosPersonalizacao(i.TipoPersonalizacao, i.CorPeca, i.CoresArte, i.Medidas, i.LocalAplicacao, i.ReferenciaArte, i.ObservacoesTecnicas),
            i.Anexos.ToList())).ToList();
        orcamento.Atualizar(orcamento.ResponsavelId,
            new DadosComerciaisOrcamento(orcamento.Validade, orcamento.PrazoEstimadoDiasUteis, orcamento.CondicaoPagamento,
                orcamento.ObservacoesComerciais, orcamento.Desconto),
            itens, quando, autor.Id);
    }

    private Pedido NovoPedido(Orcamento orcamento, int numero, Usuario autor, DateTime aprovadoEm)
    {
        var pedido = Pedido.CriarAPartirDe(orcamento, numero, _clock.ParaDataLocal(aprovadoEm), aprovadoEm, autor.Id);
        _db.Pedidos.Add(pedido);
        return pedido;
    }

    /// <summary>Mesma regra da rotina automática, executada "no dia seguinte ao fim da validade".</summary>
    private void Expirar(Orcamento orcamento)
    {
        var diaSeguinte = orcamento.Validade.AddDays(1);
        var momento = _clock.InicioDoDiaLocalEmUtc(diaSeguinte);
        orcamento.ExpirarAutomaticamente(diaSeguinte, momento, momento);
    }
}
