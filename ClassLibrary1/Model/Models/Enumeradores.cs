using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
	public enum TiposInvalidosEnums : byte
	{
		CELULARINVALIDO = 0,
		BLACKLIST = 1,
		ACIMA160CARACTERES = 2,
		HIGIENIZADO = 3,
		VALIDO = 4,
		FILTRADO = 5,
		LEIAUTEINVALIDO = 6,
		DUPLICADO = 7
	}

	public enum AtivacaoRatinhoChipEnums : byte {
		NAOATIVO = 0,
		ATIVACAO = 1,
		ATIVO = 2,
		ATIVONAREDE = 3,
		OPERACIONAL = 4 }

	public enum ActionCamp { CANCELAR, SUSPENDER, REATIVAR, REAGENDAR }

	[Flags]
	public enum Tipo : byte
	{
		SHORTCODE = 1,
		LONGCODE = 0,
		WHATSAPP = 3,
		LONGANDSHORT = 1 | 3
	}
	public enum ComandoPortaEnums
	{
		NOCOMMAND, CCID, CMGF, CGSMS, CMEE, CNMI, CSCS, CPMS, CMGD, CSMP, CMGS, NONE, AT, CNUM, CREG, COPS, ATI, ATI3, CFUN, CGMR, CPIN, CGMM, CMGL,
		CLIP,
		ATW,
		CSAS,
		ATD,
		ATE1,
		CMMS,
		QFCG,
		SETPORT,
		CURC,
		CGATT,
		CSCA,
		QCCID,
		STSF
	}

	public enum ReportDeliveryEnums : byte
	{
		ENTREGUE = 0,
		EXPIRADA = 70,
		EXCLUIDA = 68,
		REJEITADA = 64,
		ENVIADA = 55,
		ERRO = 99,
		AGENDADA = 110,
		SUSPENSA = 111,
		CANCELADA = 112,
		ENVIANDO = 113,
		BLACKLIST = 114
	}

	public enum StatusFornecedorEnums : byte
	{
		ATIVO = 0,
		INATIVO = 1,
		INTEGRADO = 2,
		PENDENTE = 3

	}

	public enum StatusEnvioEnums : byte
	{
		AGENDADOS = 0,
		ENVIANDO = 1,
		ENVIADOS = 2,
		ERROS = 3,
		SUSPENSOS = 4,
		CANCELADOS = 5
	}
	public enum StatusOperacionalFornecedorEnum : byte
	{
		OPERACIONAL = 0, FORADOAR = 1,
	}
	public enum OrigemChamadaEnums : byte
	{
		ENVIO = 0, RELATORIO = 1,
		CADASTRO = 2
	}

	public enum TipoAcessoSistemaEnums : byte
	{
		SEMACESSO = 0,
		LEITURA = 1,
		GRAVAVACAO = 2,
		EXCLUSAO = 3
	}
	public enum OperadorasEnums : byte
	{
		DESCONHECIDA = 0,
		VIVO = 3,
		CLARO = 2,
		TIM = 6,
		OI = 1,
		NEXTEL = 7,
		CTBC = 4,
		SERCOMTEL = 5,
		EUTV = 8,
		ALGAR = 9,
		PORTO = 10,
		OPTIONS = 11,
		TERAPAR = 12,
		DATORA = 13
	}
	public enum TipoEmail : byte
	{
		OUTROS = 0, ENVIOSMS = 1, REQUISICAOCARTEIRA = 2, NOVOUSUARIO = 4, NOVOCADASTRO = 5, RESETSENHA = 6,
		RELATORIOANALITICO = 7,
		LIMITECARTEIRA = 8,
		ANALITICORETORNO = 9
	}

	public enum StatusRelatorioEnum : byte
	{
		EXECUCAO = 0,
		DISPONIVEL = 1,
		SEMDADOS = 2
	}

	public enum TipoOcorrenciaFornecedor : byte
	{
		[DescriptionAttribute("Perda de conexão")]
		PERDACONEXAO = 0,
		[DescriptionAttribute("Login ou senha inválido")]
		LOGINSENHAINVALIDOS = 1,
		[DescriptionAttribute("Erro geral")]
		ERROGERAL = 2,
		[DescriptionAttribute("Interrupção de serviço")]
		INTERRUPCAO = 3,
		[DescriptionAttribute("Aumento de capacidade")]
		AUMENTOCAPACIDADE = 4,
		[DescriptionAttribute("Redução de capacidade")]
		REDUCAOCAPACIDADE = 5
	}

	public enum ModuloAtividadeEnumns : byte
	{
		[DescriptionAttribute("Usuário")]
		USUARIO = 0,
		[DescriptionAttribute("Fornecedor")]
		FORNECEDOR = 1,
		[DescriptionAttribute("Gestor")]
		GESTOR = 2,
		[DescriptionAttribute("Carteira")]
		CARTEIRA = 3,
		[DescriptionAttribute("Tipo de Campanha")]
		TIPOCAMPANHA = 4,
		[DescriptionAttribute("Leiaute")]
		LEIAUTE = 5,
		[DescriptionAttribute("Padrão de Envio")]
		PADRAOENVIO = 6,
		[DescriptionAttribute("Blacklist")]
		BLACKLIST = 7,
		[DescriptionAttribute("Envio")]
		ENVIO = 8,
		[DescriptionAttribute("Padrão")]
		PADRAO = 9,
		[DescriptionAttribute("Sms Simples")]
		SMSSIMPLE = 10,
		[DescriptionAttribute("Contratante")]
		CONTRATANTE = 11,
		[DescriptionAttribute("Perfil")]
		PERFIL = 12,
		[DescriptionAttribute("Nenhum")]
		NENHUM = 13



	}

	public enum TiposLogAtividadeEnums : byte
	{
		[DescriptionAttribute("Atualizou")]
		ATUALIZACAO = 0,
		[DescriptionAttribute("Excluiu")]
		EXCLUSAO = 1,
		[DescriptionAttribute("Incluiu")]
		GRAVACAO = 2,
		[DescriptionAttribute("Reagendou")]
		REAGENDAR = 3,
		[DescriptionAttribute("Suspendeu")]
		SUSPENDER = 4,
		[DescriptionAttribute("Redistribuiu")]
		REDISTRIBUIR = 5,
		[DescriptionAttribute("Cancelou")]
		CANCELAR = 6
	}

	public enum TipoErroUploadArquivoEnum : byte
	{
		DUPLICADO = 0, FORADOPADRAO = 1, CORROMPIDO = 2,
		INSUFICIENTE = 3,
		CARTEIRANAOATIVA = 4
	}

    public enum FornecedorEnum : int
    {
        Zenvia = 16, Conectta = 17, Pontal = 19, VEM = 18, PG = 20
    }

	public enum TipoRelatorioEnum : byte { MULTIPLASCARTEIRAS = 0, DETALHADO = 1 }

	public enum ClassificaoRetornoEnums : byte
	{
		POSITIVO = 0, NEGATIVO = 1, NEUTRO = 2
	}
	public class Enumeradores
	{
	}

    public enum TipoRetornoErroApiEnum : byte
    {
        ERRO = 1,
        BLACKLIST = 2,
        INVALIDO = 3,
        DUBPLICADO = 4,
        NUMEROINVALIDO = 5,
        BLOQUEADO = 6,
        HIGIENIZADO = 7
    }
}
