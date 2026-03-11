# 仮想タッチパッドアプリ タスク一覧

本ドキュメントは `plan.md` に基づき、各フェーズ・ステップのタスクを管理する。

---

## Phase 1 — 最小動作確認

### Step 1.1: プロジェクトスキャフォールド

- [ ] .NET 8 + WPF のプロジェクト作成（`VirtualTouchpad.sln`, `VirtualTouchpad.csproj`）
- [ ] `app.manifest` で Per-Monitor DPI Aware を宣言
- [ ] ディレクトリ構成（Views / Models / Services / Interop / Helpers）の作成
- [ ] テストプロジェクト（xUnit）の作成

### Step 1.2: メインウィンドウの基本UI

- [ ] `MainWindow.xaml` で仕様通りのレイアウトを構築（タイトルバー、タッチパッド領域、クリックボタン領域）
- [ ] `WindowStyle="None"` でカスタムクロームを実現
- [ ] `Topmost="True"` で常時最前面を設定
- [ ] タイトルバー領域のドラッグ移動（`DragMove()`）を実装
- [ ] 初期サイズ M（300×220）を設定

### Step 1.3: Win32 P/Invoke 基盤

- [ ] `Interop/NativeMethods.cs` に必要なAPI宣言（`SendInput`, `GetCursorPos`, `SetCursorPos`, `RegisterTouchWindow`）
- [ ] `Interop/NativeStructs.cs` に構造体定義（`INPUT`, `MOUSEINPUT`, `TOUCHINPUT`, `POINTER_INFO` 等）
- [ ] `Interop/NativeConstants.cs` に定数定義（`MOUSEEVENTF_*`, `WM_POINTER*` 等）

### Step 1.4: InputSimulator（マウスイベント発行）

- [ ] `Services/InputSimulator.cs` を実装
  - `MoveCursorRelative(int dx, int dy)` — 相対移動
  - `LeftClick()` — 左クリック（down + up）
  - `RightClick()` — 右クリック（down + up）
  - `DoubleClick()` — ダブルクリック
  - `LeftDown()` / `LeftUp()` — ドラッグ用インターフェースのみ

### Step 1.5: タッチ入力処理と変換抑制

- [ ] `Services/TouchInputHandler.cs` を実装
- [ ] `WndProc` オーバーライドで `WM_POINTER*` メッセージをハンドル
- [ ] `pointerType == PT_TOUCH` のみ処理（ペン入力は無視）
- [ ] OS側のタッチ→マウス変換を抑制
- [ ] パッド領域内のタッチのみ横取りし、タイトルバー・ボタン領域は通常処理

### Step 1.6: ジェスチャーステートマシン（基本版）

- [ ] `Services/GestureStateMachine.cs` を実装（`IDLE` → `TRACKING` → `CURSOR_MOVE` / `TAP`）
- [ ] 1本指カーソル移動: タッチデルタを `InputSimulator.MoveCursorRelative()` に渡す
- [ ] 1本指タップ判定（移動量 < 10px かつ接触時間 < 200ms → 左クリック）
- [ ] 1本指ダブルタップ判定（2回のタップ間隔 < 300ms → ダブルクリック）
- [ ] 加速度カーブの基本実装（`Helpers/AccelerationCurve.cs`）

### Step 1.7: 左右クリックボタン

- [ ] UI下部の左クリック / 右クリックボタンに `Click` イベントを接続
- [ ] 各ボタン押下時に `InputSimulator.LeftClick()` / `RightClick()` を呼び出し

### Step 1.8: Phase 1 統合テスト・動作確認

- [ ] `GestureStateMachineTests.cs` — ステート遷移のユニットテスト
- [ ] `AccelerationCurveTests.cs` — 加速度カーブの計算テスト
- [ ] 実機（タッチパネル搭載PC）での手動テスト

---

## Phase 2 — 日常利用可能レベル

### Step 2.1: 2本指ジェスチャー

- [ ] `TouchInputHandler` を拡張し、マルチタッチ（複数ポインターID）を追跡
- [ ] `GestureStateMachine` に2本指判定を追加（タップ→右クリック、ドラッグ→スクロール）
- [ ] スクロール方向の判定（垂直/水平を移動方向の主成分で決定）

### Step 2.2: ドラッグ＆ドロップ（ステートマシン拡張）

- [ ] ステートマシンに `HOLD_READY` / `DRAGGING` / `DROP` ステートを追加
- [ ] ホールド判定: 接触後500ms移動なし → `HOLD_READY`
- [ ] `HOLD_READY` → 移動開始 → `DRAGGING`（`LeftDown()` 発行）
- [ ] `DRAGGING` 中の移動デルタでカーソル移動、指を離す → `DROP`（`LeftUp()` 発行）
- [ ] `InputSimulator` に `LeftDown()` / `LeftUp()` の実装追加

### Step 2.3: サイズ切替（S / M / L）

- [ ] `Models/TouchpadSize.cs` にサイズ定義を実装（S: 200×150, M: 300×220, L: 400×300）
- [ ] タイトルバーに [S] [M] [L] ボタンを追加
- [ ] ボタン押下でウィンドウサイズを切替、現在選択中をハイライト表示

### Step 2.4: タスクトレイ常駐

