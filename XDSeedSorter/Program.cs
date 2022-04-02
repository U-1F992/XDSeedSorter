using XDSeedSorter;

Console.WriteLine(@"
                                            /^^        
 ___       _      __                /^^   /^^/^^^^^    
| . \ ___ | |__ _/_/._ _ _  ___ ._ _ /^^ /^^ /^^   /^^ 
|  _// . \| / // ._>| ' ' |/ . \| ' |  /^^   /^^    /^^
|_|  \___/|_\_\\___.|_|_|_|\___/|_|_|/^^ /^^ /^^    /^^
        Gale    of    Darkness      /^^   /^^/^^   /^^ 
  -----===========================/^^========/^^^^^    
");

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;
Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) => 
{
    cancellationTokenSource.Cancel();
    e.Cancel = true;
};

try
{
    using (var sorter = new Sorter())
    {
        try
        {
            await sorter.StartAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Gracefully canceled.");
        }
    }
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
}
