# 仮想タッチパッドアプリ 実装計画

## 概要

本ドキュメントは `spec.md` に基づき、C# + WPF による仮想タッチパッドアプリケーションの実装計画を定義する。実装は4フェーズに分割し、各フェーズで動作確認可能な成果物を生成する。

---

## プロジェクト構成

```
VirtualTouchpad/
├── VirtualTouchpad.sln                    # ソリューションファイル
├── src/
│   └── VirtualTouchpad/
│       ├── VirtualTouchpad.csproj          # WPFアプリケーションプロジェクト
│       ├── App.xaml / App.xaml.cs           # アプリケーションエントリポイント
│       ├── app.manifest                    # DPI Aware / UAC設定
│       │
│       ├── Views/                          # UI (XAML + コードビハインド)
│       │   ├── MainWindow.xaml / .cs        # メインウィンドウ（タッチパッド）
│       │   └── SettingsWindow.xaml / .cs    # 設定画面（Phase 3）
│       │
│       ├── ViewModels/                     # MVVM ViewModel層
│       │   ├── MainViewModel.cs
│       │   └── SettingsViewModel.cs
│       │
│       ├── Models/                         # データモデル・設定
│       │   ├── AppSettings.cs              # 設定データクラス
│       │   └── TouchpadSize.cs             # サイズ定義（S/M/L）
│       │
│       ├── Services/                       # ビジネスロジック・OS連携
│       │   ├── InputSimulator.cs           # SendInput APIラッパー
│       │   ├── TouchInputHandler.cs        # タッチ入力処理・ジェスチャー認識
│       │   ├── GestureStateMachine.cs      # ジェスチャーのステートマシン
│       │   ├── SettingsService.cs          # 設定の永続化
│       │   ├── TrayIconService.cs          # タスクトレイ管理（Phase 2）
│       │   └── HotkeyService.cs            # ホットキー管理（Phase 3）
│       │
│       ├── Interop/                        # Win32 P/Invoke定義
│       │   ├── NativeMethods.cs            # Win32 API宣言
│       │   ├── NativeStructs.cs            # Win32構造体定義
│       │   └── NativeConstants.cs          # Win32定数定義
│       │
│       ├── Helpers/                        # ユーティリティ
│       │   ├── AccelerationCurve.cs        # カーソル加速度カーブ計算
│       │   └── ScreenHelper.cs             # 画面座標・DPI関連ヘルパー
│       │
│       └── Resources/                      # リソースファイル
│           ├── Themes/                     # テーマ定義（Phase 3）
│           └── Icons/                      # アプリアイコン・トレイアイコン
│
└── tests/
    └── VirtualTouchpad.Tests/
        ├── VirtualTouchpad.Tests.csproj
        ├── GestureStateMachineTests.cs
        └── AccelerationCurveTests.cs
```

---

## Phase 1 — 最小動作確認

**目標**: 1本指でカーソルを動かし、タップでクリックできるミニマル版を完成させる。

### Step 1.1: プロジェクトスキャフォールド

- [ ] .NET 8 + WPF のプロジェクト作成（`VirtualTouchpad.sln`, `VirtualTouchpad.csproj`）
- [ ] `app.manifest` で Per-Monitor DPI Aware を宣言
- [ ] ディレクトリ構成（Views / Models / Services / Interop / Helpers）の作成
- [ ] テストプロジェクト（xUnit）の作成

**成果物**: ビルド・起動可能な空のWPFアプリケーション

### Step 1.2: メインウィンドウの基本UI

- [ ] `MainWindow.xaml` で仕様通りのレイアウトを構築
  - タイトルバー領域（ドラッグハンドル `≡`、閉じるボタン `×`）
  - タッチパッド領域（`Canvas` または `Border`）
  - 左クリック / 右クリックボタン領域
- [ ] `WindowStyle="None"` でカスタムクロームを実現
- [ ] `Topmost="True"` で常時最前面を設定
- [ ] タイトルバー領域のドラッグ移動（`DragMove()`）を実装
- [ ] 初期サイズ M（300×220）を設定

**成果物**: 常時最前面のフローティングウィンドウが表示される

### Step 1.3: Win32 P/Invoke 基盤

- [ ] `Interop/NativeMethods.cs` に必要なAPI宣言
  - `SendInput` — マウスイベント発行
  - `GetCursorPos` / `SetCursorPos` — カーソル位置取得・設定
  - `RegisterTouchWindow` — タッチ入力登録
- [ ] `Interop/NativeStructs.cs` に構造体定義
  - `INPUT`, `MOUSEINPUT`, `TOUCHINPUT`, `POINTER_INFO` 等
