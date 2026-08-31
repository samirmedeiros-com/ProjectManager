namespace ProjectManagerWebAPI.Services;

public sealed class KubernetesOptions
{
    public const string Seccao = "Kubernetes";

    /// <summary>URL do servidor de API do cluster (o mesmo do kubeconfig, incluindo a porta 6443).</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// Token da ServiceAccount criada em kubernetes/rbac/projectmanager-k8s.yaml.
    /// Não se usa o kubeconfig da OCI: esse autentica por exec do CLI `oci`, que não existe
    /// no servidor da API e devolve um token de vida curta que ninguém aqui saberia renovar.
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>O certificado do servidor de API é assinado pela CA do próprio cluster.</summary>
    public bool IgnorarCertificado { get; set; } = true;

    public int TimeoutSegundos { get; set; } = 30;

    /// <summary>
    /// Lista fechada de namespaces visíveis. É filtro de apresentação e também de segurança:
    /// o gateway recusa qualquer pedido para um namespace fora daqui antes de sair da aplicação.
    /// </summary>
    public List<string> Namespaces { get; set; } = [];

    /// <summary>
    /// Endereço que vai no email das passwords. Fica em configuração porque é o endereço
    /// público do portal, que não coincide com o do servidor onde a aplicação corre.
    /// </summary>
    public string UrlLogin { get; set; } = "http://projetos.dpd.pt/login-kubernetes";

    /// <summary>Tecto para as linhas de log pedidas de uma vez, por pod.</summary>
    public int LinhasLogMaximo { get; set; } = 2000;

    /// <summary>Desligar isto deixa o portal em leitura: parar, arrancar e reiniciar passam a 403.</summary>
    public bool PermitirComandos { get; set; } = true;
}
