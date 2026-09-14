namespace ProjectManagerWebAPI.Models.Kafka;

/// <summary>Estado de um conector ou de uma task, como o Kafka Connect o devolve.</summary>
public static class EstadoKafka
{
    public const string Running = "RUNNING";
    public const string Failed = "FAILED";
    public const string Paused = "PAUSED";

    /// <summary>FAILED é o único estado sobre o qual há alguma coisa a fazer a partir daqui.</summary>
    public static bool EmErro(string? estado) =>
        string.Equals(estado?.Trim(), Failed, StringComparison.OrdinalIgnoreCase);
}

public class TaskConector
{
    public int Id { get; set; }
    public string Estado { get; set; } = "";
    public string? Worker { get; set; }

    /// <summary>O stack trace que o Connect guarda para uma task falhada. Só vem no detalhe.</summary>
    public string? Traco { get; set; }

    public bool EmErro => EstadoKafka.EmErro(Estado);
}

public class Conector
{
    public string Nome { get; set; } = "";

    /// <summary>"source" (AS400 → tópico) ou "sink" (tópico → Oracle).</summary>
    public string Tipo { get; set; } = "";

    public string Estado { get; set; } = "";
    public string? Worker { get; set; }
    public List<TaskConector> Tasks { get; set; } = [];

    /// <summary>Tópico(s) que o conector lê ou escreve — a linha que explica para que serve.</summary>
    public string? Topico { get; set; }

    /// <summary>Tabela de destino de um sink, quando a configuração a declara.</summary>
    public string? Tabela { get; set; }

    public string? Classe { get; set; }

    public bool EmErro => EstadoKafka.EmErro(Estado) || Tasks.Any(t => t.EmErro);
    public int TasksEmErro => Tasks.Count(t => t.EmErro);
}

public class ResumoKafka
{
    public int Total { get; set; }
    public int AFuncionar { get; set; }
    public int ComErro { get; set; }
    public int EmPausa { get; set; }
    public List<Conector> Conectores { get; set; } = [];
}

public class ResultadoReinicio
{
    public string Conector { get; set; } = "";
    public int Task { get; set; }
    public bool Sucesso { get; set; }
    public string? Mensagem { get; set; }
}
