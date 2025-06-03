# 🌟 P-Deck
# コードはAssets/Scripts/フォルダ内にあります。

**ポケモンTCGポケット（ポケポケ）対応の非公式カード検索・デッキ構築ツール**  
個人開発で制作したスマートフォン向けアプリです。

---

## 🎮 アプリ概要

- **ジャンル**：カード検索・デッキ構築支援ツール  
- **対応プラットフォーム**：iOS / Android
- **開発期間**：2025年4月10日〜5月15日（約1ヶ月）  
- **開発人数**：1人（個人開発）  
- **開発環境**：Unity 2022.3.6f1 / C#

---

## ✨ 特徴

### ✔ 柔軟なカード検索

- 名前／ひらがな／技テキストで検索可能
- フィルター（進化段階、EX／非EX、エネルギー条件、逃げエネルギーなど）対応

### ✔ 独自のデッキ支援機能

- 山札シャッフルシミュレーション
- デッキメモ機能 

---

## 🛠 技術スタック

| 分類       | 使用技術               |
|------------|------------------------|
| ゲームエンジン | Unity (C#)             |
| データ取得 | GitHub Pages + JSON    |
| UI設計     | UniRx + UniTask        |
| 設計       | MV(R)P構造             |
| 画像       | UnityWebRequestで非同期取得 |

---

## 🧠 実装・設計ポイント

- **MV(R)P構造**：Model/View/Presenterで責務を明確化
- **UniTask採用**：非同期での画像・データ読み込み
- **仮想スクロール**：数百枚のカードでも快適に表示
- **コメント規約**：関数単位で目的を明記

## 📂 ディレクトリ構成（タップで開閉）
<details>
<summary>Assets/Scripts/</summary>

```text
CardUIManager.cs          ── UI 初期化 & 仮想スクロール統括
SimpleVirtualScroll.cs    ── 仮想スクロール（高速リスト）
TogglePanel.cs            ── 汎用パネル表示切替

Cards/                    ── カード機能（M-V-P）
 ├─ Model/                ── CardModel / AllCardModel / CardDatabase
 ├─ Presenter/            ── AllCardPresenter
 ├─ View/                 ── CardView / AllCardView
 └─ Enum/                 ── 列挙型＆変換ユーティリティ

Deck/                     ── デッキ機能（M-V-P）
 ├─ Model/                ── DeckModel / DeckManager
 ├─ Presenter/            ── DeckPresenter
 ├─ View/                 ── DeckView 系
 ├─ UI/                   ── SetEnergyPanel
 └─ DeckList/             ── デッキ一覧 UI & Presenter

Search/                   ── 検索機能（M-V-P）
 ├─ SearchModel / View / Presenter
 ├─ SearchNavigator.cs    ── 検索ナビゲーション
 └─ Area/ …               ── 各種フィルタ 8 クラス

ImageCache/               ── 非同期画像キャッシュ
FeedBack/                 ── ユーザーフィードバック UI
Debug/                    ── 開発支援 (CacheClearButton)
```
</details>

## 🚀 このアプリを動作させるには
## 🔍 注意事項（Unity ビルドについて）

本リポジトリには “コード・設定ファイル” をすべて含めていますが、  
UI ソフトシャドウ用の **有料アセット「TrueShadow」** はライセンス上同梱できません。  
プロジェクト内のスクリプトが TrueShadow クラスを参照しているため、 **アセットを購入して Import しない限り Unity でのビルド／実機動作はできません。**

| ビルド可否 | 条件 |
|------------|------|
| **不可** | TrueShadow 未導入（デフォルト状態） |
| **可能** | Asset Store で TrueShadow を購入 → `Import` した後に Build |

> **動作確認のみ** を行いたい場合は、下記の APK / TestFlight あるいは デモ動画をご利用ください。  
> コード設計・実装方針のレビューを主目的としたポートフォリオです。
### 🔸 Android 端末の場合

- `P-Deck.apk` を端末にインストールしてください（ https://drive.google.com/file/d/1UxiUdqzU4po2-TfWeRRg-21vvyzB8LYx/view?usp=sharing ）
- 提供元不明アプリのインストールを許可する必要があります

### 🔸 iOS 端末の場合

- TestFlight を使用してインストール（ https://testflight.apple.com/join/EQM3bd2Q ）

## 🔍 注意事項（コードのみ構成について）

本リポジトリは、**ゲームプログラマー職向けにコード設計を重視したポートフォリオ提出用として構成**しています。  
そのため以下のような要素は含めていません：

- アセット類
- 画像などの素材ファイル
- ビルド済みアプリ

**→ 採用担当者・技術者の方がコード設計・実装方針をご覧いただくことを目的としています。**  
動作確認は、別途提供しているデモ動画をご参照ください。

> 本プロジェクトでは、AIアシスタント（ChatGPT・Claude）を活用しながら開発を進めました。  
> コード自体はAIが生成した部分もありますが、設計・実装方針・リファクタリング・最終判断は全て自分で行っています。  
> 設計力やAIとの対話による問題解決力を重視した、現代的な開発プロセスを体験・活用しました。

---

## ✏️ 著者

**吉田　月輝 / Yoshida Runaki**  
文系学部出身で、2024/3月から独学でUnityを学習しており、UniRxやパフォーマンス最適化にも挑戦しています。  

---

## 📄 使用ライブラリとライセンス

本プロジェクトでは以下のライブラリおよびアセットを使用しています：

| ライブラリ名 | 説明 | ライセンス・備考 |
|--------------|------|------------------|
| [UniRx](https://github.com/neuecc/UniRx) | リアクティブプログラミング | MIT License |
| [UniTask](https://github.com/Cysharp/UniTask) | 非同期処理を効率化するライブラリ | MIT License |
| [DoTween](http://dotween.demigiant.com/) | アニメーション補助ライブラリ | 無料版使用、商用利用可（MIT相当） |
| [TrueShadow](https://assetstore.unity.com/packages/tools/gui/true-shadow-ui-soft-shadow-and-glow-205220) | UIに影を追加する有料アセット | Unity Asset Storeで購入済み、**本リポジトリには含まれていません** |


---

## ⚠ 注意事項

このアプリは非公式の個人開発アプリです。ポケモン公式とは一切関係ありません。---

## ⚖️ 著作権・商標について

- **本アプリはポケモンTCGポケット（Pokémon Trading Card Game Pocket）およびポケモンカードゲームに関連する公式コンテンツとは⼀切関係がありません。**  
- Pokémon、Pokémon Trading Card Game、Nintendo、Creatures Inc.、GAME FREAK inc.、その他関連会社の**登録商標および著作権は各社に帰属**します。  
- 権利者様から指摘・要請があった場合には、**速やかに該当データの削除・⾮公開化など適切な対応**を取ります。

