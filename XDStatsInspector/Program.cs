using System.Text.Json;

using LINENotify;
using OpenCvSharp;
using PokemonXDImageLibrary;

Config config;
Stats[] stats;

try
{
    // config.jsonを読み込む
    var jsonConfig = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "config.json")));
    config = jsonConfig != null ? jsonConfig : throw new FileNotFoundException();

    // stats.json
    var jsonStats = JsonSerializer.Deserialize<Stats[]>(File.ReadAllText(Path.Join(AppContext.BaseDirectory, "stats.json")));
    stats = jsonStats != null ? jsonStats : throw new FileNotFoundException();
}
catch
{
    Console.WriteLine("false");
    return;
}

// matを更新する
var mat = new Mat();
var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;
var ready = false;
var task = Task.Run(() =>
{
    using (var videoCapture = new VideoCapture(config.CaptureIndex) { FrameWidth = 1600, FrameHeight = 1200 })
        while (!cancellationToken.IsCancellationRequested)
            lock (mat)
                if (videoCapture.Read(mat) && !ready) ready = true;
}, cancellationToken);
while (!ready) Thread.Sleep(1);

// matから画像を取得してVideoCaptureを破棄する
var path = Path.GetTempFileName() + ".png";
lock (mat)
    if (!mat.Resize(new Size(), 0.5, 0.5).SaveImage(path))
    {
        Console.WriteLine("false");
        Notifier.Send(config.Token, "[失敗] 画像を保存できませんでした。");
        return;
    }
cancellationTokenSource.Cancel();
task.Wait();

using (var stream = File.OpenRead(path))
{
    try
    {
        var result = new Mat(path).GetStats();
        var match = false;
        foreach (var stat in stats)
        {
            if
            (
                result.HP == stat.HP &&
                result.Attack == stat.Attack &&
                result.Defense == stat.Defense &&
                result.Speed == stat.Speed &&
                result.SpAtk == stat.SpAtk &&
                result.SpDef == stat.SpDef
            )
            {
                match = true;
                break;
            }
        }
        if (!match)
        {
            Console.WriteLine("false");
            Notifier.Send(config.Token, "[失敗] 引数と一致しませんでした。", stream);
            return;
        }
    }
    catch
    {
        Console.WriteLine("false");
        Notifier.Send(config.Token, "[失敗] パラメータを取得できませんでした。", stream);
        return;
    }

    Console.WriteLine("true");
    Notifier.Send(config.Token, "[成功] 引数と一致しました。", stream);
}
