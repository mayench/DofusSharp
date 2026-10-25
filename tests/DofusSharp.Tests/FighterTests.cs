using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class FighterTests
{
    [Fact]
    public void TakeDamage_ClampsHpAtZeroAndMarksFighterAsDead()
    {
        var fighter = new Fighter(Guid.NewGuid(), "Iop", 1, 1, 6, 3, hp: 10);

        fighter.TakeDamage(15);

        Assert.Equal(0, fighter.HP);
        Assert.False(fighter.IsAlive);

        fighter.TakeDamage(4);
        Assert.Equal(0, fighter.HP);
    }
}