- [ ] `Interop/NativeConstants.cs` に定数定義
  - `MOUSEEVENTF_MOVE`, `MOUSEEVENTF_LEFTDOWN`, `MOUSEEVENTF_LEFTUP` 等
  - `WM_POINTERDOWN`, `WM_POINTERUP`, `WM_POINTERUPDATE` 等

**成果物**: P/Invoke定義が完成し、他のサービスから利用可能

### Step 1.4: InputSimulator（マウスイベント発行）

- [ ] `Services/InputSimulator.cs` を実装
  - `MoveCursorRelative(int dx, int dy)` — 相対移動
  - `LeftClick()` — 左クリック（down + up）
  - `RightClick()` — 右クリック（down + up）
  - `DoubleClick()` — ダブルクリック
  - `LeftDown()` / `LeftUp()` — ドラッグ用（Phase 2向け、インターフェースのみ）

**成果物**: `SendInput`経由でマウスイベントを発行可能

### Step 1.5: タッチ入力処理と変換抑制

- [ ] `Services/TouchInputHandler.cs` を実装
- [ ] `WndProc` をオーバーライドし、`WM_POINTER*` メッセージをハンドル
  - `WM_POINTERDOWN` — タッチ開始
  - `WM_POINTERUPDATE` — タッチ移動
  - `WM_POINTERUP` — タッチ終了
- [ ] `pointerType == PT_TOUCH` のみ処理（ペン入力は無視）
- [ ] `DefWindowProc` に渡さないことでOS側のタッチ→マウス変換を抑制
- [ ] パッド領域内のタッチのみを横取りし、タイトルバー・ボタン領域は通常処理

**成果物**: パッド領域のタッチ入力をアプリ側で捕捉可能

### Step 1.6: ジェスチャーステートマシン（基本版）

- [ ] `Services/GestureStateMachine.cs` を実装
- [ ] ステート定義: `IDLE` → `TRACKING` → `CURSOR_MOVE` / `TAP`
- [ ] 1本指カーソル移動: タッチデルタを `InputSimulator.MoveCursorRelative()` に渡す
- [ ] 1本指タップ判定
  - 移動量 < 10px かつ接触時間 < 200ms → 左クリック発行
- [ ] 1本指ダブルタップ判定
  - 2回のタップ間隔 < 300ms → ダブルクリック発行
- [ ] 加速度カーブの基本実装（`Helpers/AccelerationCurve.cs`）
  - 低速時: 1:1マッピング（精密操作）
  - 高速時: 加速倍率を適用

**成果物**: 1本指でカーソル移動、タップで左クリック、ダブルタップでダブルクリック

### Step 1.7: 左右クリックボタン

- [ ] UI下部の左クリック / 右クリックボタンに `Click` イベントを接続
- [ ] 各ボタン押下時に `InputSimulator.LeftClick()` / `RightClick()` を呼び出し

**成果物**: ボタン経由のクリック操作が動作

### Step 1.8: Phase 1 統合テスト・動作確認

- [ ] `GestureStateMachineTests.cs` — ステート遷移のユニットテスト
- [ ] `AccelerationCurveTests.cs` — 加速度カーブの計算テスト
- [ ] 実機（タッチパネル搭載PC）での手動テスト
  - カーソル移動の応答性
  - タップ / ダブルタップの認識精度
  - タッチ→マウス変換抑制の確認

---

## Phase 2 — 日常利用可能レベル

**目標**: 2本指ジェスチャー、ドラッグ＆ドロップ、サイズ切替、タスクトレイ常駐を追加し、日常利用に耐えるレベルにする。

### Step 2.1: 2本指ジェスチャー

- [ ] `TouchInputHandler` を拡張し、マルチタッチ（複数ポインターID）を追跡
- [ ] `GestureStateMachine` に2本指判定を追加
  - 2本指タップ → 右クリック発行
  - 2本指ドラッグ（上下）→ `InputSimulator` で垂直スクロール（`MOUSEEVENTF_WHEEL`）
  - 2本指ドラッグ（左右）→ `InputSimulator` で水平スクロール（`MOUSEEVENTF_HWHEEL`）
- [ ] スクロール方向の判定（垂直/水平を移動方向の主成分で決定）

### Step 2.2: ドラッグ＆ドロップ（ステートマシン拡張）

