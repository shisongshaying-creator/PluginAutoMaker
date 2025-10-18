# PluginAutoMaker

PluginAutoMaker は自然言語の要件から Paper 1.20.1 向けのプラグインを自動生成し、Gradle Wrapper でビルド・Paper サーバーでスモークテストまで実行する WPF (.NET 8) アプリケーションです。GUI には進捗バー、ログビューア、成果物の確認が備わっており、失敗時には自動修復を最大 3 回まで試みます。

## 必要ソフトウェア

| 種類 | バージョン | 備考 |
| ---- | ---------- | ---- |
| .NET SDK | 8.0 以上 | WPF アプリとテストをビルド/実行するため |
| Java Development Kit | 17 | Gradle ビルドと Paper サーバー実行に必須。`JAVA_HOME` を JDK17 に設定してください |
| PowerShell | 7 以上推奨 | `scripts/run.ps1` などの補助スクリプト実行用 |

## Paper JAR の入手

初回実行時に `paper/paper-1.20.1.jar` が存在しない場合、アプリケーションが公式 API から Paper 1.20.1 (build 400) を自動ダウンロードします。ネットワークの制限で取得できない場合は、設定画面からローカルの Paper JAR パスを指定することで代替できます。

## プロジェクト構成

```
PluginAutoMaker/
├─ PluginAutoMaker.sln
├─ Directory.Build.props
├─ README.md
├─ scripts/
│  └─ run.ps1                  # Core API を直接呼ぶテストコンソール
├─ src/
│  ├─ PluginAutoMaker.Core/    # オーケストレーションと各サービス
│  ├─ PluginAutoMaker.Spec/    # 仕様生成 (ルールベース)
│  ├─ PluginAutoMaker.UI/      # WPF GUI
│  └─ PluginAutoMaker.Tests/   # xUnit テスト
├─ tools/
│  ├─ PluginAutoMaker.ConsoleRunner/  # Core API を呼ぶ CLI
│  └─ .gitkeep
└─ paper/
   └─ .gitkeep
```

## ビルド & 実行

1. 依存を用意します (.NET 8 SDK, JDK 17)。
2. 必要に応じて `scripts/run.ps1` を使ってコア機能を CLI で試すことができます。
   ```powershell
   pwsh scripts/run.ps1 "プラグイン名: HelloGUI /hello コマンドで 'Hello' を返す"
   ```
3. GUI を起動する場合は Visual Studio 2022 などで `PluginAutoMaker.UI` プロジェクトをスタートアップに設定し実行してください。

### テスト

xUnit テストで仕様生成と plugin.yml 出力の最低限の検証を行えます。
```powershell
pwsh -c "dotnet test"
```

## 使い方

1. アプリを起動し、左ペインのテキストボックスに自然言語の要件を入力します。
2. 「生成＆ビルド＆テスト開始」ボタンを押すと、仕様生成→スキャフォールド→ビルド→Paper テスト→成果物パッケージングまで自動で進みます。
3. 成果物は設定で指定した出力ディレクトリ配下 (`<PluginId>-yyyymmddHHMMss` フォルダー) に jar とソース一式 (オプションで Zip) が配置されます。
4. 設定画面では以下を変更できます。
   - 出力ディレクトリ
   - Paper 保存ディレクトリ / 手動指定の Paper JAR パス
   - Gradle Wrapper バージョン
   - テストタイムアウト秒数

## よくある失敗と対処

| 症状 | 対処 |
| ---- | ---- |
| `Unsupported class file major version` | JDK 17 が使用されていません。`JAVA_HOME` を JDK17 に設定し、必要であれば設定画面から Gradle Wrapper のバージョンを再生成してください。 |
| `Could not find or load main class` | `plugin.yml` の `main` エントリと生成されたクラスのパッケージ名が一致しているか確認し、設定画面から Paper JAR パスを正しく指定してください。 |
| `Could not resolve io.papermc.paper` | ネットワーク接続と Maven リポジトリ (`https://repo.papermc.io/repository/maven-public/`) へのアクセスを確認し、プロキシ設定が必要な場合は環境変数を設定してください。 |
| Paper サーバーが起動せずタイムアウト | Java 17 が利用可能か、Paper JAR が破損していないか確認。オフライン環境では Paper JAR を事前にダウンロードし、設定画面でパスを指定してください。 |

## オフライン運用のヒント

- `paper/paper-1.20.1.jar` を事前に配置しておくとダウンロードをスキップできます。
- `tools/gradle/` 配下に Gradle ディストリビューションをキャッシュすることで Wrapper の取得を短縮できます。
- 生成済みの Gradle Wrapper (スクリプトと `gradle-wrapper.jar`) をテンプレートに差し替えておくと初回ダウンロードが不要になります。

## ログのエクスポート

GUI 右ペインの「ログをJSONで保存」から、実行ログを JSON 形式で保存できます。`scripts/run.ps1` で CLI を利用した場合も、標準出力に逐次ログが流れます。

## 開発メモ

- Core のサービスは `PluginAutomationOrchestrator` を中心に構成され、仕様生成→スキャフォールド→ビルド→テスト→パッケージングの順で処理します。
- 失敗時は `RuleBasedRepairEngine` がログを解析し、Gradle 設定や plugin.yml の修復を試みます (最大 3 回)。
- Paper テストは `PaperTestRunner` が headless Paper サーバーを起動し、`Done (...)` と `Enabled <PluginName> v<version>` のログを検知すると成功とみなします。

## ライセンス

このプロジェクトは MIT ライセンスの下で公開されています。
