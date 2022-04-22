using System.Text.Json;

using LINENotify;
using OpenCvSharp;
using PokemonXDImageLibrary;

ConsoleApp.Run(args, ([Option(0, "JSON-style Pokemon's stats. HP, Attack, Defense, Speed, SpAtk, SpDef")] Stats stats) =>
{
    // config.jsonを読み込む
    Config? json = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "config.json")));
    var config = json != null ? json : throw new FileNotFoundException();

    // matを更新する
    var mat = new Mat();
    var cancellationTokenSource = new CancellationTokenSource();
    var cancellationToken = cancellationTokenSource.Token;
    Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
    {
        cancellationTokenSource.Cancel();
        e.Cancel = true;
    };
    var ready = false;
    var task = Task.Run(() =>
    {
        using (var videoCapture = new VideoCapture(config.CaptureIndex) { FrameWidth = 1600, FrameHeight = 1200 })
            while (!cancellationToken.IsCancellationRequested)
                lock (mat)
                    if (videoCapture.Read(mat) && !ready) ready = true;
    }, cancellationToken);
    while (!ready) ;

    // matから画像を取得してVideoCaptureを破棄する
    var path = Path.GetTempFileName() + ".png";
    lock (mat)
        if (!mat.Resize(new Size(), 0.5, 0.5).SaveImage(path))
        {
            Console.Error.WriteLine("[Failed] Cannot save the picture.");
            Notifier.Send(config.Token, "[失敗] 画像を保存できませんでした。");
            Environment.ExitCode = 1;
            return;
        }
    cancellationTokenSource.Cancel();
    task.Wait();

    using (var stream = File.OpenRead(path))
    {
        try
        {
            var result = new Mat(path).GetStats();
            if
            (!(
                result.HP == stats.HP &&
                result.Attack == stats.Attack &&
                result.Defense == stats.Defense &&
                result.Speed == stats.Speed &&
                result.SpAtk == stats.SpAtk &&
                result.SpDef == stats.SpDef
            ))
            {
                Console.Error.WriteLine("[Failed] Result does not match.");
                Notifier.Send(config.Token, "[失敗] 引数と一致しませんでした。", stream);
                Environment.ExitCode = 1;
                return;
            }
        }
        catch
        {
            Console.Error.WriteLine("[Failed] Cannot get stats.");
            Notifier.Send(config.Token, "[失敗] パラメータを取得できませんでした。", stream);
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine("[Success] Result matches.");
        Notifier.Send(config.Token, "[成功] 引数と一致しました。", stream);
        Environment.ExitCode = 0;
    }
});

public class Stats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int SpAtk { get; set; }
    public int SpDef { get; set; }
}