- [ ] ステートマシンに `HOLD_READY` / `DRAGGING` / `DROP` ステートを追加
- [ ] ホールド判定: 接触後500ms移動なし → `HOLD_READY`
- [ ] `HOLD_READY` から移動開始 → `DRAGGING`（`LeftDown()` 発行）
- [ ] `DRAGGING` 中は移動デルタでカーソル移動
- [ ] 指を離す → `DROP`（`LeftUp()` 発行）→ `IDLE`
- [ ] `InputSimulator` に `LeftDown()` / `LeftUp()` の実装追加

### Step 2.3: サイズ切替（S / M / L）

- [ ] `Models/TouchpadSize.cs` にサイズ定義を実装
  - S: 200×150, M: 300×220, L: 400×300
- [ ] タイトルバーに [S] [M] [L] ボタンを追加
- [ ] ボタン押下でウィンドウサイズを切替
- [ ] 現在選択中のサイズボタンをハイライト表示

### Step 2.4: タスクトレイ常駐

- [ ] `Services/TrayIconService.cs` を実装
- [ ] `System.Windows.Forms.NotifyIcon` を使用してトレイアイコンを表示
- [ ] トレイアイコンの右クリックメニュー
  - 「表示 / 非表示」 — ウィンドウの表示切替
  - 「終了」 — アプリケーション終了
- [ ] `ShowInTaskbar = false` でタスクバー非表示に設定
- [ ] 最小化ボタン（`─`）でウィンドウを非表示にし、トレイのみ残す
- [ ] トレイアイコンダブルクリックで表示復帰

### Step 2.5: Phase 2 統合テスト

- [ ] 2本指ジェスチャーのステートマシンテスト追加
- [ ] ドラッグ＆ドロップのステート遷移テスト追加
- [ ] 実機テスト
  - 2本指タップの右クリック
  - スクロール操作の方向・速度
  - ドラッグ＆ドロップの安定性

---

## Phase 3 — 実用品質

**目標**: 設定画面、半透明、位置記憶、ホットキー、テーマ対応を追加し、製品品質に仕上げる。

### Step 3.1: 設定データモデルと永続化

- [ ] `Models/AppSettings.cs` に全設定項目をプロパティとして定義
  - ポインタ速度（1〜10）、加速度カーブ種別、タップ感度
  - スクロール方向（通常/ナチュラル）、スクロール速度
  - 不透明度（30〜100）、テーマ、パッドサイズ
  - 自動スタート、起動時表示、ホットキー
  - ウィンドウ位置（X, Y）
- [ ] `Services/SettingsService.cs` を実装
  - JSON形式で `%AppData%/VirtualTouchpad/settings.json` に保存
  - 起動時に読み込み、変更時に自動保存

### Step 3.2: 半透明対応

- [ ] パッド領域の `Opacity` をバインディングで設定値に連動
- [ ] デフォルト不透明度: 60%
- [ ] `AllowsTransparency="True"` + `Background="Transparent"` の設定
- [ ] パッド領域のみ半透明にし、ボタン・タイトルバーは操作性を保つ

### Step 3.3: 設定画面

- [ ] `Views/SettingsWindow.xaml` を作成
- [ ] カーソル動作セクション
  - ポインタ速度スライダー
  - 加速度カーブ選択（ドロップダウン）
  - タップ感度設定
  - スクロール方向トグル、スクロール速度スライダー
- [ ] 外観セクション
  - 不透明度スライダー（リアルタイムプレビュー）
  - テーマ選択（ダーク/ライト/システム準拠）
  - パッドサイズ選択 + カスタム数値入力
- [ ] 動作セクション
  - Windows起動時に自動スタート（チェックボックス）
  - 起動時に自動表示（チェックボックス）
  - ホットキー設定
- [ ] トレイメニューまたはタイトルバーの `≡` メニューから設定画面を開く

### Step 3.4: ウィンドウ位置の記憶・復元

- [ ] ウィンドウの `Left`, `Top` を設定に保存
- [ ] 終了時・移動完了時に位置を永続化
- [ ] 起動時に保存位置を復元
- [ ] 画面外にはみ出す場合の補正処理（モニター接続変更への対応）
- [ ] `WM_POWERBROADCAST` でスリープ復帰後に位置を再適用

### Step 3.5: ホットキーによる表示/非表示切替

- [ ] `Services/HotkeyService.cs` を実装
- [ ] `RegisterHotKey` Win32 APIでグローバルホットキーを登録
- [ ] デフォルト: `Ctrl+Alt+T`
- [ ] 設定画面からホットキーを変更可能

### Step 3.6: DPIスケーリング対応

- [ ] `app.manifest` で `<dpiAwareness>PerMonitorV2</dpiAwareness>` を設定
- [ ] カーソル移動量の計算で現在のDPIスケールを考慮
- [ ] `WM_DPICHANGED` ハンドラーでレイアウト再計算
- [ ] `Helpers/ScreenHelper.cs` にDPI取得・変換メソッドを実装