- [ ] `Services/TrayIconService.cs` を実装
- [ ] トレイアイコンの右クリックメニュー（表示/非表示、終了）
- [ ] `ShowInTaskbar = false` でタスクバー非表示
- [ ] 最小化ボタン（`─`）でウィンドウを非表示にしトレイのみ残す
- [ ] トレイアイコンダブルクリックで表示復帰

### Step 2.5: Phase 2 統合テスト

- [ ] 2本指ジェスチャーのステートマシンテスト追加
- [ ] ドラッグ＆ドロップのステート遷移テスト追加
- [ ] 実機テスト（2本指タップ、スクロール、ドラッグ＆ドロップ）

---

## Phase 3 — 実用品質

### Step 3.1: 設定データモデルと永続化

- [ ] `Models/AppSettings.cs` に全設定項目をプロパティとして定義
- [ ] `Services/SettingsService.cs` を実装（JSON形式で `%AppData%` に保存・読込）

### Step 3.2: 半透明対応

- [ ] パッド領域の `Opacity` を設定値にバインディング
- [ ] デフォルト不透明度60%、`AllowsTransparency="True"` 設定
- [ ] パッド領域のみ半透明にし、ボタン・タイトルバーは操作性を保つ

### Step 3.3: 設定画面

- [ ] `Views/SettingsWindow.xaml` を作成
- [ ] カーソル動作セクション（ポインタ速度、加速度カーブ、タップ感度、スクロール設定）
- [ ] 外観セクション（不透明度、テーマ選択、パッドサイズ）
- [ ] 動作セクション（自動スタート、起動時表示、ホットキー設定）
- [ ] トレイメニューまたは `≡` メニューから設定画面を開く

### Step 3.4: ウィンドウ位置の記憶・復元

- [ ] ウィンドウ `Left`, `Top` を設定に保存・復元
- [ ] 画面外はみ出し時の補正処理
- [ ] `WM_POWERBROADCAST` でスリープ復帰後に位置を再適用

### Step 3.5: ホットキーによる表示/非表示切替

- [ ] `Services/HotkeyService.cs` を実装（`RegisterHotKey` Win32 API使用）
- [ ] デフォルト: `Ctrl+Alt+T`、設定画面から変更可能

### Step 3.6: DPIスケーリング対応

- [ ] `app.manifest` で `PerMonitorV2` を設定
- [ ] カーソル移動量の計算でDPIスケールを考慮
- [ ] `WM_DPICHANGED` ハンドラーでレイアウト再計算
- [ ] `Helpers/ScreenHelper.cs` にDPI取得・変換メソッドを実装

### Step 3.7: テーマ対応

- [ ] `Resources/Themes/` にダーク/ライトテーマの `ResourceDictionary` を定義
- [ ] 起動時にシステムテーマを検出し適用
- [ ] 設定変更時にテーマを動的に切り替え

### Step 3.8: Phase 3 統合テスト

- [ ] 設定の保存・復元テスト
- [ ] DPIスケーリングテスト
- [ ] テーマ切替の動作確認
- [ ] ホットキーの登録・解除テスト

---

## Phase 4 — 完成度向上

### Step 4.1: 3本指・4本指ジェスチャー

- [ ] `GestureStateMachine` を拡張し3本指以上のタッチ追跡に対応
- [ ] 3本指タップ → 中クリック
- [ ] 3本指スワイプ上 → タスクビュー（`Win+Tab`）
- [ ] 3本指スワイプ下 → デスクトップ表示（`Win+D`）
- [ ] 3本指スワイプ左右 → アプリ切替（`Alt+Tab`）

### Step 4.2: 2本指ピンチズーム

- [ ] 2本指間の距離変化量を計算
- [ ] ピンチイン/アウトで `Ctrl+マウスホイール` を発行

### Step 4.3: ペン入力の区別

- [ ] `WM_POINTER` の `pointerType` でペン入力（`PT_PEN`）を検出
- [ ] デフォルトではペン入力を無視、設定で有効化可能

### Step 4.4: 画面回転対応

- [ ] `SystemEvents.DisplaySettingsChanged` イベント監視
- [ ] 回転後のウィンドウ位置補正

### Step 4.5: タッチキーボードとの共存

- [ ] タッチキーボードの表示状態監視（`IFrameworkInputPane`）
- [ ] 重なる場合のパッドウィンドウ自動退避

### Step 4.6: マルチモニター対応

- [ ] `SendInput` のカーソル座標計算を仮想デスクトップ座標系に対応
- [ ] ウィンドウ位置記憶にモニター情報を含める
- [ ] モニター取り外し時の位置補正

### Step 4.7: スタートアップ登録

- [ ] レジストリ `HKCU\...\Run` への登録/解除
- [ ] 設定画面のチェックボックスと連動

### Step 4.8: インストーラー作成

- [ ] MSIX または WiX を使用したインストーラーの作成
- [ ] デスクトップショートカット、スタートメニュー登録
- [ ] アンインストール処理（設定ファイル削除オプション）

### Step 4.9: Phase 4 統合テスト

- [ ] 3本指ジェスチャーの動作テスト
- [ ] ピンチズームの精度テスト
- [ ] マルチモニター環境でのテスト
- [ ] インストーラーの動作テスト
