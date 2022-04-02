using PokemonPRNG.LCG32.GCLCG;
using PokemonXDRNGLibrary;

namespace PokemonXDRNGLibrary;
public static class ConsumptionNavigator
{
    public static (long GenerateParties, long ChangeSetting, List<(uint pIndex, uint eIndex, uint HP, uint seed)> Parties) Calculate(UInt32 currentSeed, UInt32 targetSeed, uint tsv = 65536)
    {
        UInt32 totalConsumption = targetSeed.GetIndex(currentSeed);

        long generateParties = 0;
        long changeSetting = 0;

        // 最大のいますぐバトル生成数を取得する
        uint checkpointSeed = currentSeed;
        List<(uint pIndex, uint eIndex, uint HP, uint seed)> list = new List<(uint, uint, uint, uint)>();
        while (true)
        {
            list.Add(XDDatabase.GenerateQuickBattle(checkpointSeed, tsv));
            if (totalConsumption < targetSeed.GetIndex(list.Last().seed))
            {
                // targetSeed までの消費数が totalConsumption を越えたら、targetSeed を追い越していると考えられる
                list.RemoveAt(list.Count - 1);
                break;
            }

            generateParties++;
            checkpointSeed = list.Last().seed;
        }

        long leftover = (long)targetSeed.GetIndex(checkpointSeed);
        // ロード前にぴったり消費するためには、いますぐバトル生成後の残り消費数が40で割り切れる必要がある
        while (leftover % 40 != 0)
        {
            if (list.Count == 0)
            {
                throw new Exception(string.Format("No way to reach {0} from {1} before loading.", Convert.ToString(targetSeed, 16), Convert.ToString(currentSeed, 16)));
            }
            list.RemoveAt(list.Count - 1);
            generateParties--;
            checkpointSeed = list.Count == 0 ? currentSeed : list.Last().seed;
            leftover = (long)targetSeed.GetIndex(checkpointSeed);
        }
        changeSetting = (long)Math.Floor((decimal)((long)leftover / 40));

        return (generateParties, changeSetting, list);
    }
}
