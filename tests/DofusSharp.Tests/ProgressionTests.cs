using DofusSharp.Combat.Progression;
using DofusSharp.Combat.State;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class ProgressionTests
{
    [Fact]
    public void GainExperience_LevelsUpWithIncreasingThresholdAndAwardsPoints()
    {
        var fighter = CreatePlayer();

        var firstLevelUps = fighter.GainExperience(250);

        Assert.Equal(1, firstLevelUps);
        Assert.Equal(2, fighter.Level);
        Assert.Equal(150, fighter.Experience);
        Assert.Equal(200, fighter.ExperienceToNextLevel);
        Assert.Equal(5, fighter.AvailableCharacteristicPoints);

        Assert.Equal(1, fighter.GainExperience(50));
        Assert.Equal(3, fighter.Level);
        Assert.Equal(0, fighter.Experience);
        Assert.Equal(10, fighter.AvailableCharacteristicPoints);
    }

    [Fact]
    public void TryAllocateCharacteristicPoint_UpdatesSelectedStatAndVitalityHealth()
    {
        var fighter = CreatePlayer();
        fighter.GainExperience(100);

        Assert.True(fighter.TryAllocateCharacteristicPoint(CharacteristicAttribute.Vitality));
        Assert.True(fighter.TryAllocateCharacteristicPoint(CharacteristicAttribute.Strength));
        Assert.True(fighter.TryAllocateCharacteristicPoint(CharacteristicAttribute.Intelligence));
        Assert.True(fighter.TryAllocateCharacteristicPoint(CharacteristicAttribute.Agility));
        Assert.True(fighter.TryAllocateCharacteristicPoint(CharacteristicAttribute.Luck));

        Assert.Equal(101, fighter.MaxHP);
        Assert.Equal(101, fighter.HP);
        Assert.Equal(1, fighter.Stats.Vitality);
        Assert.Equal(1, fighter.Stats.Strength);
        Assert.Equal(1, fighter.Stats.Intelligence);
        Assert.Equal(1, fighter.Stats.Agility);
        Assert.Equal(1, fighter.Stats.Luck);
        Assert.Equal(0, fighter.AvailableCharacteristicPoints);
        Assert.False(fighter.TryAllocateCharacteristicPoint(CharacteristicAttribute.Strength));
    }

    [Fact]
    public void BattleExperienceService_AwardsWinningPlayerTeamOnlyOnce()
    {
        var player = CreatePlayer();
        var enemy = new Fighter(Guid.NewGuid(), "Iop", 1, 0, 6, 3, 100, teamId: "red");
        var service = new BattleExperienceService();
        var victory = new BattleResult(BattleStatus.Victory, "blue");

        Assert.Equal(1, service.AwardVictoryExperience(victory, [player, enemy], "blue", 100));
        Assert.Equal(2, player.Level);
        Assert.Equal(0, player.Experience);
        Assert.Equal(0, enemy.Experience);
        Assert.Equal(0, service.AwardVictoryExperience(victory, [player, enemy], "blue", 100));
        Assert.Equal(2, player.Level);
    }

    private static Fighter CreatePlayer() =>
        new(Guid.NewGuid(), "Cra", 0, 0, 6, 3, 100, teamId: "blue", playerClass: PlayerClass.Ocra);
}
