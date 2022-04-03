using PokemonPRNG.LCG32.GCLCG;
using PokemonXDRNGLibrary;

namespace PokemonXDRNGLibrary;
public static class ConsumptionNavigator
{
    public static (long GenerateParties, long ChangeSetting, long Save, long OpenItems, long WatchSteps, List<(uint pIndex, uint eIndex, uint HP, uint seed)> Parties) Calculate(UInt32 currentSeed, UInt32 targetSeed, bool allowLoad, int advancesByLoading, int advancesByOpeningItems, uint tsv = 65536)
    {
        // ロードさせる場合は、ロード時の消費数を加味した分前のseedを目標とする
        if (allowLoad) targetSeed = targetSeed.PrevSeed((uint)advancesByLoading);
        UInt32 totalConsumption = targetSeed.GetIndex(currentSeed);

        (long generateParties, long changeSetting, long save, long openItems, long watchSteps) count = (0, 0, 0, 0, 0);

        // 最大のいますぐバトル生成数を取得する
        uint checkpointSeed = currentSeed;
        var list = new List<(uint pIndex, uint eIndex, uint HP, uint seed)>();
        while (true)
        {
            list.Add(XDDatabase.GenerateQuickBattle(checkpointSeed, tsv));
            if (totalConsumption < targetSeed.GetIndex(list.Last().seed))
            {
                // targetSeed までの消費数が totalConsumption を越えたら、targetSeed を追い越していると考えられる
                list.RemoveAt(list.Count - 1);
                break;
            }

            count.generateParties++;
            checkpointSeed = list.Last().seed;
        }

        long leftover = (long)targetSeed.GetIndex(checkpointSeed);
        if (!allowLoad)
        {
            // ロード前にぴったり消費するためには、いますぐバトル生成後の残り消費数が40で割り切れる必要がある
            while (leftover % 40 != 0)
            {
                if (list.Count == 0)
                {
                    throw new Exception(string.Format("No way to reach {0} from {1} before loading.", Convert.ToString(targetSeed, 16), Convert.ToString(currentSeed, 16)));
                }
                list.RemoveAt(list.Count - 1);
                count.generateParties--;
                checkpointSeed = list.Count == 0 ? currentSeed : list.Last().seed;
                leftover = (long)targetSeed.GetIndex(checkpointSeed);
            }
        }
        else
        {
            // 持ち物消費が偶数であり残り消費数が63より少ない奇数である場合 (63より小さい奇数は消費できない)
            // 持ち物消費が奇数だが、残り消費数が持ち物消費より少ない場合 (持ち物消費より小さい奇数は消費できない)
            while (
                (advancesByOpeningItems % 2 == 0 && leftover < 63 && leftover % 2 != 0) ||
                (leftover < advancesByOpeningItems)
            )
            {
                if (list.Count == 0)
                {
                    throw new Exception(string.Format("No way to reach {0} from {1}.", Convert.ToString(targetSeed, 16), Convert.ToString(currentSeed, 16)));
                }
                list.RemoveAt(list.Count - 1);
                count.generateParties--;
                checkpointSeed = list.Count == 0 ? currentSeed : list.Last().seed;
                leftover = (long)targetSeed.GetIndex(checkpointSeed);
            }
        }

        if (!allowLoad)
        {
            count.changeSetting = (long)Math.Floor((decimal)((long)leftover / 40));
        }
        else
        {
            // レポート(63消費)
            count.save = (long)Math.Floor((decimal)(leftover / 63));
            // 持ち物消費が偶数である場合、奇数の消費手段はレポートのみになる
            //
            // 残り消費数が奇数である場合、レポート回数は奇数である
            // 残り消費数が偶数である場合、偶数である
            if (((leftover % 2 != 0 && count.save % 2 == 0) || (leftover % 2 == 0 && count.save % 2 != 0)) && count.save != 0)
            {
                count.save--;
            }
            leftover -= 63 * count.save;

            // 振動設定変更(40消費)
            count.changeSetting = (long)Math.Floor((decimal)(leftover / 40));
            leftover -= 40 * count.changeSetting;

            // 持ち物を開く(advancesByOpeningItems消費)
            count.openItems = 0;
            if (advancesByOpeningItems != 0)
            {
                count.openItems = (long)Math.Floor((decimal)(leftover / advancesByOpeningItems));
                // 残り消費数が奇数である場合、持ち物を開く回数は奇数である
                // 残り消費数が偶数である場合、偶数である
                if (((leftover % 2 != 0 && count.openItems % 2 == 0) || (leftover % 2 == 0 && count.openItems % 2 != 0)) && count.openItems != 0)
                {
                    count.openItems--;
                }
                leftover -= advancesByOpeningItems * count.openItems;
            }

            // 腰振り(2消費)
            count.watchSteps = leftover / 2;
        }

        return (count.generateParties, count.changeSetting, count.save, count.openItems, count.watchSteps, list);
    }
}
