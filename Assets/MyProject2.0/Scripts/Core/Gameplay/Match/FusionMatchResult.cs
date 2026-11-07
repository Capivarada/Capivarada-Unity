using Fusion;

/// <summary>
/// Authoritative end-of-match projection. It exists only on the simulation
/// server and is later converted into the Azure settlement payload.
/// </summary>
public sealed class FusionMatchResult
{
    public PlayerRef PlayerRef { get; init; }
    public bool IsBot { get; init; }
    public bool IsEliminated { get; init; }
    public string PlayerId { get; init; } = string.Empty;
    // O token não autentica PlayFab/Azure; ele só comprova que este jogador
    // autenticado entrou nesta partida específica e expira em poucos minutos.
    public string JoinToken { get; init; } = string.Empty;
    public string Nickname { get; init; } = "Capivara";
    public string AvatarId { get; init; } = "avatar1";
    public int Eliminations { get; init; }
    public int RoundsSurvived { get; init; }
    public int Placement { get; init; }

    public bool CanPersist => !IsBot && !string.IsNullOrWhiteSpace(PlayerId) && !string.IsNullOrWhiteSpace(JoinToken);
}