### Step 3.7: テーマ対応

- [ ] `Resources/Themes/` にダーク / ライトテーマの `ResourceDictionary` を定義
- [ ] 起動時にシステムテーマを検出し適用
- [ ] 設定変更時にテーマを動的に切り替え

### Step 3.8: Phase 3 統合テスト

- [ ] 設定の保存・復元テスト
- [ ] DPIスケーリングテスト（異なるスケール設定での動作）
- [ ] テーマ切替の動作確認
- [ ] ホットキーの登録・解除テスト

---

## Phase 4 — 完成度向上

**目標**: 3本指ジェスチャー、ピンチズーム、ペン入力区別、画面回転、マルチモニター、インストーラーを追加し完成度を高める。

### Step 4.1: 3本指・4本指ジェスチャー

- [ ] `GestureStateMachine` を拡張し、3本指以上のタッチ追跡に対応
- [ ] 3本指タップ → 中クリック（設定変更可）
- [ ] 3本指スワイプ上 → タスクビュー（`Win+Tab` キー送信）
- [ ] 3本指スワイプ下 → デスクトップ表示（`Win+D` キー送信）
- [ ] 3本指スワイプ左右 → アプリ切替（`Alt+Tab` キー送信）

### Step 4.2: 2本指ピンチズーム

- [ ] 2本指間の距離変化量を計算
- [ ] ピンチイン/アウトで `Ctrl+マウスホイール` を発行

### Step 4.3: ペン入力の区別

- [ ] `WM_POINTER` の `pointerType` でペン入力（`PT_PEN`）を検出
- [ ] デフォルトではペン入力を無視
- [ ] 設定オプション: ペン入力でもパッド操作を有効にする

### Step 4.4: 画面回転対応

- [ ] `SystemEvents.DisplaySettingsChanged` イベントを監視
- [ ] 回転後にウィンドウ位置が画面外の場合は画面内に補正

### Step 4.5: タッチキーボードとの共存

- [ ] タッチキーボードの表示状態を監視（`IFrameworkInputPane`）
- [ ] 重なる場合にパッドウィンドウを自動退避

### Step 4.6: マルチモニター対応

- [ ] `SendInput` のカーソル座標計算を仮想デスクトップ座標系に対応
- [ ] ウィンドウ位置記憶にモニター情報を含める
- [ ] モニター取り外し時の位置補正

### Step 4.7: スタートアップ登録

- [ ] レジストリ `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` への登録/解除
- [ ] 設定画面のチェックボックスと連動

### Step 4.8: インストーラー作成

- [ ] MSIX または WiX を使用したインストーラーの作成
- [ ] デスクトップショートカット、スタートメニュー登録
- [ ] アンインストール処理（設定ファイルの削除オプション）

### Step 4.9: Phase 4 統合テスト

- [ ] 3本指ジェスチャーの動作テスト
- [ ] ピンチズームの精度テスト
- [ ] マルチモニター環境でのテスト
- [ ] インストーラーの動作テスト

---

## 技術的な注意事項

### パフォーマンス

- タッチ入力のポーリングレートは高い（125Hz〜）ため、`WM_POINTER` ハンドラー内の処理は最小限に抑える
- カーソル移動の `SendInput` 呼び出しは低レイテンシ（< 1ms）で応答する必要がある
- UIスレッドをブロックしないよう、重い処理があれば非同期化する

### テスト戦略

- **ユニットテスト**: `GestureStateMachine`, `AccelerationCurve` 等のロジック部分を中心にテスト
- **手動テスト**: タッチ入力とカーソル移動は実機でのみ検証可能なため、各フェーズ完了時に実機テストを実施
- **テスト容易性**: `InputSimulator` をインターフェース化し、テスト時にモック可能にする

### セキュリティ

- Phase 1〜3: 一般権限で起動。管理者権限ウィンドウへの操作制限はドキュメントに記載
- Phase 4以降: 必要に応じて UIAccess マニフェスト対応を検討

---

## マイルストーン

| フェーズ | 目標 | 主要成果物 |
|---|---|---|
| Phase 1 | 最小動作確認 | 1本指カーソル操作 + タップクリック |
| Phase 2 | 日常利用可能 | 2本指ジェスチャー + ドラッグ＆ドロップ + トレイ常駐 |
| Phase 3 | 実用品質 | 設定画面 + 半透明 + テーマ + ホットキー |
| Phase 4 | 完成度向上 | 拡張ジェスチャー + マルチモニター + インストーラー |
