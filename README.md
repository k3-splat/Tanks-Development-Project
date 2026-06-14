# プロジェクト：Tanks Development Project

## プロジェクト概要
本プロジェクトは，大学院の授業として，Unity Learnで公開されているチュートリアル "Tanks: Make a battle game for web and mobile" (https://learn.unity.com/course/tanks-make-a-battle-game-for-web-and-mobile) を基に講師陣の出すお題に従って拡張をしていくというものの開発リポジトリになります．

具体的には，チュートリアル完了時の状態をベースとして，そこに新たな機能を追加・拡張していくという流れになります．

追加・拡張した機能は，
- タイトル画面
- ホーム画面
- 戦車の砲塔の制御
- フィールド上への砲弾カートリッジの出現
- 砲弾ストック数の導入とその表示
- 砲弾の飛距離ゲージ
- 対戦車地雷
- 対戦車地雷のストック数の表示
- フィールド上への対戦車地雷カートリッジの出現
- 三人称視点
- ミニマップ
- HPゲージ
- ラウンド勝利数の表示
- ワームホール
- オンラインロビーとオンライン対戦

です．

## 技術スタック
* **Game Engine:** Unity
* **Language:** C#
* **Network Engine:** Photon Unity Networking 2 (PUN2)
* **Version Control:** Git

## プロジェクトのポイント
- 各課題の粒度をだんだん小さく分解して，各メンバーに割り振れるぐらいのものになったらチケットとして作成．それをissuesにネスト上に配置
- webhookを活用して，Discordにdiscussionやmargeが起きたときに通知が行くようにし，メンバーへのお知らせを行う
- CODEOWNERを配置して，コンフリクトへの対応をスムーズかつ判断の統一感を失わないようにする
