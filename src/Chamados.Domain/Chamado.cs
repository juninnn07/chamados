namespace Chamados.Domain;
public enum StatusChamado
{
 Aberto,
 EmAtendimento,
 Resolvido,
 Cancelado
}
public sealed class Chamado
{
 public Guid Id { get; } = Guid.NewGuid();
 public string Titulo { get; }
 public StatusChamado Status { get; private set; } = StatusChamado.Aberto;
 public DateTimeOffset CriadoEm { get; } = DateTimeOffset.UtcNow;
 public Chamado(string titulo)
 {
  if (string.IsNullOrWhiteSpace(titulo))
  {
   throw new ArgumentException("O título do chamado não pode ser vazio.", nameof(titulo));
  }

  ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
  Titulo = titulo;
 }
}