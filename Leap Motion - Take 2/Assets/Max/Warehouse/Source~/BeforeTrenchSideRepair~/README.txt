WAREHOUSE — 通行与补缝 V12
场景 Assets/Max/WarehouseRecreation.unity
预制体 WarehouseEnvironment.prefab；截图对照 RouteReview.html

以用户最新手动摆放为基准，125个原有变换保留。
本轮基准 Source~/ManualLayoutV12Before.txt
本轮备份 Source~/BeforeAccessRepair~，包括修改前场景、依赖和ManualPreviews。

完成
- 横向灰色直坡加长到6m，下沉2.25m，坡角约20.6度；格栅同轴并跟随。
- 抬高的平台保留约1.323m顶面，周围改成接地木弧坡，后边补接墙面。
- 办公室内部中空，窗带层两侧入口贯通；楼体到屋顶，转角补缝。
- 北墙中央长弧坡保留伸出形状，顶部与两旁相同2.6m，木箱跟随。
- 门台侵入相邻木坡的端头裁回建筑边界，补截面和后边。
- 白色长条处补整面墙并接斜屋顶；按高坡当前位置修复外围平台及中央柱。
- 用户最新窗墙、杆子、各平台和高坡位置全部保留。

验证
WarehouseManualChecks.Run：保存重开，125变换一致。
366处木坡/地板、300处灰色坡/格栅采样通过。
办公室双向6次人物胶囊检测通过，入口地板和外墙转角检查通过。
81网格、112275三角形；详见validation.txt。

维护
WarehouseBuilder.CompleteAccessV12：本轮一次性几何修复，有重复执行保护。
WarehouseBuilder.FinishAccessRoofV12：新墙斜屋顶收口及办公室转角补缝。
WarehouseBuilder.CaptureAccessV12：更新截图，包含40至44新机位。
WarehouseFrameCalibration.Measure：仅导出当前机位，不声称视频配准精度。
WarehouseManualChecks.Run：本轮验证入口。
旧Build/CompleteManualLayout/FinishManualSeams适用于历史构造，不可用来覆盖当前手动布局。
未来修改以当前保存场景为准，先备份后局部编辑。

未检查
整段视频重新逐帧配准、真实滑板/Leap Motion操控及全路线实滑。
材质和灯光仍为重建近似。