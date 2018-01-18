using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class FornecedorMonitoria
    {
        [JsonProperty("detalhamento", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<DetalhamentoFornecedorMonitoria> Detalhamento { get; set; }

        [JsonProperty("grafico", NullValueHandling = NullValueHandling.Ignore)]
        public GraficoFornecedorMonitoria Grafico { get; set; }

        [JsonProperty("clientes", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<ClienteFornecedorMonitoria> Clientes { get; set; }

        [JsonProperty("servicos", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<FornecedorServicoModel> Servico { get; set; }        
    }

    public class DetalhamentoFornecedorMonitoria
    {
        [JsonProperty("cliente", NullValueHandling = NullValueHandling.Ignore)]
        public string Cliente { get; set; }

        [JsonProperty("clienteid", NullValueHandling = NullValueHandling.Ignore)]
        public int ClienteID { get; set; }

        [JsonProperty("previstos", NullValueHandling = NullValueHandling.Ignore)]
        public int Previsto { get; set; }

        [JsonProperty("recebidos", NullValueHandling = NullValueHandling.Ignore)]
        public int Recebido { get; set; }

        [JsonProperty("enviados", NullValueHandling = NullValueHandling.Ignore)]
        public int Enviado { get; set; }

        [JsonProperty("erros", NullValueHandling = NullValueHandling.Ignore)]
        public int Erro { get; set; }

        [JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
        public int Capacidade { get; set; }

        [JsonProperty("consumo", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Consumo { get; set; }
    }

    public class GraficoFornecedorMonitoria
    {
        [JsonProperty("previstos", NullValueHandling = NullValueHandling.Ignore)]
        public int Previsto { get; set; }

        [JsonProperty("recebidos", NullValueHandling = NullValueHandling.Ignore)]
        public int Recebido { get; set; }

        [JsonProperty("enviados", NullValueHandling = NullValueHandling.Ignore)]
        public int Enviado { get; set; }

        [JsonProperty("erros", NullValueHandling = NullValueHandling.Ignore)]
        public int Erro { get; set; }

        [JsonProperty("consumo", NullValueHandling = NullValueHandling.Ignore)]
        public decimal Consumo { get; set; }
    }
    
    public class ClienteFornecedorMonitoria
    {
        [JsonProperty("nome", NullValueHandling = NullValueHandling.Ignore)]
        public string Cliente { get; set; }

        [JsonProperty("clienteid", NullValueHandling = NullValueHandling.Ignore)]
        public int ClienteID { get; set; }
        
        [JsonProperty("capacidade", NullValueHandling = NullValueHandling.Ignore)]
        public int Capacidade { get; set; }

        [JsonProperty("consumo", NullValueHandling = NullValueHandling.Ignore)]
        public int Consumo { get; set; }

        [JsonProperty("eficiencia", NullValueHandling = NullValueHandling.Ignore)]
        public int Eficiencia { get; set; }

        [JsonProperty("entrega", NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan Entrega { get; set; }

        [JsonProperty("lancamentos", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<LancamentoFornecedorMonitoria> Lancamentos { get; set; }
    }

    public class LancamentoFornecedorMonitoria
    {
        [JsonProperty("hora", NullValueHandling = NullValueHandling.Ignore)]
        public int Hora { get; set; }

        [JsonProperty("previstos", NullValueHandling = NullValueHandling.Ignore)]
        public int Previsto { get; set; }

        [JsonProperty("recebidos", NullValueHandling = NullValueHandling.Ignore)]
        public int Recebido { get; set; }

        [JsonProperty("enviados", NullValueHandling = NullValueHandling.Ignore)]
        public int Enviado { get; set; }

        [JsonProperty("erros", NullValueHandling = NullValueHandling.Ignore)]
        public int Erro { get; set; }

    }
}
