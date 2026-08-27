# 桌面精靈實作計畫

用 **C# + WPF** 做 Windows 桌面精靈。在 **Cursor** 裡用 `dotnet` 指令開發，不必開 Visual Studio。產出一般 `.exe`，不包 Electron、不嵌瀏覽器。

視覺參考：[Codex Pets — Mochi Guinea](https://codex-pets.net/#/pets/mochiguinea)（`Ref/Codex Pets.url`）。

---

## 1. 目標

一隻常駐桌面的小寵物：

- 無邊框、背景透明
- 永遠疊在其他視窗上面
- 可用滑鼠拖曳
- 會走路、坐下、趴下
- 系統列有圖示
- 依使用者設定的間隔提醒喝水
- 必要時滑鼠點擊可穿透（click-through）

---

## 2. 範圍

### MVP（一定要做完）

| 功能 | 行為 |
|---|---|
| 視窗殼 | `WindowStyle=None`、`AllowsTransparency=True`、`Background=Transparent`、`Topmost=True`，不出現在工作列 |
| 拖曳 | 左鍵拖動寵物；游標不要離開精靈圖 |
| 狀態 | `Idle`、`Walk`、`Sit`、`Lie`、`Dragged` |
| 走路 | 在目前螢幕工作區內隨機水平走動，會轉身 |
| 坐下／趴下 | 待機時隨機切換，或從系統列／右鍵選單指定 |
| 系統列 | 顯示／隱藏寵物、坐下、趴下、提醒設定、結束 |
| 喝水提醒 | 間隔（分鐘）由使用者設定；寵物顯示對話泡泡；系統列氣球通知可選 |
| 持久化 | 間隔、上次位置、上次狀態、穿透開關存到 `%AppData%\TablePet\settings.json` |

### MVP 之後再做

- 爬到其他視窗、坐在標題列上（Shimeji 物理）
- 飢餓／好感度
- AI 對話
- 跨螢幕走動（MVP：只限制在寵物目前所在的那一面螢幕）
- Steam／創意工坊模組
- 安裝程式（Inno Setup／MSIX）

### 不做

- Electron、WebView2，或任何內嵌瀏覽器
- 讀其他軟體的檔案、鍵盤側錄、搬動真實桌面圖示
- macOS／Linux

---

## 3. 技術選擇

| 項目 | 選擇 | 原因 |
|---|---|---|
| 執行環境 | .NET 7、`net7.0-windows` | 本機已有 SDK 7，WPF 能力足夠。出包用 **self-contained**，Runtime 打進資料夾 |
| UI | WPF | 原生透明分層視窗，不必再裝一套 UI 執行環境 |
| 系統列 | `Hardcodet.NotifyIcon.Wpf` | WPF 原生托盤；除非必要不混 WinForms |
| 動畫 | PNG 連幀 + `DispatcherTimer` | 單純、適合像素圖、之後換素材容易 |
| 移動 | 移動**視窗**（`Left`／`Top`），不要用全螢幕 overlay 再在裡面移動精靈 | 點擊判定只落在寵物上，其餘桌面可正常用 |
| 滑鼠穿透 | P/Invoke 開關 `WS_EX_TRANSPARENT` | WPF 的 `IsHitTestVisible=False` **不會**把點擊傳給其他行程 |
| 設定 | JSON 檔 + `SettingsStore` | 不需要資料庫 |
| 常數設定 | `src/TablePet/Config/*.cs` 由 Service 直接 import | 預設間隔、素材路徑、視窗尺寸。建構子只收測試要假造的依賴 |
| 測試 | xUnit + NSubstitute | 測狀態機、提醒排程、設定讀寫。WPF 視窗**不**做單元測試。提醒用 `IClock`（.NET 7 沒有 `TimeProvider`） |

發行指令（預設，給朋友用）：

```text
dotnet publish src/TablePet/TablePet.csproj -c Release -r win-x64 --self-contained true
```

把 `bin/Release/net7.0-windows/win-x64/publish/` 整包打成 zip 傳出去。朋友解壓後雙擊 `TablePet.exe` 即可，不必裝 Visual Studio，也不必裝 .NET Desktop Runtime。資料夾會比較大（約數十 MB），這是正常的。

單檔 `.exe`（`PublishSingleFile`）WPF 不一定穩，MVP 先出「一個資料夾 + 雙擊 exe」。若之後驗證單檔可跑，再改。

---

## 4. 架構

```text
Tray / Context menu
        │
        ▼
   App（組裝入口）
        │
        ├── PetWindow          （殼：位置、拖曳、穿透）
        ├── PetController      （狀態機 + 走路 AI）
        ├── SpriteAnimator     （依狀態與面向決定幀）
        ├── ReminderService    （計時 → 泡泡 + 可選氣球）
        └── SettingsStore      （讀寫 JSON）
```

### 規則

- **PetWindow** 不知道提醒怎麼算、JSON 路徑在哪。
- **PetController** 不呼叫 Win32。它只發出意圖：`MoveBy`、`SetState`、`Face`。
- **ReminderService** 只引發 `ReminderDue`。視窗／控制器決定寵物怎麼反應（例如 `Sit` + 泡泡）。
- 視窗大小等於**一幀精靈**（對話泡泡出現時再加大）。MVP 不要用全螢幕 overlay。

### 寵物狀態

狀態是封閉集合。讀取時必須先對 `PetState` 做 `switch` 收窄，不能在 `Walk` 上讀只有 `Sit` 才有的欄位。

```text
Idle ──隨機──► Walk ──走到／超時──► Idle
  │                  │
  │                  └── 使用者拖曳 ──► Dragged ──放開──► Idle
  ├── 使用者／隨機 ──► Sit ──超時／點擊──► Idle
  └── 使用者／隨機 ──► Lie ──超時／點擊──► Idle

ReminderDue ──► Sit（或 Idle）+ SpeechBubble
拖曳永遠打斷 Walk／Sit／Lie。
```

面向：`Left` | `Right`。向左走用左向幀（若素材只有右向，就水平鏡像）。

---

## 5. 視窗殼（最難的一塊）

`PetWindow` 的 XAML：

```text
WindowStyle="None"
AllowsTransparency="True"
Background="Transparent"
Topmost="True"
ShowInTaskbar="False"
ResizeMode="NoResize"
ShowActivated="False"
```

| 問題 | 做法 |
|---|---|
| 像素圖銳利 | `Image` 上設 `RenderOptions.BitmapScalingMode="NearestNeighbor"` |
| 拖曳 | `MouseLeftButtonDown` → `DragMove()`；若和穿透衝突，改成自行 Capture + 位移 |
| 不飛出螢幕 | 移動／走路後，限制在視窗中心所在螢幕的 `SystemParameters.WorkArea` |
| 滑鼠穿透 | `GetWindowLong`／`SetWindowLong` 對 `GWL_EXSTYLE` 加減 `WS_EX_TRANSPARENT` |
| 穿透 + 拖曳 | 指標在精靈上且正在操作時**關掉**穿透；系統列提供「待機時穿透」開關 |
| 不要搶焦點 | `ShowActivated=False`；走路 tick 若還是搶焦點，用 `SetWindowPos` + `SWP_NOACTIVATE` |
| DPI | app manifest 開 `PerMonitorV2`，座標才會跟實體像素對齊 |

MVP 的穿透策略：

1. 預設**關閉**（隨時拖得動）。
2. 系統列勾選後，在 `Idle`／`Walk`／`Sit`／`Lie` 期間開啟 `WS_EX_TRANSPARENT`。
3. 快捷鍵或系統列「解除穿透」可再拖曳。

---

## 6. 動畫與素材

MVP **不依賴** Codex Pets 執行環境，只參考外觀（Q 版天竺鼠或類似角色）。

程式裡的 clip id 一律英文：

| Id | 循環 | 說明 |
|---|---|---|
| `idle` | 是 | 呼吸／眨眼 |
| `walk` | 是 | 4–8 幀 |
| `sit` | 是 | 可選坐下過場後進入坐下循環 |
| `lie` | 是 | 趴下／睡覺循環 |
| `drag` | 可選 | 沒有就凍結最後一幀 |

目錄：

```text
assets/pet/default/
  manifest.json
  idle/001.png …
  walk/001.png …
  sit/001.png …
  lie/001.png …
```

`manifest.json`（key 用英文）：

```json
{
  "id": "default",
  "frameWidth": 128,
  "frameHeight": 128,
  "fps": 8,
  "clips": {
    "idle": { "fps": 6 },
    "walk": { "fps": 10 },
    "sit": { "fps": 4 },
    "lie": { "fps": 3 }
  }
}
```

正式圖還沒有時，用色塊 + 文字標籤（`Idle`／`Walk`／…）先把邏輯跑通。

之後換成真正 PNG 只換資料，不必重寫邏輯。

---

## 7. 喝水提醒

- 預設間隔：**45 分鐘**（寫在 `Config/ReminderConfig.cs`）。
- 使用者可設 5–180 分鐘。
- 設定視窗開著，或提醒泡泡已顯示時，計時暫停（不要一次疊很多則）。
- 到期時：
  1. 寵物切到 `Sit`（或維持目前姿勢）。
  2. 顯示小對話泡泡：`Time to drink water.`
  3. 可選系統列氣球（同一句）。
- 關閉方式：點泡泡、點寵物，或等 N 秒（預設 20）。
- 泡泡上提供「延後 5 分鐘」。
- 持久化 `intervalMinutes` 與 `enabled`。

Service **不要**讀 `process.env`／`Environment` 拿這些值。預設從 `ReminderConfig` import；使用者覆寫由 `SettingsStore` 提供。

---

## 8. 建議目錄

```text
table_pet/
  IMPLEMENTATION_PLAN.md
  TablePet.sln
  src/
    TablePet/
      App.xaml
      App.xaml.cs
      TablePet.csproj
      Config/
        PetConfig.cs
        ReminderConfig.cs
        WindowConfig.cs
      Shell/
        PetWindow.xaml
        PetWindow.xaml.cs
        ClickThroughService.cs
        ScreenBounds.cs
      Pet/
        PetState.cs
        PetFacing.cs
        PetController.cs
        SpriteAnimator.cs
        SpriteAtlas.cs
        WalkBehavior.cs
      Reminder/
        ReminderService.cs
      Tray/
        TrayIcon.xaml
      Persistence/
        AppSettings.cs
        SettingsStore.cs
      Ui/
        SettingsWindow.xaml
        SpeechBubble.xaml
      Assets/
        Pet/default/…
        Tray.ico
  tests/
    TablePet.Tests/
      PetControllerTests.cs
      ReminderServiceTests.cs
      SettingsStoreTests.cs
  Ref/
    Codex Pets.url
```

組裝：在 `App.xaml.cs`／`App.OnStartup` 建立服務。傳 `SettingsStore`，不要把 config 物件傳進建構子。

---

## 9. 階段

每一階段都要有**可勾選的完成條件**。上一階段沒綠，不要開始下一階段。

### 階段 0 — 專案骨架

- 建立 `net7.0-windows` WPF 專案、solution、xUnit 測試專案。
- App manifest：`PerMonitorV2`、DPI aware。
- 空的 `Config` 類別，填好預設值。

**完成條件：** `dotnet build` 與 `dotnet test` 通過。

### 階段 1 — 透明置頂視窗

- `PetWindow`：透明背景 + 佔位 `Image`／色塊。
- `ShowInTaskbar=False`、`Topmost=True`。
- 從設定還原上次 `Left`／`Top`（沒有就放在工作區中央）。

**完成條件：** 色塊浮在記事本上面；空白像素看得到桌面桌布。

### 階段 2 — 拖曳與邊界

- 滑鼠拖曳。
- 限制在工作區內（不要蓋住工作列）。
- `MouseUp`／`Closing` 時存位置。

**完成條件：** 拖不出螢幕；重開程式位置還在。

### 階段 3 — 狀態機（還不需要圖）

- `PetState` + `PetController`。
- 右鍵選單或快捷鍵：Idle／Walk／Sit／Lie。
- 佔位文字顯示目前狀態。

**完成條件：** 單元測試涵蓋轉換（包含「拖曳打斷走路」）。

### 階段 4 — 精靈動畫

- 讀取幀資料夾 + `manifest.json`。
- `SpriteAnimator` 用 timer 播幀。
- 依 `PetFacing` 鏡像或換 clip。

**完成條件：** 切 Sit／Walk 會換圖；FPS 符合 manifest。

### 階段 5 — 自己走路

- `WalkBehavior`：在目前螢幕選一個目標 X，播 `walk`，走到後 → `Idle`。
- 待機逾時後依 `PetConfig` 權重隨機 Sit／Lie。
- `Dragged` 或提醒泡泡開著時暫停 AI。

**完成條件：** 寵物左右巡邏，不走出工作區。

### 階段 6 — 系統列

- 圖示與選單：顯示、隱藏、坐下、趴下、設定、結束。
- 關掉寵物視窗只是隱藏；從系統列結束才真正退出（`ShutdownMode.OnExplicitShutdown`）。

**完成條件：** 關掉精靈不會結束程式；選結束才會。

### 階段 7 — 喝水提醒

- `ReminderService`：測試用可假造的 `IClock`。
- 泡泡 UI + 延後 + 開關。
- 設定視窗：間隔滑桿或數字框。

**完成條件：** 測試證明「N 分鐘後到期」；手動跑得出泡泡；間隔會存檔。

### 階段 8 — 滑鼠穿透

- 系統列開關。
- Win32 `WS_EX_TRANSPARENT` 開／關。
- 拖曳中或開關關閉時，寵物吃得到滑鼠。

**完成條件：** 開啟穿透時，點擊打到精靈後面的視窗；系統列仍可關掉穿透。

### 階段 9 — 收尾與出包

- 正式天竺鼠（或選定角色）idle／walk／sit／lie 圖。
- 設定裡可選最近鄰縮放（1x／2x）。
- self-contained `dotnet publish`（`win-x64`、`--self-contained true`），把 publish 資料夾打成 zip。
- README：解壓後雙擊 `TablePet.exe`、系統列用法、提醒、素材授權。系統需求只寫 Windows 10/11 64-bit。

**完成條件：** 朋友電腦沒裝 .NET、沒裝 Visual Studio，解壓後雙擊 `.exe` 就能開。

---

## 10. 測試

WPF 畫面看起來正常，**不代表型別過關**。測試必須和主專案同一個 `net7.0-windows` TFM 編譯。

一定要測（不要開 STA 視窗）：

- `PetController` 狀態轉換，以及「拖曳優先」
- `WalkBehavior` 目標落在指定矩形內
- `ReminderService` 到期／延後／關閉
- `SettingsStore` 讀寫一輪，以及檔案損壞時的後備

單元測試不要對 `PetWindow` 畫面做 assert。

---

## 11. 風險

| 風險 | 對策 |
|---|---|
| `AllowsTransparency` + `Topmost` 會搶焦點，全螢幕遊戲時可能被壓下去 | `ShowActivated=False`；必要時再加 `WS_EX_NOACTIVATE`／`SWP_NOACTIVATE` |
| 穿透之後拖不動 | 系統列開關 + 拖曳時暫時關閉穿透 |
| WPF 透明 + 點陣邊緣發髒 | PNG 用 premultiplied alpha；不要用 `OpacityMask` 取巧 |
| 多螢幕 DPI | Per-monitor V2；必要時用 `PresentationSource` 換算 |
| 還沒有精靈圖 | 階段 1–5 先用色塊 |
| 範圍膨脹（爬窗、AI） | 留在「MVP 之後」；階段 9 做完再談 |

---

## 12. 參考專案（只讀，不要整包 fork）

| 倉庫 | 可借鏡 | 不要帶進來 |
|---|---|---|
| [Z-Oleksandr/amicus](https://github.com/Z-Oleksandr/amicus) | 狀態、提醒視窗、系統列 | 房間／飢餓養成 |
| [HelloRWA/create-desktop-pet](https://github.com/HelloRWA/create-desktop-pet) | WPF 透明視窗範本 | Mac 管線 |
| [cmarathe1/pet-assistant](https://github.com/cmarathe1/pet-assistant) | layered window／穿透 | Python LLM sidecar |
| [LorisYounger/VPet](https://github.com/LorisYounger/VPet) | 動畫 clip 命名、視窗控制器想法 | 整套遊戲 + 工坊 |
| [Adrianotiger/desktopPet](https://github.com/Adrianotiger/desktopPet) | 走路／重力／邊界狀態機 | WinForms、XML 包格式 |

---

## 13. 第一次寫程式建議順序

1. 階段 0 + 1 + 2（看得到、拖得動的透明窗）
2. 階段 3 + 測試
3. 階段 6（先有系統列才能乾淨退出）
4. 階段 5 走路 → 階段 4 精靈圖 → 階段 7 提醒 → 階段 8 穿透

視窗沒有關閉鈕，系統列要比花俏走路先做，否則很難結束程式。

---

## 14. 尚未拍板（沒改就用這些預設）

| 項目 | 預設 |
|---|---|
| 角色 | 內建一隻（`default`）；MVP 不做多角色切換 |
| 介面語言 | UI 字串用英文（程式與資源一律英文） |
| 提醒文案 | `Time to drink water.` |
| 走路方式 | 貼地：Y 固定在「工作區底部減精靈高度」。MVP 不跳躍 |
| 設定介面 | 從系統列開一個小 WPF 視窗，不做世界內面板 |

這些預設若要改，先改本檔 MVP 表，再改程式。
