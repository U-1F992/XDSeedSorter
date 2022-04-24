using System.Text.Json;
using System.IO.Ports;

using LINENotify;
using OpenCvSharp;
using PokemonPRNG.LCG32.GCLCG;
using PokemonXDImageLibrary;
using PokemonXDRNGLibrary;
using Sunameri;

namespace XDSeedSorter;
public class Sorter : IDisposable
{
    private Config _config;
    private Dictionary<string, Operation[]> _sequences;
    private List<(uint hp, uint seed)> _xddb;
    private SerialPort _serialPort;
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private Mat _mat = new Mat();
    private Task _task;

    public Sorter()
    {
        var jsonConfig = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "config.json")));
        _config = jsonConfig != null ? jsonConfig : throw new FileNotFoundException();
        var jsonSequences = JsonSerializer.Deserialize<Dictionary<string, Operation[]>>(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "sequences.json")));
        _sequences = jsonSequences != null ? jsonSequences : throw new FileNotFoundException();

        // XDDB読み込み
        _xddb = XDDatabase.LoadDB();

        // デバイス割り当て
        _serialPort = new SerialPort(_config.PortName, 4800);
        _serialPort.Open();
#if DEBUG
        _serialPort.DtrEnable = true;
        _serialPort.Encoding = System.Text.Encoding.UTF8;
        _serialPort.DataReceived += (object sender, SerialDataReceivedEventArgs e) =>
        {
            var msg = ((SerialPort)sender).ReadExisting();
            if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) return;
            Console.Error.WriteLine("[Sorter] [Trace] [{0}] {1}", DateTime.Now.ToString("HH:mm:ss.fff"), System.Text.RegularExpressions.Regex.Replace(msg, "\\s", ""));
        };
