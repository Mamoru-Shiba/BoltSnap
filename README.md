# BoltSnap

Chrome 拡張機能。ワンクリックでフルページ・可視領域・範囲選択のスクリーンショットをローカルに保存します。

## 特徴

- デフォルトでフルページキャプチャ（スクロール合成）
- 可視領域キャプチャ／範囲選択キャプチャを切替可能
- ローカル PNG ダウンロード ＋ クリップボードコピー
- 外部通信ゼロ。APIキー・アカウント不要

## 開発

```bash
npm install
npm run dev    # 開発モード（HMR）
npm run build  # dist/ に本番ビルド
npm test       # Vitest
npm run check  # Biome（lint + format チェック）
```

### Chrome への読み込み

1. `npm run build`
2. `chrome://extensions` を開き「デベロッパーモード」ON
3. 「パッケージ化されていない拡張機能を読み込む」→ `dist/` を指定

## プロジェクト構成

```
src/
├── background/   Service Worker
├── content/      Content Scripts（範囲選択 / スクロール合成）
├── popup/        Popup UI（React + MUI）
├── components/   UI コンポーネント（Feature-based）
├── engines/      純粋ロジック（Tier1 テスト対象）
├── services/     Chrome API ラッパ
├── stores/       Zustand
├── types/        TypeScript 型定義
└── utils/        ユーティリティ
```

## 貢献ガイド

- 本リポジトリは **パブリック公開** を前提としています。コミット前に `git diff --cached` で機密情報が含まれないことを確認してください。
- 詳細なコーディング規約・BDD フロー・Git ブランチ運用はプロジェクト開発者向け内部ドキュメントを参照してください。

## License

MIT
