using Xunit;

using PokemonXDRNGLibrary;

public class UnitTest1
{
    [Theory]
    [InlineData(2283031324, 3554654857, 1, 0)]
    [InlineData(2283031324, 339302124, 0, 2)]
    public void Calculate_correctly(uint currentSeed, uint targetSeed, long gerenate, long change)
    {
        var ret = ConsumptionNavigator.Calculate(currentSeed, targetSeed);
        Assert.Equal(gerenate, ret.GenerateParties);
        Assert.Equal(change, ret.ChangeSetting);
    }
}