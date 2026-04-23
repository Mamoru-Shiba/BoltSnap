# assets/

プロジェクトのデザインソース素材を置くディレクトリ。ビルド出力ではないため `dist/` には含まれない。

## icon-source.png

BoltSnap の元アイコン画像（2000×2000 PNG）。`public/icons/` 配下の各サイズアイコンの元ネタ。

## アイコンの再生成

```bash
for size in 16 32 48 128; do
  convert assets/icon-source.png -resize ${size}x${size} -strip public/icons/icon${size}.png
done
```

ImageMagick (`convert`) が必要。alpha チャンネル付きの PNG を作りたい場合は元画像を透過化したうえで再実行すること。
