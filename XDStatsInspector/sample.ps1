do {
    .\XDSeedSorter.exe
} while (![bool]::Parse($(.\XDStatsInspector.exe 2>$null)))
