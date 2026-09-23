# BoltSnap

Windows の画面最下部に常駐する、細い「時間と草」バー。

- 今日の 0〜24 時のタイムライン（PC を操作していた時間・現在時刻）
- 第何週・何日目・年末までの残り日数
- 1 日 1 マスの年間の草（稼働が長いほど濃く、未来の日は枠のみ）

キーボード・マウスの操作の有無（1 分単位）だけを記録します。入力内容・アプリ名・ウィンドウ名は取得せず、外部通信もしません。

## 仕組み

- 帯は AppBar として画面下端（タスクバーの真上）に領域を確保するため、他のウィンドウと重なりません。
- 最後の入力から 5 分未満の間を「稼働」とみなし、10 秒ごとに確認します。
- 記録は `%LOCALAPPDATA%\BoltSnap\{年}.bin` に保存します（1 日 = 1440 ビット、年 約 64KB）。
- 常駐アプリなので軽さを優先し、WPF などを使わず Win32 + GDI で直接描画しています。

## 使い方

- 草にカーソルを重ねる: 左のテキストが、その日の日付と稼働時間に切り替わります。
- 右クリック（帯またはトレイアイコン）: 「ログイン時に自動起動」の切替と「終了」
- 終了すると確保していた画面領域は元に戻ります。

## 開発

.NET 8 SDK が必要です。

```bash
dotnet build
dotnet test
```

Windows 用の実行ファイルの作成:

```bash
dotnet publish src/BoltSnap -c Release -r win-x64 --self-contained -p:PublishTrimmed=true -o publish
```

Windows 上で Native AOT（`-p:PublishAot=true`）を使うと、さらに小さく・軽くできます（Visual Studio の C++ ビルドツールが必要）。

## 構成

```
src/
├── BoltSnap.Core/   純粋ロジックと保存（OS 非依存）
│   ├── Engines/     カレンダー・稼働判定・草の配置・帯のレイアウト
│   └── Services/    稼働の記録と保存
└── BoltSnap/        Win32 の UI（AppBar・GDI 描画・トレイ）
tests/
└── BoltSnap.Tests/  xUnit（Core を対象）
```

## メモリの目安（実測）

自己完結ビルドで、プライベートメモリ 約 5MB / ワーキングセット 約 20MB（共有ページ込み）。

## License

MIT
