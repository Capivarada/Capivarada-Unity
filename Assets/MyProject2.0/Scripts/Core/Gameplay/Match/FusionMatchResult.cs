using Fusion;

/// <summary>
/// Authoritative end-of-match projection. It exists only on the simulation
/// server and is later converted into the Azure settlement payload.
/// </summary>
public sealed class FusionMatchResult
{
    public PlayerRef PlayerRef { get; set; }
    public bool IsBot { get; set; }
    public bool IsEliminated { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    // O token não autentica PlayFab/Azure; ele só comprova que este jogador
    // autenticado entrou nesta partida específica e expira em poucos minutos.
    public string JoinToken { get; set; } = string.Empty;
    public string Nickname { get; set; } = "Capivara";
    public string AvatarId { get; set; } = "avatar1";
    public int Eliminations { get; set; }
    public int RoundsSurvived { get; set; }
    public int Placement { get; set; }

    public bool CanPersist => !IsBot && !string.IsNullOrWhiteSpace(PlayerId) && !string.IsNullOrWhiteSpace(JoinToken);
}
