using DofusSharp.Combat.State;
using DofusSharp.Domain;

namespace DofusSharp.Combat.Progression;

/// <summary>Entrega una recompensa de experiencia única al equipo del jugador tras ganar la batalla.</summary>
public sealed class BattleExperienceService
{
    private bool _rewardGranted;

    public int AwardVictoryExperience(BattleResult result, IEnumerable<Fighter> fighters,
        string playerTeamId, int experiencePerFighter = 100)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fighters);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerTeamId);
        if (experiencePerFighter < 0)
            throw new ArgumentOutOfRangeException(nameof(experiencePerFighter));
        if (_rewardGranted || result.Status != BattleStatus.Victory || result.WinningTeamId != playerTeamId)
            return 0;

        var recipients = fighters.Where(fighter => fighter.IsAlive && fighter.TeamId == playerTeamId).ToArray();
        foreach (var fighter in recipients)
            fighter.GainExperience(experiencePerFighter);

        if (recipients.Length > 0) _rewardGranted = true;
        return recipients.Length;
    }
}
