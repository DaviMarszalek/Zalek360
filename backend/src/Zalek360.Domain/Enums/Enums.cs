namespace Zalek360.Domain.Enums;

public enum TipoPessoa { PessoaFisica = 1, PessoaJuridica = 2 }

public enum PerfilUsuario { Atendente = 1, Gestor = 2, Operador = 3, Administrador = 4 }

/// <summary>RN5 — situações do orçamento no MVP 1.</summary>
public enum SituacaoOrcamento
{
    EmElaboracao = 1,
    AguardandoRetorno = 2,
    Aprovado = 3,
    Recusado = 4,
    Expirado = 5,
    Cancelado = 6
}

public enum SituacaoPedido { Aberto = 1, EmProducao = 2, Pronto = 3, Entregue = 4, Cancelado = 5 }

/// <summary>Canais externos de comunicação com o cliente (o cliente não acessa o sistema no MVP 1).</summary>
public enum MeioContato { WhatsApp = 1, Ligacao = 2, Email = 3, Presencial = 4, Outro = 5 }

public enum ResultadoDecisao { Aprovado = 1, Recusado = 2, Cancelado = 3, Expirado = 4 }

public enum EscopoHistorico { Cliente = 1, Orcamento = 2, Pedido = 3 }

public enum TipoEventoHistorico
{
    ClienteCadastrado = 1,
    ClienteAtualizado = 2,
    OrcamentoCriado = 10,
    OrcamentoAlterado = 11,
    QuantidadeAlterada = 12,
    OrcamentoStatusAlterado = 13,
    ContatoRegistrado = 14,
    DecisaoRegistrada = 15,
    OrcamentoExpirado = 16,
    PedidoGerado = 17,
    PedidoCriado = 30,
    PedidoStatusInicial = 31,
    PedidoStatusAlterado = 32,
    PedidoCancelado = 33
}
