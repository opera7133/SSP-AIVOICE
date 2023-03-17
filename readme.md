# A.I.VOICE 送信プラグイン

SSPとA.I.VOICEを連携させるプラグインです。

## 元

このプラグインは[CeVIO-Talker](https://github.com/ambergon/ukagakaPlugin_CeVIO-Talker)をもとに製作されました。

## ライブラリ

- [CSaori Project / proxy_ex](https://github.com/ukatech/csaori/)
- [Newtonsoft / Json.NET](https://github.com/JamesNK/Newtonsoft.Json)
- [整備班(SEIBIHAN) / YAYA](https://github.com/YAYA-shiori/yaya-shiori)

## コンパイル（Windows）

- check.cs

```bash
csc /r:"（AI.Talk.Editor.Api.dllのパス）" /t:exe .\check.cs
```

- client.cs

```bash
csc /t:winexe .\client.cs
```

- server.cs

```bash
csc /r:"（AI.Talk.Editor.Api.dllのパス）" /r:Newtonsoft.Json.dll /t:winexe .\server.cs
```