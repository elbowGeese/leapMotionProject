WAREHOUSE — 手动布局接缝修复 V11
场景 Assets/Max/WarehouseRecreation.unity
预制体 WarehouseEnvironment.prefab；逐项截图 RouteReview.html

基准
用户手动摆放为权威。先保存、备份，再局部修改网格与碰撞。
Source~/ManualLayoutBefore.txt 记录112个原有对象。
Source~/BeforeManualRefinement~ 为修复前手动场景及依赖备份。
其ManualPreviews为手动移动后、修复前的实际渲染。

修改
- 横向灰色直坡进入凹区；排水格栅沿同轴直坡进入并连接低地板。
- 按木坡当前位置修补内外坡接缝，补回原凹坑多余的地面开口。
- 白色参考轮廓做统一平台和外围斜坡；参考对象保留并关闭显示/碰撞。
- 中央凸起在用户位置圆顺过渡；北侧重复坡面裁接，消除重叠。
- 窗墙横向位于中央大柱朝大厅的外侧；高坡尺寸一致，窗墙后仍可通行。
- 旧支架改为跟随各自跳台；旧东侧涂鸦重新贴面；清理凹区浮碎片和旧木箱碰撞。
- 门台后补接，移动后的办公室顶部接屋顶；用户杆子和其他平台位置保留。

验证
WarehouseManualChecks.Run：保存后重开，112原有变换一致。
366处木坡/地板采样、264处横向直坡/格栅采样通过。
北侧接缝、平台外坡、高坡一致性、窗墙位置、涂鸦支撑及旧装饰清理通过。
79网格、109219三角形。详见validation.txt。

维护
WarehouseBuilder.CompleteManualLayout：一次性按手动基准修复，有重复执行保护。
WarehouseBuilder.FinishManualSeams：本轮追加接缝和装饰修复。
WarehouseBuilder.CaptureManualReview：更新截图和34至39检查机位。
WarehouseFrameCalibration.Measure：仅导出相机坐标。
旧Build已保护，不能直接全场重建覆盖用户手动位置。
旧WarehouseChecks适用于V10，本版使用WarehouseManualChecks。
未来修改继续以保存场景为准，先备份后局部编辑。

未检查
整段视频相机重新配准、真实滑板/Leap Motion控制及全路线实滑未测试。
材质、光照和细节仍是重建近似。