#endif

        // _videoCaptureから_matを取得して表示を更新する
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
        var ready = false;
        _task = Task.WhenAll
        (
            Task.Run(() =>
            {
                using (var videoCapture = new VideoCapture(_config.CaptureIndex) { FrameWidth = 1600, FrameHeight = 1200 })
                    while (!_cancellationToken.IsCancellationRequested)
                        lock (_mat)
                            if (videoCapture.Read(_mat) && !ready) ready = true;
            }, _cancellationToken),
            Task.Run(() =>
            {
                while (!ready) ;
                using (var window = new Window())
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        window.ShowImage(_mat.Resize(new Size(), 0.3, 0.3));
                        Cv2.WaitKey(1);
                    }
            }, _cancellationToken)
        );
        while (!ready) Thread.Sleep(1);

        Notifier.Send(_config.Token, "「ポケモンXD 闇の旋風ダーク・ルギア」初期seed厳選を開始します。");
    }
    /// <summary>
    /// _matを一時的に保存->Streamに読み込んでNotifier.Send
    /// </summary>
    /// <param name="message"></param>
    /// <param name="mat"></param>
    /// <param name="cancellationToken"></param>
    async Task Notifier_SendWithMat(string message, CancellationToken cancellationToken)
    {
        var mat = new Mat();
        lock (_mat) mat = _mat.Clone();

        var tmpPath = Path.GetTempFileName() + ".png";
        if (mat.Resize(new Size(), 0.5, 0.5).SaveImage(tmpPath))
        {
            using (var stream = File.OpenRead(tmpPath))
                await Notifier.SendAsync(_config.Token, message, stream, cancellationToken);
            File.Delete(tmpPath);
        }
        else
            await Notifier.SendAsync(_config.Token, message + "\n(画像の保存に失敗しました。)", cancellationToken);
    }

    public async Task StartAsync() { await StartAsync(CancellationToken.None); }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="OperationCanceledException"/>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mat = new Mat();
        UInt32 currentSeed;
        (UInt32 Seed, TimeSpan WaitTime) target;

        // 初回に振動設定を「あり」に変更しておく
        await _serialPort.RunAsync
        (
            _sequences[Sequences.Reset]
                .Concat(_sequences[Sequences.MoveOptions])
                .Concat(_sequences[Sequences.EnableVibration]).ToArray(),
            cancellationToken
        );

        var previousWaitTime = new TimeSpan();
        var currentWaitTime = new TimeSpan();
        do
        {
            do
            {
                currentSeed = await GetCurrentSeedAfterReset(cancellationToken);
                target = SetTarget(currentSeed);
                Console.WriteLine(string.Format("Current seed : {0:X}", currentSeed));

            } while (target.WaitTime > _config.WaitTime[WaitTime.Maximum]);
            Console.WriteLine("");
            Console.WriteLine(string.Format("Suitable for wait : {0:X} -> {1:X}", currentSeed, target.Seed));
            previousWaitTime = target.WaitTime;

            // 算出された待機時間が、いますぐバトル生成で微調整するために残す消費時間より長い場合
            // 高速消費
            if (target.WaitTime > _config.WaitTime[WaitTime.Left])
            {
                var waitTime = target.WaitTime - _config.WaitTime[WaitTime.Left];
                Console.WriteLine(string.Format("ETA : {0}", DateTime.Now + waitTime));
                await Notifier_SendWithMat(string.Format("条件を満たすseedです。高速消費に移行します。\n{0:X} -> {1:X}\nETA : {2}", currentSeed, target.Seed, DateTime.Now + waitTime), cancellationToken);

                try
                {
                    await AdvanceByMoltres(waitTime, cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    Console.WriteLine("Could not find Moltres. Reset...");
                    Console.WriteLine("");
                    await Notifier.SendAsync(_config.Token, "いますぐバトル情報を取得できませんでした。リセットします...", cancellationToken);
                    continue;
                }
                Console.WriteLine("Faster advance has been completed.");
                Console.WriteLine("");

                UInt32? tmp;
                if ((tmp = await GetCurrentSeed(cancellationToken)) == null)
                {
                    Console.WriteLine("Could not find current seed. Reset...");
                    Console.WriteLine("");
                    await Notifier.SendAsync(_config.Token, "高速消費後、現在のseedを再特定できませんでした。リセットします...", cancellationToken);
                    continue;
                }
                currentSeed = (uint)tmp;
                Console.WriteLine(string.Format("Current seed : {0:X}", currentSeed));
                await Notifier_SendWithMat(string.Format("高速消費が完了しました。\n{0:X}", currentSeed), cancellationToken);
            }

            // 高速消費を必要としなかった場合は currentWaitTime と previousWaitTime は同じ値なのでwhileを抜ける
            // 高速消費を使った場合は、currentWaitTime は previousWaitTime より短くなっていないと失敗している(seedを通り越している)
            currentWaitTime = TimeSpan.FromSeconds(target.Seed.GetIndex(currentSeed) / _config.AdvancesPerSecond);
            if (currentWaitTime > previousWaitTime)
            {
                Console.WriteLine("Passed target seed. Reset...");
                Console.WriteLine("");
                await Notifier.SendAsync(_config.Token, "高速消費後の待機時間が、消費前の待機時間を上回りました。待機時間が長過ぎて目標のseedを通り越したと考えられます。リセットします...", cancellationToken);
            }
            else break;
        } while (true);

        try
        {
            await AdjustSeed(currentSeed, target.Seed, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // - 目標のseedまで到達する手段がない場合(近過ぎ 40で割り切れない)
            // - いますぐバトルの再生成中にseedを見失った場合 など
            // 設定変更からやり直し
            Console.WriteLine("Could not manipulate current seed properly. Reset...");
            Console.WriteLine("");
            await Notifier.SendAsync(_config.Token, "現在のseedから目標seedまで到達する手段がないか、現在のseedを見失いました。リセットします...", cancellationToken);
            await StartAsync(cancellationToken);
            return;
        }

        await _serialPort.RunAsync(_sequences[Sequences.Finalize], cancellationToken);
        Console.WriteLine(string.Format("Successfully reached to target seed : {0:X}", target.Seed));
        await Notifier_SendWithMat(string.Format("seed厳選が完了しました。\n{0:X}", target.Seed), cancellationToken);
    }

    /// <summary>
    /// <see cref="Config"/> で与えられた目標seedの中で、現在のseedから最も短い待機時間で到達できるものと、その待機時間を返す。
    /// </summary>
    /// <param name="currentSeed"></param>
    /// <returns></returns>
    private (UInt32 Seed, TimeSpan WaitTime) SetTarget(UInt32 currentSeed)
    {
        // targetSeedsそれぞれとcurrentSeed間の待機時間を算出する
        var waitTimes = new Dictionary<UInt32, TimeSpan>();
        foreach (var targetSeed in _config.Targets)
            waitTimes.Add(targetSeed, TimeSpan.FromSeconds(targetSeed.GetIndex(currentSeed) / _config.AdvancesPerSecond));

        // 最も待機時間の短いものを返す
        var pair = waitTimes.OrderBy(pair => pair.Value).First();
        return (pair.Key, pair.Value);
    }

    /// <summary>
    /// リセットして、現在のseedを求める。<br/>
    /// いますぐバトル情報を取得できなかった場合、再びリセットを行い、戻り値として必ずseedを返すようにする。<br/>
    /// ---<br/>
    /// 事前条件: ディスクが読み込まれており、<see cref="Config"/> で定義された所定の状態を行うことができる状態<br/>
    /// 事後条件: いますぐバトル「さいきょう」のパーティが表示され、「はい」にカーソルが合っている状態
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<UInt32> GetCurrentSeedAfterReset(CancellationToken cancellationToken)
    {
        UInt32? currentSeed = null;

        do // 現在seedを特定できるまで
        {
            // リセット -> いますぐバトル1回目読み込み
            await _serialPort.RunAsync
            (
                _sequences[Sequences.Reset]
                    .Concat(_sequences[Sequences.MoveQuickBattle])
                    .Concat(_sequences[Sequences.LoadParties]).ToArray(),
                cancellationToken
            );

            // いますぐバトルを数回読み込み直して現在seedを特定
            // seedを特定できなかった場合 -> do-whileの先頭へ戻り、リセットから再度特定へ
        } while ((currentSeed = await GetCurrentSeed(cancellationToken)) == null);

        // if (currentSeed == null) throw new Exception("This can't happen.");
        return (UInt32)currentSeed;
    }

    /// <summary>
    /// 現在のseedを求める。<br/>
    /// 5回連続でいますぐバトル情報を取得できなかった場合、事前条件が満たされていないものとしてnullを返す。<br/>
    /// ---<br/>
    /// 事前条件: いますぐバトル「さいきょう」のパーティが表示された状態<br/>
    /// 事後条件: いますぐバトル「さいきょう」のパーティが表示され、「はい」にカーソルが合った状態
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<UInt32?> GetCurrentSeed(CancellationToken cancellationToken)
    {
        var mat = new Mat();
        var quickbattles = new QuickBattleParties?[2];

        // いますぐバトル1回目
        await _serialPort.RunAsync(_sequences[Sequences.DiscardParties].Concat(_sequences[Sequences.LoadParties]).ToArray(), cancellationToken);
        try
        {
            lock (_mat) mat = _mat.Clone();
            quickbattles[0] = mat.GetQuickBattleParties();
        }
        catch (OperationCanceledException) { throw; }
        catch { quickbattles[0] = null; }

        List<uint> candidates;
        var count = 0;
        do
        {
            // いますぐバトル2回目以降
            await _serialPort.RunAsync(_sequences[Sequences.DiscardParties].Concat(_sequences[Sequences.LoadParties]).ToArray(), cancellationToken);
            try
            {
                lock (_mat) mat = _mat.Clone();
                quickbattles[1] = mat.GetQuickBattleParties();
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                quickbattles[1] = null;
                if (++count == 5) return null;
            }

            if (quickbattles[0] != null && quickbattles[1] != null)
            {
                // nullにはならないって言っているのに...
#pragma warning disable CS8629
                candidates = _xddb.SearchSeed(new QuickBattleParties[] { (QuickBattleParties)quickbattles[0], (QuickBattleParties)quickbattles[1] }, _config.Tsv);
#pragma warning restore
            }
            else candidates = new();

            quickbattles[0] = quickbattles[1];
        } while (candidates.Count != 1);

        mat.Dispose();
        return candidates[0];
    }

    /// <summary>
    /// いますぐバトル戦闘画面にファイヤーを出し、高速消費する。<br/>
    /// ---<br/>
    /// 事前条件: いますぐバトル「さいきょう」のパーティが表示された状態<br/>
    /// 事後条件: ファイヤーとの戦闘を離脱し、いますぐバトル「さいきょう」のパーティが表示された状態
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task AdvanceByMoltres(TimeSpan waitTime, CancellationToken cancellationToken)
    {
        var mat = new Mat();
        lock (_mat) mat = _mat.Clone();

        // ファイヤーが出るまで再生成
        var count = 0;
        while (true)
        {
            try
            {
                while (mat.GetQuickBattleParties().COM.Index != 2)
                {
                    await _serialPort.RunAsync(_sequences[Sequences.DiscardParties].Concat(_sequences[Sequences.LoadParties]).ToArray(), cancellationToken);
                    lock (_mat) mat = _mat.Clone();
                }
                break;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // ここでQuickBattleParties取得できないなら、次段の消費後現在seed特定も失敗するはず
                // 高速消費を諦めてリセットさせる
                Console.Error.WriteLine("[Sorter] [Warning] Mat.GetQuickBattleParties() failed.");
                if (++count == 10) throw;
            }
        }
        mat.Dispose();

        // 戦闘入って待機して出る
        await _serialPort.RunAsync(_sequences[Sequences.EntryToBattle], cancellationToken);
        await Task.Delay(waitTime, cancellationToken);
        await _serialPort.RunAsync
        (
            _sequences[Sequences.ExitBattle]
                .Concat(_sequences[Sequences.LoadParties]).ToArray(),
            cancellationToken
        );
    }

    /// <summary>
    /// いますぐバトル生成と設定変更で、目標seedちょうどまで消費する。<br/>
    /// 目標の生成回数直前に色回避があると、絶対に目標seedに到達できないことがあり得るかも...？<br/>
    /// ---<br/>
    /// 事前条件: いますぐバトル「さいきょう」のパーティが表示された状態<br/>
    /// 事後条件: 設定変更を終え、「せってい」にカーソルが合った状態
    /// </summary>
    /// <param name="currentSeed"></param>
    /// <param name="targetSeed"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception"/>
    /// <returns></returns>
    private async Task AdjustSeed(UInt32 currentSeed, UInt32 targetSeed, CancellationToken cancellationToken)
    {
        var mat = new Mat();

        // seed微調整のための行動を計算
        var advances = ConsumptionNavigator.Calculate(currentSeed, targetSeed, _config.AllowLoad, _config.AdvancesByLoading, _config.AdvancesByOpeningItems, _config.Tsv);
        if (!_config.AllowLoad)
        {
            Console.WriteLine(string.Format
            (
                "Generate Quick Battle parties : {0}\nChange vibration option : {1}",
                advances.GenerateParties,
                advances.ChangeSetting
            ));
            await Notifier.SendAsync(_config.Token, string.Format
            (
                "目標seedまでの端数を消費します。\nいますぐバトル生成 : {0}回\n振動設定変更 : {1}回",
                advances.GenerateParties,
                advances.ChangeSetting
            ), cancellationToken);
        }
        else
        {
            Console.WriteLine(string.Format
            (
                "Generate Quick Battle parties : {0}\nChange vibration option : {1}\nSave : {2}\nOpen items : {3}\nWatch steps : {4}",
                advances.GenerateParties,
                advances.ChangeSetting,
                advances.Save,
                advances.OpenItems,
                advances.WatchSteps
            ));
            await Notifier.SendAsync(_config.Token, string.Format
            (
                "目標seedまでの端数を消費します。\nいますぐバトル生成 : {0}回\n振動設定変更 : {1}回\n「レポート」 : {2}回\n「もちもの」 : {3}回\n腰振り観察 : {4}回",
                advances.GenerateParties,
                advances.ChangeSetting,
                advances.Save,
                advances.OpenItems,
                advances.WatchSteps
            ), cancellationToken);
        }

        // いますぐバトル生成
        var count = 0;
        for (var i = 0; i < advances.GenerateParties; i++)
        {
            await _serialPort.RunAsync(_sequences[Sequences.DiscardParties].Concat(_sequences[Sequences.LoadParties]).ToArray(), cancellationToken);

            lock (_mat) mat = _mat.Clone();
            var parties = advances.Parties[i];
            try
            {
                if (new QuickBattleParties((int)parties.pIndex, (int)parties.eIndex, parties.HP) != mat.GetQuickBattleParties())
                {
                    Console.WriteLine("Found a discrepancy between the prediction and actually generated. Attempt to re-find current seed.");
                    Console.WriteLine("");
                    await Notifier_SendWithMat("生成予測と実際に生成された手持ちに齟齬が見られます。現在seedの再特定を試みます。", cancellationToken);

                    // 予測と実際に生成された手持ちに齟齬があった場合
                    UInt32? tmp;
                    if ((tmp = await GetCurrentSeed(cancellationToken)) == null)
                    {
                        // そもそも違う画面になってしまっている場合
                        // -> StartAsyncのtry-catchに拾ってもらってリセット
                        throw new Exception();
                    }
                    // 現在のseedは特定できた場合
                    // -> 求めたseedと目標seedで微調整を仕切り直す
                    currentSeed = (UInt32)tmp;
                    Console.WriteLine(string.Format("Current seed : {0:X}", currentSeed));
                    await Notifier_SendWithMat(string.Format("現在のseedを再特定しました。\n{0:X}", currentSeed), cancellationToken);

                    await AdjustSeed(currentSeed, targetSeed, cancellationToken);
                    return;
                }
                count = 0;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                Console.Error.WriteLine("[Sorter] [Warning] Mat.GetQuickBattleParties() failed.");
                if (++count == 10) throw; // 10回連続で失敗すると大域脱出
            }
        }
        // 設定まで移動
        await _serialPort.RunAsync
        (
            _sequences[Sequences.DiscardParties]
                .Concat(_sequences[Sequences.MoveMenu])
                .Concat(_sequences[Sequences.MoveOptions]).ToArray(),
            cancellationToken
        );
        // 設定変更
        // 最初に振動をonにしているので、奇数回(iが偶数)はdisable、偶数回はenableで固定
        for (var i = 0; i < advances.ChangeSetting; i++)
            await _serialPort.RunAsync(_sequences[i % 2 == 0 ? Sequences.DisableVibration : Sequences.EnableVibration], cancellationToken);

        if (!_config.AllowLoad) return;

        // レポートまで移動
        await _serialPort.RunAsync
        (
            _sequences[Sequences.MoveContinue]
                .Concat(_sequences[Sequences.Load])
                .Concat(_sequences[Sequences.MoveSave]).ToArray(),
            cancellationToken
        );
        for (var i = 0; i < advances.Save; i++)
            await _serialPort.RunAsync(_sequences[Sequences.Save], cancellationToken);

        // もちもの
        await _serialPort.RunAsync(_sequences[Sequences.MoveItems], cancellationToken);
        for (var i = 0; i < advances.OpenItems; i++)
            await _serialPort.RunAsync(_sequences[Sequences.OpenCloseItems], cancellationToken);

        // 腰振り観察
        for (var i = 0; i < advances.WatchSteps; i++)
            await _serialPort.RunAsync(_sequences[Sequences.WatchSteps], cancellationToken);
    }

    #region IDisposable implementation
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _task.Wait();
                _mat.Dispose();

                _serialPort.Dispose();
            }

            disposedValue = true;
        }
    }
    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
