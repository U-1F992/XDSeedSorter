using Xunit;

using PokemonPRNG.LCG32.GCLCG;
using PokemonXDRNGLibrary;

public class UnitTest1
{
    [Theory]
    [InlineData(2283031324, 3554654857, 1, 0)]
    [InlineData(2283031324, 339302124, 0, 2)]
    public void Calculate_correctly(uint currentSeed, uint targetSeed, long gerenate, long change)
    {
        var ret = ConsumptionNavigator.Calculate(currentSeed, targetSeed, false, 0, 0);
        Assert.Equal(gerenate, ret.GenerateParties);
        Assert.Equal(change, ret.ChangeSetting);
    }

    [Theory]
    [InlineData(2283031324, 3554654857, false, 0, 0)]
    [InlineData(2283031324, 339302124, false, 0, 0)]
    [InlineData(0xFE645768, 0xFF7EAFAB, true, 24, 13)]
    [InlineData(0x8776EBE8, 0xDB478A6D, true, 24, 13)]
    
    public void Calculate_correctly_2(uint currentSeed, uint targetSeed, bool allow, int byLoading, int byItems)
    {
        var ret = ConsumptionNavigator.Calculate(currentSeed, targetSeed, allow, byLoading, byItems);
        
        var seed = ret.Parties.Count == 0 ? currentSeed : ret.Parties[ret.Parties.Count - 1].seed;
        seed.Advance((uint)(ret.ChangeSetting * 40));
        seed.Advance((uint)byLoading);
        seed.Advance((uint)(ret.Save * 63));
        seed.Advance((uint)(ret.OpenItems * byItems));
        seed.Advance((uint)(ret.WatchSteps * 2));

        Assert.Equal(targetSeed, seed);
    }
}