namespace ProjectManagerWebAPI.Models.Kubernetes;

/// <summary>Um namespace disponível no portal, com o resumo do que lá está.</summary>
public sealed record NamespaceInfo(
    string Nome,
    int TotalDeployments,
    int DeploymentsProntos,
    int DeploymentsParados);

/// <summary>Um deployment na listagem principal.</summary>
public sealed record DeploymentInfo(
    string Namespace,
    string Nome,
    string Imagem,
    int ReplicasDesejadas,
    int ReplicasProntas,
    int ReplicasDisponiveis,
    int ReplicasAtualizadas,
    // pronto | degradado | parado | a-atualizar
    string Estado,
    // Réplicas guardadas quando o deployment foi parado pelo portal, se for o caso.
    int? ReplicasAntesDeParar,
    DateTimeOffset? Criado,
    DateTimeOffset? UltimoReinicio,
    // Título da informação escrita pela equipa. Não vem do cluster: é acrescentado pelo
    // controlador a partir da base de dados, por isso tem valor por omissão.
    string? Titulo = null);

/// <summary>Um pod de um deployment, no painel que abre no "+".</summary>
public sealed record PodInfo(
    string Nome,
    string Estado,
    string No,
    string? Ip,
    int Reinicios,
    bool Pronto,
    int TotalContentores,
    int ContentoresProntos,
    DateTimeOffset? Criado,
    // Razão do contentor em espera (ImagePullBackOff, CrashLoopBackOff...), quando existe.
    string? Motivo,
    // Nomes dos contentores: com mais do que um, o Kubernetes exige escolher de qual vem o log.
    IReadOnlyList<string> Contentores);

/// <summary>Resultado de um comando sobre um deployment, já com o estado novo.</summary>
public sealed record ResultadoComando(
    string Mensagem,
    DeploymentInfo Deployment);

/// <summary>Uma linha da consola de um pod, já separada do carimbo temporal.</summary>
public sealed record LinhaLog(
    DateTimeOffset? Tempo,
    string Texto);

/// <summary>
/// Resposta do log. <paramref name="Ultimo"/> é o carimbo da última linha entregue e volta no
/// pedido seguinte: é assim que o portal vai buscar só o que apareceu entretanto.
/// </summary>
public sealed record ResultadoLog(
    IReadOnlyList<LinhaLog> Linhas,
    string? Ultimo);
