WAREHOUSE — 坡道侧面与中央沟槽 V13
场景 Assets/Max/WarehouseRecreation.unity
预制体 WarehouseEnvironment.prefab；前后截图 RouteReview.html

以用户最新手动摆放为基准，134个原有变换保留。
本轮基准 Source~/ManualLayoutV13Before.txt
本轮备份 Source~/BeforeTrenchSideRepair~，含场景、网格、材质、预制体与截图。

本轮
- 634个朝内木侧面三角形改为朝外；10处坡道端面补齐，碰撞同步。
- 中央红杆顶部约0.85m，平面位置和折角方向保留。
- 格栅下沉约0.70m，沟底下沉1.05m，补四壁与端面；大厅外部地面不变。

验证
WarehouseManualChecks.Run：重开保存场景，134变换一致；666处木坡/直坡采样、办公室贯通继续通过。
WarehouseTrenchChecks.Run：196处沟槽采样，四壁和外部地面、1876个朝外木侧面、10处封口通过。
91网格、110579三角形。详见validation.txt。

维护
根据用户要求，已删除38个建模、修复、贴花生成、相机校准和校验脚本，包括历史备份里的脚本副本。
已生成的场景、网格、材质、贴图、预制体、截图和验证记录保留。
上述验证结果为删除前已完成的检查记录；相应脚本入口已移除。
后续以现有场景和模型直接编辑，不再提供原辅助脚本的一键重建入口。
当前场景已删除31个辅助摄像头，仅保留 Main Camera - P1 start 0132；主摄像头位置、方向不变。旧截图与坐标表仅作历史对照。

未检查
实际滑板/Leap Motion实滑；本轮未重做整段视频机位配准。