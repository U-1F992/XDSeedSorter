# XDSeedSorter

「ポケモンXD 闇の旋風ダーク・ルギア」用 初期seed厳選自動化プログラム

## Usage

### XDSeedSorter.exe

ダブルクリックでも起動できますが終了すると消えるので、コマンドラインで実行した方がいいです。

```ps1
.\XDSeedSorter.exe
```

### config.json

`XDSeedSorter.exe` と同じディレクトリに置いてください。

```javascript
{
    /**
     * WHALEを書き込んだArduinoのポート名
     */
    "portName": "COM6",

    /**
     * カメラ番号
     * デフォルトが0だと思います。
     * デバイスを占有するので、録画や配信に乗せたい場合はOBS VirtualCam等を経由してください。
     */
    "captureIndex": 1,

    /**
     * LINE Notifyトークンを記載すると、メッセージ送信を試みます。
     * 他人に共有しないように気を付けてください。
     */
    "token": "",

    /**
     * TSV
     * 既にメモリーカードにセーブデータがあり、分かっている場合は書いたほうがいいです。
     * 不明であればプログラム側で何とかしますので、このままにしてください。
     */
    "tsv": 65536,

    /**
     * 目標seed
     * 10進数、カンマ区切りで目標seedを記載します。
     * このseedちょうどに合わせるので、ロード等の強制消費分は予め差し引いてください。
     */
    "targets": [
        195951310,
        3735943886
    ],

    /**
     * 待機時間の設定
     */
    "waitTime": {
        /**
         * 許容する最大の待機時間です。
         * これより短い時間の待機で目標seedに到達できる場合に、いますぐバトルを利用した高速消費に進みます。
         */
        "maximum": "03:00:00",

        /**
         * 待機時間から差し引く時間です。
         * 目標seedを越えてしまうことを予防するため、残り時間がここで設定した時間より短くなると高速消費を切り上げます。
         * また、この時間未満で目標seedに到達できる場合には高速消費を利用しないという閾値も兼ねています。
         */
        "left": "00:03:00"
    },

    /**
     * 高速消費時の1秒あたりの消費数です。
     * いますぐバトルにファイヤーが1体出ている場合の消費数になっています。触る必要はありません。
     */
    "advancesPerSecond": 3713.6,

    /**
     * 端数の消費に、レポート(63消費)/持ち物(場所による)/主人公の腰振りを観察 を利用できるようにします。
     * NPC等による消費があるフィールドでは使えません。
     */
    "allowLoad": true,

    /**
     * 「つづきをあそぶ」にかかる消費数です。
     * allowLoadがfalseの場合は使用されません。適当な値で結構です。
     */
    "advancesByLoading": 14,

    /**
     * 持ち物を開いた時の消費数です。
     * allowLoadがfalseの場合は使用されません。
     */
    "advancesByOpeningItems": 14,

    /**
     * プログラム内で利用する自動操作は、全てここで定義されています。
     * 通常触る必要はありませんが、自動操作をカリカリにチューニングしたり、へばっているコンソールを気遣って待機時間を長めにしたり(特にリセットやメモリーカードの読み書きは個体差が顕著です)、プログラムの終了時に操作を追加したりできます。
     */
    "sequences": {
        "reset": [], // B+X+Stでソフトリセットし、「つづきをあそぶ」まで
        "moveQuickBattle": [], // 「つづきをあそぶ」-> いますぐバトル「さいきょう」まで
        "loadParties": [], // 「さいきょう」を選択し手持ちを生成
        "discardParties": [], // いますぐバトルのパーティが表示されている画面から、B押して破棄
        "entryToBattle": [], // いますぐバトルのパーティが表示されている画面から、「はい」を押して戦闘が開始し、操作可能になるまで待機
        "exitBattle": [], // 戦闘を降参で離脱
        "moveMenu": [], // いますぐバトル「さいきょう」->「つづきをあそぶ」まで
        "moveOptions": [], // 「つづきをあそぶ」->「せってい」
        "enableVibration": [], //「せってい」-> 振動onにして「せってい」まで
        "disableVibration": [], //「せってい」-> 振動offにして「せってい」まで
        "moveContinue": [], // 「せってい」->「つづきをあそぶ」
        "load": [], //「つづきをあそぶ」-> ロードを待ってメニューを開く
        "moveSave": [], // 「ポケモン」->「レポート」
        "save": [], // レポートを書いて、「レポート」に戻るまで
        "moveItems": [], // 「レポート」->「もちもの」
        "openCloseItems": [], // 持ち物を開いて閉じる
        "watchSteps": [], // メニューを閉じて主人公の腰振りを見て、再度メニューを開く
        "finalize": [] // seedが調整されてプログラムが終了する状態から行う、任意の動作を定義できます。
    }
}
```

## Build dependencies

- [Sunameri](https://github.com/mukai1011/Sunameri)
- [PokemonXDImageLibrary](https://github.com/mukai1011/PokemonXDImageLibrary)
- [LINENotify](https://github.com/mukai1011/LINENotify)
- [PokemonXDRNGLibrary](PokemonXDRNGLibrary/README.md)

## References

- [WHALE]()
- [XDDatabase](https://github.com/yatsuna827/XDDatabase)
