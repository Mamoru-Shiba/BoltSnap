# 01. ユーザーフロー v1

BoltSnap の典型操作フロー。

```mermaid
flowchart TD
    Start([ユーザーが Web ページを閲覧]) --> Click[BoltSnap アイコンをクリック]
    Click --> Popup[Popup 表示<br/>デフォルト=フルページ]

    Popup --> ModeCheck{モード選択}
    ModeCheck -->|そのまま| FullPage[フルページ<br/>デフォルト]
    ModeCheck -->|切替| Visible[可視領域]
    ModeCheck -->|切替| Region[範囲選択]

    FullPage --> CaptureBtn[「キャプチャ」押下]
    Visible --> CaptureBtn
    Region --> CaptureBtn

    CaptureBtn --> RegionBranch{範囲選択？}
    RegionBranch -->|Yes| Overlay[画面上にオーバーレイ<br/>ドラッグで矩形指定]
    RegionBranch -->|No| Exec[キャプチャ実行]

    Overlay --> Exec
    Exec --> ClipCheck{クリップボード<br/>ON？}
    ClipCheck -->|Yes| Clip[OS クリップボードへ<br/>画像コピー]
    ClipCheck -->|No| Save
    Clip --> Save[PNG をローカル保存]

    Save --> Success([完了通知])
    Exec -->|失敗| Err[エラーメッセージ表示]

    classDef screen fill:#4A90D9,stroke:#2C5F8A,color:#fff
    classDef action fill:#F5A623,stroke:#D4891A,color:#fff
    classDef complete fill:#7ED321,stroke:#5BA018,color:#fff
    classDef warning fill:#D0021B,stroke:#A00116,color:#fff

    class Popup,Overlay screen
    class Click,CaptureBtn,Exec,FullPage,Visible,Region,ModeCheck,RegionBranch,ClipCheck,Clip,Save action
    class Success complete
    class Err warning
```

## 凡例

| 色 | 用途 |
|----|------|
| 青 | 画面（Popup / オーバーレイ） |
| オレンジ | 操作・分岐 |
| 緑 | 完了状態 |
| 赤 | 警告・エラー |

## 対応 BDD シナリオ

| フロー | シナリオ |
|--------|----------|
| デフォルト → キャプチャ | デフォルトはフルページキャプチャ |
| フルページ → 保存 | フルページキャプチャで PNG がローカル保存される |
| 可視領域 → 保存 | 可視領域キャプチャ |
| 範囲選択 → オーバーレイ → 保存 | 範囲選択キャプチャ |
| クリップボード ON → コピー＋保存 | クリップボードへ同時コピー |
| 制限付きページ → 失敗 | キャプチャ失敗時にユーザーへ通知する |
