namespace Zalek360.Domain.Common;

/// <summary>Códigos de erro estáveis devolvidos pela API (contrato com o frontend).</summary>
public static class CodigosErro
{
    public const string Validacao = "VALIDACAO";
    public const string NaoEncontrado = "NAO_ENCONTRADO";
    public const string NaoAutenticado = "NAO_AUTENTICADO";
    public const string AcessoNegado = "ACESSO_NEGADO";
    public const string CredenciaisInvalidas = "CREDENCIAIS_INVALIDAS";
    public const string ConflitoConcorrencia = "CONFLITO_CONCORRENCIA";
    public const string RegistroDuplicado = "REGISTRO_DUPLICADO";
    public const string RegistroRelacionado = "REGISTRO_RELACIONADO";
    public const string ErroInterno = "ERRO_INTERNO";

    public const string ClienteDadosInvalidos = "CLIENTE_DADOS_INVALIDOS";
    public const string ClienteDocumentoDuplicado = "CLIENTE_CPF_CNPJ_DUPLICADO";
    public const string ObservacaoInvalida = "OBSERVACAO_INVALIDA";

    public const string OrcamentoSemItens = "ORCAMENTO_SEM_ITENS";
    public const string OrcamentoDadosInvalidos = "ORCAMENTO_DADOS_INVALIDOS";
    public const string OrcamentoNaoEditavel = "ORCAMENTO_NAO_EDITAVEL";
    public const string OrcamentoTransicaoInvalida = "ORCAMENTO_TRANSICAO_INVALIDA";
    public const string OrcamentoValidadeVencida = "ORCAMENTO_VALIDADE_VENCIDA";
    public const string OrcamentoJaDecidido = "ORCAMENTO_JA_DECIDIDO";
    public const string OrcamentoFechado = "ORCAMENTO_FECHADO";

    public const string ContatoDadosInvalidos = "CONTATO_DADOS_INVALIDOS";
    public const string DecisaoDadosObrigatorios = "DECISAO_DADOS_OBRIGATORIOS";
    public const string DecisaoDadosInvalidos = "DECISAO_DADOS_INVALIDOS";

    public const string PedidoOrigemInvalida = "PEDIDO_ORIGEM_INVALIDA";
    public const string PedidoCriacaoDiretaNaoPermitida = "PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA";
    public const string PedidoTransicaoInvalida = "PEDIDO_TRANSICAO_INVALIDA";
    public const string PedidoCancelamentoSemMotivo = "PEDIDO_CANCELAMENTO_SEM_MOTIVO";

    public const string AnexoTipoNaoPermitido = "ANEXO_TIPO_NAO_PERMITIDO";
    public const string AnexoTamanhoExcedido = "ANEXO_TAMANHO_EXCEDIDO";
    public const string AnexoVazio = "ANEXO_VAZIO";
    public const string AnexoConteudoInvalido = "ANEXO_CONTEUDO_INVALIDO";
    public const string AnexoIndisponivel = "ANEXO_INDISPONIVEL";
}
