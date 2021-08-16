[[English](About-PgTools)]

# PgTools
PgTool is management tools for PostgreSQL on Windows, requires .NET Framework 4.6.1 or later.<br>
Commandline arguments are similar to psql / pg_dump.<br>

PgToolsはWindows上でPostgreSQLデータベースを管理するためのツール類です。.NET Framework 4.6.1以降が必要です。<br>
コマンドライン引数は psql / pg_dump に準じています。

## DB2Src.NET

**DB2Src.NET** は PostgreSQL用のデータベースブラウザです。

![アプリケーション イメージ](https://github.com/calc33/PgTools/blob/master/DB2Src.NET/doc/image/appmain01.png)

## ExpSch / ExpTbl
ExpSch および ExpTbl はデータベースの構成をテキストファイル化して git / subversion 等のバージョン管理システムで管理することを目的としたコマンドラインユーティリティです。pgpass.conf にパスワードが登録されている場合、パスワードの入力を省略できます。

### ExpSch
**ExpSch** はオブジェクトの定義SQLをオブジェクト毎にファイルに出力します。

<pre>
使用方法:
ExpSch [OPTIONS]... [Directory]

一般的なオプション:
  -?, --help                このヘルプを表示し、終了します

出力内容を制御するためのオプション:
  -E, --encoding=ENCODING  ENCODING符号化方式でデータをダンプ(規定値:UTF-8)
  -n, --schema=SCHEMA      指名したスキーマのみをダンプ
  -N, --exclude-schema=SCHEMA 指名されたスキーマをダンプしません
  --no-tablespaces         テーブルスペースの割り当てを出力しません
接続オプション
  -d, --dbname=データベース名  ダンプするデータベース
  -h, --host=HOSTNAME      データベースサーバのホストまたはソケットディレクトリです
  -p, --port=PORT          データベースサーバのポート番号です
  -U, --username=NAME      指定したデータベースユーザで接続します
  </pre>

### ExpTbl
**ExpTbl** はテーブルのデータをテーブル毎にファイルに出力します。
データはCOPY文形式で出力します。

本番系とテスト系等複数のデータベース間でマスタ構成の差異を把握することを目的に作られているため、
pg_dumpにはない以下の機能があります。

* 特定項目の除外<br>タイムスタンプ用項目など比較したくない項目を出力に含めないようにすることができます。
* 特定項目の値を偽装<br>初期値がないため除外できないが差分比較したくない項目を固定値にできます。

<pre>
使用方法:
ExpTbl exp [OPTIONS]... [Directory]
ExpTbl genconf [OPTIONS]... [CONFIGFILE]
ExpTbl genrule [OPTIONS]... [RULEFILE]

  exp        テーブルのデータをファイルに出力します
  genconf    ファイルの出力を制御する設定ファイルを生成します
             CONFIGFILE を省略すると "ExpTBl.cfg" に書き出します
  genrule    genconf で使用するルールファイルのテンプレートを生成します
             RULEFILE を省略すると "ExpTBlRule.cfg" に書き出します
一般的なオプション:
  -?, --help                このヘルプを表示し、終了します

出力内容を制御するためのオプション:
  -E, --encoding=ENCODING  ENCODING符号化方式でデータをダンプ(規定値:UTF-8)
  -n, --schema=SCHEMA      指名したスキーマのみをダンプ
  -N, --exclude-schema=SCHEMA 指名されたスキーマをダンプしません
  -c --cofig=CONFIGFILE    CONFIGFILEを指定します。指定しない場合 "ExpTbl.cfg" になります
  -r --rule=RULEFILE       RULEFILEを指定します。指定しない場合 "ExpTblRule.cfg" になります
接続オプション
  -d, --dbname=データベース名  ダンプするデータベース
  -h, --host=HOSTNAME      データベースサーバのホストまたはソケットディレクトリです
  -p, --port=PORT          データベースサーバのポート番号です
  -U, --username=NAME      指定したデータベースユーザで接続します
</pre>

使用手順
<pre style="background-color:black; color: whitesmoke;">
cd &lt;作業フォルダ&gt;
ExpTbl genrule -h localhost -p 5432 -d postgres -U postgres
ExpTbl genconf -h localhost -p 5432 -d postgres -U postgres
ExpTbl exp -h localhost -p 5432 -d postgres -U postgres
</pre>

1. **ExpTbl genrule** で ExpTblRule.cfg を作成<br>ExpTblRule.cfgのテンプレートを作成します。作成後、用途に合わせて内容を修正してください。
2. **ExpTbl genconf** で ExpTblRul.ecfg から ExpTbl.cfg を作成<br>ExpTbl.cfg にはデータを出力するテーブルの一覧と、各テーブルの設定が記述されています。テーブル毎に出力を調整したい場合はこのファイルを編集してください。
3. **ExpTbl exp** でデータ出力<br>ExpTbl.cfg の設定をもとにデータを出力します。
